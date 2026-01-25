"""
Brain Tumor Clinical Module - Kafka Consumer Service
Listens to Kafka topic for clinical symptom diagnostic requests
Performs inference using Logistic Regression model
Sends results back via Kafka
"""

import json
import logging
from kafka import KafkaConsumer, KafkaProducer
import joblib
import pandas as pd
import numpy as np

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Kafka configuration
KAFKA_BOOTSTRAP_SERVERS = ['localhost:9092']
INPUT_TOPIC = 'clinical-requests'
OUTPUT_TOPIC = 'clinical-results'

# Model configuration
MODEL_PATH = 'neurological_clinical_lr.pkl'
SCALER_PATH = 'neurological_clinical_scaler.pkl'
METADATA_PATH = 'neurological_clinical_metadata.pkl'
LABEL_MAP_PATH = 'neurological_label_map.pkl'


class ClinicalService:
    def __init__(self):
        """Initialize the clinical service with model and Kafka connections"""
        self.model = None
        self.scaler = None
        self.metadata = None
        self.label_map = None
        self.consumer = None
        self.producer = None
        
    def load_model(self):
        """Load the trained Logistic Regression model and preprocessors"""
        try:
            logger.info("Loading clinical models...")
            
            # Load model
            self.model = joblib.load(MODEL_PATH)
            logger.info(f"✓ Loaded model from {MODEL_PATH}")
            
            # Load scaler
            self.scaler = joblib.load(SCALER_PATH)
            logger.info(f"✓ Loaded scaler from {SCALER_PATH}")
            
            # Load metadata
            self.metadata = joblib.load(METADATA_PATH)
            logger.info(f"✓ Loaded metadata from {METADATA_PATH}")
            
            # Load label map
            self.label_map = joblib.load(LABEL_MAP_PATH)
            logger.info(f"✓ Loaded label map from {LABEL_MAP_PATH}")
            
            logger.info("✓ All clinical models loaded successfully!")
            return True
            
        except Exception as e:
            logger.error(f"Failed to load models: {str(e)}")
            return False
    
    def setup_kafka(self):
        """Initialize Kafka consumer and producer"""
        try:
            logger.info("Connecting to Kafka...")
            
            # Create consumer
            self.consumer = KafkaConsumer(
                INPUT_TOPIC,
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                auto_offset_reset='latest',
                enable_auto_commit=True,
                group_id='clinical-service-group',
                value_deserializer=lambda x: json.loads(x.decode('utf-8'))
            )
            
            # Create producer
            self.producer = KafkaProducer(
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                value_serializer=lambda x: json.dumps(x).encode('utf-8')
            )
            
            logger.info("✓ Connected to Kafka!")
            return True
            
        except Exception as e:
            logger.error(f"Failed to connect to Kafka: {str(e)}")
            return False
    
    def preprocess_symptoms(self, symptoms_data):
        """Preprocess symptom data for prediction"""
        try:
            # Convert symptoms dict to DataFrame
            df = pd.DataFrame([symptoms_data])
            
            # Ensure all expected features are present
            expected_features = self.metadata.get('feature_names', df.columns.tolist())
            for feature in expected_features:
                if feature not in df.columns:
                    df[feature] = 0  # Default value for missing features
            
            # Select and order features
            df = df[expected_features]
            
            # Scale features
            scaled_features = self.scaler.transform(df)
            
            return scaled_features
            
        except Exception as e:
            logger.error(f"Symptom preprocessing error: {str(e)}")
            raise
    
    def predict(self, features):
        """Make prediction using the model"""
        try:
            # Get prediction
            prediction = self.model.predict(features)[0]
            
            # Get probability scores
            probabilities = self.model.predict_proba(features)[0]
            
            # Get disease name from label map
            disease_name = self.label_map.get(prediction, f"Unknown_{prediction}")
            
            # Get confidence
            confidence = float(np.max(probabilities))
            
            return {
                'predicted_class': int(prediction),
                'disease_name': disease_name,
                'confidence': confidence,
                'probabilities': probabilities.tolist()
            }
            
        except Exception as e:
            logger.error(f"Prediction error: {str(e)}")
            raise
    
    def process_message(self, message):
        """Process incoming diagnostic request"""
        try:
            request_id = message.get('request_id')
            patient_id = message.get('patient_id')
            symptoms = message.get('symptoms')
            
            logger.info(f"Processing request {request_id} for patient {patient_id}")
            
            # Preprocess symptoms
            features = self.preprocess_symptoms(symptoms)
            
            # Make prediction
            prediction = self.predict(features)
            
            # Prepare response
            response = {
                'request_id': request_id,
                'patient_id': patient_id,
                'module': 'clinical',
                'prediction': prediction,
                'status': 'success'
            }
            
            # Send result
            self.producer.send(OUTPUT_TOPIC, value=response)
            logger.info(f"✓ Sent result for request {request_id}: {prediction['disease_name']}")
            
        except Exception as e:
            logger.error(f"Error processing message: {str(e)}")
            
            # Send error response
            error_response = {
                'request_id': message.get('request_id'),
                'patient_id': message.get('patient_id'),
                'module': 'clinical',
                'status': 'error',
                'error': str(e)
            }
            self.producer.send(OUTPUT_TOPIC, value=error_response)
    
    def start(self):
        """Start the service"""
        logger.info("Starting Brain Tumor Clinical Service...")
        
        # Load models
        if not self.load_model():
            logger.error("Failed to start service - model loading failed")
            return
        
        # Connect to Kafka
        if not self.setup_kafka():
            logger.error("Failed to start service - Kafka connection failed")
            return
        
        # Start consuming messages
        logger.info(f"Listening on topic: {INPUT_TOPIC}")
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
        """Clean up resources"""
        if self.consumer:
            self.consumer.close()
        if self.producer:
            self.producer.close()
        logger.info("Service stopped")


if __name__ == '__main__':
    service = ClinicalService()
    service.start()
