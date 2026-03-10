import boto3
import os
import io
import logging

logger = logging.getLogger(__name__)

s3 = boto3.client('s3')
BUCKET = os.environ.get('S3_BUCKET_NAME', '')

def download_image(s3_key: str) -> bytes:
    """Download image bytes from S3."""
    logger.info(f"Downloading from S3: {s3_key}")
    response = s3.get_object(Bucket=BUCKET, Key=s3_key)
    return response['Body'].read()

def upload_gradcam(case_id: str, prefix: str, name: str, png_bytes: bytes) -> str:
    """Upload Grad-CAM PNG to S3. Returns S3 key."""
    key = f"gradcam/{case_id}/{prefix}_{name}.png"
    s3.put_object(Bucket=BUCKET, Key=key, Body=png_bytes, ContentType='image/png')
    logger.info(f"✓ Grad-CAM uploaded to S3: {key}")
    return key