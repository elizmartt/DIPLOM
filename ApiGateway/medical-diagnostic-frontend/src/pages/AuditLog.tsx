import { useEffect, useState } from 'react';
import apiClient from '@/lib/axios';

// ── Types ─────────────────────────────────────────────────────────────────
interface AuditLogEntry {
    log_id: string;
    created_at: string;
    doctor_id: string;
    case_id: string | null;
    action: string;
    entity_type: string;
    entity_id: string;
    action_details: Record<string, unknown> | null;
    user_agent: string | null;
    doctor: { full_name: string; email: string } | null;
}

interface AuditApiResponse {
    success: boolean;
    data: AuditLogEntry[];
    message: string;
}

async function fetchAuditLogs(params: {
    action?: string;
    dateFrom?: string;
    dateTo?: string;
    limit?: number;
}): Promise<AuditLogEntry[]> {
    const query: Record<string, string | number> = { limit: params.limit ?? 200 };
    if (params.action) query.action = params.action;
    if (params.dateFrom) query.dateFrom = params.dateFrom;
    if (params.dateTo) query.dateTo = params.dateTo;

    const res = await apiClient.get<AuditApiResponse>('/Audit', { params: query });
    return res.data.data ?? [];
}

// ── Armenian Entity Names ──────────────────────────────────────────────────
const ENTITY_ARMENIAN: Record<string, string> = {
    DiagnosisCase: 'Ախտորոշման Գործ',
    Patient: 'Հիվանդ',
    Doctor: 'Բժիշկ',
    MedicalImage: 'Բժշկական Նկար',
    LabTest: 'Լաբ․ Թեստ',
    ClinicalSymptom: 'Ախտանիշ',
    AuditLog: 'Արձանագրություն',
    User: 'Օգտատեր',
    Report: 'Հաշվետվություն',
    Session: 'Նիստ',
};

function armenianEntity(raw: string): string {
    return ENTITY_ARMENIAN[raw] ?? raw;
}

// ── Human-readable detail formatter ───────────────────────────────────────
function formatDetails(action: string, d: Record<string, unknown> | null): string {
    if (!d) return '—';

    const act = action.toUpperCase();

    if (act === 'LOGIN') {
        return `Մուտք գործեց համակարգ`;
    }
    if (act === 'CREATE_PATIENT') {
        const code = d.patient_code ?? d.patientCode ?? d.code;
        const age = d.age;
        const gender = d.gender === 'male' ? 'արական' : d.gender === 'female' ? 'իգական' : d.gender;
        const parts: string[] = [];
        if (code) parts.push(`Կոդ՝ ${code}`);
        if (age) parts.push(`Տարիք՝ ${age}`);
        if (gender) parts.push(`Սեռ՝ ${gender}`);
        return parts.length ? parts.join(', ') : 'Ստեղծվեց նոր հիվանդ';
    }
    if (act === 'CREATE_DIAGNOSIS_CASE') {
        const type = d.diagnosis_type ?? d.diagnosisType ?? d.type;
        const priority = d.priority;
        const parts: string[] = [];
        if (type) parts.push(`Տեսակ՝ ${type}`);
        if (priority) parts.push(`Առաջնահերթ՝ ${priority}`);
        return parts.length ? parts.join(', ') : 'Ստեղծվեց ախտորոշման գործ';
    }
    if (act === 'UPLOAD_MEDICAL_IMAGE') {
        const type = d.image_type ?? d.imageType ?? d.type;
        const area = d.scan_area ?? d.scanArea ?? d.area;
        const size = d.file_size_bytes ?? d.fileSize;
        const parts: string[] = [];
        if (type) parts.push(`Տեսակ՝ ${type}`);
        if (area) parts.push(`Հատված՝ ${area}`);
        if (size) parts.push(`Չափ՝ ${Math.round(Number(size) / 1024)} ԿԲ`);
        return parts.length ? parts.join(', ') : 'Վերբեռնվեց բժշկական նկար';
    }
    if (act === 'SUBMIT_SYMPTOMS') {
        const bp = d.blood_pressure ?? d.bloodPressure;
        const hr = d.heart_rate ?? d.heartRate;
        const temp = d.temperature;
        const parts: string[] = [];
        if (bp) parts.push(`ԱՃ՝ ${bp}`);
        if (hr) parts.push(`Սրտ․ հաճ․՝ ${hr}`);
        if (temp) parts.push(`Ջերմ․՝ ${temp}°C`);
        return parts.length ? parts.join(', ') : 'Ախտանիշներ ներկայացվեցին';
    }
    if (act === 'SUBMIT_LAB_TESTS') {
        const lab = d.lab_name ?? d.labName ?? d.name;
        const date = d.test_date ?? d.testDate;
        const parts: string[] = [];
        if (lab) parts.push(`Թեստ՝ ${lab}`);
        if (date) parts.push(`Ամսաթիվ՝ ${String(date).slice(0, 10)}`);
        return parts.length ? parts.join(', ') : 'Լաբ․ թեստ ներկայացվեց';
    }
    if (act === 'TRIGGER_AI_ANALYSIS') {
        const diagnosis = d.final_diagnosis ?? d.diagnosis ?? d.result;
        const confidence = d.overall_confidence ?? d.confidence;
        const parts: string[] = [];
        if (diagnosis) parts.push(`Արդյունք՝ ${diagnosis}`);
        if (confidence != null) parts.push(`Վստահ․՝ ${Math.round(Number(confidence) * 100)}%`);
        return parts.length ? parts.join(', ') : 'ԱԻ վերլուծություն կատարվեց';
    }
    if (act === 'DELETE_CASE') {
        const id = d.case_id ?? d.caseId ?? d.id;
        return id ? `Ջնջվեց գործ #${String(id).slice(0, 8)}` : 'Ջնջվեց ախտորոշման գործ';
    }

    // Generic fallback — show key=value pairs in Armenian-friendly format
    const entries = Object.entries(d).slice(0, 3);
    return entries.map(([k, v]) => `${k}՝ ${v}`).join(', ');
}

// ── Action Badge ──────────────────────────────────────────────────────────
const ACTION_STYLES: Record<string, { bg: string; color: string; border: string }> = {
    CREATE_DIAGNOSIS_CASE: { bg: 'rgba(16,185,129,0.15)', color: '#34d399', border: 'rgba(16,185,129,0.3)' },
    CREATE_PATIENT:        { bg: 'rgba(16,185,129,0.15)', color: '#34d399', border: 'rgba(16,185,129,0.3)' },
    DELETE_CASE:           { bg: 'rgba(239,68,68,0.15)',  color: '#f87171', border: 'rgba(239,68,68,0.3)' },
    UPLOAD_MEDICAL_IMAGE:  { bg: 'rgba(59,130,246,0.15)', color: '#60a5fa', border: 'rgba(59,130,246,0.3)' },
    SUBMIT_SYMPTOMS:       { bg: 'rgba(14,165,233,0.15)', color: '#38bdf8', border: 'rgba(14,165,233,0.3)' },
    SUBMIT_LAB_TESTS:      { bg: 'rgba(14,165,233,0.15)', color: '#38bdf8', border: 'rgba(14,165,233,0.3)' },
    TRIGGER_AI_ANALYSIS:   { bg: 'rgba(167,139,250,0.15)',color: '#a78bfa', border: 'rgba(167,139,250,0.3)' },
    LOGIN:                 { bg: 'rgba(251,146,60,0.15)', color: '#fb923c', border: 'rgba(251,146,60,0.3)' },
};

const ACTION_LABELS: Record<string, string> = {
    CREATE_DIAGNOSIS_CASE: 'ՍՏԵՂԾՈՒՄ',
    CREATE_PATIENT:        'ՀԻՎԱՆԴ',
    UPLOAD_MEDICAL_IMAGE:  'ՆԿԱՐ',
    SUBMIT_SYMPTOMS:       'ԱԽՏԱՆԻՇ',
    SUBMIT_LAB_TESTS:      'ԼԱԲ',
    TRIGGER_AI_ANALYSIS:   'ՎԵՐԼՈՒԾՈՒՄ',
    DELETE_CASE:           'ՋՆՋՈՒՄ',
    LOGIN:                 'ՄՈՒՏՔ',
};

function ActionBadge({ action }: { action: string }) {
    const key = action.toUpperCase();
    const s = ACTION_STYLES[key] ?? { bg: 'rgba(148,163,184,0.15)', color: '#94a3b8', border: 'rgba(148,163,184,0.3)' };
    const label = ACTION_LABELS[key] ?? key;

    return (
        <span style={{
            padding: '3px 9px',
            borderRadius: 6,
            background: s.bg,
            color: s.color,
            border: `1px solid ${s.border}`,
            fontSize: 10,
            fontWeight: 800,
            whiteSpace: 'nowrap',
            letterSpacing: '0.05em',
        }}>
            {label}
        </span>
    );
}

// ── Skeleton ──────────────────────────────────────────────────────────────
function Skeleton() {
    return (
        <div style={{ padding: '12px 20px' }}>
            {[1, 2, 3, 4, 5].map(i => (
                <div key={i} style={{
                    height: 48,
                    borderRadius: 10,
                    marginBottom: 6,
                    background: 'linear-gradient(90deg, #1e293b 25%, #243044 50%, #1e293b 75%)',
                    backgroundSize: '200% 100%',
                    animation: 'al-shimmer 1.4s ease-in-out infinite',
                    animationDelay: `${(i - 1) * 80}ms`,
                }} />
            ))}
        </div>
    );
}

// ── Main ──────────────────────────────────────────────────────────────────
export default function History() {
    const [logs, setLogs] = useState<AuditLogEntry[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [entityFilter, setEntityFilter] = useState('');
    const [dateFrom, setDateFrom] = useState('');
    const [dateTo, setDateTo] = useState('');
    const [search, setSearch] = useState('');
    const [page, setPage] = useState(1);
    const [expanded, setExpanded] = useState<string | null>(null);

    const PER_PAGE = 20;

    const load = (from: string, to: string) => {
        setLoading(true);
        fetchAuditLogs({ dateFrom: from || undefined, dateTo: to || undefined })
            .then(setLogs)
            .catch((e: Error) => setError(e.message))
            .finally(() => setLoading(false));
    };

    useEffect(() => { load(dateFrom, dateTo); }, [dateFrom, dateTo]);

    const uniqueEntities = Array.from(new Set(logs.map(l => l.entity_type)));

    const filtered = logs.filter(l => {
        const q = search.toLowerCase();
        const matchSearch =
            !q ||
            l.action.toLowerCase().includes(q) ||
            l.entity_type.toLowerCase().includes(q) ||
            armenianEntity(l.entity_type).toLowerCase().includes(q) ||
            (l.case_id ?? '').toLowerCase().includes(q) ||
            (l.doctor?.full_name ?? '').toLowerCase().includes(q);
        const matchEntity = !entityFilter || l.entity_type === entityFilter;
        return matchSearch && matchEntity;
    });

    const totalPages = Math.max(1, Math.ceil(filtered.length / PER_PAGE));
    const paginated = filtered.slice((page - 1) * PER_PAGE, page * PER_PAGE);

    const fmt = (iso: string) =>
        new Date(iso).toLocaleDateString('hy-AM', {
            day: '2-digit', month: 'short', year: 'numeric',
            hour: '2-digit', minute: '2-digit',
        });

    const pageNumbers = () => {
        const nums: number[] = [];
        const start = Math.max(1, page - 2);
        const end = Math.min(totalPages, page + 2);
        for (let i = start; i <= end; i++) nums.push(i);
        return nums;
    };

    return (
        <>
            <style>{`
                @keyframes al-shimmer {
                    0%   { background-position: -200% 0 }
                    100% { background-position:  200% 0 }
                }
                @keyframes fadeUp {
                    from { opacity: 0; transform: translateY(6px) }
                    to   { opacity: 1; transform: translateY(0) }
                }

                .al-root {
                    background: #0d1117;
                    min-height: 100vh;
                    color: #e2e8f0;
                    font-family: 'Segoe UI', system-ui, sans-serif;
                }

                .al-row {
                    display: grid;
                    grid-template-columns: 155px 120px 130px 1fr 130px;
                    gap: 12px;
                    align-items: center;
                    padding: 13px 20px;
                    border-radius: 10px;
                    cursor: pointer;
                    transition: background 0.15s, border-color 0.15s;
                    border: 1px solid transparent;
                    animation: fadeUp .2s ease both;
                    margin: 0 8px;
                }
                .al-row:hover {
                    background: rgba(255,255,255,0.04);
                    border-color: rgba(255,255,255,0.08);
                }

                .al-pill {
                    padding: 5px 13px;
                    border-radius: 99px;
                    border: 1px solid rgba(255,255,255,0.1);
                    background: rgba(255,255,255,0.04);
                    color: #94a3b8;
                    font-size: 12px;
                    font-weight: 600;
                    cursor: pointer;
                    white-space: nowrap;
                    transition: all 0.15s;
                }
                .al-pill:hover { background: rgba(255,255,255,0.08); color: #cbd5e1; }
                .al-pill.active {
                    background: rgba(14,165,233,0.2);
                    border-color: rgba(14,165,233,0.5);
                    color: #38bdf8;
                }

                .al-input {
                    background: rgba(255,255,255,0.05);
                    border: 1px solid rgba(255,255,255,0.1);
                    border-radius: 8px;
                    color: #e2e8f0;
                    padding: 7px 11px;
                    font-size: 13px;
                    outline: none;
                    transition: border-color 0.15s;
                }
                .al-input:focus { border-color: rgba(14,165,233,0.5); }
                .al-input::placeholder { color: #475569; }
                .al-input::-webkit-calendar-picker-indicator { filter: invert(0.6); }

                .al-pgbtn {
                    width: 32px;
                    height: 32px;
                    border-radius: 8px;
                    border: 1px solid rgba(255,255,255,0.1);
                    background: rgba(255,255,255,0.04);
                    color: #94a3b8;
                    font-size: 12px;
                    font-weight: 700;
                    cursor: pointer;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    transition: all 0.15s;
                }
                .al-pgbtn:hover:not(:disabled) { background: rgba(255,255,255,0.08); color: #e2e8f0; }
                .al-pgbtn.active {
                    background: rgba(14,165,233,0.25);
                    border-color: rgba(14,165,233,0.5);
                    color: #38bdf8;
                }
                .al-pgbtn:disabled { opacity: 0.3; cursor: default; }

                .al-card {
                    background: rgba(255,255,255,0.03);
                    border: 1px solid rgba(255,255,255,0.07);
                    border-radius: 16px;
                }

                .al-expanded {
                    background: rgba(255,255,255,0.03);
                    border-radius: 10px;
                    margin: 0 8px 6px;
                    padding: 16px 20px;
                    border: 1px solid rgba(255,255,255,0.08);
                }

                .al-label {
                    font-size: 11px;
                    font-weight: 700;
                    color: #475569;
                    letter-spacing: 0.06em;
                    margin-bottom: 8px;
                }

                .al-dot {
                    width: 8px; height: 8px;
                    border-radius: 50%;
                    background: #1d4ed8;
                    display: inline-block;
                    margin-right: 6px;
                }
            `}</style>

            <div className="al-root page-container" style={{ maxWidth: 1200, paddingBottom: 48 }}>
                {/* Header */}
                <div style={{ marginBottom: 28 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 6 }}>
                        <div style={{
                            width: 36, height: 36, borderRadius: 10,
                            background: 'linear-gradient(135deg, #1d4ed8, #0ea5e9)',
                            display: 'flex', alignItems: 'center', justifyContent: 'center',
                            fontSize: 18,
                        }}></div>
                        <h1 style={{ fontSize: 24, fontWeight: 800, color: '#f1f5f9', margin: 0 }}>
                            Պատմություն
                        </h1>
                    </div>
                    <p style={{ color: '#475569', fontSize: 13, margin: 0, paddingLeft: 48 }}>
                        Համակարգում կատարված գործողությունների մատյան
                    </p>
                </div>

                {/* Filters */}
                <div className="al-card" style={{ padding: 20, marginBottom: 16 }}>
                    <div style={{ display: 'flex', gap: 20, flexWrap: 'wrap', alignItems: 'flex-start' }}>
                        {/* Entity filter */}
                        <div style={{ flex: 1, minWidth: 200 }}>
                            <div className="al-label">ՕԲՅԵԿՏ</div>
                            <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                                <button
                                    className={`al-pill${entityFilter === '' ? ' active' : ''}`}
                                    onClick={() => { setEntityFilter(''); setPage(1); }}
                                >
                                    Բոլորը
                                </button>
                                {uniqueEntities.slice(0, 6).map(e => (
                                    <button
                                        key={e}
                                        className={`al-pill${entityFilter === e ? ' active' : ''}`}
                                        onClick={() => { setEntityFilter(entityFilter === e ? '' : e); setPage(1); }}
                                    >
                                        {armenianEntity(e)}
                                    </button>
                                ))}
                            </div>
                        </div>

                        {/* Date range */}
                        <div>
                            <div className="al-label">ԱՄՍԱԹԻՎ</div>
                            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                                <input
                                    className="al-input" type="date"
                                    value={dateFrom}
                                    onChange={e => { setDateFrom(e.target.value); setPage(1); }}
                                />
                                <span style={{ color: '#334155' }}>—</span>
                                <input
                                    className="al-input" type="date"
                                    value={dateTo}
                                    onChange={e => { setDateTo(e.target.value); setPage(1); }}
                                />
                            </div>
                        </div>

                        {/* Search */}
                        <div>
                            <div className="al-label">ՈՐՈՆՈՒՄ</div>
                            <input
                                className="al-input"
                                type="text"
                                placeholder="Անուն, գործ, գործողություն…"
                                value={search}
                                style={{ width: 210 }}
                                onChange={e => { setSearch(e.target.value); setPage(1); }}
                            />
                        </div>
                    </div>
                </div>

                {/* Stats row */}
                <div style={{ display: 'flex', gap: 8, marginBottom: 16, alignItems: 'center' }}>
                    <span style={{ fontSize: 12, color: '#475569' }}>
                        {filtered.length} գրառում
                    </span>
                    {filtered.length !== logs.length && (
                        <>
                            <span style={{ color: '#1e293b' }}>·</span>
                            <span style={{ fontSize: 12, color: '#38bdf8' }}>
                                {logs.length - filtered.length} թաքնված ֆիլտրով
                            </span>
                        </>
                    )}
                </div>

                {/* Error */}
                {error && (
                    <div style={{
                        background: 'rgba(239,68,68,0.1)',
                        border: '1px solid rgba(239,68,68,0.3)',
                        borderRadius: 12, padding: '12px 16px',
                        color: '#f87171', fontSize: 14, marginBottom: 16,
                    }}>
                        Սխալ: {error}
                    </div>
                )}

                {/* Table */}
                <div className="al-card" style={{ overflow: 'hidden' }}>
                    {/* Header */}
                    <div style={{
                        display: 'grid',
                        gridTemplateColumns: '155px 120px 130px 1fr 130px',
                        gap: 12,
                        padding: '11px 20px',
                        borderBottom: '1px solid rgba(255,255,255,0.06)',
                        background: 'rgba(0,0,0,0.2)',
                    }}>
                        {['Ամսաթիվ', 'Գործ', 'Օբյեկտ', 'Մանրամաս', 'Բժիշկ'].map(h => (
                            <span key={h} style={{ fontSize: 11, fontWeight: 700, color: '#334155', letterSpacing: '0.06em' }}>
                                {h.toUpperCase()}
                            </span>
                        ))}
                    </div>

                    {/* Body */}
                    <div style={{ padding: '8px 0' }}>
                        {loading ? (
                            <Skeleton />
                        ) : paginated.length === 0 ? (
                            <div style={{ padding: '56px 20px', textAlign: 'center' }}>
                                <div style={{ fontSize: 36, marginBottom: 12 }}>📋</div>
                                <div style={{ fontSize: 14, color: '#334155' }}>Գրառումներ չկան</div>
                            </div>
                        ) : (
                            paginated.map((log, i) => (
                                <div key={log.log_id}>
                                    <div
                                        className="al-row"
                                        style={{ animationDelay: `${i * 15}ms` }}
                                        onClick={() => setExpanded(expanded === log.log_id ? null : log.log_id)}
                                    >
                                        {/* Date */}
                                        <div style={{ fontSize: 11, color: '#475569', lineHeight: 1.4 }}>
                                            {fmt(log.created_at)}
                                        </div>

                                        {/* Action badge */}
                                        <div><ActionBadge action={log.action} /></div>

                                        {/* Entity — Armenian */}
                                        <div style={{ fontSize: 12, fontWeight: 600, color: '#94a3b8' }}>
                                            {armenianEntity(log.entity_type)}
                                        </div>

                                        {/* Human-readable detail */}
                                        <div style={{ fontSize: 12, color: '#64748b', lineHeight: 1.4 }}>
                                            {formatDetails(log.action, log.action_details)}
                                        </div>

                                        {/* Doctor */}
                                        <div style={{ fontSize: 12, fontWeight: 500, color: '#7c8fa3' }}>
                                            {log.doctor?.full_name ?? log.doctor_id.slice(0, 8) + '…'}
                                        </div>
                                    </div>

                                    {/* Expanded raw JSON (for power users) */}
                                    {expanded === log.log_id && (
                                        <div className="al-expanded">
                                            <div style={{ fontSize: 11, color: '#475569', marginBottom: 8, fontWeight: 700, letterSpacing: '0.05em' }}>
                                                ՄԱՆՐԱՄԱՍՆ ՏՎՅԱԼՆԵՐ
                                            </div>
                                            <pre style={{
                                                margin: 0, fontSize: 11,
                                                color: '#64748b', lineHeight: 1.6,
                                                whiteSpace: 'pre-wrap', wordBreak: 'break-all',
                                            }}>
                                                {JSON.stringify(log.action_details, null, 2)}
                                            </pre>
                                        </div>
                                    )}
                                </div>
                            ))
                        )}
                    </div>
                </div>

                {/* Pagination */}
                {totalPages > 1 && (
                    <div style={{ display: 'flex', gap: 6, marginTop: 16, justifyContent: 'center', alignItems: 'center' }}>
                        <button
                            className="al-pgbtn"
                            disabled={page === 1}
                            onClick={() => setPage(p => p - 1)}
                        >‹</button>

                        {pageNumbers().map(n => (
                            <button
                                key={n}
                                className={`al-pgbtn${page === n ? ' active' : ''}`}
                                onClick={() => setPage(n)}
                            >{n}</button>
                        ))}

                        <button
                            className="al-pgbtn"
                            disabled={page === totalPages}
                            onClick={() => setPage(p => p + 1)}
                        >›</button>

                        <span style={{ fontSize: 11, color: '#334155', marginLeft: 8 }}>
                            {page} / {totalPages}
                        </span>
                    </div>
                )}
            </div>
        </>
    );
}