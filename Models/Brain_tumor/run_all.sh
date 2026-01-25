#!/bin/bash

echo "=========================================="
echo "Neurological Disease Diagnostic System"
echo "Complete Training Pipeline"
echo "=========================================="

echo ""
echo "Step 1: Generating datasets..."
python generate_data.py

echo ""
echo "Step 2: Training Module 1 - Imaging Analysis (ResNet18)..."
python module1_imaging.py

echo ""
echo "Step 3: Training Module 2 - Clinical Features Analysis (Logistic Regression)..."
python module2_clinical.py

echo ""
echo "Step 4: Training Module 3 - Laboratory Results Analysis (Random Forest)..."
python module3_lab.py

echo ""
echo "=========================================="
echo "Training Complete!"
echo "=========================================="
echo ""
echo "Generated Models:"
echo "  - neurological_resnet18_best.pth"
echo "  - neurological_resnet18_final.pth"
echo "  - neurological_clinical_lr.pkl"
echo "  - neurological_clinical_scaler.pkl"
echo "  - neurological_lab_rf.pkl"
echo "  - neurological_lab_scaler.pkl"
echo ""
echo "Generated Datasets:"
echo "  - neurological_clinical_features.tsv"
echo "  - neurological_lab_results.tsv"#!/bin/bash

echo "=========================================="
echo "Neurological Disease Diagnostic System"
echo "Complete Training Pipeline"
echo "=========================================="

echo ""
echo "Step 1: Generating datasets..."
python generate_data.py

echo ""
echo "Step 2: Training Module 1 - Imaging Analysis (ResNet18)..."
python module1_imaging.py

echo ""
echo "Step 3: Training Module 2 - Clinical Features Analysis (Logistic Regression)..."
python module2_clinical.py

echo ""
echo "Step 4: Training Module 3 - Laboratory Results Analysis (Random Forest)..."
python module3_lab.py

echo ""
echo "=========================================="
echo "Training Complete!"
echo "=========================================="
echo ""
echo "Generated Models:"
echo "  - neurological_resnet18_best.pth"
echo "  - neurological_resnet18_final.pth"
echo "  - neurological_clinical_lr.pkl"
echo "  - neurological_clinical_scaler.pkl"
echo "  - neurological_lab_rf.pkl"
echo "  - neurological_lab_scaler.pkl"
echo ""
echo "Generated Datasets:"
echo "  - neurological_clinical_features.tsv"
echo "  - neurological_lab_results.tsv"