"""
Brain Tumor Imaging Module - Kafka Consumer Service
Listens to Kafka topic for imaging diagnostic requests
Performs inference using ResNet18 model
Sends results back via Kafka
"""

import json
import logging
from kafka import KafkaConsumer, KafkaProducer
import torch
import torch.nn as nn
from torchvision import models, transforms
from PIL import Image
import io
import base64

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Kafka configuration
KAFKA_BOOTSTRAP_SERVERS = ['localhost:9092']
INPUT_TOPIC = 'imaging-requests'
OUTPUT_TOPIC = 'imaging-results'

# Model configuration
MODEL_PATH = 'neurological_resnet18_best.pth'
DEVICE = torch.device('cpu')


class ImagingService:
    def __init__(self):
        """Initialize the imaging service with model and Kafka connections"""
        self.model = None
        self.transform = None
        self.consumer = None
        self.producer = None
        
    def load_model(self):
        """Load the trained ResNet18 model"""
        try:
            logger.info(f"Loading model from {MODEL_PATH}...")
            
            # Create ResNet18 architecture (modify final layer for your number of classes)
            # Brain tumor model has 8 classes
            num_classes = 8
            
            self.model = models.resnet18(pretrained=False)
            num_features = self.model.fc.in_features
            self.model.fc = nn.Linear(num_features, num_classes)
            
            # Load the saved weights
            state_dict = torch.load(MODEL_PATH, map_location=DEVICE, weights_only=False)
            self.model.load_state_dict(state_dict)
            
            # Set to evaluation mode
            self.model.to(DEVICE)
            self.model.eval()
            
            # Define image preprocessing
            self.transform = transforms.Compose([
                transforms.Resize((224, 224)),
                transforms.ToTensor(),
                transforms.Normalize(mean=[0.485, 0.456, 0.406], 
                                   std=[0.229, 0.224, 0.225])
            ])
            
            logger.info("✓ Model loaded successfully!")
            return True
            
        except Exception as e:
            logger.error(f"Failed to load model: {str(e)}")
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
                group_id='imaging-service-group',
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
    
    def preprocess_image(self, image_data):
        """Preprocess image from base64 string"""
        try:
            # Decode base64 image
            image_bytes = base64.b64decode(image_data)
            image = Image.open(io.BytesIO(image_bytes)).convert('RGB')
            
            # Apply transforms
            image_tensor = self.transform(image)
            image_tensor = image_tensor.unsqueeze(0)  # Add batch dimension
            
            return image_tensor
            
        except Exception as e:
            logger.error(f"Image preprocessing error: {str(e)}")
            raise
    
    def predict(self, image_tensor):
        """Make prediction using the model"""
        try:
            with torch.no_grad():
                outputs = self.model(image_tensor)
                probabilities = torch.nn.functional.softmax(outputs, dim=1)
                confidence, predicted = torch.max(probabilities, 1)
                
            return {
                'predicted_class': int(predicted.item()),
                'confidence': float(confidence.item()),
                'probabilities': probabilities.squeeze().tolist()
            }
            
        except Exception as e:
            logger.error(f"Prediction error: {str(e)}")
            raise
    
    def process_message(self, message):
        """Process incoming diagnostic request"""
        try:
            request_id = message.get('request_id')
            patient_id = message.get('patient_id')
            image_data = message.get('image_data')
            
            logger.info(f"Processing request {request_id} for patient {patient_id}")
            
            # Preprocess image
            image_tensor = self.preprocess_image(image_data)
            
            # Make prediction
            prediction = self.predict(image_tensor)
            
            # Prepare response
            response = {
                'request_id': request_id,
                'patient_id': patient_id,
                'module': 'imaging',
                'prediction': prediction,
                'status': 'success'
            }
            
            # Send result
            self.producer.send(OUTPUT_TOPIC, value=response)
            logger.info(f"✓ Sent result for request {request_id}")
            
        except Exception as e:
            logger.error(f"Error processing message: {str(e)}")
            
            # Send error response
            error_response = {
                'request_id': message.get('request_id'),
                'patient_id': message.get('patient_id'),
                'module': 'imaging',
                'status': 'error',
                'error': str(e)
            }
            self.producer.send(OUTPUT_TOPIC, value=error_response)
    
    def start(self):
        """Start the service"""
        logger.info("Starting Brain Tumor Imaging Service...")
        
        # Load model
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
    service = ImagingService()
    service.start()
