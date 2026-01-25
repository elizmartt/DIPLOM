"""
Create synthetic laboratory results dataset for lung cancer prediction
Features: Tumor markers, blood work, chemistry panel
"""

import numpy as np
import pandas as pd
from pathlib import Path


def create_lab_results_dataset(n_samples=2000):
    """Create synthetic lab results data"""

    np.random.seed(42)

    print("=" * 60)
    print("CREATING LABORATORY RESULTS DATASET")
    print("=" * 60)

    data = []

    for i in range(n_samples):
        # Determine if patient has lung cancer (50/50 split)
        has_cancer = i % 2

        # TUMOR MARKERS (elevated in cancer patients)
        if has_cancer:
            # CEA: Normal <3 ng/mL, elevated 5-50+ in cancer
            cea = np.random.normal(25, 15)
            cea = max(3, min(100, cea))

            # NSE: Normal <12.5 ng/mL, elevated 15-100+ in SCLC
            nse = np.random.normal(40, 20)
            nse = max(10, min(150, nse))

            # CYFRA 21-1: Normal <3.3 ng/mL, elevated 5-50+ in NSCLC
            cyfra = np.random.normal(18, 10)
            cyfra = max(2, min(80, cyfra))

            # SCC: Normal <1.5 ng/mL, elevated 2-20+ in squamous cell
            scc = np.random.normal(8, 5)
            scc = max(0.5, min(30, scc))

            # ProGRP: Normal <50 pg/mL, elevated 100-5000+ in SCLC
            progrp = np.random.normal(300, 150)
            progrp = max(30, min(1000, progrp))

        else:
            # Normal ranges for non-cancer patients
            cea = np.random.normal(1.5, 0.8)
            cea = max(0.1, min(4, cea))

            nse = np.random.normal(8, 2)
            nse = max(3, min(15, nse))

            cyfra = np.random.normal(1.8, 0.8)
            cyfra = max(0.5, min(4, cyfra))

            scc = np.random.normal(0.8, 0.4)
            scc = max(0.1, min(2, scc))

            progrp = np.random.normal(30, 10)
            progrp = max(10, min(60, progrp))

        # COMPLETE BLOOD COUNT
        if has_cancer:
            # Anemia common in cancer
            wbc = np.random.normal(9.5, 3)  # Normal: 4-11 × 10^9/L
            wbc = max(3, min(25, wbc))

            hemoglobin = np.random.normal(11.5, 2)  # Low in cancer (anemia)
            hemoglobin = max(7, min(14, hemoglobin))

            platelets = np.random.normal(280, 100)  # Normal: 150-400 × 10^9/L
            platelets = max(100, min(600, platelets))

        else:
            wbc = np.random.normal(7, 2)
            wbc = max(4, min(11, wbc))

            hemoglobin = np.random.normal(14, 1.5)
            hemoglobin = max(12, min(17, hemoglobin))

            platelets = np.random.normal(250, 50)
            platelets = max(150, min(400, platelets))

        # CHEMISTRY PANEL
        if has_cancer:
            # LDH: Elevated in cancer (normal: 140-280 U/L)
            ldh = np.random.normal(450, 150)
            ldh = max(200, min(1000, ldh))

            # Albumin: Low in cancer/malnutrition (normal: 3.5-5.5 g/dL)
            albumin = np.random.normal(3.2, 0.6)
            albumin = max(2, min(4, albumin))

            # Alkaline Phosphatase: Elevated if bone/liver mets (normal: 44-147 U/L)
            alp = np.random.normal(180, 80)
            alp = max(50, min(500, alp))

            # Calcium: Can be elevated (paraneoplastic) (normal: 8.5-10.5 mg/dL)
            calcium = np.random.normal(10.8, 1.2)
            calcium = max(8, min(14, calcium))

            # CRP: Elevated inflammation (normal: <3 mg/L)
            crp = np.random.normal(25, 15)
            crp = max(3, min(100, crp))

        else:
            ldh = np.random.normal(200, 40)
            ldh = max(140, min(280, ldh))

            albumin = np.random.normal(4.2, 0.5)
            albumin = max(3.5, min(5.5, albumin))

            alp = np.random.normal(90, 30)
            alp = max(44, min(147, alp))

            calcium = np.random.normal(9.5, 0.5)
            calcium = max(8.5, min(10.5, calcium))

            crp = np.random.normal(1.5, 1)
            crp = max(0.1, min(5, crp))

        # ADDITIONAL MARKERS
        if has_cancer:
            # ESR: Elevated (normal: <20 mm/hr)
            esr = np.random.normal(45, 20)
            esr = max(15, min(120, esr))

            # Ferritin: Can be elevated (normal: 12-300 ng/mL)
            ferritin = np.random.normal(350, 150)
            ferritin = max(50, min(1000, ferritin))

        else:
            esr = np.random.normal(12, 5)
            esr = max(1, min(25, esr))

            ferritin = np.random.normal(100, 80)
            ferritin = max(12, min(300, ferritin))

        # Patient demographics (for context)
        age = np.random.normal(65, 10) if has_cancer else np.random.normal(50, 12)
        age = max(20, min(90, age))

        gender = np.random.choice([0, 1])  # 0=Female, 1=Male

        # Create record
        record = {
            'patient_id': f'P{i:05d}',
            'age': round(age, 1),
            'gender': gender,

            # Tumor Markers
            'cea': round(cea, 2),
            'nse': round(nse, 2),
            'cyfra_21_1': round(cyfra, 2),
            'scc': round(scc, 2),
            'progrp': round(progrp, 2),

            # Complete Blood Count
            'wbc': round(wbc, 2),
            'hemoglobin': round(hemoglobin, 2),
            'platelets': round(platelets, 1),

            # Chemistry Panel
            'ldh': round(ldh, 1),
            'albumin': round(albumin, 2),
            'alp': round(alp, 1),
            'calcium': round(calcium, 2),
            'crp': round(crp, 2),

            # Additional Markers
            'esr': round(esr, 1),
            'ferritin': round(ferritin, 1),

            # Target
            'diagnosis': has_cancer  # 0 = No Cancer, 1 = Lung Cancer
        }

        data.append(record)

    # Create DataFrame
    df = pd.DataFrame(data)

    # Save to CSV
    output_dir = Path(__file__).parent / 'data'
    output_dir.mkdir(exist_ok=True)

    output_file = output_dir / 'lab_results.csv'
    df.to_csv(output_file, index=False)

    print(f"\n✅ Created {n_samples} patient lab records")
    print(f"✅ Saved to: {output_file}")

    # Display statistics
    print("\n" + "=" * 60)
    print("DATASET STATISTICS")
    print("=" * 60)

    print(f"\nDiagnosis Distribution:")
    print(df['diagnosis'].value_counts())
    print(f"\n{df['diagnosis'].value_counts(normalize=True) * 100}")

    print(f"\nTumor Marker Statistics (by diagnosis):")
    print(df.groupby('diagnosis')[['cea', 'nse', 'cyfra_21_1']].describe())

    print(f"\nBlood Work Statistics (by diagnosis):")
    print(df.groupby('diagnosis')[['wbc', 'hemoglobin', 'platelets']].describe())

    print("\n" + "=" * 60)
    print("FEATURE INFORMATION")
    print("=" * 60)
    print(f"\nTotal Features: {len(df.columns) - 2}")  # Exclude patient_id and diagnosis
    print(f"\nFeature Categories:")
    print(f"  • Demographics: age, gender")
    print(f"  • Tumor Markers: CEA, NSE, CYFRA 21-1, SCC, ProGRP")
    print(f"  • Blood Count: WBC, Hemoglobin, Platelets")
    print(f"  • Chemistry: LDH, Albumin, ALP, Calcium, CRP")
    print(f"  • Inflammation: ESR, Ferritin")

    return output_file


if __name__ == "__main__":
    create_lab_results_dataset(n_samples=2000)