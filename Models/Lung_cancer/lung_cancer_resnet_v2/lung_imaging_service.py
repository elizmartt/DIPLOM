import json
import logging
import os
import sys
import time
import io
import base64
import numpy as np
from kafka import KafkaConsumer, KafkaProducer
from PIL import Image
import torch
import torch.nn as nn
from torchvision import models

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from gradcam_utils import GradCAM, overlay_heatmap, generate_gradcam_explanation
from s3_client import download_image, upload_gradcam

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

KAFKA_BOOTSTRAP_SERVERS = [os.getenv('KAFKA_BOOTSTRAP_SERVERS', 'localhost:9092')]
INPUT_TOPIC  = 'lung-imaging-requests'
OUTPUT_TOPIC = 'lung-imaging-results'

MODEL_PATH = 'models/checkpoints/best_real_model.pth'
DEVICE = torch.device('cpu')

IMAGENET_MEAN = torch.tensor([0.485, 0.456, 0.406]).view(3, 1, 1)
IMAGENET_STD  = torch.tensor([0.229, 0.224, 0.225]).view(3, 1, 1)


def pil_to_tensor(pil_img: Image.Image) -> torch.Tensor:
    """
    PIL Image → normalised (1,3,224,224) tensor.
    Goes PIL → raw bytes → torch.frombuffer — never touches numpy at all,
    so no numpy-version conflict can occur.
    """
    pil_224 = pil_img.convert('RGB').resize((224, 224), Image.BILINEAR)
    raw     = pil_224.tobytes()                          # pure Python bytes
    tensor  = torch.frombuffer(bytearray(raw), dtype=torch.uint8).clone()
    tensor  = tensor.view(224, 224, 3).float() / 255.0  # (224,224,3)
    tensor  = tensor.permute(2, 0, 1)                   # (3,224,224)
    tensor  = (tensor - IMAGENET_MEAN) / IMAGENET_STD
    return tensor.unsqueeze(0)                           # (1,3,224,224)


def pil_to_numpy_via_bytes(pil_img: Image.Image) -> np.ndarray:
    """
    PIL Image → uint8 HxWx3 numpy array via raw bytes.
    np.frombuffer on a bytearray (not a PIL internal buffer) creates an array
    owned entirely by the *current* numpy instance.
    """
    w, h = pil_img.size
    pil_rgb = pil_img.convert('RGB')
    raw  = pil_rgb.tobytes()
    arr  = np.frombuffer(bytearray(raw), dtype=np.uint8).copy()
    return arr.reshape((h, w, 3))


class LungImagingService:
    def __init__(self):
        self.model    = None
        self.consumer = None
        self.producer = None
        self.gradcam  = None
        self.class_names = ['no_cancer', 'lung_cancer']

    # ------------------------------------------------------------------ #
    #  Model loading                                                       #
    # ------------------------------------------------------------------ #
    def load_model(self):
        try:
            logger.info(f"Loading model from {MODEL_PATH}")
            self.model = models.resnet18(weights=None)
            self.model.fc = nn.Linear(self.model.fc.in_features, len(self.class_names))

            checkpoint = torch.load(MODEL_PATH, map_location=DEVICE, weights_only=False)
            state_dict = checkpoint.get('model_state_dict', checkpoint)
            state_dict = {
                k.replace('resnet.', '') if k.startswith('resnet.') else k: v
                for k, v in state_dict.items()
            }
            self.model.load_state_dict(state_dict)
            self.model.to(DEVICE)
            self.model.eval()

            self.gradcam = GradCAM(self.model, self.model.layer4)
            logger.info("✓ Model loaded + Grad-CAM initialised on layer4")
            return True
        except Exception as e:
            logger.error(f"Model loading failed: {e}", exc_info=True)
            return False

    # ------------------------------------------------------------------ #
    #  Kafka                                                               #
    # ------------------------------------------------------------------ #
    def setup_kafka(self):
        try:
            self.consumer = KafkaConsumer(
                INPUT_TOPIC,
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                auto_offset_reset='latest',
                enable_auto_commit=True,
                group_id='lung-imaging-service-group',
                value_deserializer=lambda x: json.loads(x.decode('utf-8'))
            )
            self.producer = KafkaProducer(
                bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
                value_serializer=lambda x: json.dumps(x).encode('utf-8')
            )
            logger.info("✓ Kafka connected")
            return True
        except Exception as e:
            logger.error(f"Kafka connection failed: {e}", exc_info=True)
            return False

    # ------------------------------------------------------------------ #
    #  Image loading                                                       #
    # ------------------------------------------------------------------ #
    def load_pil_image(self, image_source: str) -> Image.Image:
        """Download from S3 key or decode base64; always returns PIL Image."""
        if not image_source.startswith('data:'):
            logger.info(f"Downloading image from S3: {image_source}")
            image_bytes = download_image(image_source)
            try:
                from dicom_processor import load_image_from_bytes
                image, _ = load_image_from_bytes(image_bytes)
                if isinstance(image, np.ndarray):
                    image = Image.fromarray(image)
                logger.info("Loaded DICOM image from S3")
                return image.convert('RGB')
            except Exception:
                pass
            image = Image.open(io.BytesIO(image_bytes)).convert('RGB')
            logger.info("Loaded regular image from S3")
        else:
            image_bytes = base64.b64decode(image_source.split(',', 1)[-1])
            image = Image.open(io.BytesIO(image_bytes)).convert('RGB')
            logger.info("Loaded image from base64")
        return image

    # ------------------------------------------------------------------ #
    #  Prediction + Grad-CAM                                              #
    # ------------------------------------------------------------------ #
    def predict_with_gradcam(self, pil_image: Image.Image, case_id: str, s3_key: str) -> dict:
        # Tensor via pure bytes — no numpy involved
        image_tensor = pil_to_tensor(pil_image)

        # Inference
        with torch.no_grad():
            outputs    = self.model(image_tensor)
            probs      = torch.softmax(outputs, dim=1)
            confidence, predicted = torch.max(probs, 1)

        predicted_class  = int(predicted.item())
        confidence_value = float(confidence.item())

        # Grad-CAM heatmap
        heatmap = self.gradcam.generate_heatmap(image_tensor, predicted_class)

        # Overlay numpy — via bytearray, owned by current numpy
        overlay_np = pil_to_numpy_via_bytes(pil_image)
        overlay    = overlay_heatmap(overlay_np, heatmap, alpha=0.4, x_offset=-20, y_offset=0)

        # Encode overlay PNG
        overlay_buf = io.BytesIO()
        Image.fromarray(overlay).save(overlay_buf, format='PNG')
        overlay_bytes = overlay_buf.getvalue()
        overlay_b64   = base64.b64encode(overlay_bytes).decode()

        # Encode heatmap PNG
        heatmap_buf = io.BytesIO()
        Image.fromarray((heatmap * 255).astype(np.uint8), mode='L').save(heatmap_buf, format='PNG')
        heatmap_bytes = heatmap_buf.getvalue()
        heatmap_b64   = base64.b64encode(heatmap_bytes).decode()

        # Upload to S3
        base_name      = os.path.splitext(os.path.basename(s3_key))[0] if '/' in s3_key else 'image'
        overlay_s3_key = upload_gradcam(case_id, 'overlay', base_name, overlay_bytes)
        heatmap_s3_key = upload_gradcam(case_id, 'heatmap', base_name, heatmap_bytes)
        logger.info(f"✓ Grad-CAM uploaded to S3: {overlay_s3_key}")

        explanation = generate_gradcam_explanation(
            heatmap, predicted_class, confidence_value, self.class_names[predicted_class]
        )

        return {
            'predicted_class':     predicted_class,
            'confidence':          confidence_value,
            'probabilities':       probs.squeeze().tolist(),
            'gradcam_heatmap':     heatmap_b64,
            'gradcam_overlay':     overlay_b64,
            'gradcam_explanation': explanation,
            'saved_files': {
                'gradcam_overlay_s3_key': overlay_s3_key,
                'gradcam_heatmap_s3_key': heatmap_s3_key,
            }
        }

    # ------------------------------------------------------------------ #
    #  Message handler                                                     #
    # ------------------------------------------------------------------ #
    def process_message(self, message: dict):
        start = time.time()
        request_id = message.get('requestId') or message.get('request_id', '')
        case_id    = message.get('caseId')    or message.get('case_id', request_id)

        logger.info(f"Processing lung imaging request {request_id}")

        try:
            imaging_data = message.get('imagingData') or message.get('imaging_data', {})
            image_source = (
                imaging_data.get('image_path')
                or imaging_data.get('file_path')
                or imaging_data.get('imageData')
            )

            if not image_source:
                raise ValueError("No image path or image data provided")

            pil_image  = self.load_pil_image(image_source)
            prediction = self.predict_with_gradcam(pil_image, case_id, image_source)

            response = {
                'RequestId':  request_id,
                'Prediction': self.class_names[prediction['predicted_class']],
                'Confidence': prediction['confidence'],
                'Probabilities': dict(zip(self.class_names, prediction['probabilities'])),
                'ExplainabilityData': {
                    'method':                 'grad_cam',
                    'gradcam_heatmap_base64': prediction['gradcam_heatmap'],
                    'gradcam_overlay_base64': prediction['gradcam_overlay'],
                    'activation_statistics':  prediction['gradcam_explanation']['activation_statistics'],
                    'focus_pattern':          prediction['gradcam_explanation']['focus_pattern'],
                    'focus_description':      prediction['gradcam_explanation']['focus_description'],
                    'top_regions':            prediction['gradcam_explanation']['top_regions'],
                    'interpretation':         prediction['gradcam_explanation']['interpretation'],
                    'saved_files':            prediction['saved_files'],
                },
                'ProcessingTimeMs': (time.time() - start) * 1000,
                'Success':      True,
                'ErrorMessage': None,
            }

            self.producer.send(OUTPUT_TOPIC, key=request_id.encode(), value=response)
            logger.info("✓ Lung imaging result sent successfully")

        except Exception as e:
            logger.error(f"Error processing lung imaging {request_id}: {e}", exc_info=True)
            self.producer.send(OUTPUT_TOPIC, key=request_id.encode(), value={
                'RequestId':        request_id,
                'Success':          False,
                'ErrorMessage':     str(e),
                'ProcessingTimeMs': (time.time() - start) * 1000,
            })

    # ------------------------------------------------------------------ #
    #  Lifecycle                                                           #
    # ------------------------------------------------------------------ #
    def start(self):
        logger.info("Starting Lung Cancer Imaging Service with Grad-CAM...")
        if not self.load_model() or not self.setup_kafka():
            return
        logger.info("✓ Service ready! Listening for lung imaging requests...")
        try:
            for msg in self.consumer:
                self.process_message(msg.value)
        except KeyboardInterrupt:
            logger.info("Service stopped by user")
        finally:
            self.cleanup()

    def cleanup(self):
        if self.gradcam:
            self.gradcam.remove_hooks()
        if self.consumer:
            self.consumer.close()
        if self.producer:
            self.producer.close()
        logger.info("Lung imaging service stopped")


if __name__ == '__main__':
    LungImagingService().start()