import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import json
import logging
from kafka import KafkaConsumer, KafkaProducer
import joblib
import pandas as pd
import numpy as np
import time

from clinical_shap_explainer import SymptomsShapExplainer

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)

KAFKA_BOOTSTRAP_SERVERS = [os.getenv('KAFKA_BOOTSTRAP_SERVERS', 'localhost:9092')]
INPUT_TOPIC = "lung-clinical-requests"
OUTPUT_TOPIC = "lung-clinical-results"



SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

BASE_DIR = os.path.dirname(__file__)
MODEL_PATH = os.path.join(BASE_DIR, 'models/logistic_regression_real.pkl')
SCALER_PATH = os.path.join(BASE_DIR, 'models/scaler_real.pkl')

class LungClinicalService:

    def __init__(self):
        self.model = None
        self.scaler = None
        self.consumer = None
        self.producer = None
        self.shap_explainer = None


        self.class_names = ['no_cancer', 'lung_cancer']

        self.symptom_features = []

    def load_models(self):
        try:
            logger.info("Loading models...")
            self.model = joblib.load(MODEL_PATH)
            self.scaler = joblib.load(SCALER_PATH)

            if hasattr(self.scaler, 'feature_names_in_'):
                self.symptom_features = list(self.scaler.feature_names_in_)
                logger.info(f"Loaded {len(self.symptom_features)} symptom features")

            logger.info("✓ Models loaded successfully")
            return True
        except Exception as e:
            logger.error(f"Model loading failed: {e}")
            return False

    def initialize_shap(self):
        try:
            logger.info("Initializing SHAP explainer...")

            if not self.symptom_features:
                logger.warning("No feature names available")
                return False

            self.shap_explainer = SymptomsShapExplainer(
                model=self.model,
                feature_names=self.symptom_features
            )

            logger.info(" SHAP explainer initialized!")
            return True
        except Exception as e:
            logger.error(f"Failed to initialize SHAP: {str(e)}")
            logger.warning("Service will continue WITHOUT SHAP explanations")
            return False

    def setup_kafka(self):
        try:
            self.consumer = KafkaConsumer(
                INPUT_TOPIC,
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                auto_offset_reset="latest",
                enable_auto_commit=True,
                group_id="lung-clinical-service-group",
                value_deserializer=lambda x: json.loads(x.decode("utf-8"))
            )

            self.producer = KafkaProducer(
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                value_serializer=lambda x: json.dumps(x).encode("utf-8"))

            logger.info("✓ Connected to Kafka")
            return True
        except Exception as e:
            logger.error(f"Kafka connection failed: {e}")
            return False

    def preprocess(self, symptoms: dict):
        df = pd.DataFrame([symptoms])

        if hasattr(self.scaler, 'feature_names_in_'):
            expected = list(self.scaler.feature_names_in_)
        else:
            expected = df.columns.tolist()

        for f in expected:
            if f not in df.columns:
                df[f] = 0

        df = df[expected]
        scaled = self.scaler.transform(df)
        return scaled, df.values[0]

    def predict(self, features):
        predicted_class = int(self.model.predict(features)[0])
        probs = self.model.predict_proba(features)[0]

        condition = self.class_names[predicted_class]
        confidence = float(np.max(probs))

        return predicted_class, condition, confidence, probs.tolist()

    def generate_shap_explanation(self, scaled_features, predicted_class, diagnosis_case_id):
        try:
            if not self.shap_explainer:
                logger.warning("SHAP explainer not initialized, skipping explanation")
                return None

            logger.info(f"Generating SHAP explanation for case {diagnosis_case_id}...")

            explanation = self.shap_explainer.explain_prediction(
                symptoms_data=scaled_features[0],
                predicted_class=predicted_class
            )

            logger.info(f" SHAP explanation generated for case {diagnosis_case_id}")
            return explanation

        except Exception as e:
            logger.error(f"SHAP generation error: {str(e)}", exc_info=True)
            return None

    def send_shap_to_kafka(self, diagnosis_case_id, request_id, explanation):
        try:
            if not explanation:
                return False

            shap_message = {
                'RequestId': request_id,
                'DiagnosisCaseId': diagnosis_case_id,
                'ModelName': 'lung_clinical_logistic_regression',
                'Explanation': explanation,
                'Timestamp': time.time()
            }

            self.producer.send(
                'shap-explanations',
                key=str(diagnosis_case_id).encode('utf-8'),
                value=shap_message
            )

            logger.info(f" Sent SHAP explanation to Kafka for case {diagnosis_case_id}")
            return True

        except Exception as e:
            logger.error(f"Failed to send SHAP to Kafka: {str(e)}", exc_info=True)
            return False

    def process_message(self, message: dict):
        start_time = time.time()
        request_id = None
        diagnosis_case_id = None

        try:
            request_id = (
                    message.get("requestId")
                    or message.get("request_id")
                    or message.get("diagnosisCaseId", "")
            )

            diagnosis_case_id = message.get("diagnosisCaseId") or message.get("diagnosis_case_id")

            if not request_id:
                raise ValueError("Missing requestId")

            if "clinicalData" in message:
                symptoms = message["clinicalData"].get("symptoms", {})
            else:
                symptoms = message.get("symptoms", {})

            logger.info(f"Processing lung clinical request {request_id} (case {diagnosis_case_id})")

            features, original_values = self.preprocess(symptoms)
            predicted_class, condition, confidence, probabilities = self.predict(features)

            shap_explanation = None
            if diagnosis_case_id and self.shap_explainer:
                shap_explanation = self.generate_shap_explanation(
                    features,
                    predicted_class,
                    diagnosis_case_id
                )

                if shap_explanation:
                    self.send_shap_to_kafka(diagnosis_case_id, request_id, shap_explanation)

            processing_time = (time.time() - start_time) * 1000

            probabilities_dict = {
                self.class_names[i]: float(probabilities[i])
                for i in range(len(self.class_names))
            }

            response = {
                "RequestId": request_id,
                "Prediction": condition,
                "Confidence": confidence,
                "Probabilities": probabilities_dict,
                "ProcessingTimeMs": processing_time,
                "Success": True,
                "ErrorMessage": None,
                "HasShapExplanation": shap_explanation is not None  # NEW: Flag
            }

            logger.info(f"Sending result: {condition} (confidence: {confidence:.2%})")

            self.producer.send(OUTPUT_TOPIC, key=request_id.encode('utf-8'), value=response)

            shap_status = "✓ with SHAP" if shap_explanation else ""
            logger.info(f"✓ Sent result ({processing_time:.2f}ms) {shap_status}")

        except Exception as e:
            logger.error(f"Processing failed: {e}", exc_info=True)
            processing_time = (time.time() - start_time) * 1000

            error_response = {
                "RequestId": request_id or 'unknown',
                "Prediction": "inconclusive",
                "Confidence": 0.0,
                "Probabilities": {name: 0.0 for name in self.class_names},
                "ProcessingTimeMs": processing_time,
                "Success": False,
                "ErrorMessage": str(e),
                "HasShapExplanation": False
            }

            if request_id:
                self.producer.send(OUTPUT_TOPIC, key=request_id.encode('utf-8'), value=error_response)

    def start(self):
        logger.info("Starting Lung Cancer Clinical Service WITH SHAP...")
        logger.info(f"Detecting conditions: {', '.join(self.class_names)}")
        logger.info("Architecture: AI Service → Kafka → Orchestrator → Database")

        if not self.load_models():
            return

        self.initialize_shap()

        if not self.setup_kafka():
            return

        logger.info(f"Listening on topic: {INPUT_TOPIC}")
        logger.info(f"Sending results to: {OUTPUT_TOPIC}")
        logger.info(f"Sending SHAP to: shap-explanations")
        logger.info("Clinical service READY!")

        try:
            for msg in self.consumer:
                self.process_message(msg.value)
        except KeyboardInterrupt:
            logger.info("Service stopped by user")
        except Exception as e:
            logger.error(f"Service error: {e}")
        finally:
            self.cleanup()

    def cleanup(self):
        if self.consumer:
            self.consumer.close()
        if self.producer:
            self.producer.close()
        logger.info("Service stopped")


if __name__ == "__main__":
    LungClinicalService().start()