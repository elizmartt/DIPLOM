import apiClient from '@/lib/axios';

// PatientController.MapToResponse returns PascalCase (C# default)
export interface PatientApiResponse {
    patientId: string;
    patientCode: string;
    firstName: string;
    lastName: string;
    age: number;
    gender: string;
    createdAt: string;
    updatedAt: string;
}

interface ApiResponse<T> {
    success: boolean;
    data: T;
    message: string;
    errors: string[] | null;
}

export interface CreatePatientRequest {
    firstName: string;
    lastName: string;
    age: number;
    gender: string;
}

export const patientsApi = {
    list: async (limit = 100): Promise<{ data: PatientApiResponse[] }> => {
        const res = await apiClient.get<ApiResponse<PatientApiResponse[]>>('/Patient', { params: { limit } });
        return { data: res.data.data ?? [] };
    },

    get: async (id: string): Promise<PatientApiResponse> => {
        const res = await apiClient.get<ApiResponse<PatientApiResponse>>(`/Patient/${id}`);
        return res.data.data;
    },

    getByCode: async (code: string): Promise<PatientApiResponse> => {
        const res = await apiClient.get<ApiResponse<PatientApiResponse>>(`/Patient/by-code/${code}`);
        return res.data.data;
    },

    create: async (data: CreatePatientRequest): Promise<PatientApiResponse> => {
        const res = await apiClient.post<ApiResponse<PatientApiResponse>>('/Patient', data);
        return res.data.data;
    },
};