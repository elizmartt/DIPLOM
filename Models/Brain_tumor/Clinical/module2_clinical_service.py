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

KAFKA_BOOTSTRAP_SERVERS = [os.getenv('KAFKA_BOOTSTRAP_SERVERS') or 'localhost:9092']
INPUT_TOPIC = "brain-clinical-requests"
OUTPUT_TOPIC = "brain-clinical-results"

BASE_DIR = os.path.dirname(__file__)
MODEL_PATH = os.path.join(BASE_DIR, 'neurological_clinical_lr.pkl')
SCALER_PATH = os.path.join(BASE_DIR, 'neurological_clinical_scaler.pkl')
METADATA_PATH = os.path.join(BASE_DIR, 'neurological_clinical_metadata.pkl')
LABEL_MAP_PATH = os.path.join(BASE_DIR, 'neurological_label_map.pkl')


class ClinicalService:

    def __init__(self):
        self.model = None
        self.scaler = None
        self.metadata = None
        self.label_map = None
        self.consumer = None
        self.producer = None
        self.shap_explainer = None
        self.class_names = [
            'alzheimer_mild',
            'alzheimer_moderate',
            'alzheimer_very_mild',
            'glioma',
            'meningioma',
            'multiple_sclerosis',
            'normal',
            'pituitary'
        ]
        self.symptom_features = []

    def load_models(self):
        try:
            logger.info("Loading models...")
            self.model = joblib.load(MODEL_PATH)
            self.scaler = joblib.load(SCALER_PATH)
            self.metadata = joblib.load(METADATA_PATH)
            try:
                self.label_map = joblib.load(LABEL_MAP_PATH)
            except:
                self.label_map = {i: name for i, name in enumerate(self.class_names)}
            self.symptom_features = self.metadata.get("feature_names", [])
            logger.info(f"Loaded {len(self.symptom_features)} symptom features")
            logger.info("Models loaded successfully")
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
            logger.info("SHAP explainer initialized")
            return True
        except Exception as e:
            logger.error(f"Failed to initialize SHAP: {str(e)}")
            logger.warning("Service will continue without SHAP explanations")
            return False

    def setup_kafka(self):
        try:
            self.consumer = KafkaConsumer(
                INPUT_TOPIC,
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                auto_offset_reset="latest",
                enable_auto_commit=True,
                group_id="clinical-service-group",
                value_deserializer=lambda x: json.loads(x.decode("utf-8"))
            )
            self.producer = KafkaProducer(
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                value_serializer=lambda x: json.dumps(x).encode("utf-8")
            )
            logger.info("Connected to Kafka")
            return True
        except Exception as e:
            logger.error(f"Kafka connection failed: {e}")
            return False

    def preprocess(self, symptoms: dict):
        df = pd.DataFrame([symptoms])
        expected = self.metadata.get("feature_names", df.columns.tolist())
        for f in expected:
            if f not in df.columns:
                df[f] = 0
        df = df[expected]
        scaled = self.scaler.transform(df)
        return scaled, df.values[0]

    def predict(self, features):
        logger.info("predict() called")
        logger.info(f"Features shape: {features.shape}")
        logger.info(f"Features: {features}")
        try:
            prediction_result = self.model.predict(features)
            predicted_class = int(prediction_result[0])
        except Exception as e:
            logger.error(f"Error in model.predict(): {e}", exc_info=True)
            raise
        try:
            probs_result = self.model.predict_proba(features)
            probs = probs_result[0]
        except Exception as e:
            logger.error(f"Error in model.predict_proba(): {e}", exc_info=True)
            raise
        try:
            disease = self.label_map.get(predicted_class, self.class_names[predicted_class])
        except Exception as e:
            logger.error(f"Error getting disease name: {e}", exc_info=True)
            raise
        confidence = float(np.max(probs))
        return predicted_class, disease, confidence, probs.tolist()

    def generate_shap_explanation(self, scaled_features, predicted_class, diagnosis_case_id):
        try:
            if not self.shap_explainer:
                return None
            explanation = self.shap_explainer.explain_prediction(
                symptoms_data=scaled_features[0],
                predicted_class=predicted_class
            )
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
                'ModelName': 'brain_clinical_logistic_regression',
                'Explanation': explanation,
                'Timestamp': time.time()
            }
            self.producer.send(
                'shap-explanations',
                key=str(diagnosis_case_id).encode('utf-8'),
                value=shap_message
            )
            return True
        except Exception as e:
            logger.error(f"Failed to send SHAP to Kafka: {str(e)}", exc_info=True)
            return False

    def process_message(self, message: dict):
        start_time = time.time()
        request_id = None
        diagnosis_case_id = None
        try:
            request_id = message.get("requestId") or message.get("request_id") or message.get("diagnosisCaseId", "")
            diagnosis_case_id = message.get("diagnosisCaseId") or message.get("diagnosis_case_id")
            if "clinicalData" in message:
                symptoms = message["clinicalData"].get("symptoms", {})
            else:
                symptoms = message.get("symptoms", {})
            features, original_values = self.preprocess(symptoms)
            predicted_class, disease, confidence, probabilities = self.predict(features)
            shap_explanation = None
            if diagnosis_case_id and self.shap_explainer:
                shap_explanation = self.generate_shap_explanation(features, predicted_class, diagnosis_case_id)
                if shap_explanation:
                    self.send_shap_to_kafka(diagnosis_case_id, request_id, shap_explanation)
            processing_time = (time.time() - start_time) * 1000
            probabilities_dict = {
                self.class_names[i]: float(probabilities[i])
                for i in range(min(len(self.class_names), len(probabilities)))
            }
            response = {
                "RequestId": request_id,
                "Prediction": disease,
                "Confidence": confidence,
                "Probabilities": probabilities_dict,
                "ProcessingTimeMs": processing_time,
                "Success": True,
                "ErrorMessage": None,
                "HasShapExplanation": shap_explanation is not None
            }
            self.producer.send(OUTPUT_TOPIC, key=request_id.encode('utf-8'), value=response)
        except Exception as e:
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
        if not self.load_models():
            return
        self.initialize_shap()
        if not self.setup_kafka():
            return
        try:
            for msg in self.consumer:
                self.process_message(msg.value)
        except KeyboardInterrupt:
            pass
        except Exception as e:
            logger.error(f"Service error: {e}")
        finally:
            self.cleanup()

    def cleanup(self):
        if self.consumer:
            self.consumer.close()
        if self.producer:
            self.producer.close()


if __name__ == "__main__":
    ClinicalService().start()
