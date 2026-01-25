# Sample Test Data for Diagnosis Orchestrator

## Test Case 1: High Confidence Malignant

```json
{
  "diagnosisCaseId": "test-case-malignant-001",
  "patientId": "patient-001",
  "doctorId": "doctor-001",
  "imagingData": {
    "imagePath": "/data/ct_scans/suspicious_nodule_001.dcm",
    "imagingType": "CT",
    "bodyRegion": "Chest",
    "metadata": {
      "sliceThickness": "5mm",
      "contrast": true
    }
  },
  "clinicalData": {
    "symptoms": {
      "cough": true,
      "fever": false,
      "chest_pain": true,
      "weight_loss": true,
      "fatigue": true,
      "dyspnea": true,
      "hemoptysis": true
    },
    "age": 62,
    "gender": "M",
    "medicalHistory": [
      "smoking_history_40_pack_years",
      "copd"
    ],
    "vitalSigns": {
      "temperature": 37.2,
      "blood_pressure": "145/92",
      "heart_rate": 88,
      "respiratory_rate": 22
    }
  },
  "laboratoryData": {
    "tumorMarkers": {
      "CEA": 12.5,
      "NSE": 25.3,
      "CYFRA 21-1": 8.9,
      "SCC-Ag": 4.2
    },
    "bloodWork": {
      "WBC": 9200,
      "RBC": 4.2,
      "hemoglobin": 12.1,
      "platelets": 235000
    },
    "additionalTests": {
      "LDH": 580,
      "alkaline_phosphatase": 145
    }
  }
}
```

Expected Result:
- Final Diagnosis: Malignant
- Confidence: > 0.85
- Risk Level: High or Critical

---

## Test Case 2: High Confidence Benign

```json
{
  "diagnosisCaseId": "test-case-benign-001",
  "patientId": "patient-002",
  "doctorId": "doctor-001",
  "imagingData": {
    "imagePath": "/data/ct_scans/calcified_granuloma_001.dcm",
    "imagingType": "CT",
    "bodyRegion": "Chest"
  },
  "clinicalData": {
    "symptoms": {
      "cough": false,
      "fever": false,
      "chest_pain": false,
      "weight_loss": false,
      "fatigue": false
    },
    "age": 35,
    "gender": "F",
    "medicalHistory": [],
    "vitalSigns": {
      "temperature": 36.8,
      "blood_pressure": "118/75",
      "heart_rate": 72
    }
  },
  "laboratoryData": {
    "tumorMarkers": {
      "CEA": 1.2,
      "NSE": 8.5,
      "CYFRA 21-1": 1.1,
      "SCC-Ag": 0.5
    },
    "bloodWork": {
      "WBC": 7500,
      "RBC": 4.8,
      "hemoglobin": 14.2,
      "platelets": 280000
    }
  }
}
```

Expected Result:
- Final Diagnosis: Benign
- Confidence: > 0.85
- Risk Level: Low

---

## Test Case 3: Inconclusive (Low Confidence)

```json
{
  "diagnosisCaseId": "test-case-inconclusive-001",
  "patientId": "patient-003",
  "doctorId": "doctor-002",
  "imagingData": {
    "imagePath": "/data/ct_scans/indeterminate_nodule_001.dcm",
    "imagingType": "CT",
    "bodyRegion": "Chest"
  },
  "clinicalData": {
    "symptoms": {
      "cough": true,
      "fever": false,
      "chest_pain": false,
      "weight_loss": false,
      "fatigue": true
    },
    "age": 45,
    "gender": "M",
    "medicalHistory": []
  },
  "laboratoryData": {
    "tumorMarkers": {
      "CEA": 4.8,
      "NSE": 11.5,
      "CYFRA 21-1": 2.9,
      "SCC-Ag": 1.2
    }
  }
}
```

Expected Result:
- Final Diagnosis: Inconclusive
- Confidence: < 0.70
- Risk Level: Moderate
- Recommendations include additional testing

---

## Test Case 4: Missing Module (Only 2 Modules)

```json
{
  "diagnosisCaseId": "test-case-partial-001",
  "patientId": "patient-004",
  "doctorId": "doctor-001",
  "imagingData": {
    "imagePath": "/data/ct_scans/mass_001.dcm",
    "imagingType": "CT",
    "bodyRegion": "Chest"
  },
  "clinicalData": {
    "symptoms": {
      "cough": true,
      "weight_loss": true
    },
    "age": 58,
    "gender": "M"
  }
}
```

Expected Result:
- Should work with just imaging and clinical modules
- Laboratory module will be skipped
- Status: Completed or PartialSuccess

---

## Test Case 5: Async Processing

```json
{
  "diagnosisCaseId": "test-case-async-001",
  "patientId": "patient-005",
  "doctorId": "doctor-003",
  "imagingData": {
    "imagePath": "/data/ct_scans/nodule_002.dcm",
    "imagingType": "CT",
    "bodyRegion": "Chest"
  },
  "clinicalData": {
    "symptoms": {
      "cough": true,
      "fever": true
    },
    "age": 50,
    "gender": "F"
  },
  "laboratoryData": {
    "tumorMarkers": {
      "CEA": 6.2,
      "NSE": 15.0
    }
  }
}
```

Send to: POST /api/diagnosis/async
Then poll: GET /api/diagnosis/{id}

---

## Test Case 6: Module Disagreement

This case tests when modules give conflicting predictions:

```json
{
  "diagnosisCaseId": "test-case-disagreement-001",
  "patientId": "patient-006",
  "doctorId": "doctor-001",
  "imagingData": {
    "imagePath": "/data/ct_scans/ambiguous_001.dcm",
    "imagingType": "CT",
    "bodyRegion": "Chest"
  },
  "clinicalData": {
    "symptoms": {
      "cough": false,
      "fever": false,
      "weight_loss": false
    },
    "age": 40,
    "gender": "M"
  },
  "laboratoryData": {
    "tumorMarkers": {
      "CEA": 8.5,
      "NSE": 18.0,
      "CYFRA 21-1": 5.2
    }
  }
}
```

Expected Result:
- Recommendations should include warning about module disagreement
- Manual review recommended

---

## Test Case 7: Minimal Data

```json
{
  "diagnosisCaseId": "test-case-minimal-001",
  "patientId": "patient-007",
  "doctorId": "doctor-001",
  "clinicalData": {
    "symptoms": {
      "cough": true
    },
    "age": 45,
    "gender": "M"
  },
  "laboratoryData": {
    "tumorMarkers": {
      "CEA": 3.0
    }
  }
}
```

Expected Result:
- Should handle minimal data gracefully
- May return Inconclusive if confidence is low

---

## Curl Commands for Testing

### Test Case 1: Synchronous
```bash
curl -X POST http://localhost:5000/api/diagnosis/sync \
  -H "Content-Type: application/json" \
  -d '{
    "diagnosisCaseId": "test-001",
    "patientId": "patient-001",
    "doctorId": "doctor-001",
    "imagingData": {
      "imagePath": "/data/test.dcm",
      "imagingType": "CT"
    },
    "clinicalData": {
      "symptoms": {"cough": true, "weight_loss": true},
      "age": 62,
      "gender": "M"
    },
    "laboratoryData": {
      "tumorMarkers": {"CEA": 12.5, "NSE": 25.3}
    }
  }' | jq
```

### Test Case 2: Asynchronous
```bash
# Submit request
CASE_ID=$(curl -X POST http://localhost:5000/api/diagnosis/async \
  -H "Content-Type: application/json" \
  -d '{
    "diagnosisCaseId": "test-async-001",
    "patientId": "patient-002",
    "doctorId": "doctor-001",
    "imagingData": {"imagePath": "/data/test.dcm", "imagingType": "CT"},
    "clinicalData": {"symptoms": {"cough": true}, "age": 45, "gender": "M"},
    "laboratoryData": {"tumorMarkers": {"CEA": 5.2}}
  }' | jq -r '.requestId')

# Wait a bit
sleep 3

# Check status
curl http://localhost:5000/api/diagnosis/$CASE_ID | jq
```

---

## Performance Test

Run multiple requests to test concurrent processing:

```bash
for i in {1..10}; do
  curl -X POST http://localhost:5000/api/diagnosis/async \
    -H "Content-Type: application/json" \
    -d "{
      \"diagnosisCaseId\": \"perf-test-$i\",
      \"patientId\": \"patient-$i\",
      \"doctorId\": \"doctor-001\",
      \"clinicalData\": {
        \"symptoms\": {\"cough\": true},
        \"age\": $((30 + i * 3)),
        \"gender\": \"M\"
      },
      \"laboratoryData\": {
        \"tumorMarkers\": {\"CEA\": $((3 + i))}
      }
    }" &
done

wait
echo "All requests submitted!"
```

---

## Validation Checklist

When testing, verify:

- ✅ All three modules are called
- ✅ Weights are applied correctly
- ✅ Confidence scores are reasonable (0-1)
- ✅ Risk level matches diagnosis and confidence
- ✅ Recommendations are appropriate
- ✅ Results are saved to database
- ✅ Kafka messages are published
- ✅ Processing time is recorded
- ✅ Error handling works when modules fail
- ✅ Explainability data is aggregated
