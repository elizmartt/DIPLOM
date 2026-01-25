import numpy as np
import pandas as pd
from pathlib import Path
from PIL import Image
import sys
sys.path.append(str(Path(__file__).parent.parent))
from config import PROCESSED_DATA_DIR

def create_synthetic_dataset(num_samples=1000):
    """Create synthetic lung CT images and labels"""
    
    print("Creating synthetic dataset for testing...")
    
    images_dir = PROCESSED_DATA_DIR / "images"
    images_dir.mkdir(parents=True, exist_ok=True)
    
    data = []
    
    for i in range(num_samples):
        img = np.random.randint(50, 150, (224, 224), dtype=np.uint8)
        
        is_malignant = i % 2
        
        if is_malignant:
            center_x, center_y = 112 + np.random.randint(-30, 30), 112 + np.random.randint(-30, 30)
            y, x = np.ogrid[:224, :224]
            mask = (x - center_x)**2 + (y - center_y)**2 <= 15**2
            img[mask] = np.random.randint(200, 255)
        
        img_path = images_dir / f"sample_{i:04d}.png"
        Image.fromarray(img).save(img_path)
        
        data.append({
            'image_path': str(img_path),
            'label': is_malignant,
            'patient_id': f"P{i:04d}"
        })
    
    df = pd.DataFrame(data)
    csv_path = PROCESSED_DATA_DIR / "labels.csv"
    df.to_csv(csv_path, index=False)
    
    print(f"✅ Created {num_samples} synthetic images")
    print(f"✅ Images saved to: {images_dir}")
    print(f"✅ Labels saved to: {csv_path}")
    print(f"\nLabel distribution:")
    print(df['label'].value_counts())
    
    return csv_path

if __name__ == "__main__":
    create_synthetic_dataset(num_samples=1000)