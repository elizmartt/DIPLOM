"""
Train Random Forest on REAL lung cancer tumor marker data
Source: PMC6311751 - Published clinical study
"""

import numpy as np
import pandas as pd
from pathlib import Path
import pickle
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import (
    accuracy_score, precision_score, recall_score, f1_score,
    confusion_matrix, classification_report, roc_curve, roc_auc_score
)


def load_real_tumor_marker_data():
    """Load and prepare real tumor marker data"""

    print("=" * 60)
    print("LOADING REAL TUMOR MARKER DATA")
    print("=" * 60)

    # Find Excel file
    data_dir = Path(__file__).parent / 'data' / 'raw_lab'
    xlsx_file = data_dir / 'tumor_markers.xlsx'

    if not xlsx_file.exists():
        print(f"\n❌ Data file not found: {xlsx_file}")
        print("Please run: python module3_labresults/download_real_lab_data.py")
        return None, None, None

    print(f"\n📂 Loading: {xlsx_file.name}")

    try:
        # Load both sheets
        xlsx = pd.ExcelFile(xlsx_file)

        # Sheet 1: NSCLC patients (Cancer = 1)
        df_cancer = pd.read_excel(xlsx_file, sheet_name=0)
        df_cancer['diagnosis'] = 1  # Lung Cancer
        print(f"✅ Loaded {len(df_cancer)} cancer patients")

        # Sheet 2: Benign chest disease (Cancer = 0)
        df_benign = pd.read_excel(xlsx_file, sheet_name=1)
        df_benign['diagnosis'] = 0  # No Cancer
        print(f"✅ Loaded {len(df_benign)} benign disease patients")

        # Combine datasets
        # First, align column names
        print("\n🔄 Processing data...")

        # Get common tumor marker columns
        tumor_markers = ['CEA', 'CA125', 'CA15-3', 'CA19-9', 'CA72-4', 'CYFRA21-1', 'SCC-Ag']

        # Standardize column names (handle variations)
        for df in [df_cancer, df_benign]:
            df.columns = df.columns.str.strip()
            # Handle potential naming variations
            col_mapping = {}
            for col in df.columns:
                col_upper = col.upper()
                if 'CYFRA' in col_upper:
                    col_mapping[col] = 'CYFRA21-1'
                elif 'SCC' in col_upper and 'AG' in col_upper:
                    col_mapping[col] = 'SCC-Ag'
            if col_mapping:
                df.rename(columns=col_mapping, inplace=True)

        # Select common columns
        common_cols = ['GENDER', 'AGE'] + tumor_markers

        # Ensure columns exist
        cancer_cols = [col for col in common_cols if col in df_cancer.columns]
        benign_cols = [col for col in common_cols if col in df_benign.columns]

        print(f"\n📊 Cancer dataset columns: {df_cancer.columns.tolist()}")
        print(f"📊 Benign dataset columns: {df_benign.columns.tolist()}")

        # Select and combine
        df_cancer_subset = df_cancer[cancer_cols + ['diagnosis']].copy()
        df_benign_subset = df_benign[benign_cols + ['diagnosis']].copy()

        # Combine
        df = pd.concat([df_cancer_subset, df_benign_subset], axis=0, ignore_index=True)

        print(f"\n✅ Combined dataset: {len(df)} total patients")
        print(f"   - Cancer: {len(df[df['diagnosis'] == 1])}")
        print(f"   - Benign: {len(df[df['diagnosis'] == 0])}")

        # Handle missing values
        print(f"\n🔄 Handling missing values...")
        print(f"Missing values before:")
        print(df.isnull().sum())

        # Fill missing values with median for numeric columns
        for col in df.columns:
            if df[col].dtype in ['float64', 'int64'] and col != 'diagnosis':
                df[col].fillna(df[col].median(), inplace=True)

        print(f"\n✅ Missing values after:")
        print(df.isnull().sum())

        # Encode gender (M=1, F=0)
        if 'GENDER' in df.columns:
            df['GENDER'] = df['GENDER'].map({'M': 1, 'F': 0, 1: 1, 2: 0})

        # Prepare features and target
        X = df.drop('diagnosis', axis=1)
        y = df['diagnosis']

        # Standardize column names to lowercase
        X.columns = X.columns.str.lower()
        feature_names = X.columns.tolist()

        print(f"\n📊 Final dataset statistics:")
        print(df.describe())

        print(f"\n📊 Target distribution:")
        print(y.value_counts())
        print(f"\n{y.value_counts(normalize=True) * 100}")

        print(f"\n✅ Features prepared: {len(feature_names)}")
        print(f"   Features: {feature_names}")

        return X, y, feature_names

    except Exception as e:
        print(f"\n❌ Error loading data: {e}")
        import traceback
        traceback.print_exc()
        return None, None, None


def train_model(X, y, feature_names):
    """Train Random Forest on real tumor marker data"""

    print("\n" + "=" * 60)
    print("TRAINING RANDOM FOREST ON REAL LAB DATA")
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

    # Train Random Forest
    print("\n🔄 Training Random Forest...")
    print("Hyperparameters:")
    print("  • n_estimators: 200 trees")
    print("  • max_depth: 15")
    print("  • min_samples_split: 5")
    print("  • min_samples_leaf: 2")

    model = RandomForestClassifier(
        n_estimators=200,
        max_depth=15,
        min_samples_split=5,
        min_samples_leaf=2,
        random_state=42,
        n_jobs=-1,
        class_weight='balanced'  # Handle class imbalance
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
    print("MODEL PERFORMANCE ON REAL DATA")
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
    print("TUMOR MARKER IMPORTANCE")
    print("=" * 60)

    feature_importance = pd.DataFrame({
        'feature': feature_names,
        'importance': model.feature_importances_
    }).sort_values('importance', ascending=False)

    print("\nTumor Marker Rankings:")
    print(feature_importance.to_string(index=False))

    # Save model
    model_dir = Path(__file__).parent / 'models'
    model_dir.mkdir(exist_ok=True)

    model_path = model_dir / 'random_forest_real.pkl'
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

    fig, axes = plt.subplots(2, 2, figsize=(16, 12))

    # Confusion Matrix
    cm = confusion_matrix(y_test, y_test_pred)
    sns.heatmap(cm, annot=True, fmt='d', cmap='Greens', ax=axes[0, 0],
                xticklabels=['No Cancer', 'Lung Cancer'],
                yticklabels=['No Cancer', 'Lung Cancer'])
    axes[0, 0].set_title('Confusion Matrix (Real Lab Data)',
                         fontsize=14, fontweight='bold')
    axes[0, 0].set_ylabel('True Label')
    axes[0, 0].set_xlabel('Predicted Label')

    # ROC Curve
    fpr, tpr, _ = roc_curve(y_test, y_test_proba)
    auc = roc_auc_score(y_test, y_test_proba)

    axes[0, 1].plot(fpr, tpr, linewidth=2,
                    label=f'ROC Curve (AUC = {auc:.3f})')
    axes[0, 1].plot([0, 1], [0, 1], 'k--', linewidth=1,
                    label='Random Classifier')
    axes[0, 1].set_xlim([0.0, 1.0])
    axes[0, 1].set_ylim([0.0, 1.05])
    axes[0, 1].set_xlabel('False Positive Rate')
    axes[0, 1].set_ylabel('True Positive Rate')
    axes[0, 1].set_title('ROC Curve (Real Lab Data)',
                         fontsize=14, fontweight='bold')
    axes[0, 1].legend(loc="lower right")
    axes[0, 1].grid(True, alpha=0.3)

    # Feature Importance
    colors = plt.cm.Greens(np.linspace(0.4, 0.9, len(feature_importance)))

    axes[1, 0].barh(range(len(feature_importance)),
                    feature_importance['importance'],
                    color=colors)
    axes[1, 0].set_yticks(range(len(feature_importance)))
    axes[1, 0].set_yticklabels(feature_importance['feature'])
    axes[1, 0].set_xlabel('Importance Score')
    axes[1, 0].set_title('Tumor Marker Importance Rankings',
                         fontsize=14, fontweight='bold')
    axes[1, 0].grid(True, alpha=0.3, axis='x')
    axes[1, 0].invert_yaxis()

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

    plot_path = save_dir / 'random_forest_real_results.png'
    plt.savefig(plot_path, dpi=300, bbox_inches='tight')

    print(f"\n✅ Visualizations saved to: {plot_path}")
    plt.close()


def main():
    """Main training function"""

    # Load real data
    X, y, feature_names = load_real_tumor_marker_data()

    if X is None:
        return

    # Train model
    model, scaler, metrics = train_model(X, y, feature_names)

    print("\n" + "=" * 60)
    print("✅ MODULE 3 TRAINING COMPLETE (REAL DATA)!")
    print("=" * 60)
    print(f"\nFinal Test Accuracy: {metrics['test_accuracy'] * 100:.2f}%")
    print(f"AUC-ROC Score: {metrics['test_auc']:.4f}")

    print("\n" + "=" * 60)
    print("SUMMARY: ALL THREE MODULES STATUS")
    print("=" * 60)
    print("✅ Module 1: ResNet18 for CT Imaging (trained)")
    print("✅ Module 2: Logistic Regression for Symptoms (93.62% - REAL)")
    print("✅ Module 3: Random Forest for Lab Results (REAL DATA!)")
    print("\n🎯 NEXT STEP: Build Multi-Modal Integration!")


if __name__ == "__main__":
    main()