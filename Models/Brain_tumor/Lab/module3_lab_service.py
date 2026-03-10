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
from lab_shap_explainer import LabResultsShapExplainer

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

KAFKA_BOOTSTRAP_SERVERS = [os.getenv('KAFKA_BOOTSTRAP_SERVERS') or 'localhost:9092']
INPUT_TOPIC = 'brain-lab-requests'
OUTPUT_TOPIC = 'brain-lab-results'

BASE_DIR = os.path.dirname(__file__)
MODEL_PATH = os.path.join(BASE_DIR, 'neurological_lab_rf.pkl')
SCALER_PATH = os.path.join(BASE_DIR, 'neurological_lab_scaler.pkl')
METADATA_PATH = os.path.join(BASE_DIR, 'neurological_lab_metadata.pkl')


class LabResultsService:
    def __init__(self):
        self.model = None
        self.scaler = None
        self.metadata = None
        self.consumer = None
        self.producer = None
        self.shap_explainer = None  # NEW: SHAP explainer


        self.class_names = ['normal', 'abnormal']


        self.lab_features = [
            'hemoglobin', 'wbc_count', 'platelet_count', 'creatinine',
            'albumin', 'bilirubin', 'glucose', 'ldh'
        ]


        self.reference_ranges = {
            'hemoglobin': (12.0, 16.0),
            'wbc_count': (4000, 11000),
            'platelet_count': (150000, 450000),
            'creatinine': (0.6, 1.2),
            'albumin': (3.5, 5.5),
            'bilirubin': (0.1, 1.2),
            'glucose': (70, 100),
            'ldh': (140, 280)
        }

    def load_model(self):
        try:
            logger.info("Loading lab results models...")
            self.model = joblib.load(MODEL_PATH)
            self.scaler = joblib.load(SCALER_PATH)
            self.metadata = joblib.load(METADATA_PATH)


            if self.metadata and 'feature_names' in self.metadata:
                self.lab_features = self.metadata['feature_names']
                logger.info(f"Using features from metadata: {self.lab_features}")

            logger.info(" All lab results models loaded successfully!")
            return True
        except Exception as e:
            logger.error(f"Failed to load models: {str(e)}")
            return False

    def initialize_shap(self):

        try:
            logger.info("Initializing SHAP explainer...")


            self.shap_explainer = LabResultsShapExplainer(
                model=self.model,
                feature_names=self.lab_features,
                background_data=None
            )

            logger.info(" SHAP explainer initialized!")
            return True
        except Exception as e:
            logger.error(f"Failed to initialize SHAP: {str(e)}")
            logger.warning("Service will continue WITHOUT SHAP explanations")
            return False

    def setup_kafka(self):
        try:
            logger.info("Connecting to Kafka...")

            self.consumer = KafkaConsumer(
                INPUT_TOPIC,
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                auto_offset_reset='latest',
                enable_auto_commit=True,
                group_id='lab-service-group',
                value_deserializer=lambda x: json.loads(x.decode('utf-8'))
            )

            self.producer = KafkaProducer(
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                value_serializer=lambda x: json.dumps(x).encode('utf-8')
            )

            logger.info(" Connected to Kafka!")
            return True
        except Exception as e:
            logger.error(f"Failed to connect to Kafka: {str(e)}")
            return False

    def preprocess_lab_results(self, lab_data):
        try:
            logger.info(f"Preprocessing lab data: {lab_data}")
            df = pd.DataFrame([lab_data])
            expected_features = self.metadata.get('feature_names', df.columns.tolist())

            for feature in expected_features:
                if feature not in df.columns:
                    default_value = self.metadata.get('feature_defaults', {}).get(feature, 0)
                    df[feature] = default_value

            df = df[expected_features]
            logger.info(f"Features after preprocessing: {df.columns.tolist()}")
            scaled_features = self.scaler.transform(df)
            return scaled_features
        except Exception as e:
            logger.error(f"Lab results preprocessing error: {str(e)}", exc_info=True)
            raise

    def predict(self, features):
        try:
            predicted_class = int(self.model.predict(features)[0])
            probabilities = self.model.predict_proba(features)[0]

            confidence = float(np.max(probabilities))

            # Safely get condition name
            if predicted_class < len(self.class_names):
                condition = self.class_names[predicted_class]
            else:
                condition = f"class_{predicted_class}"


            top_features = None
            if hasattr(self.model, 'feature_importances_'):
                feature_names = self.metadata.get('feature_names', [])
                importances = self.model.feature_importances_
                feature_importance = dict(zip(feature_names, importances.tolist()))
                sorted_features = sorted(feature_importance.items(),
                                       key=lambda x: x[1],
                                       reverse=True)[:5]
                top_features = dict(sorted_features)

            return {
                'predicted_class': predicted_class,
                'condition': condition,
                'confidence': confidence,
                'probabilities': probabilities.tolist(),
                'top_features': top_features
            }
        except Exception as e:
            logger.error(f"Prediction error: {str(e)}", exc_info=True)
            raise

    def generate_shap_explanation(self, scaled_features, predicted_class, diagnosis_case_id):

        try:
            if not self.shap_explainer:
                logger.warning("SHAP explainer not initialized, skipping explanation")
                return None

            logger.info(f"Generating SHAP explanation for case {diagnosis_case_id}...")


            explanation = self.shap_explainer.explain_prediction(
                lab_data=scaled_features[0],
                predicted_class=predicted_class,
                reference_ranges=self.reference_ranges
            )

            logger.info(f"✓ SHAP explanation generated for case {diagnosis_case_id}")
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
                'ModelName': 'brain_lab_random_forest',
                'Explanation': explanation,
                'Timestamp': time.time()
            }


            self.producer.send(
                'shap-explanations',
                key=str(diagnosis_case_id).encode('utf-8'),
                value=shap_message
            )

            logger.info(f"✓ Sent SHAP explanation to Kafka for case {diagnosis_case_id}")
            return True

        except Exception as e:
            logger.error(f"Failed to send SHAP to Kafka: {str(e)}", exc_info=True)
            return False

    def process_message(self, message):
        start_time = time.time()
        request_id = None
        diagnosis_case_id = None

        try:
            request_id = message.get('requestId') or message.get('request_id', '')
            diagnosis_case_id = message.get('diagnosisCaseId') or message.get('diagnosis_case_id')

            logger.info(f"Processing lab request {request_id} (case {diagnosis_case_id})")


            lab_data_obj = message.get('laboratoryData') or message.get('lab_results', {})
            if isinstance(lab_data_obj, dict):
                lab_data = lab_data_obj.get('bloodTests') or lab_data_obj
            else:
                lab_data = {}


            features = self.preprocess_lab_results(lab_data)
            prediction = self.predict(features)


            shap_explanation = None
            if diagnosis_case_id and self.shap_explainer:
                shap_explanation = self.generate_shap_explanation(
                    features,
                    prediction['predicted_class'],
                    diagnosis_case_id
                )


                if shap_explanation:
                    self.send_shap_to_kafka(diagnosis_case_id, request_id, shap_explanation)

            processing_time = (time.time() - start_time) * 1000


            num_classes = min(len(self.class_names), len(prediction['probabilities']))
            probabilities_dict = {}

            for i in range(num_classes):
                probabilities_dict[self.class_names[i]] = float(prediction['probabilities'][i])

            for i in range(num_classes, len(prediction['probabilities'])):
                probabilities_dict[f"class_{i}"] = float(prediction['probabilities'][i])

            response = {
                'RequestId': request_id,
                'Prediction': prediction['condition'],
                'Confidence': prediction['confidence'],
                'Probabilities': probabilities_dict,
                'ProcessingTimeMs': processing_time,
                'Success': True,
                'ErrorMessage': None,
                'HasShapExplanation': shap_explanation is not None  # NEW: Flag for orchestrator
            }

            self.producer.send(OUTPUT_TOPIC, key=request_id.encode('utf-8'), value=response)

            shap_status = " with SHAP" if shap_explanation else ""
            logger.info(f" Sent result: {prediction['condition']} (confidence: {prediction['confidence']:.2%}, {processing_time:.2f}ms) {shap_status}")

        except Exception as e:
            logger.error(f"Error processing message: {str(e)}", exc_info=True)
            processing_time = (time.time() - start_time) * 1000

            error_response = {
                'RequestId': request_id or 'unknown',
                'Prediction': 'inconclusive',
                'Confidence': 0.0,
                'Probabilities': {name: 0.0 for name in self.class_names},
                'ProcessingTimeMs': processing_time,
                'Success': False,
                'ErrorMessage': str(e),
                'HasShapExplanation': False
            }

            if request_id:
                self.producer.send(OUTPUT_TOPIC, key=request_id.encode('utf-8'), value=error_response)

    def start(self):
        logger.info("Starting Brain Tumor Lab Results Service WITH SHAP...")
        logger.info(f"Detecting conditions: {', '.join(self.class_names)}")
        logger.info("Architecture: AI Service → Kafka → Orchestrator → Database")

        if not self.load_model():
            logger.error("Failed to start service - model loading failed")
            return

        self.initialize_shap()

        if not self.setup_kafka():
            logger.error("Failed to start service - Kafka connection failed")
            return

        logger.info(f"Listening on topic: {INPUT_TOPIC}")
        logger.info(f"Sending results to: {OUTPUT_TOPIC}")
        logger.info(f"Sending SHAP to: shap-explanations")
        logger.info("Service is ready! Waiting for diagnostic requests...")

        try:
            for message in self.consumer:
                self.process_message(message.value)
        except KeyboardInterrupt:
            logger.info("Service stopped by user")
        except Exception as e:
            logger.error(f"Service error: {str(e)}")
        finally:
            self.cleanup()

    def cleanup(self):
        if self.consumer:
            self.consumer.close()
        if self.producer:
            self.producer.close()
        logger.info("Service stopped")


if __name__ == '__main__':
    service = LabResultsService()
    service.start()