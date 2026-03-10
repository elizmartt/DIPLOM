import apiClient from '@/lib/axios';

// StatisticsController returns Ok(result) directly — no ApiResponse wrapper

export interface Overview {
    totalCases: number;
    brainCases: number;
    lungCases: number;
    completedCases: number;
    pendingCases: number;       // used by DoctorProfile
    analyzingCases: number;     // used by DoctorProfile
    casesLast30Days: number;
    monthlyTrends: {
        month: string;
        total: number;
        brain: number;
        lung: number;
    }[];
}

export interface DiagnosisItem {
    diagnosis: string;
    count: number;
    averageConfidence: number;
    percentage: number;
}

export interface ModelAccuracy {
    models: {
        name: string;
        architecture: string;
        trainedAccuracy: number;
        weighting: number;
    }[];
    liveStats: {
        totalCasesProcessed: number;
        averageProcessingTimeMs: number;
    };
}

export interface ConfidenceTrend {
    month: string;
    overall: number;
    imaging: number;
    laboratory: number;
    clinical: number;
}

export const statisticsApi = {
    // GET /api/statistics/overview — returns Overview directly (no wrapper)
    getOverview: async (): Promise<Overview> => {
        const response = await apiClient.get<Overview>('/statistics/overview');
        return response.data;
    },

    // GET /api/statistics/diagnoses — returns DiagnosisItem[] directly
    getDiagnoses: async (): Promise<DiagnosisItem[]> => {
        const response = await apiClient.get<DiagnosisItem[]>('/statistics/diagnoses');
        return response.data;
    },

    // GET /api/statistics/model-accuracy — returns ModelAccuracy directly
    getModelAccuracy: async (): Promise<ModelAccuracy> => {
        const response = await apiClient.get<ModelAccuracy>('/statistics/model-accuracy');
        return response.data;
    },

    // GET /api/statistics/confidence-trends — returns ConfidenceTrend[] directly
    getConfidenceTrends: async (): Promise<ConfidenceTrend[]> => {
        const response = await apiClient.get<ConfidenceTrend[]>('/statistics/confidence-trends');
        return response.data;
    },
};