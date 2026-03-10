import pandas as pd
import numpy as np


class NeurologicalDataGenerator:
    def __init__(self, n_samples=2400, random_state=42):
        self.n_samples = n_samples
        np.random.seed(random_state)
        self.conditions = [
            'glioma', 'meningioma', 'pituitary',
            'alzheimer_very_mild', 'alzheimer_mild', 'alzheimer_moderate',
            'multiple_sclerosis', 'normal'
        ]

    def generate_clinical_features(self):
        data = []
        samples_per_class = self.n_samples // len(self.conditions)

        for condition in self.conditions:
            for i in range(samples_per_class):
                record = self._generate_single_record(condition, i)
                data.append(record)

        df = pd.DataFrame(data)
        return df

    def _generate_single_record(self, condition, idx):
        base_record = {
            'patient_id': f'P{len(self.conditions) * idx + self.conditions.index(condition):05d}',
            'age': np.random.randint(20, 85),
            'gender': np.random.choice(['M', 'F']),
        }

        if condition == 'glioma':
            record = {
                **base_record,
                'headache_severity': np.random.choice([3, 4, 5], p=[0.2, 0.4, 0.4]),
                'headache_frequency': np.random.choice(['daily', 'weekly'], p=[0.7, 0.3]),
                'seizures': np.random.choice([0, 1], p=[0.2, 0.8]),
                'vision_problems': np.random.choice([0, 1], p=[0.3, 0.7]),
                'motor_weakness': np.random.choice([0, 1], p=[0.3, 0.7]),
                'speech_difficulty': np.random.choice([0, 1], p=[0.4, 0.6]),
                'nausea': np.random.choice([0, 1], p=[0.3, 0.7]),
                'cognitive_decline': np.random.choice([0, 1], p=[0.4, 0.6]),
                'personality_changes': np.random.choice([0, 1], p=[0.5, 0.5]),
                'balance_issues': np.random.choice([0, 1], p=[0.4, 0.6]),
                'memory_problems': np.random.choice([0, 1], p=[0.5, 0.5]),
                'confusion': np.random.choice([0, 1], p=[0.5, 0.5]),
                'numbness': np.random.choice([0, 1], p=[0.5, 0.5]),
                'tremor': 0,
                'gait_disturbance': np.random.choice([0, 1], p=[0.6, 0.4]),
                'diagnosis': 'glioma'
            }

        elif condition == 'meningioma':
            record = {
                **base_record,
                'headache_severity': np.random.choice([2, 3, 4], p=[0.3, 0.4, 0.3]),
                'headache_frequency': np.random.choice(['daily', 'weekly', 'occasional'], p=[0.4, 0.4, 0.2]),
                'seizures': np.random.choice([0, 1], p=[0.4, 0.6]),
                'vision_problems': np.random.choice([0, 1], p=[0.3, 0.7]),
                'motor_weakness': np.random.choice([0, 1], p=[0.5, 0.5]),
                'speech_difficulty': np.random.choice([0, 1], p=[0.6, 0.4]),
                'nausea': np.random.choice([0, 1], p=[0.5, 0.5]),
                'cognitive_decline': np.random.choice([0, 1], p=[0.6, 0.4]),
                'personality_changes': np.random.choice([0, 1], p=[0.7, 0.3]),
                'balance_issues': np.random.choice([0, 1], p=[0.5, 0.5]),
                'memory_problems': np.random.choice([0, 1], p=[0.6, 0.4]),
                'confusion': np.random.choice([0, 1], p=[0.7, 0.3]),
                'numbness': np.random.choice([0, 1], p=[0.6, 0.4]),
                'tremor': 0,
                'gait_disturbance': np.random.choice([0, 1], p=[0.7, 0.3]),
                'diagnosis': 'meningioma'
            }

        elif condition == 'pituitary':
            record = {
                **base_record,
                'headache_severity': np.random.choice([2, 3, 4], p=[0.3, 0.5, 0.2]),
                'headache_frequency': np.random.choice(['daily', 'weekly'], p=[0.5, 0.5]),
                'seizures': 0,
                'vision_problems': np.random.choice([0, 1], p=[0.1, 0.9]),
                'motor_weakness': np.random.choice([0, 1], p=[0.8, 0.2]),
                'speech_difficulty': np.random.choice([0, 1], p=[0.8, 0.2]),
                'nausea': np.random.choice([0, 1], p=[0.6, 0.4]),
                'cognitive_decline': np.random.choice([0, 1], p=[0.7, 0.3]),
                'personality_changes': np.random.choice([0, 1], p=[0.6, 0.4]),
                'balance_issues': np.random.choice([0, 1], p=[0.7, 0.3]),
                'memory_problems': np.random.choice([0, 1], p=[0.7, 0.3]),
                'confusion': np.random.choice([0, 1], p=[0.8, 0.2]),
                'numbness': np.random.choice([0, 1], p=[0.8, 0.2]),
                'tremor': 0,
                'gait_disturbance': np.random.choice([0, 1], p=[0.8, 0.2]),
                'diagnosis': 'pituitary'
            }

        elif condition == 'alzheimer_very_mild':
            record = {
                **base_record,
                'age': np.random.randint(60, 85),
                'headache_severity': np.random.choice([0, 1, 2], p=[0.5, 0.3, 0.2]),
                'headache_frequency': np.random.choice(['never', 'occasional', 'weekly'], p=[0.5, 0.3, 0.2]),
                'seizures': 0,
                'vision_problems': np.random.choice([0, 1], p=[0.8, 0.2]),
                'motor_weakness': np.random.choice([0, 1], p=[0.9, 0.1]),
                'speech_difficulty': np.random.choice([0, 1], p=[0.7, 0.3]),
                'nausea': np.random.choice([0, 1], p=[0.9, 0.1]),
                'cognitive_decline': np.random.choice([0, 1], p=[0.3, 0.7]),
                'personality_changes': np.random.choice([0, 1], p=[0.6, 0.4]),
                'balance_issues': np.random.choice([0, 1], p=[0.8, 0.2]),
                'memory_problems': np.random.choice([0, 1], p=[0.2, 0.8]),
                'confusion': np.random.choice([0, 1], p=[0.6, 0.4]),
                'numbness': np.random.choice([0, 1], p=[0.9, 0.1]),
                'tremor': 0,
                'gait_disturbance': np.random.choice([0, 1], p=[0.8, 0.2]),
                'diagnosis': 'alzheimer_very_mild'
            }

        elif condition == 'alzheimer_mild':
            record = {
                **base_record,
                'age': np.random.randint(65, 88),
                'headache_severity': np.random.choice([0, 1, 2], p=[0.4, 0.4, 0.2]),
                'headache_frequency': np.random.choice(['never', 'occasional', 'weekly'], p=[0.4, 0.4, 0.2]),
                'seizures': 0,
                'vision_problems': np.random.choice([0, 1], p=[0.7, 0.3]),
                'motor_weakness': np.random.choice([0, 1], p=[0.8, 0.2]),
                'speech_difficulty': np.random.choice([0, 1], p=[0.4, 0.6]),
                'nausea': np.random.choice([0, 1], p=[0.9, 0.1]),
                'cognitive_decline': np.random.choice([0, 1], p=[0.1, 0.9]),
                'personality_changes': np.random.choice([0, 1], p=[0.3, 0.7]),
                'balance_issues': np.random.choice([0, 1], p=[0.6, 0.4]),
                'memory_problems': np.random.choice([0, 1], p=[0.1, 0.9]),
                'confusion': np.random.choice([0, 1], p=[0.3, 0.7]),
                'numbness': np.random.choice([0, 1], p=[0.9, 0.1]),
                'tremor': 0,
                'gait_disturbance': np.random.choice([0, 1], p=[0.6, 0.4]),
                'diagnosis': 'alzheimer_mild'
            }

        elif condition == 'alzheimer_moderate':
            record = {
                **base_record,
                'age': np.random.randint(68, 90),
                'headache_severity': np.random.choice([0, 1, 2], p=[0.5, 0.3, 0.2]),
                'headache_frequency': np.random.choice(['never', 'occasional'], p=[0.6, 0.4]),
                'seizures': 0,
                'vision_problems': np.random.choice([0, 1], p=[0.6, 0.4]),
                'motor_weakness': np.random.choice([0, 1], p=[0.5, 0.5]),
                'speech_difficulty': np.random.choice([0, 1], p=[0.2, 0.8]),
                'nausea': np.random.choice([0, 1], p=[0.9, 0.1]),
                'cognitive_decline': 1,
                'personality_changes': np.random.choice([0, 1], p=[0.1, 0.9]),
                'balance_issues': np.random.choice([0, 1], p=[0.4, 0.6]),
                'memory_problems': 1,
                'confusion': np.random.choice([0, 1], p=[0.1, 0.9]),
                'numbness': np.random.choice([0, 1], p=[0.8, 0.2]),
                'tremor': 0,
                'gait_disturbance': np.random.choice([0, 1], p=[0.3, 0.7]),
                'diagnosis': 'alzheimer_moderate'
            }

        elif condition == 'multiple_sclerosis':
            record = {
                **base_record,
                'age': np.random.randint(20, 60),
                'headache_severity': np.random.choice([1, 2, 3], p=[0.4, 0.4, 0.2]),
                'headache_frequency': np.random.choice(['occasional', 'weekly', 'daily'], p=[0.4, 0.4, 0.2]),
                'seizures': 0,
                'vision_problems': np.random.choice([0, 1], p=[0.2, 0.8]),
                'motor_weakness': np.random.choice([0, 1], p=[0.2, 0.8]),
                'speech_difficulty': np.random.choice([0, 1], p=[0.6, 0.4]),
                'nausea': np.random.choice([0, 1], p=[0.7, 0.3]),
                'cognitive_decline': np.random.choice([0, 1], p=[0.5, 0.5]),
                'personality_changes': np.random.choice([0, 1], p=[0.7, 0.3]),
                'balance_issues': np.random.choice([0, 1], p=[0.3, 0.7]),
                'memory_problems': np.random.choice([0, 1], p=[0.5, 0.5]),
                'confusion': np.random.choice([0, 1], p=[0.7, 0.3]),
                'numbness': np.random.choice([0, 1], p=[0.2, 0.8]),
                'tremor': np.random.choice([0, 1], p=[0.5, 0.5]),
                'gait_disturbance': np.random.choice([0, 1], p=[0.3, 0.7]),
                'diagnosis': 'multiple_sclerosis'
            }

        else:
            record = {
                **base_record,
                'headache_severity': np.random.choice([0, 1, 2], p=[0.6, 0.3, 0.1]),
                'headache_frequency': np.random.choice(['never', 'occasional'], p=[0.7, 0.3]),
                'seizures': 0,
                'vision_problems': np.random.choice([0, 1], p=[0.9, 0.1]),
                'motor_weakness': 0,
                'speech_difficulty': 0,
                'nausea': np.random.choice([0, 1], p=[0.9, 0.1]),
                'cognitive_decline': 0,
                'personality_changes': 0,
                'balance_issues': np.random.choice([0, 1], p=[0.9, 0.1]),
                'memory_problems': 0,
                'confusion': 0,
                'numbness': 0,
                'tremor': 0,
                'gait_disturbance': 0,
                'diagnosis': 'normal'
            }

        return record

    def generate_lab_results(self):
        data = []
        samples_per_class = self.n_samples // len(self.conditions)

        for condition in self.conditions:
            for i in range(samples_per_class):
                record = self._generate_lab_record(condition, i)
                data.append(record)

        df = pd.DataFrame(data)
        return df

    def _generate_lab_record(self, condition, idx):
        base_record = {
            'patient_id': f'P{len(self.conditions) * idx + self.conditions.index(condition):05d}',
            'age': np.random.randint(20, 85),
            'gender': np.random.choice(['M', 'F']),
        }

        if 'glioma' in condition or 'meningioma' in condition or 'pituitary' in condition:
            record = {
                **base_record,
                'S100B': np.random.uniform(0.15, 0.9),
                'GFAP': np.random.uniform(0.8, 4.0),
                'NSE': np.random.uniform(15, 50),
                'WBC': np.random.uniform(4.0, 15.0),
                'RBC': np.random.uniform(3.8, 5.5),
                'Hemoglobin': np.random.uniform(10.5, 16.5),
                'Platelets': np.random.uniform(150, 400),
                'Glucose': np.random.uniform(70, 200),
                'BUN': np.random.uniform(8, 30),
                'Creatinine': np.random.uniform(0.6, 1.8),
                'ALT': np.random.uniform(10, 65),
                'AST': np.random.uniform(10, 65),
                'CRP': np.random.uniform(1.0, 18.0),
                'ESR': np.random.uniform(10, 55),
                'Amyloid_Beta': np.random.uniform(800, 1200),
                'Tau_Protein': np.random.uniform(150, 350),
                'Oligoclonal_Bands': 0,
                'IgG_Index': np.random.uniform(0.3, 0.7),
                'diagnosis': condition
            }

        elif 'alzheimer' in condition:
            severity_map = {'alzheimer_very_mild': 0.3, 'alzheimer_mild': 0.6, 'alzheimer_moderate': 0.9}
            severity = severity_map[condition]

            record = {
                **base_record,
                'age': np.random.randint(60, 90),
                'S100B': np.random.uniform(0.05, 0.15),
                'GFAP': np.random.uniform(0.2, 0.8),
                'NSE': np.random.uniform(5, 15),
                'WBC': np.random.uniform(4.5, 11.0),
                'RBC': np.random.uniform(4.2, 5.5),
                'Hemoglobin': np.random.uniform(12.0, 17.0),
                'Platelets': np.random.uniform(150, 400),
                'Glucose': np.random.uniform(70, 120),
                'BUN': np.random.uniform(7, 25),
                'Creatinine': np.random.uniform(0.7, 1.4),
                'ALT': np.random.uniform(10, 50),
                'AST': np.random.uniform(10, 45),
                'CRP': np.random.uniform(0.5, 5.0),
                'ESR': np.random.uniform(5, 30),
                'Amyloid_Beta': np.random.uniform(200 + severity * 200, 600 - severity * 200),
                'Tau_Protein': np.random.uniform(400 + severity * 300, 900 + severity * 200),
                'Oligoclonal_Bands': 0,
                'IgG_Index': np.random.uniform(0.3, 0.7),
                'diagnosis': condition
            }

        elif condition == 'multiple_sclerosis':
            record = {
                **base_record,
                'age': np.random.randint(20, 60),
                'S100B': np.random.uniform(0.05, 0.20),
                'GFAP': np.random.uniform(0.3, 1.2),
                'NSE': np.random.uniform(8, 18),
                'WBC': np.random.uniform(4.5, 13.0),
                'RBC': np.random.uniform(4.0, 5.5),
                'Hemoglobin': np.random.uniform(11.5, 16.5),
                'Platelets': np.random.uniform(150, 400),
                'Glucose': np.random.uniform(70, 120),
                'BUN': np.random.uniform(7, 25),
                'Creatinine': np.random.uniform(0.7, 1.4),
                'ALT': np.random.uniform(10, 55),
                'AST': np.random.uniform(10, 50),
                'CRP': np.random.uniform(2.0, 15.0),
                'ESR': np.random.uniform(15, 60),
                'Amyloid_Beta': np.random.uniform(800, 1200),
                'Tau_Protein': np.random.uniform(150, 350),
                'Oligoclonal_Bands': 1,
                'IgG_Index': np.random.uniform(0.7, 1.5),
                'diagnosis': condition
            }

        else:
            record = {
                **base_record,
                'S100B': np.random.uniform(0.02, 0.12),
                'GFAP': np.random.uniform(0.1, 0.6),
                'NSE': np.random.uniform(5, 12),
                'WBC': np.random.uniform(4.5, 11.0),
                'RBC': np.random.uniform(4.5, 5.5),
                'Hemoglobin': np.random.uniform(13.5, 17.5),
                'Platelets': np.random.uniform(150, 400),
                'Glucose': np.random.uniform(70, 100),
                'BUN': np.random.uniform(7, 20),
                'Creatinine': np.random.uniform(0.7, 1.3),
                'ALT': np.random.uniform(7, 56),
                'AST': np.random.uniform(10, 40),
                'CRP': np.random.uniform(0.1, 3.0),
                'ESR': np.random.uniform(0, 20),
                'Amyloid_Beta': np.random.uniform(900, 1300),
                'Tau_Protein': np.random.uniform(100, 300),
                'Oligoclonal_Bands': 0,
                'IgG_Index': np.random.uniform(0.3, 0.7),
                'diagnosis': 'normal'
            }

        return record

    def save_to_tsv(self, df, filename):
        df.to_csv(filename, sep='\t', index=False)


if __name__ == "__main__":
    generator = NeurologicalDataGenerator(n_samples=2400, random_state=42)

    clinical_df = generator.generate_clinical_features()
    generator.save_to_tsv(clinical_df, 'neurological_clinical_features.tsv')

    lab_df = generator.generate_lab_results()
    generator.save_to_tsv(lab_df, 'neurological_lab_results.tsv')

    print(f"Clinical features dataset: {len(clinical_df)} records")
    print(f"Lab results dataset: {len(lab_df)} records")
    print("\nClass distribution:")
    print(clinical_df['diagnosis'].value_counts())