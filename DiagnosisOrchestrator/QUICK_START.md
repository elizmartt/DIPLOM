# 🚀 Quick Start Guide

Get the Diagnosis Orchestrator running in 5 minutes!

## Prerequisites

Make sure you have installed:
- Docker Desktop (or Docker Engine + Docker Compose)
- .NET 8 SDK
- Git

## Step 1: Clone & Navigate

```bash
cd /path/to/DiagnosisOrchestrator
```

## Step 2: Start Infrastructure (60 seconds)

```bash
# Start all infrastructure services
docker-compose up -d

# Check all services are running
docker-compose ps
```

You should see these services running:
- ✅ medical-timescaledb (Port 5432)
- ✅ medical-kafka (Port 9092)
- ✅ medical-zookeeper (Port 2181)
- ✅ medical-redis (Port 6379)
- ✅ medical-kafka-ui (Port 8080)
- ✅ medical-pgadmin (Port 5050)

## Step 3: Verify Database Setup

```bash
# Check if schema was created
docker exec -it medical-timescaledb psql -U postgres -d medical_diagnosis -c "\dt"
```

You should see these tables:
- unified_diagnosis_results
- imaging_results
- clinical_results
- laboratory_results

## Step 4: Update Configuration

Edit `appsettings.json` if needed (default values should work):

```json
{
  "ConnectionStrings": {
    "TimescaleDB": "Host=localhost;Port=5432;Database=medical_diagnosis;Username=postgres;Password=postgres123"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  }
}
```

## Step 5: Build & Run Orchestrator

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

The orchestrator will start on:
- 🌐 HTTP: http://localhost:5000
- 🔒 HTTPS: https://localhost:5001
- 📚 Swagger UI: http://localhost:5000

## Step 6: Test the System

### Test Health Endpoint
```bash
curl http://localhost:5000/api/diagnosis/health
```

Expected response:
```json
{
  "status": "Healthy",
  "timestamp": "2025-01-15T10:30:00Z",
  "service": "Diagnosis Orchestrator"
}
```

### Test with Mock Data (Without AI Modules)

At this point, the orchestrator is ready, but you need to:
1. Start your Python AI modules (Imaging, Clinical, Laboratory)
2. Or use mock services for testing

## Step 7: Start Python AI Modules

In separate terminals:

```bash
# Terminal 1 - Imaging Module
cd /path/to/imaging-module
python imaging_service.py

# Terminal 2 - Clinical Module
cd /path/to/clinical-module
python clinical_service.py

# Terminal 3 - Laboratory Module
cd /path/to/laboratory-module
python laboratory_service.py
```

## Step 8: Run Full Diagnosis

```bash
# Create a test request
cat > test_request.json << EOF
{
  "diagnosisCaseId": "$(uuidgen)",
  "patientId": "patient-test-001",
  "doctorId": "doctor-test-001",
  "imagingData": {
    "imagePath": "/data/test_ct_scan.dcm",
    "imagingType": "CT",
    "bodyRegion": "Chest"
  },
  "clinicalData": {
    "symptoms": {
      "cough": true,
      "fever": true,
      "weight_loss": true
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
EOF

# Send synchronous diagnosis request
curl -X POST http://localhost:5000/api/diagnosis/sync \
  -H "Content-Type: application/json" \
  -d @test_request.json | jq
```

## Step 9: Monitor the System

### View Kafka Messages
Open Kafka UI: http://localhost:8080
- Topic: `diagnosis-requests`
- Topic: `diagnosis-results`

### View Database Results
Open pgAdmin: http://localhost:5050
- Email: admin@medical-diagnosis.com
- Password: admin123

Connect to server:
- Host: timescaledb
- Port: 5432
- Database: medical_diagnosis
- Username: postgres
- Password: postgres123

Query recent diagnoses:
```sql
SELECT 
    diagnosis_case_id,
    final_diagnosis,
    overall_confidence,
    risk_level,
    created_at
FROM unified_diagnosis_results
ORDER BY created_at DESC
LIMIT 10;
```

## Troubleshooting

### Problem: "Connection refused" when calling AI modules
**Solution**: Make sure all Python services are running on correct ports:
- Imaging: http://localhost:5001
- Clinical: http://localhost:5002
- Laboratory: http://localhost:5003

### Problem: Kafka connection failed
**Solution**: 
```bash
# Restart Kafka
docker-compose restart kafka

# Check logs
docker-compose logs kafka
```

### Problem: Database connection failed
**Solution**:
```bash
# Check TimescaleDB is running
docker ps | grep timescale

# Test connection
docker exec -it medical-timescaledb psql -U postgres -l
```

### Problem: Schema not created
**Solution**:
```bash
# Manually run schema
docker exec -i medical-timescaledb psql -U postgres -d medical_diagnosis < Database/schema.sql
```

## Stopping Everything

```bash
# Stop orchestrator
Ctrl+C in the terminal running dotnet run

# Stop Python modules
Ctrl+C in each Python service terminal

# Stop infrastructure
docker-compose down

# Stop and remove volumes (⚠️ DELETES ALL DATA)
docker-compose down -v
```

## Next Steps

1. ✅ Infrastructure is running
2. ✅ Orchestrator is running
3. 🎯 Now implement/connect your Python AI modules
4. 🧪 Test with real medical data
5. 📊 Monitor performance metrics
6. 🚀 Deploy to production environment

## Useful Commands

```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f timescaledb
docker-compose logs -f kafka

# Restart a service
docker-compose restart kafka

# Check service health
docker-compose ps

# Access database directly
docker exec -it medical-timescaledb psql -U postgres -d medical_diagnosis

# View Kafka topics
docker exec -it medical-kafka kafka-topics --list --bootstrap-server localhost:9092

# View Kafka messages
docker exec -it medical-kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic diagnosis-requests \
  --from-beginning
```

## Architecture Recap

```
┌─────────────────────────────────────────┐
│   Diagnosis Orchestrator (Port 5000)   │  ← You are here!
└────────────┬────────────────────────────┘
             │
             ├── Kafka (Port 9092)
             ├── TimescaleDB (Port 5432)
             ├── Redis (Port 6379)
             │
             └── Calls these modules:
                 ├── Imaging Service (Port 5001)    ← Need to run
                 ├── Clinical Service (Port 5002)   ← Need to run
                 └── Laboratory Service (Port 5003) ← Need to run
```

## Development Workflow

1. Make code changes in C#
2. Rebuild: `dotnet build`
3. Test: `dotnet test` (if you add tests)
4. Run: `dotnet run`
5. Test with curl or Postman
6. Check logs and database
7. Iterate!

---

**Ready to diagnose!** 🏥🤖

For more details, see the main [README.md](README.md) and [PythonModuleInterface.md](PythonModuleInterface.md).
