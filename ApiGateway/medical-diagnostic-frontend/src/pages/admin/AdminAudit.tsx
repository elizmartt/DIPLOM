import { useEffect, useState, useCallback } from 'react';
import apiClient from '@/lib/axios';

const PAGE_SIZE = 50;

function formatDate(iso: string) {
    return new Date(iso).toLocaleString('hy-AM', { dateStyle: 'short', timeStyle: 'short' });
}

const ACTION_LABELS: Record<string, string> = {
    LOGIN:                                      'Մուտք',
    LOGOUT:                                     'Ելք',
    CREATE:                                     'Ստեղծում',
    UPDATE:                                     'Թարմացում',
    DELETE:                                     'Ջնջում',
    VIEW:                                       'Դիտում',
    DIAGNOSIS:                                  'Ախտ.',
    REPORT:                                     'Հաշվ.',
    SAVE_IMAGING_RESULT:                        'Պատ. արդ.',
    SAVE_CLINICAL_RESULT:                       'Կլին. արդ.',
    SAVE_LABORATORY_RESULT:                     'Լաբ. արդ.',
    SAVE_UNIFIED_RESULT:                        'Ամփ. արդ.',
    UPDATE_CASE_STATUS_COMPLETED_WITH_WARNINGS: 'Ավ. նախազ.',
    UPDATE_CASE_STATUS:                         'Կարգ. թարմ.',
    CREATE_CASE:                                'Դեպք ստեղծ.',
    CREATE_PATIENT:                             'Հիվ. ստեղծ.',
    UPDATE_CASE_STATUS_PROCESSING: 'Մշակման մեջ',
    TRIGGER_AI_ANALYSIS:           'AI վերլուծ.',
    SUBMIT_LAB_TESTS:              'Լաբ. ներկայ.',
    SUBMIT_SYMPTOMS:               'Ախտ. ներկայ.',
    UPLOAD_MEDICAL_IMAGE:          'Պատկ. բեռն.',
    CREATE_DIAGNOSIS_CASE:         'Դեպք ստեղծ.',
};

const ENTITY_LABELS: Record<string, string> = {
    DiagnosisCase:          'Ախտ. դեպք',
    Patient:                'Հիվանդ',
    Doctor:                 'Բժիշկ',
    MedicalImage:           'Բժ. պատկեր',
    ClinicalSymptom:        'Կլին. ախտ.',
    LabTest:                'Լաբ. թեստ',
    ImagingResult:          'Պատ. արդ.',
    ClinicalResult:         'Կլին. արդ.',
    LaboratoryResult:       'Լաբ. արդ.',
    UnifiedDiagnosisResult: 'Ամփ. արդ.',
    AuditLog:               'Աուդիտ',
};

function translateAction(action: string): string {
    const key = Object.keys(ACTION_LABELS).find(k => action.toUpperCase() === k.toUpperCase());
    return key ? ACTION_LABELS[key] : action;
}

function translateEntity(entity: string): string {
    return ENTITY_LABELS[entity] ?? entity;
}

function ActionBadge({ action }: { action: string }) {
    const colors: Record<string, { bg: string; color: string }> = {
        LOGIN:     { bg: 'rgba(16,185,129,0.1)',  color: '#059669' },
        LOGOUT:    { bg: 'rgba(100,116,139,0.1)', color: '#475569' },
        CREATE:    { bg: 'rgba(59,130,246,0.1)',  color: '#1d4ed8' },
        UPDATE:    { bg: 'rgba(245,158,11,0.1)',  color: '#b45309' },
        DELETE:    { bg: 'rgba(239,68,68,0.1)',   color: '#dc2626' },
        VIEW:      { bg: 'rgba(139,92,246,0.1)',  color: '#7c3aed' },
        DIAGNOSIS: { bg: 'rgba(14,165,233,0.1)',  color: '#0369a1' },
        REPORT:    { bg: 'rgba(236,72,153,0.1)',  color: '#be185d' },
        SAVE:      { bg: 'rgba(16,185,129,0.1)',  color: '#059669' },
    };
    const key   = Object.keys(colors).find(k => action.toUpperCase().includes(k)) ?? 'VIEW';
    const style = colors[key];
    return (
        <span style={{ padding: '3px 9px', borderRadius: 20, fontSize: 11, fontWeight: 700, background: style.bg, color: style.color, whiteSpace: 'nowrap' }}>
            {translateAction(action)}
        </span>
    );
}

interface AuditEntry {
    log_id: string;
    created_at: string;
    doctor_id: string | null;
    case_id: string | null;
    action: string;
    entity_type: string;
    doctor?: { full_name: string; email: string } | null;
}

export default function AdminAudit() {
    const [logs, setLogs]               = useState<AuditEntry[]>([]);
    const [filtered, setFiltered]       = useState<AuditEntry[]>([]);
    const [loading, setLoading]         = useState(true);
    const [error, setError]             = useState('');
    const [search, setSearch]           = useState('');
    const [searchInput, setSearchInput] = useState('');
    const [page, setPage]               = useState(1);

    const load = useCallback(() => {
        setLoading(true);
        apiClient.get('/Audit', { params: { limit: 500 } })
            .then(r => {
                const d = r.data?.data ?? r.data;
                setLogs(Array.isArray(d) ? d : []);
                setFiltered(Array.isArray(d) ? d : []);
            })
            .catch(() => setError('Աուդիտ մատյանը բեռնելը ձախողվեց'))
            .finally(() => setLoading(false));
    }, []);

    useEffect(() => { load(); }, [load]);

    useEffect(() => {
        if (!search) {
            setFiltered(logs);
        } else {
            const q = search.toLowerCase();
            setFiltered(logs.filter(l =>
                l.action?.toLowerCase().includes(q) ||
                l.entity_type?.toLowerCase().includes(q) ||
                l.doctor?.full_name?.toLowerCase().includes(q) ||
                l.doctor?.email?.toLowerCase().includes(q) ||
                translateAction(l.action).toLowerCase().includes(q) ||
                translateEntity(l.entity_type).toLowerCase().includes(q)
            ));
        }
        setPage(1);
    }, [search, logs]);

    const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
    const paginated  = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <div>
            <div style={{ marginBottom: 28 }}>
                <h1 style={{ fontSize: 26, fontWeight: 800, color: '#0f172a', margin: 0 }}>Աուդիտ Մատյան</h1>
                <p style={{ fontSize: 14, color: '#64748b', margin: '6px 0 0' }}>Համակարգի բոլոր գործողությունների պատմություն</p>
            </div>

            <div style={{ display: 'flex', gap: 10, marginBottom: 20 }}>
                <input
                    value={searchInput}
                    onChange={e => setSearchInput(e.target.value)}
                    onKeyDown={e => e.key === 'Enter' && setSearch(searchInput.trim())}
                    placeholder="Որոնել գործողություն, բժիշկ, օբյեկտ..."
                    style={{ flex: 1, padding: '9px 14px', borderRadius: 10, border: '1px solid #e2e8f0', fontSize: 14, outline: 'none', background: '#fff' }}
                />
                <button onClick={() => setSearch(searchInput.trim())}
                        style={{ padding: '9px 20px', borderRadius: 10, background: '#3b82f6', color: '#fff', fontSize: 14, fontWeight: 600, border: 'none', cursor: 'pointer' }}>
                    Որոնել
                </button>
                {search && (
                    <button onClick={() => { setSearchInput(''); setSearch(''); }}
                            style={{ padding: '9px 16px', borderRadius: 10, background: '#f1f5f9', color: '#374151', fontSize: 14, fontWeight: 600, border: 'none', cursor: 'pointer' }}>
                        Մաքրել
                    </button>
                )}
            </div>

            {error && (
                <div style={{ background: '#fef2f2', border: '1px solid #fecaca', borderRadius: 10, padding: '12px 18px', color: '#dc2626', fontSize: 14, marginBottom: 20 }}>
                    {error}
                </div>
            )}

            <div style={{ background: '#fff', borderRadius: 14, boxShadow: '0 1px 4px rgba(0,0,0,0.06)', overflow: 'hidden' }}>
                {loading ? (
                    <div style={{ padding: 32 }}>
                        {Array.from({ length: 6 }).map((_, i) => (
                            <div key={i} style={{ height: 44, background: '#f1f5f9', borderRadius: 8, marginBottom: 10, animation: 'pulse 1.5s infinite' }} />
                        ))}
                    </div>
                ) : (
                    <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                        <thead>
                        <tr style={{ background: '#f8fafc', borderBottom: '1px solid #e2e8f0' }}>
                            {['Ամ/Ժ', 'Բժիշկ', 'Գործողություն', 'Օբյեկտ'].map(h => (
                                <th key={h} style={{ padding: '12px 16px', fontSize: 12, fontWeight: 700, color: '#64748b', textAlign: 'left', textTransform: 'uppercase', letterSpacing: 0.5 }}>
                                    {h}
                                </th>
                            ))}
                        </tr>
                        </thead>
                        <tbody>
                        {paginated.length === 0 ? (
                            <tr>
                                <td colSpan={4} style={{ padding: '40px 16px', textAlign: 'center', color: '#94a3b8', fontSize: 14 }}>
                                    Գրառումներ չեն գտնվել
                                </td>
                            </tr>
                        ) : paginated.map((log, idx) => (
                            <tr key={log.log_id} style={{ borderBottom: '1px solid #f1f5f9', background: idx % 2 === 0 ? '#fff' : '#fafafa' }}>
                                <td style={{ padding: '12px 16px', fontSize: 12, color: '#64748b', whiteSpace: 'nowrap' }}>
                                    {formatDate(log.created_at)}
                                </td>
                                <td style={{ padding: '12px 16px' }}>
                                    <div style={{ fontSize: 13, fontWeight: 600, color: '#0f172a' }}>
                                        {log.doctor?.full_name ?? '—'}
                                    </div>
                                    {log.doctor?.email && (
                                        <div style={{ fontSize: 11, color: '#94a3b8' }}>{log.doctor.email}</div>
                                    )}
                                </td>
                                <td style={{ padding: '12px 16px' }}>
                                    <ActionBadge action={log.action} />
                                </td>
                                <td style={{ padding: '12px 16px' }}>
                                    <div style={{ fontSize: 13, color: '#374151' }}>
                                        {translateEntity(log.entity_type) || '—'}
                                    </div>
                                    {log.case_id && (
                                        <div style={{ fontSize: 11, color: '#94a3b8' }}>#{log.case_id.slice(0, 8)}</div>
                                    )}
                                </td>
                            </tr>
                        ))}
                        </tbody>
                    </table>
                )}

                {!loading && totalPages > 1 && (
                    <div style={{ padding: '16px 20px', borderTop: '1px solid #f1f5f9', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                        <span style={{ fontSize: 13, color: '#64748b' }}>
                            {filtered.length} գրառում · Էջ {page}/{totalPages}
                        </span>
                        <div style={{ display: 'flex', gap: 8 }}>
                            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}
                                    style={{ padding: '6px 14px', borderRadius: 8, border: '1px solid #e2e8f0', background: page === 1 ? '#f8fafc' : '#fff', color: page === 1 ? '#94a3b8' : '#374151', fontSize: 13, cursor: page === 1 ? 'not-allowed' : 'pointer' }}>
                                ← Նախ.
                            </button>
                            <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}
                                    style={{ padding: '6px 14px', borderRadius: 8, border: '1px solid #e2e8f0', background: page === totalPages ? '#f8fafc' : '#fff', color: page === totalPages ? '#94a3b8' : '#374151', fontSize: 13, cursor: page === totalPages ? 'not-allowed' : 'pointer' }}>
                                Հաջ. →
                            </button>
                        </div>
                    </div>
                )}
            </div>

            <style>{`@keyframes pulse { 0%,100%{opacity:1} 50%{opacity:0.5} }`}</style>
        </div>
    );
}