# Python AI Module Interface Specification

## Overview
This document defines the expected API contract between the C# Orchestrator and Python AI modules.

## Endpoint Requirements

Each AI module (Imaging, Clinical, Laboratory) must expose a `/predict` endpoint that:
- Accepts POST requests
- Processes JSON input
- Returns predictions in standardized format
- Includes confidence scores and probabilities

---

## 1. Imaging Module

### Endpoint
```
POST http://localhost:5001/api/imaging/predict
```

### Request Format
```json
{
  "imagePath": "/path/to/ct_scan.dcm",
  "imagingType": "CT",
  "bodyRegion": "Chest",
  "metadata": {
    "sliceThickness": "5mm",
    "pixelSpacing": "0.5mm"
  }
}
```

### Response Format
```json
{
  "prediction": "malignant",
  "confidence": 0.87,
  "probabilities": {
    "benign": 0.13,
    "malignant": 0.87
  },
  "explainability": {
    "grad_cam_heatmap": "base64_encoded_image",
    "top_features": [
      {"region": "upper_lobe", "importance": 0.65},
      {"region": "pleural_space", "importance": 0.22}
    ],
    "model_version": "resnet18_v1.2"
  }
}
```

### Flask Implementation Example
```python
from flask import Flask, request, jsonify
import torch
import numpy as np
from torchvision import transforms
from PIL import Image

app = Flask(__name__)

# Load your trained ResNet18 model
model = torch.load('models/imaging_model.pth')
model.eval()

@app.route('/api/imaging/predict', methods=['POST'])
def predict():
    try:
        data = request.get_json()
        image_path = data['imagePath']
        
        # Load and preprocess image
        image = Image.open(image_path)
        transform = transforms.Compose([
            transforms.Resize(224),
            transforms.ToTensor(),
            transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
        ])
        image_tensor = transform(image).unsqueeze(0)
        
        # Get prediction
        with torch.no_grad():
            outputs = model(image_tensor)
            probabilities = torch.softmax(outputs, dim=1)[0]
            
        # Extract probabilities
        benign_prob = float(probabilities[0])
        malignant_prob = float(probabilities[1])
        
        prediction = "malignant" if malignant_prob > benign_prob else "benign"
        confidence = max(benign_prob, malignant_prob)
        
        # Generate explainability (simplified)
        explainability = {
            "model_version": "resnet18_v1.2",
            "top_features": []  # Add Grad-CAM or SHAP here
        }
        
        return jsonify({
            "prediction": prediction,
            "confidence": confidence,
            "probabilities": {
                "benign": benign_prob,
                "malignant": malignant_prob
            },
            "explainability": explainability
        })
        
    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001)
```

---

## 2. Clinical Symptoms Module

### Endpoint
```
POST http://localhost:5002/api/clinical/predict
```

### Request Format
```json
{
  "symptoms": {
    "cough": true,
    "fever": true,
    "chest_pain": false,
    "weight_loss": true,
    "fatigue": true
  },
  "age": 45,
  "gender": "M",
  "medicalHistory": ["smoking", "hypertension"],
  "vitalSigns": {
    "temperature": 38.5,
    "blood_pressure": "140/90",
    "heart_rate": 88
  }
}
```

### Response Format
```json
{
  "prediction": "malignant",
  "confidence": 0.73,
  "probabilities": {
    "benign": 0.27,
    "malignant": 0.73
  },
  "explainability": {
    "top_contributing_symptoms": [
      {"symptom": "weight_loss", "importance": 0.35},
      {"symptom": "cough", "importance": 0.28},
      {"symptom": "fever", "importance": 0.18}
    ],
    "risk_factors": ["smoking", "age_over_40"],
    "model_version": "logistic_regression_v1.1"
  }
}
```

### Flask Implementation Example
```python
from flask import Flask, request, jsonify
import joblib
import numpy as np
from sklearn.preprocessing import StandardScaler

app = Flask(__name__)

# Load trained model
model = joblib.load('models/clinical_model.pkl')
scaler = joblib.load('models/scaler.pkl')

# Feature mapping
SYMPTOM_FEATURES = [
    'cough', 'fever', 'chest_pain', 'weight_loss', 'fatigue',
    'dyspnea', 'hemoptysis'
]

@app.route('/api/clinical/predict', methods=['POST'])
def predict():
    try:
        data = request.get_json()
        
        # Extract features
        symptoms = data.get('symptoms', {})
        age = data.get('age', 0)
        gender = 1 if data.get('gender') == 'M' else 0
        
        # Create feature vector
        features = []
        for symptom in SYMPTOM_FEATURES:
            features.append(1 if symptoms.get(symptom, False) else 0)
        features.extend([age, gender])
        
        # Scale features
        features_scaled = scaler.transform([features])
        
        # Get prediction
        prediction_class = model.predict(features_scaled)[0]
        probabilities = model.predict_proba(features_scaled)[0]
        
        prediction = "malignant" if prediction_class == 1 else "benign"
        confidence = max(probabilities)
        
        # Feature importance (if using logistic regression)
        coefficients = model.coef_[0]
        top_features = []
        for i, coef in enumerate(coefficients):
            if i < len(SYMPTOM_FEATURES):
                if features[i] == 1:  # Only show active symptoms
                    top_features.append({
                        "symptom": SYMPTOM_FEATURES[i],
                        "importance": abs(coef)
                    })
        
        top_features.sort(key=lambda x: x['importance'], reverse=True)
        
        return jsonify({
            "prediction": prediction,
            "confidence": float(confidence),
            "probabilities": {
                "benign": float(probabilities[0]),
                "malignant": float(probabilities[1])
            },
            "explainability": {
                "top_contributing_symptoms": top_features[:5],
                "model_version": "logistic_regression_v1.1"
            }
        })
        
    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5002)
```

---

## 3. Laboratory Results Module

### Endpoint
```
POST http://localhost:5003/api/laboratory/predict
```

### Request Format
```json
{
  "tumorMarkers": {
    "CEA": 5.2,
    "NSE": 12.5,
    "CYFRA 21-1": 3.1,
    "SCC-Ag": 1.8
  },
  "bloodWork": {
    "WBC": 8500,
    "RBC": 4.5,
    "hemoglobin": 13.2,
    "platelets": 250000
  },
  "additionalTests": {
    "LDH": 450,
    "alkaline_phosphatase": 120
  }
}
```

### Response Format
```json
{
  "prediction": "malignant",
  "confidence": 0.81,
  "probabilities": {
    "benign": 0.19,
    "malignant": 0.81
  },
  "explainability": {
    "elevated_markers": [
      {"marker": "CEA", "value": 5.2, "normal_range": "<5.0", "importance": 0.42},
      {"marker": "NSE", "value": 12.5, "normal_range": "<12.0", "importance": 0.35}
    ],
    "feature_importance": {
      "CEA": 0.42,
      "NSE": 0.35,
      "CYFRA 21-1": 0.15,
      "SCC-Ag": 0.08
    },
    "model_version": "random_forest_v1.0"
  }
}
```

### Flask Implementation Example
```python
from flask import Flask, request, jsonify
import joblib
import numpy as np

app = Flask(__name__)

# Load trained Random Forest model
model = joblib.load('models/laboratory_model.pkl')

# Feature names
TUMOR_MARKERS = ['CEA', 'NSE', 'CYFRA 21-1', 'SCC-Ag']
BLOOD_WORK = ['WBC', 'RBC', 'hemoglobin', 'platelets']

# Normal ranges for reference
NORMAL_RANGES = {
    'CEA': (0, 5.0),
    'NSE': (0, 12.0),
    'CYFRA 21-1': (0, 3.3),
    'SCC-Ag': (0, 1.5)
}

@app.route('/api/laboratory/predict', methods=['POST'])
def predict():
    try:
        data = request.get_json()
        
        # Extract tumor markers
        tumor_markers = data.get('tumorMarkers', {})
        blood_work = data.get('bloodWork', {})
        
        # Create feature vector
        features = []
        for marker in TUMOR_MARKERS:
            features.append(tumor_markers.get(marker, 0))
        
        for test in BLOOD_WORK:
            features.append(blood_work.get(test, 0))
        
        # Get prediction
        prediction_class = model.predict([features])[0]
        probabilities = model.predict_proba([features])[0]
        
        prediction = "malignant" if prediction_class == 1 else "benign"
        confidence = max(probabilities)
        
        # Feature importance from Random Forest
        feature_importances = model.feature_importances_
        
        # Identify elevated markers
        elevated_markers = []
        for i, marker in enumerate(TUMOR_MARKERS):
            value = tumor_markers.get(marker, 0)
            normal_max = NORMAL_RANGES.get(marker, (0, 999))[1]
            
            if value > normal_max:
                elevated_markers.append({
                    "marker": marker,
                    "value": value,
                    "normal_range": f"<{normal_max}",
                    "importance": float(feature_importances[i])
                })
        
        elevated_markers.sort(key=lambda x: x['importance'], reverse=True)
        
        # Create feature importance dict
        feature_importance_dict = {}
        for i, marker in enumerate(TUMOR_MARKERS):
            feature_importance_dict[marker] = float(feature_importances[i])
        
        return jsonify({
            "prediction": prediction,
            "confidence": float(confidence),
            "probabilities": {
                "benign": float(probabilities[0]),
                "malignant": float(probabilities[1])
            },
            "explainability": {
                "elevated_markers": elevated_markers,
                "feature_importance": feature_importance_dict,
                "model_version": "random_forest_v1.0"
            }
        })
        
    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5003)
```

---

## Error Handling

All modules should return appropriate HTTP status codes:

### Success (200)
```json
{
  "prediction": "malignant",
  "confidence": 0.85,
  ...
}
```

### Bad Request (400)
```json
{
  "error": "Missing required field: imagePath"
}
```

### Internal Server Error (500)
```json
{
  "error": "Model inference failed: dimension mismatch"
}
```

---

## Testing Your Module

Use curl to test:

```bash
# Test Imaging Module
curl -X POST http://localhost:5001/api/imaging/predict \
  -H "Content-Type: application/json" \
  -d '{
    "imagePath": "/data/ct_scan_001.dcm",
    "imagingType": "CT",
    "bodyRegion": "Chest"
  }'

# Test Clinical Module
curl -X POST http://localhost:5002/api/clinical/predict \
  -H "Content-Type: application/json" \
  -d '{
    "symptoms": {"cough": true, "fever": true},
    "age": 45,
    "gender": "M"
  }'

# Test Laboratory Module
curl -X POST http://localhost:5003/api/laboratory/predict \
  -H "Content-Type: application/json" \
  -d '{
    "tumorMarkers": {
      "CEA": 5.2,
      "NSE": 12.5
    }
  }'
```

---

## Performance Requirements

- **Response Time**: < 2 seconds per prediction
- **Availability**: 99.9% uptime
- **Concurrent Requests**: Handle at least 10 simultaneous requests
- **Error Rate**: < 1% failure rate

---

## Deployment Checklist

- [ ] Model files are loaded on startup (not per request)
- [ ] Input validation is implemented
- [ ] Proper error handling with meaningful messages
- [ ] Logging is configured (requests, predictions, errors)
- [ ] Health check endpoint: `GET /health`
- [ ] Model version is tracked in responses
- [ ] CORS is configured if needed
- [ ] Environment variables for configuration (ports, paths)

---

## Integration Testing

Once all three modules are running, test the orchestrator:

```bash
# Full system test
curl -X POST http://localhost:5000/api/diagnosis/sync \
  -H "Content-Type: application/json" \
  -d '{
    "diagnosisCaseId": "test-case-001",
    "patientId": "patient-123",
    "doctorId": "doctor-456",
    "imagingData": {
      "imagePath": "/data/ct_scan_001.dcm",
      "imagingType": "CT"
    },
    "clinicalData": {
      "symptoms": {"cough": true, "fever": true},
      "age": 45,
      "gender": "M"
    },
    "laboratoryData": {
      "tumorMarkers": {
        "CEA": 5.2,
        "NSE": 12.5
      }
    }
  }'
```

Expected response shows ensemble of all three predictions!

---

**Remember**: The C# orchestrator will automatically:
- Call all three modules in parallel
- Handle failures gracefully
- Combine predictions using weighted voting
- Save results to TimescaleDB
- Publish to Kafka for async processing
