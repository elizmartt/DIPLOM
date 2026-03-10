import apiClient from '@/lib/axios';

export interface DiagnosisCaseResponse {
    caseId: string;
    patientId: string;
    doctorId: string;
    diagnosisType: string;
    status: string;
    priority: string | null;
    doctorDiagnosis: string | null;
    doctorNotes: string | null;
    createdAt: string;
    updatedAt: string;
    completedAt: string | null;
    patientCode: string | null;
    patientAge: number | null;
    patientName: string | null;
    doctorName: string | null;
}

export interface ApiResponse<T> {
    success: boolean;
    data: T;
    message: string;
    errors: string[] | null;
}

export interface CreateCaseRequest {
    patientId: string;
    diagnosisType: string;
    doctorNotes?: string;
}

export interface DiagnosisResults {
    id: string;
    diagnosisCaseId: string;
    finalDiagnosis: string;
    overallConfidence: number;
    ensembleProbabilities: Record<string, number>;
    contributingModules: string | string[];
    riskLevel: string;
    recommendations: string | string[];
    explainabilitySummary: Record<string, unknown>;
    totalProcessingTimeMs: number;
    status: string;
    createdAt: string;
    updatedAt: string;
}

export interface MedicalImage {
    imageId: string;
    caseId: string;
    imageType: string;
    scanArea: string;
    filePath: string;
    fileSizeBytes: number;
    isPreprocessed: boolean;
    uploadedAt: string;
    modality?: string;
}

export interface ClinicalSymptom {
    symptomId: string;
    caseId: string;
    symptoms: string; // JSON string
    bloodPressure: string;
    heartRate: number;
    temperature: number;
    smokingHistory: boolean;
    familyHistory: string; // JSON string
    recordedAt: string;
}

export interface LabTest {
    labId: string;
    caseId: string;
    testDate: string;
    labName: string;
    testResults: string; // JSON string
    referenceRanges: string; // JSON string
    uploadedAt: string;
}

export const casesApi = {
    list: async (status?: string, limit = 50): Promise<DiagnosisCaseResponse[]> => {
        const params: Record<string, string | number> = { limit };
        if (status) params.status = status;
        const response = await apiClient.get<ApiResponse<DiagnosisCaseResponse[]>>('/Diagnosis/cases', { params });
        return response.data.data;
    },

    getById: async (caseId: string): Promise<DiagnosisCaseResponse> => {
        const response = await apiClient.get<ApiResponse<DiagnosisCaseResponse>>(`/Diagnosis/cases/${caseId}`);
        return response.data.data;
    },

    create: async (data: CreateCaseRequest): Promise<DiagnosisCaseResponse> => {
        const response = await apiClient.post<ApiResponse<DiagnosisCaseResponse>>('/Diagnosis/cases', data);
        return response.data.data;
    },

    delete: async (caseId: string): Promise<void> => {
        await apiClient.delete(`/Diagnosis/cases/${caseId}`);
    },

    getResults: async (caseId: string): Promise<DiagnosisResults> => {
        const response = await apiClient.get<ApiResponse<DiagnosisResults>>(`/Diagnosis/cases/${caseId}/results`);
        return response.data.data;
    },

    // POST /api/Diagnosis/cases/{caseId}/images (multipart/form-data)
    uploadImage: async (caseId: string, file: File, scanArea: string): Promise<void> => {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('scanArea', scanArea);
        await apiClient.post(`/Diagnosis/cases/${caseId}/images`, formData);
    },

    // POST /api/Diagnosis/cases/{caseId}/symptoms
    submitSymptoms: async (caseId: string, data: {
        symptoms: string[];
        bloodPressure?: string;
        heartRate?: number;
        temperature?: number;
        smokingHistory: boolean;
        familyHistory?: Record<string, unknown>;
    }): Promise<void> => {
        await apiClient.post(`/Diagnosis/cases/${caseId}/symptoms`, data);
    },
    getGradCam: async (caseId: string, filename: string): Promise<string> => {
        const response = await apiClient.get(
            `/Diagnosis/cases/${caseId}/gradcam/${encodeURIComponent(filename)}`,
            { responseType: 'blob' }
        );
        return URL.createObjectURL(response.data as Blob);
    },
    // POST /api/Diagnosis/cases/{caseId}/lab-tests
    submitLabTests: async (caseId: string, data: {
        testDate: string;
        labName: string;
        testResults: Record<string, unknown>;
        referenceRanges?: Record<string, unknown>;
    }): Promise<void> => {
        await apiClient.post(`/Diagnosis/cases/${caseId}/lab-tests`, data);
    },

    triggerAnalysis: async (caseId: string, options: {
        includeImaging: boolean;
        includeClinical: boolean;
        includeLaboratory: boolean;
    }): Promise<void> => {
        await apiClient.post(`/Diagnosis/cases/${caseId}/analyze`, {
            includeImaging: options.includeImaging,
            includeClinical: options.includeClinical,
            includeLaboratory: options.includeLaboratory,
        });
    },

    // GET /api/Diagnosis/cases/{caseId}/images
    getImages: async (caseId: string): Promise<MedicalImage[]> => {
        try {
            const response = await apiClient.get<ApiResponse<MedicalImage[]>>(`/Diagnosis/cases/${caseId}/images`);
            return response.data.data ?? [];
        } catch {
            return [];
        }
    },

    downloadReport: async (caseId: string): Promise<Blob> => {
        const response = await apiClient.get(`/cases/${caseId}/report`, {
            responseType: 'blob',
        });
        return response.data;
    },

    // Fetch an image file as a blob object URL (handles auth headers)
    getImageBlob: async (caseId: string, filename: string): Promise<string> => {
        const response = await apiClient.get(
            `/Diagnosis/cases/${caseId}/file/${encodeURIComponent(filename)}`,
            { responseType: 'blob' }
        );
        return URL.createObjectURL(response.data as Blob);
    },
};