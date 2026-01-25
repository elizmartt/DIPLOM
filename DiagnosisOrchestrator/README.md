# Diagnosis Service Orchestrator

**Multi-Modal Medical Diagnostic System - Orchestration Layer**

A C# (.NET 8) microservice that combines outputs from three AI modules (Imaging, Clinical Symptoms, Laboratory Results) using weighted ensemble logic to provide unified diagnosis with confidence scores.

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        API Gateway (C#)                          │
│                   (HTTP/HTTPS Endpoints)                         │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Apache Kafka Message Broker                    │
│              (diagnosis-requests / diagnosis-results)            │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│              Diagnosis Orchestrator Service (C#)                 │
│                                                                   │
│  ┌──────────────────────────────────────────────────────┐       │
│  │          Ensemble Logic Engine                       │       │
│  │  • Weighted Voting (default)                         │       │
│  │  • Confidence-Weighted Voting                        │       │
│  │  • Majority Voting                                   │       │
│  └──────────────────────────────────────────────────────┘       │
│                                                                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Module 1   │  │   Module 2   │  │   Module 3   │          │
│  │   Imaging    │  │   Clinical   │  │  Laboratory  │          │
│  │  Weight: 40% │  │  Weight: 30% │  │  Weight: 30% │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
└─────────┼──────────────────┼──────────────────┼─────────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ Imaging Service │ │Clinical Service │ │  Lab Service    │
│   (Python)      │ │   (Python)      │ │   (Python)      │
│   ResNet18      │ │Logistic Reg.    │ │ Random Forest   │
│   CT/MRI        │ │  Symptoms       │ │ Tumor Markers   │
└─────────────────┘ └─────────────────┘ └─────────────────┘
          │                  │                  │
          └──────────────────┴──────────────────┘
                             │
                             ▼
                   ┌──────────────────┐
                   │   TimescaleDB    │
                   │  (PostgreSQL)    │
                   └──────────────────┘
```

## 🎯 Key Features

### 1. **Weighted Ensemble Logic**
- Configurable module weights (default: Imaging 40%, Clinical 30%, Laboratory 30%)
- Multiple ensemble strategies:
  - **Weighted Voting**: Fixed weights based on module reliability
  - **Confidence-Weighted**: Dynamic weights based on prediction confidence
  - **Majority Voting**: Each module gets equal weight

### 2. **Robust Error Handling**
- Graceful degradation: Works with minimum 2 of 3 modules
- Individual module failure doesn't crash the system
- Detailed error logging and reporting

### 3. **Async Processing via Kafka**
- Synchronous API endpoint for immediate results
- Asynchronous processing for high-volume scenarios
- Request tracking and status monitoring

### 4. **Comprehensive Diagnostics**
- Risk level calculation (Low, Moderate, High, Critical)
- Clinical recommendations based on diagnosis and confidence
- Module agreement tracking
- Explainability data aggregation

### 5. **TimescaleDB Integration**
- Time-series optimized storage
- Separate tables for each module's predictions
- Performance analytics views
- HIPAA-compliant data handling

## 📋 Prerequisites

- **.NET 8 SDK**
- **PostgreSQL with TimescaleDB extension**
- **Apache Kafka**
- **Python 3.10+** (for AI modules)
- **Docker** (optional, for containerization)

## 🚀 Quick Start

### 1. Database Setup

```bash
# Connect to PostgreSQL
psql -U postgres

# Create database
CREATE DATABASE medical_diagnosis;

# Connect to the database
\c medical_diagnosis

# Run the schema
\i Database/schema.sql
```

### 2. Kafka Setup

```bash
# Start Kafka using Docker
docker run -d \
  --name kafka \
  -p 9092:9092 \
  -e KAFKA_ZOOKEEPER_CONNECT=zookeeper:2181 \
  -e KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://localhost:9092 \
  confluentinc/cp-kafka:latest

# Create topics
kafka-topics --create --topic diagnosis-requests --bootstrap-server localhost:9092
kafka-topics --create --topic diagnosis-results --bootstrap-server localhost:9092
```

### 3. Configuration

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "TimescaleDB": "Host=localhost;Port=5432;Database=medical_diagnosis;Username=postgres;Password=your_password"
  },
  
  "ModuleEndpoints": {
    "ImagingServiceUrl": "http://localhost:5001/api/imaging",
    "ClinicalServiceUrl": "http://localhost:5002/api/clinical",
    "LaboratoryServiceUrl": "http://localhost:5003/api/laboratory"
  },
  
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  
  "OrchestratorOptions": {
    "ModuleWeights": {
      "Imaging": 0.40,
      "Clinical": 0.30,
      "Laboratory": 0.30
    },
    "ConfidenceThreshold": 0.70,
    "MinModulesRequired": 2,
    "EnsembleStrategy": "WeightedVoting"
  }
}
```

### 4. Build and Run

```bash
# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run the service
dotnet run
```

The service will start on `https://localhost:5000` (or configured port).

## 📡 API Endpoints

### Synchronous Diagnosis

**POST** `/api/diagnosis/sync`

Processes diagnosis immediately and returns result.

```json
{
  "diagnosisCaseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "patientId": "patient-123",
  "doctorId": "doctor-456",
  "imagingData": {
    "imagePath": "/path/to/ct_scan.dcm",
    "imagingType": "CT",
    "bodyRegion": "Chest"
  },
  "clinicalData": {
    "symptoms": {
      "cough": true,
      "fever": true,
      "chest_pain": false
    },
    "age": 45,
    "gender": "M"
  },
  "laboratoryData": {
    "tumorMarkers": {
      "CEA": 5.2,
      "NSE": 12.5,
      "CYFRA 21-1": 3.1
    }
  }
}
```

**Response:**
```json
{
  "diagnosisCaseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "finalDiagnosis": "Malignant",
  "overallConfidence": 0.85,
  "ensembleProbabilities": {
    "benign": 0.15,
    "malignant": 0.85
  },
  "contributingModules": ["imaging", "clinical", "laboratory"],
  "riskLevel": "High",
  "recommendations": [
    "Immediate consultation with oncology specialist recommended",
    "Consider biopsy for histological confirmation"
  ],
  "status": "Completed",
  "totalProcessingTimeMs": 2345.67,
  "timestamp": "2025-01-15T10:30:00Z"
}
```

### Asynchronous Diagnosis

**POST** `/api/diagnosis/async`

Queues diagnosis request for background processing.

**Response:**
```json
{
  "requestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Queued",
  "message": "Diagnosis request queued for processing",
  "statusUrl": "/api/diagnosis/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### Get Diagnosis Status

**GET** `/api/diagnosis/{id}`

Retrieves the status and result of a diagnosis.

### Health Check

**GET** `/api/diagnosis/health`

Returns service health status.

## 🧪 Testing

### Unit Tests Example

```csharp
[Fact]
public async Task WeightedVoting_ShouldCalculateCorrectly()
{
    // Arrange
    var predictions = new List<ModulePrediction>
    {
        new() { 
            ModuleName = "imaging", 
            Prediction = DiagnosisType.Malignant, 
            Confidence = 0.90,
            Probabilities = new() { ["malignant"] = 0.90, ["benign"] = 0.10 }
        },
        new() { 
            ModuleName = "clinical", 
            Prediction = DiagnosisType.Benign, 
            Confidence = 0.60,
            Probabilities = new() { ["malignant"] = 0.40, ["benign"] = 0.60 }
        }
    };

    // Act
    var result = orchestrator.OrchestrateAsync(request);

    // Assert
    Assert.Equal(DiagnosisType.Malignant, result.FinalDiagnosis);
}
```

### Integration Test with curl

```bash
# Test synchronous diagnosis
curl -X POST https://localhost:5000/api/diagnosis/sync \
  -H "Content-Type: application/json" \
  -d @sample_request.json

# Test asynchronous diagnosis
curl -X POST https://localhost:5000/api/diagnosis/async \
  -H "Content-Type: application/json" \
  -d @sample_request.json

# Check status
curl https://localhost:5000/api/diagnosis/{case-id}
```

## 🔍 Ensemble Strategies Explained

### 1. Weighted Voting (Default)
```
Final Probability = Σ (Module_Weight × Module_Probability)

Example:
Imaging (40%): Malignant = 0.90
Clinical (30%): Malignant = 0.60
Laboratory (30%): Malignant = 0.80

Final = (0.40 × 0.90) + (0.30 × 0.60) + (0.30 × 0.80)
      = 0.36 + 0.18 + 0.24 = 0.78 (78% confidence)
```

### 2. Confidence-Weighted Voting
```
Adjusted_Weight = Base_Weight × Module_Confidence

Example:
Imaging (40%) with confidence 0.95: Effective weight = 0.38
Clinical (30%) with confidence 0.60: Effective weight = 0.18
Laboratory (30%) with confidence 0.85: Effective weight = 0.255
```

### 3. Majority Voting
```
Simple majority rule: Each module gets equal vote
2/3 = Malignant → Final diagnosis = Malignant (66.7% confidence)
```

## 📊 Performance Monitoring

### Database Queries

```sql
-- Overall performance
SELECT * FROM diagnosis_performance_stats 
WHERE date >= NOW() - INTERVAL '7 days';

-- Module comparison
SELECT * FROM module_performance_comparison;

-- Recent diagnoses with all predictions
SELECT 
    udr.diagnosis_case_id,
    udr.final_diagnosis,
    udr.overall_confidence,
    ir.confidence as imaging_confidence,
    cr.confidence as clinical_confidence,
    lr.confidence as laboratory_confidence
FROM unified_diagnosis_results udr
LEFT JOIN imaging_results ir ON udr.diagnosis_case_id = ir.diagnosis_case_id
LEFT JOIN clinical_results cr ON udr.diagnosis_case_id = cr.diagnosis_case_id
LEFT JOIN laboratory_results lr ON udr.diagnosis_case_id = lr.diagnosis_case_id
WHERE udr.created_at >= NOW() - INTERVAL '1 day'
ORDER BY udr.created_at DESC;
```

## 🐳 Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DiagnosisOrchestrator.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiagnosisOrchestrator.dll"]
```

```bash
# Build image
docker build -t diagnosis-orchestrator:latest .

# Run container
docker run -d \
  -p 5000:80 \
  -e ConnectionStrings__TimescaleDB="Host=db;..." \
  -e Kafka__BootstrapServers="kafka:9092" \
  diagnosis-orchestrator:latest
```

## 🔒 Security Considerations

1. **HIPAA Compliance**: All patient data is anonymized
2. **HTTPS Only**: Enforce TLS in production
3. **API Authentication**: Implement OAuth2/JWT (not included in base version)
4. **Database Encryption**: Enable at-rest encryption in TimescaleDB
5. **Audit Logging**: All diagnosis requests are logged with timestamps

## 📈 Scalability

- **Horizontal Scaling**: Deploy multiple instances behind load balancer
- **Kafka Partitioning**: Distribute load across partitions
- **Database Sharding**: Use TimescaleDB's distributed hypertables
- **Caching**: Add Redis for frequently accessed diagnoses

## 🛠️ Troubleshooting

### Module Connection Failures
```csharp
// Check module endpoints in logs
// Ensure Python services are running
// Verify network connectivity
```

### Kafka Connection Issues
```bash
# Verify Kafka is running
docker ps | grep kafka

# Check topic exists
kafka-topics --list --bootstrap-server localhost:9092
```

### Database Connection
```bash
# Test PostgreSQL connection
psql -h localhost -U postgres -d medical_diagnosis

# Verify TimescaleDB extension
SELECT * FROM timescaledb_information.hypertables;
```

## 📝 Contributing

For thesis development, follow these guidelines:
1. Branch naming: `feature/module-name`
2. Commit messages: Follow conventional commits
3. Code review: All changes require review before merge

## 📄 License

Academic use only - Part of Master's thesis project

## 👥 Contact

**Thesis Author**: Eliza
**Institution**: [Your University]
**Supervisor**: [Supervisor Name]

---

**Last Updated**: January 2025
**Version**: 1.0.0
