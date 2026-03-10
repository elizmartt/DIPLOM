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

KAFKA_BOOTSTRAP_SERVERS = [os.getenv('KAFKA_BOOTSTRAP_SERVERS', 'localhost:9092')]
INPUT_TOPIC = 'lung-lab-requests'
OUTPUT_TOPIC = 'lung-lab-results'

MODEL_PATH = 'models/random_forest_real.pkl'
SCALER_PATH = 'models/scaler_real.pkl'


class LungLabService:
    def __init__(self):
        self.model = None
        self.scaler = None
        self.consumer = None
        self.producer = None
        self.shap_explainer = None
        self.class_names = ['no_cancer', 'lung_cancer']
        self.lab_features = []
        self.reference_ranges = {
            'cea': (0, 3.0),
            'cyfra_21_1': (0, 3.3),
            'nse': (0, 16.3),
            'scc': (0, 2.0),
            'hemoglobin': (12.0, 16.0),
            'ldh': (140, 280),
            'alkaline_phosphatase': (30, 120),
            'calcium': (8.5, 10.5)
        }

    def load_model(self):
        try:
            logger.info("Loading lab results models...")
            self.model = joblib.load(MODEL_PATH)
            self.scaler = joblib.load(SCALER_PATH)
            if hasattr(self.scaler, 'feature_names_in_'):
                self.lab_features = list(self.scaler.feature_names_in_)
                logger.info(f"Detected features:{self.lab_features}")
            logger.info("All lab results models loaded successfully")
            return True
        except Exception as e:
            logger.error(f"Failed to load models: {str(e)}")
            return False

    def initialize_shap(self):
        try:
            logger.info("Initializing SHAP explainer...")
            if not self.lab_features:
                logger.warning("No feature names available, SHAP may not work correctly")
                return False
            self.shap_explainer = LabResultsShapExplainer(
                model=self.model,
                feature_names=self.lab_features,
                background_data=None
            )
            logger.info("SHAP explainer initialized")
            return True
        except Exception as e:
            logger.error(f"Failed to initialize SHAP: {str(e)}")
            logger.warning("Service will continue without SHAP explanations")
            return False

    def setup_kafka(self):
        try:
            logger.info("Connecting to Kafka...")
            self.consumer = KafkaConsumer(
                INPUT_TOPIC,
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                auto_offset_reset='latest',
                enable_auto_commit=True,
                group_id='lung-lab-service-group',
                value_deserializer=lambda x: json.loads(x.decode('utf-8'))
            )
            self.producer = KafkaProducer(
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                value_serializer=lambda x: json.dumps(x).encode('utf-8')
            )
            logger.info("Connected to Kafka")
            return True
        except Exception as e:
            logger.error(f"Failed to connect to Kafka: {str(e)}")
            return False

    def preprocess_lab_results(self, lab_data):
        try:
            df = pd.DataFrame([lab_data])
            if hasattr(self.scaler, 'feature_names_in_'):
                expected_features = list(self.scaler.feature_names_in_)
            else:
                expected_features = df.columns.tolist()
            for feature in expected_features:
                if feature not in df.columns:
                    df[feature] = 0
            df = df[expected_features]
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
            condition = self.class_names[predicted_class]
            top_features = None
            if hasattr(self.model, 'feature_importances_') and hasattr(self.scaler, 'feature_names_in_'):
                feature_names = list(self.scaler.feature_names_in_)
                importances = self.model.feature_importances_
                feature_importance = dict(zip(feature_names, importances.tolist()))
                sorted_features = sorted(feature_importance.items(), key=lambda x: x[1], reverse=True)[:5]
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
                return None
            explanation = self.shap_explainer.explain_prediction(
                lab_data=scaled_features[0],
                predicted_class=predicted_class,
                reference_ranges=self.reference_ranges
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
                'ModelName': 'lung_lab_random_forest',
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

    def process_message(self, message):
        start_time = time.time()
        request_id = None
        diagnosis_case_id = None
        try:
            request_id = message.get('requestId') or message.get('request_id', '')
            diagnosis_case_id = message.get('diagnosisCaseId') or message.get('diagnosis_case_id')
            lab_data_obj = message.get('laboratoryData') or message.get('lab_results', {})
            lab_data = lab_data_obj.get('bloodTests') or lab_data_obj if isinstance(lab_data_obj, dict) else {}
            features = self.preprocess_lab_results(lab_data)
            prediction = self.predict(features)
            shap_explanation = None
            if diagnosis_case_id and self.shap_explainer:
                shap_explanation = self.generate_shap_explanation(features, prediction['predicted_class'], diagnosis_case_id)
                if shap_explanation:
                    self.send_shap_to_kafka(diagnosis_case_id, request_id, shap_explanation)
            processing_time = (time.time() - start_time) * 1000
            probabilities_dict = {self.class_names[i]: float(prediction['probabilities'][i]) for i in range(len(self.class_names))}
            response = {
                'RequestId': request_id,
                'Prediction': prediction['condition'],
                'Confidence': prediction['confidence'],
                'Probabilities': probabilities_dict,
                'ProcessingTimeMs': processing_time,
                'Success': True,
                'ErrorMessage': None,
                'HasShapExplanation': shap_explanation is not None
            }
            self.producer.send(OUTPUT_TOPIC, key=request_id.encode('utf-8'), value=response)
        except Exception as e:
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
        logger.info("Starting Lung Cancer Lab Results Service WITH SHAP...")
        if not self.load_model():
            logger.error("Failed to start service - model loading failed")
            return
        self.initialize_shap()
        if not self.setup_kafka():
            logger.error("Failed to start service - Kafka connection failed")
            return
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
    service = LungLabService()
    service.start()
