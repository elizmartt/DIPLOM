import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.linear_model import LogisticRegression
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import classification_report, confusion_matrix, accuracy_score, roc_auc_score
import joblib


class ClinicalFeaturesClassifier:
    def __init__(self):
        self.model = LogisticRegression(
            max_iter=2000,
            random_state=42,
            solver='lbfgs'
        )
        self.scaler = StandardScaler()
        self.feature_names = None
        self.label_map = None

    def load_data_from_tsv(self, filepath):
        print(f"Loading data from {filepath}...")
        df = pd.read_csv(filepath, sep='\t')

        print(f"Loaded {len(df)} records")
        print(f"Columns: {df.columns.tolist()}")

        return df

    def preprocess_data(self, df, target_column='diagnosis'):
        if target_column not in df.columns:
            available_cols = df.columns.tolist()
            raise ValueError(f"Target column '{target_column}' not found. Available: {available_cols}")

        columns_to_drop = [target_column]
        if 'patient_id' in df.columns:
            columns_to_drop.append('patient_id')

        X = df.drop(columns=columns_to_drop)
        y = df[target_column]

        print("\nHandling missing values...")
        for col in X.columns:
            if X[col].dtype in ['float64', 'int64']:
                if X[col].isnull().sum() > 0:
                    X[col].fillna(X[col].median(), inplace=True)
            else:
                if X[col].isnull().sum() > 0:
                    X[col].fillna(X[col].mode()[0], inplace=True)

        categorical_cols = X.select_dtypes(include=['object']).columns
        if len(categorical_cols) > 0:
            print(f"Encoding categorical columns: {categorical_cols.tolist()}")
            X = pd.get_dummies(X, columns=categorical_cols, drop_first=True)

        self.feature_names = X.columns.tolist()

        unique_labels = sorted(y.unique())
        self.label_map = {i: label for i, label in enumerate(unique_labels)}

        if y.dtype == 'object':
            label_to_idx = {label: i for i, label in enumerate(unique_labels)}
            y = y.map(label_to_idx)

        print(f"\nPreprocessing complete:")
        print(f"Features: {len(self.feature_names)}")
        print(f"Classes: {len(self.label_map)}")
        print(f"Label mapping: {self.label_map}")
        print(f"\nClass distribution:")
        print(y.value_counts())

        return X, y

    def prepare_data(self, df, target_column='diagnosis', test_size=0.2):
        X, y = self.preprocess_data(df, target_column)

        X_train, X_test, y_train, y_test = train_test_split(
            X, y, test_size=test_size, stratify=y, random_state=42
        )

        X_train_scaled = self.scaler.fit_transform(X_train)
        X_test_scaled = self.scaler.transform(X_test)

        print(f"\nData split:")
        print(f"Training set: {len(X_train)} samples")
        print(f"Test set: {len(X_test)} samples")

        return X_train_scaled, X_test_scaled, y_train, y_test

    def train(self, X_train, y_train):
        print("\nTraining Logistic Regression model...")

        self.model.fit(X_train, y_train)

        cv_scores = cross_val_score(self.model, X_train, y_train, cv=5)

        print(f"Training complete!")
        print(f"Cross-validation scores: {cv_scores}")
        print(f"Mean CV accuracy: {cv_scores.mean():.4f} (+/- {cv_scores.std() * 2:.4f})")

        return cv_scores

    def evaluate(self, X_test, y_test):
        y_pred = self.model.predict(X_test)
        y_pred_proba = self.model.predict_proba(X_test)

        accuracy = accuracy_score(y_test, y_pred)
        conf_matrix = confusion_matrix(y_test, y_pred)

        try:
            if len(self.label_map) == 2:
                roc_auc = roc_auc_score(y_test, y_pred_proba[:, 1])
            else:
                roc_auc = roc_auc_score(y_test, y_pred_proba, multi_class='ovr')
        except:
            roc_auc = None

        target_names = [self.label_map[i] for i in sorted(self.label_map.keys())]
        class_report = classification_report(y_test, y_pred, target_names=target_names)

        print("\n" + "=" * 60)
        print("MODULE 2 - CLINICAL FEATURES ANALYSIS RESULTS")
        print("=" * 60)
        print(f"\nOverall Accuracy: {accuracy * 100:.2f}%")
        if roc_auc:
            print(f"ROC AUC Score: {roc_auc:.4f}")
        print(f"\nConfusion Matrix:\n{conf_matrix}")
        print(f"\nClassification Report:\n{class_report}")

        return {
            'accuracy': accuracy,
            'roc_auc': roc_auc,
            'confusion_matrix': conf_matrix,
            'classification_report': class_report
        }

    def get_feature_importance(self, top_n=15):
        if len(self.label_map) == 2:
            coefficients = self.model.coef_[0]
            feature_importance = pd.DataFrame({
                'feature': self.feature_names,
                'coefficient': coefficients,
                'abs_coefficient': np.abs(coefficients)
            }).sort_values('abs_coefficient', ascending=False)
        else:
            coefficients = np.abs(self.model.coef_).mean(axis=0)
            feature_importance = pd.DataFrame({
                'feature': self.feature_names,
                'avg_coefficient': coefficients
            }).sort_values('avg_coefficient', ascending=False)

        print(f"\nTop {top_n} Most Important Features:")
        print(feature_importance.head(top_n))

        return feature_importance

    def save_model(self, model_path='neurological_clinical_lr.pkl',
                   scaler_path='neurological_clinical_scaler.pkl'):
        joblib.dump(self.model, model_path)
        joblib.dump(self.scaler, scaler_path)
        joblib.dump({
            'feature_names': self.feature_names,
            'label_map': self.label_map
        }, 'neurological_clinical_metadata.pkl')

        print(f"\nModel saved to {model_path}")
        print(f"Scaler saved to {scaler_path}")

    def load_model(self, model_path='neurological_clinical_lr.pkl',
                   scaler_path='neurological_clinical_scaler.pkl'):
        self.model = joblib.load(model_path)
        self.scaler = joblib.load(scaler_path)
        metadata = joblib.load('neurological_clinical_metadata.pkl')
        self.feature_names = metadata['feature_names']
        self.label_map = metadata['label_map']

        print(f"Model loaded from {model_path}")


if __name__ == "__main__":
    classifier = ClinicalFeaturesClassifier()

    tsv_file = "neurological_clinical_features.tsv"
    df = classifier.load_data_from_tsv(tsv_file)

    X_train, X_test, y_train, y_test = classifier.prepare_data(
        df, target_column='diagnosis', test_size=0.2
    )

    cv_scores = classifier.train(X_train, y_train)

    results = classifier.evaluate(X_test, y_test)

    feature_importance = classifier.get_feature_importance(top_n=15)

    classifier.save_model()