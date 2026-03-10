import apiClient from '@/lib/axios';

export interface AuditLogEntry {
    log_id: string;
    created_at: string;
    doctor_id: string;
    case_id: string | null;
    action: string;
    entity_type: string;
    entity_id: string;
    action_details: Record<string, unknown> | null;
    ip_address: string | null;
    user_agent: string | null;
    doctor: { full_name: string; email: string } | null;
}

interface ApiResponse<T> {
    success: boolean;
    data: T;
    message: string;
    errors: string[] | null;
}

export interface AuditLogParams {
    action?: string;
    dateFrom?: string;
    dateTo?: string;
    limit?: number;
}

// Controller: GET /api/Audit?action=&dateFrom=&dateTo=&limit=200
export const auditApi = {
    list: async (params?: AuditLogParams): Promise<AuditLogEntry[]> => {
        const query: Record<string, string | number> = { limit: params?.limit ?? 200 };
        if (params?.action) query.action = params.action;
        if (params?.dateFrom) query.dateFrom = params.dateFrom;
        if (params?.dateTo) query.dateTo = params.dateTo;

        const response = await apiClient.get<ApiResponse<AuditLogEntry[]>>('/Audit', { params: query });
        return response.data.data ?? [];
    },
};