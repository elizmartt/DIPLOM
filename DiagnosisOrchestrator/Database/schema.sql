-- TimescaleDB Schema for Diagnosis Orchestrator
-- Database: medical_diagnosis

-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- ============================================
-- Unified Diagnosis Results Table
-- ============================================
CREATE TABLE IF NOT EXISTS unified_diagnosis_results (
    id SERIAL PRIMARY KEY,
    diagnosis_case_id UUID UNIQUE NOT NULL,
    final_diagnosis VARCHAR(50) NOT NULL, -- benign, malignant, inconclusive
    overall_confidence DOUBLE PRECISION NOT NULL,
    ensemble_probabilities JSONB NOT NULL,
    contributing_modules TEXT[] NOT NULL,
    risk_level VARCHAR(50) NOT NULL, -- low, moderate, high, critical
    recommendations TEXT[] NOT NULL,
    explainability_summary JSONB NOT NULL,
    status VARCHAR(50) NOT NULL, -- pending, in_progress, completed, failed, partial_success
    total_processing_time_ms DOUBLE PRECISION NOT NULL,
    error_details TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create hypertable for time-series optimization
SELECT create_hypertable('unified_diagnosis_results', 'created_at', if_not_exists => TRUE);

-- Indexes for efficient querying
CREATE INDEX IF NOT EXISTS idx_unified_diagnosis_case_id 
    ON unified_diagnosis_results(diagnosis_case_id);

CREATE INDEX IF NOT EXISTS idx_unified_final_diagnosis 
    ON unified_diagnosis_results(final_diagnosis);

CREATE INDEX IF NOT EXISTS idx_unified_status 
    ON unified_diagnosis_results(status);

CREATE INDEX IF NOT EXISTS idx_unified_risk_level 
    ON unified_diagnosis_results(risk_level);

CREATE INDEX IF NOT EXISTS idx_unified_created_at 
    ON unified_diagnosis_results(created_at DESC);

-- ============================================
-- Imaging Module Results Table
-- ============================================
CREATE TABLE IF NOT EXISTS imaging_results (
    id SERIAL PRIMARY KEY,
    diagnosis_case_id UUID NOT NULL,
    prediction VARCHAR(50) NOT NULL,
    confidence DOUBLE PRECISION NOT NULL,
    probabilities JSONB NOT NULL,
    processing_time_ms DOUBLE PRECISION NOT NULL,
    explainability_data JSONB,
    success BOOLEAN NOT NULL DEFAULT TRUE,
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    FOREIGN KEY (diagnosis_case_id) REFERENCES unified_diagnosis_results(diagnosis_case_id) ON DELETE CASCADE
);

-- Create hypertable
SELECT create_hypertable('imaging_results', 'created_at', if_not_exists => TRUE);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_imaging_diagnosis_case_id 
    ON imaging_results(diagnosis_case_id);

CREATE INDEX IF NOT EXISTS idx_imaging_prediction 
    ON imaging_results(prediction);

CREATE INDEX IF NOT EXISTS idx_imaging_created_at 
    ON imaging_results(created_at DESC);

-- ============================================
-- Clinical Symptoms Module Results Table
-- ============================================
CREATE TABLE IF NOT EXISTS clinical_results (
    id SERIAL PRIMARY KEY,
    diagnosis_case_id UUID NOT NULL,
    prediction VARCHAR(50) NOT NULL,
    confidence DOUBLE PRECISION NOT NULL,
    probabilities JSONB NOT NULL,
    processing_time_ms DOUBLE PRECISION NOT NULL,
    explainability_data JSONB,
    success BOOLEAN NOT NULL DEFAULT TRUE,
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    FOREIGN KEY (diagnosis_case_id) REFERENCES unified_diagnosis_results(diagnosis_case_id) ON DELETE CASCADE
);

-- Create hypertable
SELECT create_hypertable('clinical_results', 'created_at', if_not_exists => TRUE);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_clinical_diagnosis_case_id 
    ON clinical_results(diagnosis_case_id);

CREATE INDEX IF NOT EXISTS idx_clinical_prediction 
    ON clinical_results(prediction);

CREATE INDEX IF NOT EXISTS idx_clinical_created_at 
    ON clinical_results(created_at DESC);

-- ============================================
-- Laboratory Results Module Table
-- ============================================
CREATE TABLE IF NOT EXISTS laboratory_results (
    id SERIAL PRIMARY KEY,
    diagnosis_case_id UUID NOT NULL,
    prediction VARCHAR(50) NOT NULL,
    confidence DOUBLE PRECISION NOT NULL,
    probabilities JSONB NOT NULL,
    processing_time_ms DOUBLE PRECISION NOT NULL,
    explainability_data JSONB,
    success BOOLEAN NOT NULL DEFAULT TRUE,
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    FOREIGN KEY (diagnosis_case_id) REFERENCES unified_diagnosis_results(diagnosis_case_id) ON DELETE CASCADE
);

-- Create hypertable
SELECT create_hypertable('laboratory_results', 'created_at', if_not_exists => TRUE);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_laboratory_diagnosis_case_id 
    ON laboratory_results(diagnosis_case_id);

CREATE INDEX IF NOT EXISTS idx_laboratory_prediction 
    ON laboratory_results(prediction);

CREATE INDEX IF NOT EXISTS idx_laboratory_created_at 
    ON laboratory_results(created_at DESC);

-- ============================================
-- Performance Statistics View
-- ============================================
CREATE OR REPLACE VIEW diagnosis_performance_stats AS
SELECT 
    DATE_TRUNC('day', created_at) as date,
    final_diagnosis,
    COUNT(*) as total_cases,
    AVG(overall_confidence) as avg_confidence,
    AVG(total_processing_time_ms) as avg_processing_time_ms,
    STDDEV(overall_confidence) as stddev_confidence,
    MIN(overall_confidence) as min_confidence,
    MAX(overall_confidence) as max_confidence
FROM unified_diagnosis_results
WHERE status = 'completed'
GROUP BY DATE_TRUNC('day', created_at), final_diagnosis
ORDER BY date DESC, final_diagnosis;

-- ============================================
-- Module Performance Comparison View
-- ============================================
CREATE OR REPLACE VIEW module_performance_comparison AS
SELECT 
    'imaging' as module_name,
    COUNT(*) as total_predictions,
    AVG(confidence) as avg_confidence,
    AVG(processing_time_ms) as avg_processing_time_ms,
    SUM(CASE WHEN success THEN 1 ELSE 0 END)::FLOAT / COUNT(*) as success_rate
FROM imaging_results
WHERE created_at >= NOW() - INTERVAL '30 days'
UNION ALL
SELECT 
    'clinical' as module_name,
    COUNT(*) as total_predictions,
    AVG(confidence) as avg_confidence,
    AVG(processing_time_ms) as avg_processing_time_ms,
    SUM(CASE WHEN success THEN 1 ELSE 0 END)::FLOAT / COUNT(*) as success_rate
FROM clinical_results
WHERE created_at >= NOW() - INTERVAL '30 days'
UNION ALL
SELECT 
    'laboratory' as module_name,
    COUNT(*) as total_predictions,
    AVG(confidence) as avg_confidence,
    AVG(processing_time_ms) as avg_processing_time_ms,
    SUM(CASE WHEN success THEN 1 ELSE 0 END)::FLOAT / COUNT(*) as success_rate
FROM laboratory_results
WHERE created_at >= NOW() - INTERVAL '30 days';

-- ============================================
-- Updated At Trigger
-- ============================================
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_unified_diagnosis_updated_at 
    BEFORE UPDATE ON unified_diagnosis_results 
    FOR EACH ROW 
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- Retention Policy (Optional - for production)
-- ============================================
-- Keep diagnosis results for 7 years (regulatory requirement)
-- SELECT add_retention_policy('unified_diagnosis_results', INTERVAL '7 years');
-- SELECT add_retention_policy('imaging_results', INTERVAL '7 years');
-- SELECT add_retention_policy('clinical_results', INTERVAL '7 years');
-- SELECT add_retention_policy('laboratory_results', INTERVAL '7 years');

-- ============================================
-- Sample Query Examples
-- ============================================

-- Get diagnosis with all module predictions
-- SELECT 
--     udr.*,
--     ir.prediction as imaging_prediction,
--     ir.confidence as imaging_confidence,
--     cr.prediction as clinical_prediction,
--     cr.confidence as clinical_confidence,
--     lr.prediction as laboratory_prediction,
--     lr.confidence as laboratory_confidence
-- FROM unified_diagnosis_results udr
-- LEFT JOIN imaging_results ir ON udr.diagnosis_case_id = ir.diagnosis_case_id
-- LEFT JOIN clinical_results cr ON udr.diagnosis_case_id = cr.diagnosis_case_id
-- LEFT JOIN laboratory_results lr ON udr.diagnosis_case_id = lr.diagnosis_case_id
-- WHERE udr.diagnosis_case_id = '...';

-- Performance statistics for last 30 days
-- SELECT * FROM diagnosis_performance_stats 
-- WHERE date >= NOW() - INTERVAL '30 days'
-- ORDER BY date DESC;

-- Module comparison
-- SELECT * FROM module_performance_comparison;
