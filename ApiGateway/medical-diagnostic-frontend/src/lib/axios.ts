import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';

const BASE_URL = import.meta.env.VITE_API_URL || '/api';

export const apiClient = axios.create({
    baseURL: BASE_URL,
    timeout: 30000,
    headers: {
        'Content-Type': 'application/json',
    },
});

const getStoredTokens = (): { accessToken: string | null; refreshToken: string | null } => {
    try {
        const raw = localStorage.getItem('auth-storage');
        if (!raw) return { accessToken: null, refreshToken: null };
        const parsed = JSON.parse(raw);
        return {
            accessToken: parsed?.state?.accessToken ?? null,
            refreshToken: parsed?.state?.refreshToken ?? null,
        };
    } catch {
        return { accessToken: null, refreshToken: null };
    }
};

// ─── Request interceptor: attach JWT + handle FormData ───────────────────────
apiClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        const { accessToken } = getStoredTokens();
        if (accessToken && config.headers) {
            config.headers.Authorization = `Bearer ${accessToken}`;
        }
        // For FormData, delete Content-Type so browser sets it with correct boundary
        if (config.data instanceof FormData) {
            delete config.headers['Content-Type'];
        }
        return config;
    },
    (error) => Promise.reject(error)
);

// ─── Response interceptor: handle 401 + refresh ─────────────────────────────
let isRefreshing = false;
let failedQueue: Array<{
    resolve: (token: string) => void;
    reject: (error: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
    failedQueue.forEach((prom) => {
        if (error) prom.reject(error);
        else if (token) prom.resolve(token);
    });
    failedQueue = [];
};

const forceLogout = () => {
    try {
        const raw = localStorage.getItem('auth-storage');
        if (raw) {
            const parsed = JSON.parse(raw);
            parsed.state = {
                ...parsed.state,
                doctor: null,
                accessToken: null,
                refreshToken: null,
                isAuthenticated: false,
            };
            localStorage.setItem('auth-storage', JSON.stringify(parsed));
        }
    } catch {
        localStorage.removeItem('auth-storage');
    }
    window.location.href = '/login';
};

apiClient.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & {
            _retry?: boolean;
        };

        const isRefreshEndpoint = originalRequest.url?.includes('/auth/refresh') ||
            originalRequest.url?.includes('/Auth/refresh');

        if (error.response?.status === 401 && !originalRequest._retry && !isRefreshEndpoint) {
            if (isRefreshing) {
                return new Promise((resolve, reject) => {
                    failedQueue.push({ resolve, reject });
                }).then((token) => {
                    if (originalRequest.headers) {
                        originalRequest.headers.Authorization = `Bearer ${token}`;
                    }
                    return apiClient(originalRequest);
                });
            }

            originalRequest._retry = true;
            isRefreshing = true;

            const { refreshToken } = getStoredTokens();

            if (!refreshToken) {
                isRefreshing = false;
                forceLogout();
                return Promise.reject(error);
            }

            try {
                const { data } = await axios.post(`${BASE_URL}/Auth/refresh`, {
                    refresh_token: refreshToken,
                });

                const newAccessToken: string = data.access_token;
                const newRefreshToken: string = data.refresh_token;

                try {
                    const raw = localStorage.getItem('auth-storage');
                    if (raw) {
                        const parsed = JSON.parse(raw);
                        parsed.state.accessToken = newAccessToken;
                        parsed.state.refreshToken = newRefreshToken;
                        localStorage.setItem('auth-storage', JSON.stringify(parsed));
                    }
                } catch {
                    forceLogout();
                    return Promise.reject(error);
                }

                processQueue(null, newAccessToken);

                if (originalRequest.headers) {
                    originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
                }

                return apiClient(originalRequest);
            } catch (refreshError) {
                processQueue(refreshError, null);
                forceLogout();
                return Promise.reject(refreshError);
            } finally {
                isRefreshing = false;
            }
        }

        return Promise.reject(error);
    }
);

export interface ApiError {
    message: string;
    status: number;
    errors?: Record<string, string[]>;
}

export const getApiError = (error: unknown): ApiError => {
    if (axios.isAxiosError(error)) {
        return {
            message:
                error.response?.data?.message ||
                error.response?.data?.detail ||
                error.message ||
                'An unexpected error occurred',
            status: error.response?.status ?? 500,
            errors: error.response?.data?.errors,
        };
    }
    return {
        message: 'An unexpected error occurred',
        status: 500,
    };
};

export default apiClient;