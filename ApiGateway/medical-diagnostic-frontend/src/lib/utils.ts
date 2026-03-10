import {type ClassValue, clsx} from 'clsx';
import {twMerge} from 'tailwind-merge';
import {CasePriority, CaseStatus, RiskLevel} from '@/types';

// ─── Tailwind class merger ────────────────────────────────────────────────────
export function cn(...inputs: ClassValue[]) {
    return twMerge(clsx(inputs));
}

// ─── Date formatting ─────────────────────────────────────────────────────────
export function formatDate(date: string | Date, options?: Intl.DateTimeFormatOptions): string {
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleDateString('en-GB', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        ...options,
    });
}

export function formatDateTime(date: string | Date): string {
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleString('en-GB', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    });
}

export function formatRelative(date: string | Date): string {
    const d = typeof date === 'string' ? new Date(date) : date;
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return formatDate(d);
}

// ─── Status / priority helpers ────────────────────────────────────────────────
export function getStatusLabel(status: CaseStatus): string {
    const labels: Record<CaseStatus, string> = {
        pending: 'Pending',
        in_review: 'In Review',
        completed: 'Completed',
        cancelled: 'Cancelled',
    };
    return labels[status] ?? status;
}

export function getPriorityLabel(priority: CasePriority): string {
    const labels: Record<CasePriority, string> = {
        low: 'Low',
        medium: 'Medium',
        high: 'High',
        critical: 'Critical',
    };
    return labels[priority] ?? priority;
}

export function getRiskLabel(risk: RiskLevel): string {
    const labels: Record<RiskLevel, string> = {
        low: 'Low Risk',
        moderate: 'Moderate Risk',
        high: 'High Risk',
        critical: 'Critical Risk',
    };
    return labels[risk] ?? risk;
}

// ─── Confidence color ─────────────────────────────────────────────────────────
export function getConfidenceColor(confidence: number): string {
    if (confidence >= 0.85) return '#10b981'; // green
    if (confidence >= 0.70) return '#f59e0b'; // yellow
    return '#ef4444'; // red
}

export function getConfidenceLabel(confidence: number): string {
    if (confidence >= 0.85) return 'High confidence';
    if (confidence >= 0.70) return 'Moderate confidence';
    return 'Low confidence';
}

// ─── Risk color ────────────────────────────────────────────────────────────────
export function getRiskColor(risk: RiskLevel): string {
    const colors: Record<RiskLevel, string> = {
        low: '#10b981',
        moderate: '#f59e0b',
        high: '#f97316',
        critical: '#ef4444',
    };
    return colors[risk];
}

// ─── File size formatter ──────────────────────────────────────────────────────
export function formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}

// ─── Percentage formatter ─────────────────────────────────────────────────────
export function formatPercent(value: number, decimals = 1): string {
    return `${(value * 100).toFixed(decimals)}%`;
}

// ─── Truncate text ────────────────────────────────────────────────────────────
export function truncate(str: string, maxLength: number): string {
    if (str.length <= maxLength) return str;
    return `${str.slice(0, maxLength)}…`;
}

// ─── Generate initials ─────────────────────────────────────────────────────────
export function getInitials(name: string): string {
    return name
        .split(' ')
        .map((n) => n[0])
        .slice(0, 2)
        .join('')
        .toUpperCase();
}
