"""
Brain Tumor Lab Results Module - Kafka Consumer Service
Listens to Kafka topic for laboratory results diagnostic requests
Performs inference using Random Forest model
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
INPUT_TOPIC = 'lab-requests'
OUTPUT_TOPIC = 'lab-results'

# Model configuration
MODEL_PATH = 'neurological_lab_rf.pkl'
SCALER_PATH = 'neurological_lab_scaler.pkl'
METADATA_PATH = 'neurological_lab_metadata.pkl'


class LabResultsService:
    def __init__(self):
        """Initialize the lab results service with model and Kafka connections"""
        self.model = None
        self.scaler = None
        self.metadata = None
        self.consumer = None
        self.producer = None
        
    def load_model(self):
        """Load the trained Random Forest model and preprocessors"""
        try:
            logger.info("Loading lab results models...")
            
            # Load model
            self.model = joblib.load(MODEL_PATH)
            logger.info(f"✓ Loaded model from {MODEL_PATH}")
            
            # Load scaler
            self.scaler = joblib.load(SCALER_PATH)
            logger.info(f"✓ Loaded scaler from {SCALER_PATH}")
            
            # Load metadata
            self.metadata = joblib.load(METADATA_PATH)
            logger.info(f"✓ Loaded metadata from {METADATA_PATH}")
            
            logger.info("✓ All lab results models loaded successfully!")
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
                group_id='lab-service-group',
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
    
    def preprocess_lab_results(self, lab_data):
        """Preprocess laboratory results for prediction"""
        try:
            # Convert lab results dict to DataFrame
            df = pd.DataFrame([lab_data])
            
            # Ensure all expected features are present
            expected_features = self.metadata.get('feature_names', df.columns.tolist())
            for feature in expected_features:
                if feature not in df.columns:
                    # Use median or mean from training data if available
                    default_value = self.metadata.get('feature_defaults', {}).get(feature, 0)
                    df[feature] = default_value
            
            # Select and order features
            df = df[expected_features]
            
            # Scale features
            scaled_features = self.scaler.transform(df)
            
            return scaled_features
            
        except Exception as e:
            logger.error(f"Lab results preprocessing error: {str(e)}")
            raise
    
    def predict(self, features):
        """Make prediction using the model"""
        try:
            # Get prediction
            prediction = self.model.predict(features)[0]
            
            # Get probability scores
            probabilities = self.model.predict_proba(features)[0]
            
            # Get confidence
            confidence = float(np.max(probabilities))
            
            # Get feature importances if available
            feature_importance = None
            if hasattr(self.model, 'feature_importances_'):
                feature_names = self.metadata.get('feature_names', [])
                importances = self.model.feature_importances_
                feature_importance = dict(zip(feature_names, importances.tolist()))
            
            result = {
                'predicted_class': int(prediction),
                'confidence': confidence,
                'probabilities': probabilities.tolist()
            }
            
            if feature_importance:
                # Get top 5 most important features
                sorted_features = sorted(feature_importance.items(), 
                                       key=lambda x: x[1], 
                                       reverse=True)[:5]
                result['top_features'] = dict(sorted_features)
            
            return result
            
        except Exception as e:
            logger.error(f"Prediction error: {str(e)}")
            raise
    
    def process_message(self, message):
        """Process incoming diagnostic request"""
        try:
            request_id = message.get('request_id')
            patient_id = message.get('patient_id')
            lab_results = message.get('lab_results')
            
            logger.info(f"Processing request {request_id} for patient {patient_id}")
            
            # Preprocess lab results
            features = self.preprocess_lab_results(lab_results)
            
            # Make prediction
            prediction = self.predict(features)
            
            # Prepare response
            response = {
                'request_id': request_id,
                'patient_id': patient_id,
                'module': 'laboratory',
                'prediction': prediction,
                'status': 'success'
            }
            
            # Send result
            self.producer.send(OUTPUT_TOPIC, value=response)
            logger.info(f"✓ Sent result for request {request_id} (confidence: {prediction['confidence']:.2%})")
            
        except Exception as e:
            logger.error(f"Error processing message: {str(e)}")
            
            # Send error response
            error_response = {
                'request_id': message.get('request_id'),
                'patient_id': message.get('patient_id'),
                'module': 'laboratory',
                'status': 'error',
                'error': str(e)
            }
            self.producer.send(OUTPUT_TOPIC, value=error_response)
    
    def start(self):
        """Start the service"""
        logger.info("Starting Brain Tumor Lab Results Service...")
        
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
    service = LabResultsService()
    service.start()
