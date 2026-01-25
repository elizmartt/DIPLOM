"""
Create synthetic clinical symptoms dataset for lung cancer prediction
Features: Demographics, smoking history, symptoms, risk factors
"""

import numpy as np
import pandas as pd
from pathlib import Path


def create_symptoms_dataset(n_samples=2000):
    """Create synthetic patient symptom data"""

    np.random.seed(42)

    print("=" * 60)
    print("CREATING CLINICAL SYMPTOMS DATASET")
    print("=" * 60)

    data = []

    for i in range(n_samples):
        # Determine if patient has lung cancer (50/50 split)
        has_cancer = i % 2

        # Demographics
        if has_cancer:
            # Cancer patients: older, more likely male, smokers
            age = np.random.normal(65, 10)
            gender = np.random.choice([0, 1], p=[0.4, 0.6])  # 0=Female, 1=Male
            smoking_status = np.random.choice([0, 1, 2], p=[0.1, 0.2, 0.7])  # 0=Never, 1=Former, 2=Current
            pack_years = np.random.normal(30, 15) if smoking_status > 0 else 0
        else:
            # Non-cancer: younger, balanced gender, fewer smokers
            age = np.random.normal(50, 12)
            gender = np.random.choice([0, 1], p=[0.5, 0.5])
            smoking_status = np.random.choice([0, 1, 2], p=[0.6, 0.3, 0.1])
            pack_years = np.random.normal(10, 8) if smoking_status > 0 else 0

        # Symptoms (more prevalent in cancer patients)
        if has_cancer:
            persistent_cough = np.random.choice([0, 1], p=[0.2, 0.8])
            coughing_blood = np.random.choice([0, 1], p=[0.6, 0.4])
            chest_pain = np.random.choice([0, 1], p=[0.3, 0.7])
            shortness_breath = np.random.choice([0, 1], p=[0.2, 0.8])
            wheezing = np.random.choice([0, 1], p=[0.4, 0.6])
            hoarseness = np.random.choice([0, 1], p=[0.5, 0.5])
            weight_loss = np.random.choice([0, 1], p=[0.3, 0.7])
            bone_pain = np.random.choice([0, 1], p=[0.7, 0.3])
            fatigue = np.random.choice([0, 1], p=[0.2, 0.8])
        else:
            persistent_cough = np.random.choice([0, 1], p=[0.8, 0.2])
            coughing_blood = np.random.choice([0, 1], p=[0.95, 0.05])
            chest_pain = np.random.choice([0, 1], p=[0.7, 0.3])
            shortness_breath = np.random.choice([0, 1], p=[0.7, 0.3])
            wheezing = np.random.choice([0, 1], p=[0.8, 0.2])
            hoarseness = np.random.choice([0, 1], p=[0.85, 0.15])
            weight_loss = np.random.choice([0, 1], p=[0.8, 0.2])
            bone_pain = np.random.choice([0, 1], p=[0.9, 0.1])
            fatigue = np.random.choice([0, 1], p=[0.6, 0.4])

        # Risk factors
        family_history = np.random.choice([0, 1], p=[0.85, 0.15])

        if has_cancer:
            copd = np.random.choice([0, 1], p=[0.5, 0.5])
            asbestos_exposure = np.random.choice([0, 1], p=[0.8, 0.2])
        else:
            copd = np.random.choice([0, 1], p=[0.9, 0.1])
            asbestos_exposure = np.random.choice([0, 1], p=[0.95, 0.05])

        # Ensure age and pack_years are within reasonable bounds
        age = max(20, min(90, age))
        pack_years = max(0, pack_years)

        # Create record
        record = {
            'patient_id': f'P{i:05d}',
            'age': round(age, 1),
            'gender': gender,
            'smoking_status': smoking_status,
            'pack_years': round(pack_years, 1),
            'persistent_cough': persistent_cough,
            'coughing_blood': coughing_blood,
            'chest_pain': chest_pain,
            'shortness_of_breath': shortness_breath,
            'wheezing': wheezing,
            'hoarseness': hoarseness,
            'weight_loss': weight_loss,
            'bone_pain': bone_pain,
            'fatigue': fatigue,
            'family_history': family_history,
            'copd': copd,
            'asbestos_exposure': asbestos_exposure,
            'diagnosis': has_cancer  # 0 = No Cancer, 1 = Lung Cancer
        }

        data.append(record)

    # Create DataFrame
    df = pd.DataFrame(data)

    # Save to CSV
    output_dir = Path(__file__).parent / 'data'
    output_dir.mkdir(exist_ok=True)

    output_file = output_dir / 'clinical_symptoms.csv'
    df.to_csv(output_file, index=False)

    print(f"\n✅ Created {n_samples} patient records")
    print(f"✅ Saved to: {output_file}")

    # Display statistics
    print("\n" + "=" * 60)
    print("DATASET STATISTICS")
    print("=" * 60)

    print(f"\nDiagnosis Distribution:")
    print(df['diagnosis'].value_counts())
    print(f"\n{df['diagnosis'].value_counts(normalize=True) * 100}")

    print(f"\nAge Statistics:")
    print(df.groupby('diagnosis')['age'].describe())

    print(f"\nSmoking Status Distribution:")
    print(pd.crosstab(df['diagnosis'], df['smoking_status'],
                      rownames=['Diagnosis'], colnames=['Smoking Status']))

    print(f"\nTop Symptoms in Cancer Patients:")
    cancer_df = df[df['diagnosis'] == 1]
    symptom_cols = ['persistent_cough', 'coughing_blood', 'chest_pain',
                    'shortness_of_breath', 'wheezing', 'hoarseness',
                    'weight_loss', 'bone_pain', 'fatigue']
    symptom_prevalence = cancer_df[symptom_cols].sum().sort_values(ascending=False)
    print(symptom_prevalence)

    print("\n" + "=" * 60)
    print("FEATURE INFORMATION")
    print("=" * 60)
    print(f"\nTotal Features: {len(df.columns) - 2}")  # Exclude patient_id and diagnosis
    print(f"Numerical Features: age, pack_years")
    print(f"Categorical Features: gender, smoking_status")
    print(f"Binary Features: {len(symptom_cols)} symptoms + 3 risk factors")

    return output_file


if __name__ == "__main__":
    create_symptoms_dataset(n_samples=2000)