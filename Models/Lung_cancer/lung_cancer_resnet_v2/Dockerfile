FROM python:3.10-slim

WORKDIR /app

RUN apt-get update && apt-get install -y \
    build-essential \
    gcc \
    g++ \
    libgomp1 \
    && rm -rf /var/lib/apt/lists/*

RUN pip install --upgrade pip

# Step 1: Install PyTorch CPU-only first
RUN pip install --no-cache-dir \
    torch==2.6.0+cpu \
    torchvision==0.21.0+cpu \
    --index-url https://download.pytorch.org/whl/cpu

# Step 2: Pin numpy AFTER torch so nothing overwrites it
# numpy 1.26.4 is the last 1.x — fully compatible with torch 2.6 and cv2
RUN pip install --no-cache-dir --force-reinstall "numpy==1.26.4"

# Step 3: Install remaining deps, excluding torch/torchvision/numpy from overwrite
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt \
    --ignore-installed torch torchvision numpy

COPY . .

ENV PYTHONUNBUFFERED=1
ENV KAFKA_BOOTSTRAP_SERVERS=kafka:29092

CMD ["python", "lung_imaging_service.py"]