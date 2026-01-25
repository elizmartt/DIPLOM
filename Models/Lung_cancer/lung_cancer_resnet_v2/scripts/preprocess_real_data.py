import numpy as np
import pandas as pd
from pathlib import Path
from PIL import Image, UnidentifiedImageError
from tqdm import tqdm
import sys

sys.path.append(str(Path(__file__).parent.parent))
from config import RAW_DATA_DIR, PROCESSED_DATA_DIR

# Map folder names to labels
LABEL_MAP = {
    "benign cases": 0,
    "malignant cases": 1,
    "normal cases": 0
}

def preprocess_ct_images():
    print("="*50)
    print("PREPROCESSING REAL CT DATA")
    print("="*50)

    # Find all image files
    image_extensions = ['.png', '.jpg', '.jpeg']
    all_images = []
    for ext in image_extensions:
        all_images.extend(RAW_DATA_DIR.rglob(f"*{ext}"))

    print(f"\n📊 Found {len(all_images)} images")
    if len(all_images) == 0:
        print("❌ No images found! Exiting.")
        return

    # Output directory
    output_dir = PROCESSED_DATA_DIR / "real_images"
    output_dir.mkdir(parents=True, exist_ok=True)

    data = []

    print("\n🔄 Processing images...")
    for i, img_path in enumerate(tqdm(all_images)):
        try:
            # Look at grandparent folder to determine label
            folder_name = img_path.parent.parent.name.lower()
            label = LABEL_MAP.get(folder_name)
            if label is None:
                print(f"⚠️ Skipping unknown folder: {folder_name}")
                continue

            # Open and preprocess image
            try:
                img = Image.open(img_path).convert('L')
            except UnidentifiedImageError:
                print(f"⚠️ Skipping invalid image: {img_path}")
                continue

            img = img.resize((224, 224))
            new_name = f"ct_{i:05d}.png"
            new_path = output_dir / new_name
            img.save(new_path)

            data.append({
                'image_path': str(new_path),
                'label': label,
                'original_path': str(img_path),
                'patient_id': f"P{i:05d}"
            })

        except Exception as e:
            print(f"⚠️ Error processing {img_path}: {e}")
            continue

    if not data:
        print("❌ No valid images processed. Check folder names!")
        return

    # Save CSV
    df = pd.DataFrame(data, columns=['image_path', 'label', 'original_path', 'patient_id'])
    csv_path = PROCESSED_DATA_DIR / "real_labels.csv"
    df.to_csv(csv_path, index=False)

    print(f"\n✅ Processed {len(df)} images")
    print(f"✅ Images saved to: {output_dir}")
    print(f"✅ Labels saved to: {csv_path}")

    print("\n📊 Label distribution:")
    print(df['label'].value_counts())

    return csv_path


if __name__ == "__main__":
    preprocess_ct_images()
