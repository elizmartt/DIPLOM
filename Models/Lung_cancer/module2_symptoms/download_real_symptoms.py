"""
Download real lung cancer symptoms dataset from Kaggle
Dataset: mysarahmadbhat/lung-cancer
"""

import os
import zipfile
from pathlib import Path
import subprocess


def check_kaggle_setup():
    """Check if kaggle.json exists"""
    kaggle_path = Path.home() / '.kaggle' / 'kaggle.json'

    if not kaggle_path.exists():
        print("=" * 60)
        print("❌ KAGGLE API NOT CONFIGURED")
        print("=" * 60)
        print("\nPlease follow these steps:")
        print("1. Go to https://www.kaggle.com/account")
        print("2. Scroll to 'API' section")
        print("3. Click 'Create New Token'")
        print("4. Save kaggle.json to: C:\\Users\\Eliza\\.kaggle\\")
        print("\nDO NOT share your API token publicly!")
        return False

    print("✅ Kaggle API configured")
    return True


def download_dataset():
    """Download lung cancer symptoms dataset"""

    print("\n" + "=" * 60)
    print("DOWNLOADING REAL LUNG CANCER SYMPTOMS DATASET")
    print("=" * 60)

    # Create data directory
    data_dir = Path(__file__).parent / 'data'
    data_dir.mkdir(exist_ok=True)

    raw_dir = data_dir / 'raw_symptoms'
    raw_dir.mkdir(exist_ok=True)

    print(f"\n📁 Download directory: {raw_dir}")

    # Download using kaggle CLI
    dataset_name = 'mysarahmadbhat/lung-cancer'

    print(f"\n🔄 Downloading: {dataset_name}")
    print("This may take a minute...")

    try:
        result = subprocess.run(
            ['kaggle', 'datasets', 'download', '-d', dataset_name, '-p', str(raw_dir)],
            capture_output=True,
            text=True,
            check=True
        )

        print("✅ Download complete!")

        # Find and extract zip file
        zip_files = list(raw_dir.glob('*.zip'))

        if zip_files:
            zip_file = zip_files[0]
            print(f"\n📦 Extracting: {zip_file.name}")

            with zipfile.ZipFile(zip_file, 'r') as zip_ref:
                zip_ref.extractall(raw_dir)

            print("✅ Extraction complete!")

            # Remove zip file
            zip_file.unlink()

            # List extracted files
            print("\n📄 Extracted files:")
            for file in raw_dir.glob('*'):
                print(f"  - {file.name}")

        return True

    except subprocess.CalledProcessError as e:
        print(f"\n❌ Error downloading dataset: {e}")
        print(f"STDOUT: {e.stdout}")
        print(f"STDERR: {e.stderr}")
        return False
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        return False


def main():
    print("=" * 60)
    print("REAL LUNG CANCER SYMPTOMS DATASET DOWNLOADER")
    print("=" * 60)

    # Check Kaggle setup
    if not check_kaggle_setup():
        return

    # Download dataset
    if download_dataset():
        print("\n" + "=" * 60)
        print("✅ DOWNLOAD SUCCESSFUL!")
        print("=" * 60)
        print("\nNext step: Run train_real_symptoms.py")
    else:
        print("\n" + "=" * 60)
        print("❌ DOWNLOAD FAILED")
        print("=" * 60)


if __name__ == "__main__":
    main()