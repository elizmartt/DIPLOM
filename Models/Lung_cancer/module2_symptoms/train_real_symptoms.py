"""
Train Logistic Regression on real lung cancer symptoms data
"""

import numpy as np
import pandas as pd
from pathlib import Path
import pickle
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.linear_model import LogisticRegression
from sklearn.preprocessing import StandardScaler, LabelEncoder
from sklearn.metrics import (
    accuracy_score, precision_score, recall_score, f1_score,
    confusion_matrix, classification_report, roc_curve, roc_auc_score
)


def load_real_data():
    """Load and prepare real lung cancer symptoms data"""

    print("=" * 60)
    print("LOADING REAL LUNG CANCER SYMPTOMS DATA")
    print("=" * 60)

    # Find CSV file in raw_symptoms directory
    data_dir = Path(__file__).parent / 'data' / 'raw_symptoms'

    csv_files = list(data_dir.glob('*.csv'))

    if not csv_files:
        print(f"\n❌ No CSV files found in {data_dir}")
        print("Please run: python module2_symptoms/download_real_symptoms.py")
        return None, None, None

    csv_file = csv_files[0]
    print(f"\n📂 Loading: {csv_file.name}")

    df = pd.read_csv(csv_file)

    print(f"✅ Loaded {len(df)} patient records")
    print(f"✅ Columns: {len(df.columns)}")

    print("\n📊 Dataset Structure:")
    print(df.head())

    print("\n📋 Column Names:")
    for i, col in enumerate(df.columns, 1):
        print(f"  {i}. {col}")

    # Data preprocessing
    print("\n" + "=" * 60)
    print("PREPROCESSING DATA")
    print("=" * 60)

    # Convert column names to lowercase for consistency
    df.columns = df.columns.str.lower().str.strip()

    # Identify target column (should be 'lung_cancer' or similar)
    target_col = None
    for col in ['lung_cancer', 'lungcancer', 'cancer', 'result', 'diagnosis']:
        if col in df.columns:
            target_col = col
            break

    if target_col is None:
        print("❌ Could not find target column!")
        print(f"Available columns: {df.columns.tolist()}")
        # Try to guess the last column
        target_col = df.columns[-1]
        print(f"Using last column as target: {target_col}")

    print(f"\n✅ Target column: {target_col}")

    # Separate features and target
    X = df.drop(target_col, axis=1)
    y = df[target_col]

    # Handle non-numeric features
    print("\n🔄 Encoding categorical features...")

    label_encoders = {}
    for col in X.columns:
        if X[col].dtype == 'object' or X[col].dtype.name == 'category':
            print(f"  Encoding: {col}")
            le = LabelEncoder()
            X[col] = le.fit_transform(X[col].astype(str))
            label_encoders[col] = le

    # Encode target if needed
    if y.dtype == 'object' or y.dtype.name == 'category':
        print(f"\n🔄 Encoding target: {target_col}")
        y = LabelEncoder().fit_transform(y)

    # Handle any YES/NO values (convert to 1/0)
    for col in X.columns:
        if X[col].dtype in ['int64', 'float64']:
            unique_vals = X[col].unique()
            # If values are 1 and 2, convert to 0 and 1
            if set(unique_vals).issubset({1, 2}):
                X[col] = X[col] - 1
                print(f"  Converted {col}: [1,2] → [0,1]")

    # Check target distribution
    print("\n📊 Target Distribution:")
    if hasattr(y, 'value_counts'):
        print(y.value_counts())
        print(f"\n{y.value_counts(normalize=True) * 100}")
    else:
        unique, counts = np.unique(y, return_counts=True)
        for val, count in zip(unique, counts):
            print(f"  Class {val}: {count} ({count / len(y) * 100:.1f}%)")

    feature_names = X.columns.tolist()

    print(f"\n✅ Features prepared: {len(feature_names)}")
    print(f"✅ Target prepared: {len(y)} samples")

    return X, y, feature_names


def train_model(X, y, feature_names):
    """Train Logistic Regression model"""

    print("\n" + "=" * 60)
    print("TRAINING LOGISTIC REGRESSION ON REAL DATA")
    print("=" * 60)

    # Split data
    X_train, X_temp, y_train, y_temp = train_test_split(
        X, y, test_size=0.3, random_state=42, stratify=y
    )
    X_val, X_test, y_val, y_test = train_test_split(
        X_temp, y_temp, test_size=0.5, random_state=42, stratify=y_temp
    )

    print(f"\nData Split:")
    print(f"  Training:   {len(X_train)} samples")
    print(f"  Validation: {len(X_val)} samples")
    print(f"  Test:       {len(X_test)} samples")

    # Scale features
    scaler = StandardScaler()
    X_train_scaled = scaler.fit_transform(X_train)
    X_val_scaled = scaler.transform(X_val)
    X_test_scaled = scaler.transform(X_test)

    print("\n✅ Features scaled (StandardScaler)")

    # Train Logistic Regression
    print("\n🔄 Training Logistic Regression...")

    model = LogisticRegression(
        max_iter=1000,
        random_state=42,
        solver='lbfgs',
        C=1.0
    )

    model.fit(X_train_scaled, y_train)

    print("✅ Training complete!")

    # Predictions
    y_train_pred = model.predict(X_train_scaled)
    y_val_pred = model.predict(X_val_scaled)
    y_test_pred = model.predict(X_test_scaled)

    # Probabilities
    y_test_proba = model.predict_proba(X_test_scaled)[:, 1]

    # Calculate metrics
    train_acc = accuracy_score(y_train, y_train_pred)
    val_acc = accuracy_score(y_val, y_val_pred)
    test_acc = accuracy_score(y_test, y_test_pred)

    print("\n" + "=" * 60)
    print("MODEL PERFORMANCE")
    print("=" * 60)

    print(f"\nAccuracy:")
    print(f"  Training:   {train_acc:.4f} ({train_acc * 100:.2f}%)")
    print(f"  Validation: {val_acc:.4f} ({val_acc * 100:.2f}%)")
    print(f"  Test:       {test_acc:.4f} ({test_acc * 100:.2f}%)")

    # Detailed test metrics
    precision = precision_score(y_test, y_test_pred)
    recall = recall_score(y_test, y_test_pred)
    f1 = f1_score(y_test, y_test_pred)
    auc = roc_auc_score(y_test, y_test_proba)

    print(f"\nDetailed Test Metrics:")
    print(f"  Precision: {precision:.4f}")
    print(f"  Recall:    {recall:.4f}")
    print(f"  F1-Score:  {f1:.4f}")
    print(f"  AUC-ROC:   {auc:.4f}")

    # Classification report
    print("\n" + "=" * 60)
    print("CLASSIFICATION REPORT")
    print("=" * 60)
    print(classification_report(y_test, y_test_pred,
                                target_names=['No Cancer', 'Lung Cancer']))

    # Feature importance
    print("\n" + "=" * 60)
    print("FEATURE IMPORTANCE (Top 10)")
    print("=" * 60)

    feature_importance = pd.DataFrame({
        'feature': feature_names,
        'coefficient': model.coef_[0]
    }).sort_values('coefficient', key=abs, ascending=False)

    print("\nTop 10 Most Important Features:")
    print(feature_importance.head(10).to_string(index=False))

    # Save model
    model_dir = Path(__file__).parent / 'models'
    model_dir.mkdir(exist_ok=True)

    model_path = model_dir / 'logistic_regression_real.pkl'
    scaler_path = model_dir / 'scaler_real.pkl'

    with open(model_path, 'wb') as f:
        pickle.dump(model, f)

    with open(scaler_path, 'wb') as f:
        pickle.dump(scaler, f)

    print(f"\n✅ Model saved to: {model_path}")
    print(f"✅ Scaler saved to: {scaler_path}")

    # Visualizations
    generate_visualizations(y_test, y_test_pred, y_test_proba,
                            feature_importance, model_dir)

    return model, scaler, {
        'test_accuracy': test_acc,
        'test_precision': precision,
        'test_recall': recall,
        'test_f1': f1,
        'test_auc': auc
    }


def generate_visualizations(y_test, y_test_pred, y_test_proba,
                            feature_importance, save_dir):
    """Generate visualization plots"""

    print("\n" + "=" * 60)
    print("GENERATING VISUALIZATIONS")
    print("=" * 60)

    fig, axes = plt.subplots(2, 2, figsize=(14, 12))

    # Confusion Matrix
    cm = confusion_matrix(y_test, y_test_pred)
    sns.heatmap(cm, annot=True, fmt='d', cmap='Blues', ax=axes[0, 0],
                xticklabels=['No Cancer', 'Lung Cancer'],
                yticklabels=['No Cancer', 'Lung Cancer'])
    axes[0, 0].set_title('Confusion Matrix (Real Data)', fontsize=14, fontweight='bold')
    axes[0, 0].set_ylabel('True Label')
    axes[0, 0].set_xlabel('Predicted Label')

    # ROC Curve
    fpr, tpr, _ = roc_curve(y_test, y_test_proba)
    auc = roc_auc_score(y_test, y_test_proba)

    axes[0, 1].plot(fpr, tpr, linewidth=2, label=f'ROC Curve (AUC = {auc:.3f})')
    axes[0, 1].plot([0, 1], [0, 1], 'k--', linewidth=1, label='Random Classifier')
    axes[0, 1].set_xlim([0.0, 1.0])
    axes[0, 1].set_ylim([0.0, 1.05])
    axes[0, 1].set_xlabel('False Positive Rate')
    axes[0, 1].set_ylabel('True Positive Rate')
    axes[0, 1].set_title('ROC Curve (Real Data)', fontsize=14, fontweight='bold')
    axes[0, 1].legend(loc="lower right")
    axes[0, 1].grid(True, alpha=0.3)

    # Feature Importance
    top_features = feature_importance.head(10)
    colors = ['red' if x < 0 else 'green' for x in top_features['coefficient']]

    axes[1, 0].barh(range(len(top_features)), top_features['coefficient'], color=colors)
    axes[1, 0].set_yticks(range(len(top_features)))
    axes[1, 0].set_yticklabels(top_features['feature'])
    axes[1, 0].set_xlabel('Coefficient Value')
    axes[1, 0].set_title('Top 10 Feature Coefficients', fontsize=14, fontweight='bold')
    axes[1, 0].axvline(x=0, color='black', linestyle='--', linewidth=1)
    axes[1, 0].grid(True, alpha=0.3, axis='x')

    # Prediction Distribution
    axes[1, 1].hist(y_test_proba[y_test == 0], bins=20, alpha=0.6,
                    label='No Cancer', color='blue', edgecolor='black')
    axes[1, 1].hist(y_test_proba[y_test == 1], bins=20, alpha=0.6,
                    label='Lung Cancer', color='red', edgecolor='black')
    axes[1, 1].set_xlabel('Predicted Probability')
    axes[1, 1].set_ylabel('Frequency')
    axes[1, 1].set_title('Prediction Probability Distribution',
                         fontsize=14, fontweight='bold')
    axes[1, 1].legend()
    axes[1, 1].grid(True, alpha=0.3)

    plt.tight_layout()

    plot_path = save_dir / 'logistic_regression_real_results.png'
    plt.savefig(plot_path, dpi=300, bbox_inches='tight')

    print(f"\n✅ Visualizations saved to: {plot_path}")
    plt.close()


def main():
    """Main training function"""

    # Load real data
    X, y, feature_names = load_real_data()

    if X is None:
        return

    # Train model
    model, scaler, metrics = train_model(X, y, feature_names)

    print("\n" + "=" * 60)
    print("✅ MODULE 2 TRAINING COMPLETE (REAL DATA)!")
    print("=" * 60)
    print(f"\nFinal Test Accuracy: {metrics['test_accuracy'] * 100:.2f}%")
    print(f"AUC-ROC Score: {metrics['test_auc']:.4f}")
    print("\nModel ready for multi-modal integration!")


if __name__ == "__main__":
    main()