"""
Download and process real lung cancer tumor marker dataset
Source: PMC6311751 - NSCLC Tumor Markers Study
"""

import pandas as pd
import urllib.request
from pathlib import Path


def download_tumor_marker_data():
    """Download real tumor marker dataset from PMC"""

    print("=" * 60)
    print("DOWNLOADING REAL TUMOR MARKER DATASET")
    print("=" * 60)

    # Dataset URL
    url = "https://www.ncbi.nlm.nih.gov/pmc/articles/PMC6311751/bin/9845123.f1.xlsx"

    # Create data directory
    data_dir = Path(__file__).parent / 'data'
    data_dir.mkdir(exist_ok=True)

    raw_dir = data_dir / 'raw_lab'
    raw_dir.mkdir(exist_ok=True)

    output_file = raw_dir / 'tumor_markers.xlsx'

    print(f"\n📥 Downloading from: {url}")
    print(f"📁 Saving to: {output_file}")

    try:
        # Download file
        urllib.request.urlretrieve(url, output_file)
        print("\n✅ Download complete!")

        # Load and inspect the data
        print("\n" + "=" * 60)
        print("INSPECTING DATASET")
        print("=" * 60)

        # Load Excel file (has two sheets)
        xlsx = pd.ExcelFile(output_file)

        print(f"\n📊 Excel sheets found: {xlsx.sheet_names}")

        # Sheet 1: NSCLC patients
        if len(xlsx.sheet_names) > 0:
            df_cancer = pd.read_excel(output_file, sheet_name=0)
            print(f"\n✅ Sheet 1 (Cancer patients): {len(df_cancer)} records")
            print(f"   Columns: {list(df_cancer.columns)}")
            print(f"\n   Preview:")
            print(df_cancer.head())

        # Sheet 2: Benign chest disease
        if len(xlsx.sheet_names) > 1:
            df_benign = pd.read_excel(output_file, sheet_name=1)
            print(f"\n✅ Sheet 2 (Benign disease): {len(df_benign)} records")
            print(f"   Columns: {list(df_benign.columns)}")

        return True

    except Exception as e:
        print(f"\n❌ Error downloading dataset: {e}")
        return False


def main():
    print("=" * 60)
    print("REAL LUNG CANCER LAB DATA DOWNLOADER")
    print("=" * 60)
    print("\nDataset: NSCLC Tumor Markers (PMC6311751)")
    print("Source: Published research paper")
    print("Patients: 693 total (590 cancer + 103 benign)")
    print("Features: 7 tumor markers + demographics")

    if download_tumor_marker_data():
        print("\n" + "=" * 60)
        print("✅ DOWNLOAD SUCCESSFUL!")
        print("=" * 60)
        print("\nNext step: Run train_real_lab_data.py")
    else:
        print("\n" + "=" * 60)
        print("❌ DOWNLOAD FAILED")
        print("=" * 60)


if __name__ == "__main__":
    main()