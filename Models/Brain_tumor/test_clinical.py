"""
Direct Kafka test for Brain Clinical Service
Bypasses API Gateway completely
"""
from kafka import KafkaProducer, KafkaConsumer
import json
import uuid
import time
import threading

# Configuration
KAFKA_SERVERS = ['localhost:9092']
INPUT_TOPIC = 'clinical-requests'
OUTPUT_TOPIC = 'clinical-results'

producer = KafkaProducer(
    bootstrap_servers=KAFKA_SERVERS,
    value_serializer=lambda v: json.dumps(v).encode('utf-8')
)

consumer = KafkaConsumer(
    OUTPUT_TOPIC,
    bootstrap_servers=KAFKA_SERVERS,
    auto_offset_reset='latest',
    enable_auto_commit=True,
    group_id='test-group-' + str(uuid.uuid4()),
    value_deserializer=lambda x: json.loads(x.decode('utf-8'))
)

print("=" * 60)
print("DIRECT KAFKA TEST - BRAIN CLINICAL SERVICE")
print("=" * 60)

def listen_for_response():

    print("\nListening for response on topic:", OUTPUT_TOPIC)
    try:
        for message in consumer:
            result = message.value
            print("\n" + "=" * 60)
            print("RECEIVED RESPONSE FROM CLINICAL SERVICE:")
            print("=" * 60)
            print(json.dumps(result, indent=2))
            print("=" * 60)
            if result.get('Success'):
                print(" SUCCESS! Prediction:", result.get('Prediction'))
                print("   Confidence:", result.get('Confidence'))
            else:
                print(" ERROR:", result.get('ErrorMessage'))
            print("=" * 60)
            break
    except Exception as e:
        print(f" Error receiving response: {e}")


listener = threading.Thread(target=listen_for_response, daemon=True)
listener.start()
time.sleep(2)


print("\n TEST 1: Sending CORRECT format (symptoms as dictionary)")
print("-" * 60)

request_id = str(uuid.uuid4())
case_id = "c201acd4-b9b2-4d60-a3e4-f15dbf72f8ea"

test_message_correct = {
    'requestId': request_id,
    'diagnosisCaseId': case_id,
    'symptoms': {
        'age': 45,
        'gender': 1,
        'headache': 1,
        'nausea': 1,
        'vision_problems': 0,
        'dizziness': 1,
        'seizures': 0,
        'memory_loss': 0,
        'weakness': 0,
        'speech_difficulty': 0
    }
}

print("Message to send:")
print(json.dumps(test_message_correct, indent=2))

producer.send(INPUT_TOPIC, test_message_correct)
producer.flush()

print("\n✓ Message sent to topic:", INPUT_TOPIC)
print(" Waiting for response (10 seconds)...")
time.sleep(10)


print("\n\n TEST 2: Sending WRONG format (symptoms as array)")
print("-" * 60)

request_id_2 = str(uuid.uuid4())

test_message_wrong = {
    'requestId': request_id_2,
    'diagnosisCaseId': case_id,
    'symptoms': ["headache", "nausea", "dizziness"],  # Array instead of dict
    'bloodPressure': "120/80",
    'heartRate': 72,
    'temperature': 36.8
}

print("Message to send:")
print(json.dumps(test_message_wrong, indent=2))

producer.send(INPUT_TOPIC, test_message_wrong)
producer.flush()

print("\n✓ Message sent to topic:", INPUT_TOPIC)
print("⏳ Waiting for response (10 seconds)...")
time.sleep(10)


print("\n\n TEST 3: Sending EMPTY symptoms")
print("-" * 60)

request_id_3 = str(uuid.uuid4())

test_message_empty = {
    'requestId': request_id_3,
    'diagnosisCaseId': case_id,
    'symptoms': {}
}

print("Message to send:")
print(json.dumps(test_message_empty, indent=2))

producer.send(INPUT_TOPIC, test_message_empty)
producer.flush()

print("\n Message sent to topic:", INPUT_TOPIC)
print(" Waiting for response (10 seconds)...")
time.sleep(10)

print("\n" + "=" * 60)
print("ALL TESTS COMPLETED!")
print("=" * 60)
print("\n Now check your Clinical Service console logs!")
print("   Look for which test caused 'list index out of range'")
print("=" * 60)


time.sleep(5)