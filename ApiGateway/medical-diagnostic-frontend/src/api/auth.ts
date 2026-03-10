import apiClient from '@/lib/axios';
import { AuthResponse, LoginRequest } from '@/types';

export const authApi = {
    login: async (data: LoginRequest): Promise<AuthResponse> => {
        const response = await apiClient.post<AuthResponse>('/Auth/login', data);
        console.log('Login response:', response.data); // temp debug
        return response.data;
    },

    refresh: async (refreshToken: string): Promise<{ access_token: string; refresh_token: string }> => {
        const response = await apiClient.post('/Auth/refresh', { refresh_token: refreshToken });
        return response.data;
    },

    logout: async (): Promise<void> => {
        await apiClient.post('/Auth/logout');
    },
};