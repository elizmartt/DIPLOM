import apiClient from '@/lib/axios';

// ReportController: GET /api/cases/{id}/report → returns PDF blob
export const reportsApi = {
    downloadPdf: async (caseId: string): Promise<Blob> => {
        const response = await apiClient.get(`/cases/${caseId}/report`, {
            responseType: 'blob',
        });
        return response.data;
    },
};

export const downloadFile = (blob: Blob, filename: string) => {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};