import os
import torch
from pathlib import Path

# Project paths
PROJECT_ROOT = Path(__file__).parent
DATA_DIR = PROJECT_ROOT / "data"
RAW_DATA_DIR = DATA_DIR / "raw"
PROCESSED_DATA_DIR = DATA_DIR / "processed"
SPLITS_DIR = DATA_DIR / "splits"
MODEL_DIR = PROJECT_ROOT / "models" / "checkpoints"

# Create directories
for dir_path in [RAW_DATA_DIR, PROCESSED_DATA_DIR, SPLITS_DIR, MODEL_DIR]:
    dir_path.mkdir(parents=True, exist_ok=True)

# Image preprocessing parameters
IMG_SIZE = 224  # ResNet input size
PATCH_SIZE = 64  # Size of patch around nodule
HU_MIN = -1000  # Hounsfield unit window
HU_MAX = 400
NORMALIZE_MEAN = [0.485, 0.456, 0.406]  # ImageNet stats
NORMALIZE_STD = [0.229, 0.224, 0.225]

# Training parameters
BATCH_SIZE = 32
NUM_EPOCHS = 50
LEARNING_RATE = 0.001
NUM_WORKERS = 4
DEVICE = "cuda" if torch.cuda.is_available() else "cpu"

# Model parameters
NUM_CLASSES = 2  # Benign vs Malignant
PRETRAINED = True

# Data split ratios
TRAIN_RATIO = 0.7
VAL_RATIO = 0.15
TEST_RATIO = 0.15

print(f" Config loaded. Using device: {DEVICE}")