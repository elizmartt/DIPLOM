import kagglehub
from pathlib import Path
import shutil
from PIL import Image

PROJECT_ROOT = Path(__file__).parent.parent
RAW_DATA_DIR = PROJECT_ROOT / "data" / "raw"
PROCESSED_DATA_DIR = PROJECT_ROOT / "data" / "processed"

RAW_DATA_DIR.mkdir(parents=True, exist_ok=True)
PROCESSED_DATA_DIR.mkdir(parents=True, exist_ok=True)

# --------------------------
# Download dataset
# --------------------------
print("📥 Downloading IQ-OTHNCCD augmented dataset...")
path = kagglehub.dataset_download("aleksandarcvetanov/iq-othnccd-lung-cancer-augmented-dataset")
path = Path(path)

# If kagglehub auto-extracted, path might already be a folder
if path.is_dir():
    extracted_dir = path
    print(f"✅ Dataset already extracted at: {extracted_dir}")
else:
    # If it’s a zip, move & unzip
    dst_zip_path = RAW_DATA_DIR / path.name
    shutil.move(str(path), dst_zip_path)
    import zipfile
    with zipfile.ZipFile(dst_zip_path, 'r') as zip_ref:
        zip_ref.extractall(RAW_DATA_DIR)
    extracted_dir = RAW_DATA_DIR
    dst_zip_path.unlink()
    print(f"✅ Dataset extracted to: {RAW_DATA_DIR}")

# --------------------------
# Check images & remove corrupted
# --------------------------
print("🔍 Checking for corrupted images...")
for img_path in extracted_dir.rglob("*.*"):
    try:
        Image.open(img_path).verify()
    except Exception:
        print(f"⚠️  Removing corrupted image: {img_path}")
        img_path.unlink()

print("✅ Dataset ready! You can now run preprocessing scripts.")
