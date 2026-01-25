# 🎯 Diagnosis Service Orchestrator - Complete Overview

## What We Built

A production-ready **C# (.NET 8) Diagnosis Service Orchestrator** that:
- ✅ Combines predictions from 3 AI modules using weighted ensemble logic
- ✅ Supports both synchronous and asynchronous processing via Kafka
- ✅ Stores all results in TimescaleDB with time-series optimization
- ✅ Provides comprehensive error handling and graceful degradation
- ✅ Includes explainability aggregation and clinical recommendations
- ✅ Implements risk level calculation and module agreement tracking

---

## 📁 Project Structure

```
DiagnosisOrchestrator/
│
├── 📄 DiagnosisOrchestrator.csproj    # .NET project file
├── 📄 Program.cs                       # App startup & DI configuration
├── 📄 appsettings.json                 # Configuration
├── 📄 docker-compose.yml               # Infrastructure setup
├── 📄 Makefile                         # Easy commands
│
├── 📂 Models/
│   └── DiagnosisModels.cs              # Domain models & records
│
├── 📂 Services/
│   ├── IDiagnosisOrchestratorService.cs       # Main interface
│   ├── DiagnosisOrchestratorService.cs        # Core orchestration logic
│   ├── IModuleClientService.cs                # Module client interface
│   ├── ModuleClientService.cs                 # HTTP client for AI modules
│   ├── IDiagnosisRepository.cs                # Repository interface
│   ├── DiagnosisRepository.cs                 # TimescaleDB integration
│   ├── IKafkaProducerService.cs               # Kafka producer interface
│   ├── KafkaProducerService.cs                # Kafka producer
│   └── KafkaConsumerService.cs                # Kafka consumer (background)
│
├── 📂 Controllers/
│   └── DiagnosisController.cs          # REST API endpoints
│
├── 📂 Database/
│   └── schema.sql                      # TimescaleDB schema
│
├── 📂 TestData/
│   ├── sample_requests.md              # Test cases & examples
│   └── mock_services.py                # Mock Python AI modules
│
└── 📂 Documentation/
    ├── README.md                       # Main documentation
    ├── QUICK_START.md                  # Quick start guide
    └── PythonModuleInterface.md        # API contract for Python modules
```

---

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     API Gateway / Frontend                   │
│                  (Your existing .NET 8 Gateway)              │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTP/HTTPS
                         ▼
┌─────────────────────────────────────────────────────────────┐
│           Diagnosis Orchestrator (C# .NET 8)                │
│                                                               │
│  ┌────────────────────────────────────────────────────┐     │
│  │     Ensemble Logic Engine                          │     │
│  │  • Weighted Voting (default)                       │     │
│  │  • Confidence-Weighted Voting                      │     │
│  │  • Majority Voting                                 │     │
│  │  • Risk Calculation                                │     │
│  │  • Recommendations Generation                      │     │
│  └────────────────────────────────────────────────────┘     │
│                                                               │
│  Calls modules in parallel:                                  │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                   │
│  │ Imaging  │  │ Clinical │  │   Lab    │                   │
│  │  40%     │  │   30%    │  │   30%    │                   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘                   │
└───────┼─────────────┼─────────────┼─────────────────────────┘
        │             │             │
        │ HTTP        │ HTTP        │ HTTP
        ▼             ▼             ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│   Imaging    │ │   Clinical   │ │  Laboratory  │
│   Service    │ │   Service    │ │   Service    │
│  (Python)    │ │  (Python)    │ │  (Python)    │
│              │ │              │ │              │
│  ResNet18    │ │  Logistic    │ │   Random     │
│  for CT/MRI  │ │  Regression  │ │   Forest     │
│              │ │              │ │              │
│ Port: 5001   │ │ Port: 5002   │ │ Port: 5003   │
└──────────────┘ └──────────────┘ └──────────────┘
        │             │             │
        └─────────────┴─────────────┘
                      │
        ┌─────────────┴─────────────┐
        │                           │
        ▼                           ▼
┌──────────────┐            ┌──────────────┐
│ TimescaleDB  │            │    Kafka     │
│              │            │              │
│  • Results   │            │ • Async      │
│  • Metrics   │            │ • Queuing    │
│  • History   │            │ • Events     │
│              │            │              │
│ Port: 5432   │            │ Port: 9092   │
└──────────────┘            └──────────────┘
```

---

## 🚀 Getting Started (3 Steps)

### Step 1: Start Infrastructure (1 minute)

```bash
cd DiagnosisOrchestrator

# Start all infrastructure
docker-compose up -d

# Verify
docker-compose ps
```

Services that start:
- ✅ TimescaleDB (Port 5432)
- ✅ Kafka (Port 9092)
- ✅ Zookeeper (Port 2181)
- ✅ Redis (Port 6379)
- ✅ Kafka UI (Port 8080)
- ✅ pgAdmin (Port 5050)

### Step 2: Run Mock AI Modules (1 minute)

```bash
# Install Flask
pip install flask

# Start mock services
cd TestData
python mock_services.py
```

This starts three mock Python services:
- 🔬 Imaging → http://localhost:5001
- 🏥 Clinical → http://localhost:5002
- 🧪 Laboratory → http://localhost:5003

### Step 3: Start Orchestrator (30 seconds)

```bash
# Build and run
dotnet restore
dotnet build
dotnet run
```

Orchestrator starts on:
- 🌐 http://localhost:5000
- 📚 Swagger UI at root

---

## 🧪 Testing

### Quick Health Check

```bash
curl http://localhost:5000/api/diagnosis/health
```

### Test Synchronous Diagnosis

```bash
curl -X POST http://localhost:5000/api/diagnosis/sync \
  -H "Content-Type: application/json" \
  -d '{
    "diagnosisCaseId": "test-001",
    "patientId": "patient-001",
    "doctorId": "doctor-001",
    "imagingData": {
      "imagePath": "/data/suspicious_nodule.dcm",
      "imagingType": "CT",
      "bodyRegion": "Chest"
    },
    "clinicalData": {
      "symptoms": {
        "cough": true,
        "weight_loss": true,
        "chest_pain": true
      },
      "age": 62,
      "gender": "M"
    },
    "laboratoryData": {
      "tumorMarkers": {
        "CEA": 12.5,
        "NSE": 25.3,
        "CYFRA 21-1": 8.9
      }
    }
  }' | jq
```

### Expected Response

```json
{
  "diagnosisCaseId": "test-001",
  "finalDiagnosis": "Malignant",
  "overallConfidence": 0.87,
  "ensembleProbabilities": {
    "benign": 0.13,
    "malignant": 0.87
  },
  "contributingModules": ["imaging", "clinical", "laboratory"],
  "riskLevel": "High",
  "recommendations": [
    "Immediate consultation with oncology specialist recommended",
    "Consider biopsy for histological confirmation",
    "Schedule comprehensive staging workup"
  ],
  "status": "Completed",
  "totalProcessingTimeMs": 1543.21,
  "timestamp": "2025-01-15T10:30:00Z"
}
```

---

## 📊 Monitoring & Management

### View Results in Database

```bash
# Connect to database
make db-connect

# Or manually
docker exec -it medical-timescaledb psql -U postgres -d medical_diagnosis
```

```sql
-- Recent diagnoses
SELECT 
    diagnosis_case_id,
    final_diagnosis,
    overall_confidence,
    risk_level,
    created_at
FROM unified_diagnosis_results
ORDER BY created_at DESC
LIMIT 10;

-- Performance stats
SELECT * FROM diagnosis_performance_stats 
WHERE date >= NOW() - INTERVAL '7 days';

-- Module comparison
SELECT * FROM module_performance_comparison;
```

### View Kafka Messages

```bash
# Open Kafka UI
open http://localhost:8080

# Or consume from terminal
make kafka-consume-requests
make kafka-consume-results
```

### View Logs

```bash
# All services
make logs

# Specific service
docker-compose logs -f timescaledb
docker-compose logs -f kafka
```

---

## 🎯 How The Ensemble Works

### Weighted Voting (Default)

```
Module Weights:
  Imaging:    40%  (direct visual evidence)
  Clinical:   30%  (symptom patterns)
  Laboratory: 30%  (biomarkers)

Calculation:
  Final Probability = 
    (0.40 × Imaging_Prob) + 
    (0.30 × Clinical_Prob) + 
    (0.30 × Laboratory_Prob)

Example:
  Imaging:    90% malignant
  Clinical:   70% malignant
  Laboratory: 85% malignant
  
  Result = (0.40×0.90) + (0.30×0.70) + (0.30×0.85)
         = 0.36 + 0.21 + 0.255
         = 0.825 → 82.5% confidence MALIGNANT
```

### Risk Level Calculation

```
Malignant:
  - Confidence ≥ 90% → Critical
  - Confidence ≥ 75% → High
  - Confidence < 75% → Moderate

Benign:
  - Confidence ≥ 90% → Low
  - Confidence ≥ 70% → Moderate
  - Confidence < 70% → High (low confidence in benign = caution)

Inconclusive:
  - Always Moderate
```

### Recommendations Logic

The system generates recommendations based on:
1. **Final Diagnosis**: Different recommendations for malignant/benign/inconclusive
2. **Confidence Level**: Lower confidence → more follow-up recommended
3. **Module Agreement**: Disagreement → manual review recommended
4. **Failed Modules**: Which modules failed → retry suggestions

---

## 🔧 Configuration Options

### Changing Module Weights

Edit `appsettings.json`:

```json
{
  "OrchestratorOptions": {
    "ModuleWeights": {
      "Imaging": 0.50,    // Increase imaging weight
      "Clinical": 0.25,   // Decrease clinical
      "Laboratory": 0.25  // Decrease lab
    }
  }
}
```

### Changing Ensemble Strategy

```json
{
  "OrchestratorOptions": {
    "EnsembleStrategy": "ConfidenceWeighted"  // or "MajorityVoting"
  }
}
```

### Changing Confidence Threshold

```json
{
  "OrchestratorOptions": {
    "ConfidenceThreshold": 0.80  // More conservative (default: 0.70)
  }
}
```

---

## 🔗 Integration with Your Thesis Components

### You Already Have:
- ✅ TimescaleDB schema
- ✅ API Gateway in C# (.NET 8)
- ✅ AI Module 1: Imaging (ResNet18 - trained)
- ✅ AI Module 2: Clinical (Logistic Regression - trained)
- ✅ AI Module 3: Laboratory (Random Forest - in progress)

### What This Adds:
- ✅ **Orchestration Layer**: Combines all three modules
- ✅ **Ensemble Logic**: Weighted voting with configurable strategies
- ✅ **Async Processing**: Kafka integration for scalability
- ✅ **Comprehensive Results**: Risk levels, recommendations, explainability
- ✅ **Production Ready**: Error handling, logging, monitoring

### Integration Steps:

1. **Connect Your Python Modules**
   - Make each module expose the required API (see `PythonModuleInterface.md`)
   - Update ports in `appsettings.json` if needed

2. **Connect Your API Gateway**
   - Call orchestrator from your gateway:
   ```csharp
   var httpClient = new HttpClient();
   var response = await httpClient.PostAsJsonAsync(
       "http://orchestrator:5000/api/diagnosis/sync",
       diagnosisRequest);
   ```

3. **Connect Your Frontend**
   - React frontend calls your API Gateway
   - API Gateway forwards to Orchestrator
   - Results flow back through the chain

---

## 📈 Next Steps

### Immediate (This Week):
1. ✅ Test with mock services (already done!)
2. 🔄 Connect your real Python AI modules
3. 🔄 Test end-to-end with real data
4. 🔄 Verify database storage and Kafka messages

### Short Term (Next 2 Weeks):
1. Implement explainability features (SHAP, Grad-CAM) in Python modules
2. Add more test cases and validation
3. Optimize module weights based on real performance
4. Add monitoring dashboards (optional: Grafana + Prometheus)

### Before Thesis Defense:
1. Prepare demo with real medical cases
2. Document ensemble performance metrics
3. Create comparison: individual modules vs. ensemble
4. Prepare slides showing architecture and results

---

## 🎓 Thesis Integration

### What to Highlight:

1. **Innovation**:
   - Multi-modal approach (imaging + clinical + lab)
   - Weighted ensemble with explainability
   - Production-grade microservices architecture

2. **Technical Depth**:
   - C# orchestration with Python AI
   - Asynchronous processing with Kafka
   - Time-series optimization with TimescaleDB
   - RESTful API design

3. **Medical Relevance**:
   - Risk level calculation
   - Clinical recommendations
   - Doctor-centric design (second opinion tool)
   - HIPAA compliance considerations

4. **Results to Present**:
   - Individual module accuracy
   - Ensemble accuracy improvement
   - Processing time metrics
   - Module agreement statistics
   - Error handling robustness

---

## 🆘 Troubleshooting

### "Connection refused" from orchestrator
→ Make sure Python modules are running on correct ports

### "Kafka connection failed"
→ Run: `docker-compose restart kafka`

### "Database connection failed"
→ Verify: `docker ps | grep timescale`

### "Module returned 500 error"
→ Check Python module logs for exceptions

### Need help?
→ Check README.md, QUICK_START.md, or documentation files

---

## 📚 Documentation Files

- `README.md` - Complete technical documentation
- `QUICK_START.md` - Step-by-step setup guide
- `PythonModuleInterface.md` - API contract for Python modules
- `TestData/sample_requests.md` - Test cases and examples
- `TestData/mock_services.py` - Mock Python services

---

## ✨ Key Features Summary

| Feature | Status | Description |
|---------|--------|-------------|
| Multi-modal fusion | ✅ Complete | Combines 3 AI modules |
| Weighted ensemble | ✅ Complete | Configurable weights |
| Async processing | ✅ Complete | Kafka integration |
| Database storage | ✅ Complete | TimescaleDB |
| Error handling | ✅ Complete | Graceful degradation |
| Risk calculation | ✅ Complete | 4-level risk assessment |
| Recommendations | ✅ Complete | Clinical guidance |
| Explainability | ✅ Complete | Aggregation ready |
| API documentation | ✅ Complete | Swagger UI |
| Monitoring tools | ✅ Complete | Kafka UI, pgAdmin |
| Docker setup | ✅ Complete | One-command infrastructure |
| Test data | ✅ Complete | Mock services included |

---

## 🎉 You're Ready!

Your orchestrator is production-ready. Now:
1. Connect your real AI modules
2. Test with real medical data
3. Fine-tune weights and thresholds
4. Document results for thesis

**Good luck with your thesis defense!** 🎓🚀

---

**Questions or Issues?**
- Review documentation in this directory
- Check logs: `make logs`
- Test with mock services first
- Verify all services are running: `make status`
