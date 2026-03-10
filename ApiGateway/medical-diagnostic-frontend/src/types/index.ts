// ─── Auth ────────────────────────────────────────────────────────────────────

export interface LoginRequest {
    email: string;
    password: string;
}

export interface AuthResponse {
    success: boolean;
    message: string;
    data: {
        token: string;
        email: string;
        fullName: string;
        role: string;
        userId: string;
        expiresAt: string;
        mustChangePassword: boolean;
    };
    errors: string[];
}

// ─── Doctor ──────────────────────────────────────────────────────────────────

export interface Doctor {
    doctor_id: string;
    email: string;
    full_name: string;
    role: string;
    specialization: string;
    hospital_affiliation: string;
    is_active: boolean;
    created_at: string;
    updated_at: string;
    must_change_password?: boolean;
}

// ─── Patient ─────────────────────────────────────────────────────────────────

export interface Patient {
    patient_id: string;
    patient_code: string;
    age: number;
    gender: 'male' | 'female' | 'other';
    created_at: string;
    updated_at: string;
}

export interface CreatePatientRequest {
    patient_code: string;
    age: number;
    gender: 'male' | 'female' | 'other';
}

// ─── Diagnosis Case ──────────────────────────────────────────────────────────

export type CaseStatus = 'pending' | 'in_review' | 'completed' | 'cancelled';
export type CasePriority = 'low' | 'medium' | 'high' | 'critical';
export type DiagnosisType = 'brain_tumor' | 'lung_cancer' | 'general';

export interface DiagnosisCase {
    case_id: string;
    patient_id: string;
    doctor_id: string;
    diagnosis_type: DiagnosisType;
    status: CaseStatus;
    priority: CasePriority;
    doctor_diagnosis: string | null;
    doctor_notes: string | null;
    created_at: string;
    completed_at: string | null;
    updated_at: string;
    // Joined
    patient?: Patient;
    doctor?: Doctor;
}

export interface CreateCaseRequest {
    patient_id: string;
    diagnosis_type: DiagnosisType;
    priority: CasePriority;
}

export interface UpdateCaseRequest {
    status?: CaseStatus;
    doctor_diagnosis?: string;
    doctor_notes?: string;
}

// ─── Medical Images ──────────────────────────────────────────────────────────

export type ImageType = 'CT' | 'MRI' | 'X-Ray' | 'PET' | 'Ultrasound';
export type ScanArea = 'brain' | 'chest' | 'abdomen' | 'spine' | 'pelvis' | 'full_body';

export interface MedicalImage {
    image_id: string;
    case_id: string;
    image_type: ImageType;
    scan_area: ScanArea;
    file_path: string;
    file_size_bytes: number;
    dicom_metadata: Record<string, unknown> | null;
    is_preprocessed: boolean;
    preprocessing_steps: string[] | null;
    uploaded_at: string;
}

// ─── Clinical Symptoms ───────────────────────────────────────────────────────

export interface ClinicalSymptoms {
    symptom_id: string;
    case_id: string;
    symptoms: string[];
    blood_pressure: string | null;
    heart_rate: number | null;
    temperature: number | null;
    smoking_history: boolean;
    family_history: Record<string, unknown> | null;
    recorded_at: string;
}

export interface CreateSymptomsRequest {
    case_id: string;
    symptoms: string[];
    blood_pressure?: string;
    heart_rate?: number;
    temperature?: number;
    smoking_history: boolean;
    family_history?: Record<string, unknown>;
}

// ─── Lab Tests ───────────────────────────────────────────────────────────────

export interface LabTest {
    lab_id: string;
    case_id: string;
    test_date: string;
    lab_name: string;
    test_results: Record<string, unknown>;
    reference_ranges: Record<string, unknown> | null;
    uploaded_at: string;
}

export interface CreateLabTestRequest {
    case_id: string;
    test_date: string;
    lab_name: string;
    test_results: Record<string, unknown>;
    reference_ranges?: Record<string, unknown>;
}

// ─── AI Results ──────────────────────────────────────────────────────────────

export interface ImagingResult {
    id: string;
    diagnosis_case_id: string;
    prediction: string;
    confidence: number;
    probabilities: Record<string, number>;
    processing_time_ms: number;
    explainability_data: ExplainabilityData | null;
    success: boolean;
    error_message: string | null;
    created_at: string;
}

export interface ClinicalResult {
    id: string;
    diagnosis_case_id: string;
    prediction: string;
    confidence: number;
    probabilities: Record<string, number>;
    processing_time_ms: number;
    explainability_data: ExplainabilityData | null;
    success: boolean;
    error_message: string | null;
    created_at: string;
}

export interface LaboratoryResult {
    id: string;
    diagnosis_case_id: string;
    prediction: string;
    confidence: number;
    probabilities: Record<string, number>;
    processing_time_ms: number;
    explainability_data: ExplainabilityData | null;
    success: boolean;
    error_message: string | null;
    created_at: string;
}

export interface ExplainabilityData {
    grad_cam_path?: string;
    shap_values?: Record<string, number>;
    feature_importance?: Record<string, number>;
    attention_map?: string;
}

export type RiskLevel = 'low' | 'moderate' | 'high' | 'critical';

export interface UnifiedDiagnosisResult {
    id: string;
    diagnosis_case_id: string;
    final_diagnosis: string;
    overall_confidence: number;
    ensemble_probabilities: Record<string, number>;
    contributing_modules: ContributingModule[];
    risk_level: RiskLevel;
    recommendations: string[];
    explainability_summary: string;
    status: string;
    total_processing_time_ms: number;
    error_details: string | null;
    created_at: string;
    updated_at: string;
}

export interface ContributingModule {
    module: 'imaging' | 'clinical' | 'laboratory';
    weight: number;
    prediction: string;
    confidence: number;
}

// Full case results bundle
export interface CaseResults {
    unified: UnifiedDiagnosisResult | null;
    imaging: ImagingResult | null;
    clinical: ClinicalResult | null;
    laboratory: LaboratoryResult | null;
}

// ─── Audit Log ───────────────────────────────────────────────────────────────

export interface AuditLog {
    log_id: string;
    created_at: string;
    doctor_id: string;
    case_id: string | null;
    action: string;
    entity_type: string;
    entity_id: string;
    action_details: Record<string, unknown> | null;
    ip_address: string;
    user_agent: string;
    // Joined
    doctor?: Doctor;
}

// ─── API Pagination ──────────────────────────────────────────────────────────

export interface PaginatedResponse<T> {
    data: T[];
    total: number;
    page: number;
    per_page: number;
    total_pages: number;
}

export interface ListParams {
    page?: number;
    per_page?: number;
    search?: string;
    sort_by?: string;
    sort_order?: 'asc' | 'desc';
}

export interface CaseListParams extends ListParams {
    status?: CaseStatus;
    priority?: CasePriority;
    diagnosis_type?: DiagnosisType;
    doctor_id?: string;
    date_from?: string;
    date_to?: string;
}

// ─── Dashboard Stats ─────────────────────────────────────────────────────────

export interface DashboardStats {
    total_cases: number;
    pending_cases: number;
    in_review_cases: number;
    completed_cases: number;
    total_patients: number;
    avg_confidence: number;
    cases_today: number;
    accuracy_rate: number;
}
