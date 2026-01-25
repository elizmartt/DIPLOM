.PHONY: help setup start stop restart clean build run test health db-connect kafka-topics logs

# Default target
help:
	@echo "📋 Diagnosis Orchestrator - Available Commands:"
	@echo ""
	@echo "🚀 Setup & Start:"
	@echo "  make setup          - Initial setup (start infrastructure + build)"
	@echo "  make start          - Start all infrastructure services"
	@echo "  make run            - Run the orchestrator"
	@echo "  make build          - Build the .NET project"
	@echo ""
	@echo "🛠️  Management:"
	@echo "  make stop           - Stop all services"
	@echo "  make restart        - Restart all services"
	@echo "  make clean          - Stop and remove all volumes (⚠️  DELETES DATA)"
	@echo "  make logs           - View all logs"
	@echo ""
	@echo "🧪 Testing:"
	@echo "  make test           - Run tests (if available)"
	@echo "  make health         - Check orchestrator health"
	@echo "  make test-sync      - Test synchronous diagnosis"
	@echo ""
	@echo "🔍 Monitoring:"
	@echo "  make db-connect     - Connect to TimescaleDB"
	@echo "  make db-query       - View recent diagnoses"
	@echo "  make kafka-topics   - List Kafka topics"
	@echo "  make kafka-ui       - Open Kafka UI in browser"
	@echo "  make pgadmin        - Open pgAdmin in browser"
	@echo ""
	@echo "📊 Status:"
	@echo "  make status         - Show status of all services"

# Initial setup
setup: start build
	@echo "✅ Setup complete! Infrastructure is running and project is built."
	@echo "📝 Next steps:"
	@echo "   1. Start Python AI modules on ports 5001, 5002, 5003"
	@echo "   2. Run: make run"
	@echo "   3. Test: make health"

# Start infrastructure
start:
	@echo "🚀 Starting infrastructure services..."
	docker-compose up -d
	@echo "⏳ Waiting for services to be healthy..."
	@sleep 10
	@docker-compose ps
	@echo "✅ Infrastructure started!"

# Stop all services
stop:
	@echo "🛑 Stopping all services..."
	docker-compose down
	@echo "✅ All services stopped!"

# Restart services
restart: stop start

# Clean (remove volumes)
clean:
	@echo "⚠️  WARNING: This will delete all data!"
	@read -p "Are you sure? (y/N): " confirm && [ "$$confirm" = "y" ] || exit 1
	@echo "🧹 Cleaning up..."
	docker-compose down -v
	@echo "✅ Cleanup complete!"

# Build .NET project
build:
	@echo "🔨 Building .NET project..."
	dotnet restore
	dotnet build
	@echo "✅ Build complete!"

# Run orchestrator
run:
	@echo "🚀 Starting Diagnosis Orchestrator..."
	dotnet run

# Run tests
test:
	@echo "🧪 Running tests..."
	dotnet test

# Check health
health:
	@echo "🏥 Checking orchestrator health..."
	@curl -s http://localhost:5000/api/diagnosis/health | jq || echo "❌ Orchestrator not responding"

# Test synchronous diagnosis
test-sync:
	@echo "🧪 Testing synchronous diagnosis..."
	@curl -X POST http://localhost:5000/api/diagnosis/sync \
		-H "Content-Type: application/json" \
		-d '{"diagnosisCaseId":"test-$(shell date +%s)","patientId":"patient-001","doctorId":"doctor-001","imagingData":{"imagePath":"/test.dcm","imagingType":"CT"},"clinicalData":{"symptoms":{"cough":true},"age":45,"gender":"M"},"laboratoryData":{"tumorMarkers":{"CEA":5.2}}}' \
		| jq || echo "❌ Request failed"

# Connect to database
db-connect:
	@echo "🔌 Connecting to TimescaleDB..."
	docker exec -it medical-timescaledb psql -U postgres -d medical_diagnosis

# Query recent diagnoses
db-query:
	@echo "📊 Recent diagnoses:"
	@docker exec -it medical-timescaledb psql -U postgres -d medical_diagnosis \
		-c "SELECT diagnosis_case_id, final_diagnosis, overall_confidence, risk_level, created_at FROM unified_diagnosis_results ORDER BY created_at DESC LIMIT 10;"

# List Kafka topics
kafka-topics:
	@echo "📋 Kafka topics:"
	@docker exec medical-kafka kafka-topics --list --bootstrap-server localhost:9092

# Open Kafka UI
kafka-ui:
	@echo "🌐 Opening Kafka UI..."
	@open http://localhost:8080 || xdg-open http://localhost:8080 || echo "Open http://localhost:8080 in your browser"

# Open pgAdmin
pgadmin:
	@echo "🌐 Opening pgAdmin..."
	@open http://localhost:5050 || xdg-open http://localhost:5050 || echo "Open http://localhost:5050 in your browser"

# View logs
logs:
	@echo "📜 Viewing logs (Ctrl+C to exit)..."
	docker-compose logs -f

# Show status
status:
	@echo "📊 Service Status:"
	@docker-compose ps
	@echo ""
	@echo "🏥 Orchestrator Health:"
	@curl -s http://localhost:5000/api/diagnosis/health 2>/dev/null | jq || echo "Not running"

# Development helpers
watch:
	@echo "👀 Watching for changes and auto-restarting..."
	dotnet watch run

swagger:
	@echo "📚 Opening Swagger UI..."
	@open http://localhost:5000 || xdg-open http://localhost:5000 || echo "Open http://localhost:5000 in your browser"

# Database management
db-reset:
	@echo "⚠️  Resetting database..."
	@docker exec -i medical-timescaledb psql -U postgres -d medical_diagnosis -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"
	@docker exec -i medical-timescaledb psql -U postgres -d medical_diagnosis < Database/schema.sql
	@echo "✅ Database reset complete!"

db-backup:
	@echo "💾 Backing up database..."
	@docker exec medical-timescaledb pg_dump -U postgres medical_diagnosis > backup_$(shell date +%Y%m%d_%H%M%S).sql
	@echo "✅ Backup created!"

# Kafka management
kafka-create-topics:
	@echo "📝 Creating Kafka topics..."
	@docker exec medical-kafka kafka-topics --create --topic diagnosis-requests --bootstrap-server localhost:9092 --if-not-exists
	@docker exec medical-kafka kafka-topics --create --topic diagnosis-results --bootstrap-server localhost:9092 --if-not-exists
	@echo "✅ Topics created!"

kafka-consume-requests:
	@echo "📨 Consuming diagnosis requests (Ctrl+C to exit)..."
	@docker exec -it medical-kafka kafka-console-consumer \
		--bootstrap-server localhost:9092 \
		--topic diagnosis-requests \
		--from-beginning

kafka-consume-results:
	@echo "📤 Consuming diagnosis results (Ctrl+C to exit)..."
	@docker exec -it medical-kafka kafka-console-consumer \
		--bootstrap-server localhost:9092 \
		--topic diagnosis-results \
		--from-beginning
