"""
Mock AI Module Services for Testing the Orchestrator

This file provides simple mock implementations of the three AI modules.
Use these to test the orchestrator before your real AI models are ready.

Usage:
    python mock_services.py

This will start all three services on ports 5001, 5002, 5003
"""

from flask import Flask, request, jsonify
from threading import Thread
import random
import time

# ============================================
# Mock Imaging Service (Port 5001)
# ============================================
imaging_app = Flask("imaging")

@imaging_app.route('/api/imaging/predict', methods=['POST'])
def imaging_predict():
    try:
        data = request.get_json()
        image_path = data.get('imagePath', '')
        
        # Simulate processing time
        time.sleep(random.uniform(0.5, 1.5))
        
        # Generate mock prediction
        # Higher malignant probability if "suspicious" or "mass" in path
        if 'suspicious' in image_path.lower() or 'mass' in image_path.lower():
            malignant_prob = random.uniform(0.75, 0.95)
        elif 'granuloma' in image_path.lower() or 'benign' in image_path.lower():
            malignant_prob = random.uniform(0.05, 0.25)
        else:
            malignant_prob = random.uniform(0.3, 0.7)
        
        benign_prob = 1.0 - malignant_prob
        prediction = "malignant" if malignant_prob > 0.5 else "benign"
        
        return jsonify({
            "prediction": prediction,
            "confidence": max(malignant_prob, benign_prob),
            "probabilities": {
                "benign": round(benign_prob, 4),
                "malignant": round(malignant_prob, 4)
            },
            "explainability": {
                "grad_cam_regions": [
                    {"region": "upper_lobe", "importance": random.uniform(0.5, 0.9)},
                    {"region": "pleural_space", "importance": random.uniform(0.1, 0.4)}
                ],
                "model_version": "resnet18_mock_v1.0"
            }
        })
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@imaging_app.route('/health', methods=['GET'])
def imaging_health():
    return jsonify({"status": "healthy", "service": "imaging"})


# ============================================
# Mock Clinical Service (Port 5002)
# ============================================
clinical_app = Flask("clinical")

@clinical_app.route('/api/clinical/predict', methods=['POST'])
def clinical_predict():
    try:
        data = request.get_json()
        symptoms = data.get('symptoms', {})
        age = data.get('age', 40)
        
        # Simulate processing time
        time.sleep(random.uniform(0.3, 1.0))
        
        # Calculate risk based on symptoms and age
        risk_score = 0.0
        
        # High-risk symptoms
        if symptoms.get('weight_loss', False):
            risk_score += 0.25
        if symptoms.get('hemoptysis', False):
            risk_score += 0.30
        if symptoms.get('dyspnea', False):
            risk_score += 0.15
        if symptoms.get('chest_pain', False):
            risk_score += 0.10
        if symptoms.get('cough', False):
            risk_score += 0.10
        if symptoms.get('fever', False):
            risk_score += 0.05
        
        # Age factor
        if age > 60:
            risk_score += 0.15
        elif age > 50:
            risk_score += 0.10
        
        # Cap at 0.95
        malignant_prob = min(risk_score, 0.95)
        benign_prob = 1.0 - malignant_prob
        
        prediction = "malignant" if malignant_prob > 0.5 else "benign"
        
        # Find contributing symptoms
        contributing = []
        for symptom, present in symptoms.items():
            if present:
                contributing.append({
                    "symptom": symptom,
                    "importance": random.uniform(0.1, 0.4)
                })
        contributing.sort(key=lambda x: x['importance'], reverse=True)
        
        return jsonify({
            "prediction": prediction,
            "confidence": max(malignant_prob, benign_prob),
            "probabilities": {
                "benign": round(benign_prob, 4),
                "malignant": round(malignant_prob, 4)
            },
            "explainability": {
                "top_contributing_symptoms": contributing[:5],
                "risk_factors": ["age_over_50"] if age > 50 else [],
                "model_version": "logistic_regression_mock_v1.0"
            }
        })
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@clinical_app.route('/health', methods=['GET'])
def clinical_health():
    return jsonify({"status": "healthy", "service": "clinical"})


# ============================================
# Mock Laboratory Service (Port 5003)
# ============================================
laboratory_app = Flask("laboratory")

@laboratory_app.route('/api/laboratory/predict', methods=['POST'])
def laboratory_predict():
    try:
        data = request.get_json()
        tumor_markers = data.get('tumorMarkers', {})
        
        # Simulate processing time
        time.sleep(random.uniform(0.2, 0.8))
        
        # Calculate risk based on tumor marker levels
        risk_score = 0.0
        elevated_markers = []
        
        # Normal ranges
        normal_ranges = {
            'CEA': 5.0,
            'NSE': 12.0,
            'CYFRA 21-1': 3.3,
            'SCC-Ag': 1.5
        }
        
        # Check each marker
        for marker, value in tumor_markers.items():
            if marker in normal_ranges:
                normal_max = normal_ranges[marker]
                if value > normal_max:
                    # Calculate how much it's elevated
                    elevation_factor = value / normal_max
                    importance = min(elevation_factor * 0.2, 0.4)
                    risk_score += importance
                    
                    elevated_markers.append({
                        "marker": marker,
                        "value": value,
                        "normal_range": f"<{normal_max}",
                        "importance": importance
                    })
        
        # Cap risk score
        malignant_prob = min(risk_score, 0.95)
        benign_prob = 1.0 - malignant_prob
        
        prediction = "malignant" if malignant_prob > 0.5 else "benign"
        
        elevated_markers.sort(key=lambda x: x['importance'], reverse=True)
        
        return jsonify({
            "prediction": prediction,
            "confidence": max(malignant_prob, benign_prob),
            "probabilities": {
                "benign": round(benign_prob, 4),
                "malignant": round(malignant_prob, 4)
            },
            "explainability": {
                "elevated_markers": elevated_markers,
                "feature_importance": {
                    marker: round(random.uniform(0.1, 0.4), 2)
                    for marker in tumor_markers.keys()
                },
                "model_version": "random_forest_mock_v1.0"
            }
        })
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@laboratory_app.route('/health', methods=['GET'])
def laboratory_health():
    return jsonify({"status": "healthy", "service": "laboratory"})


# ============================================
# Service Runners
# ============================================
def run_imaging():
    print("🔬 Starting Imaging Service on http://localhost:5001")
    imaging_app.run(host='0.0.0.0', port=5001, threaded=True)

def run_clinical():
    print("🏥 Starting Clinical Service on http://localhost:5002")
    clinical_app.run(host='0.0.0.0', port=5002, threaded=True)

def run_laboratory():
    print("🧪 Starting Laboratory Service on http://localhost:5003")
    laboratory_app.run(host='0.0.0.0', port=5003, threaded=True)


if __name__ == '__main__':
    print("=" * 60)
    print("🚀 Starting Mock AI Module Services")
    print("=" * 60)
    print()
    print("These mock services simulate the three AI modules:")
    print("  • Imaging Module    → http://localhost:5001")
    print("  • Clinical Module   → http://localhost:5002")
    print("  • Laboratory Module → http://localhost:5003")
    print()
    print("The orchestrator can now call these services for testing!")
    print("Press Ctrl+C to stop all services")
    print("=" * 60)
    print()
    
    # Start all services in separate threads
    thread1 = Thread(target=run_imaging)
    thread2 = Thread(target=run_clinical)
    thread3 = Thread(target=run_laboratory)
    
    thread1.daemon = True
    thread2.daemon = True
    thread3.daemon = True
    
    thread1.start()
    time.sleep(0.5)  # Stagger startup
    thread2.start()
    time.sleep(0.5)
    thread3.start()
    
    try:
        # Keep main thread alive
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("\n\n🛑 Shutting down all mock services...")
        print("✅ All services stopped!")
