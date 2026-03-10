import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { casesApi, DiagnosisCaseResponse } from '@/api/cases';
import { formatDate } from '@/lib/utils';

// ── Constants ────────────────────────────────────────────────────────────────
const FILTERS = [
    { label: 'Բոլորը',      value: '' },
    { label: 'Սպասվող',     value: 'pending' },
    { label: 'Տվյալ հավաք', value: 'data_collection' },
    { label: 'Վերլուծում',  value: 'analyzing' },
    { label: 'Ավարտված',    value: 'completed' },
    { label: 'Ձախողված',    value: 'failed' },
];

// ── StatusBadge ──────────────────────────────────────────────────────────────
const STATUS_MAP: Record<string, { bg: string; color: string; dot: string; border: string; label: string }> = {
    pending:                 { bg: 'rgba(245,158,11,0.12)', color: '#fbbf24', dot: '#f59e0b', border: 'rgba(245,158,11,0.25)', label: 'Մշակման փուլում' },
    data_collection:         { bg: 'rgba(59,130,246,0.12)', color: '#60a5fa', dot: '#3b82f6', border: 'rgba(59,130,246,0.25)', label: 'Տվյալների Հավաքագրում' },
    processing:              { bg: 'rgba(139,92,246,0.12)', color: '#a78bfa', dot: '#8b5cf6', border: 'rgba(139,92,246,0.25)', label: 'Մշակում' },
    analyzing:               { bg: 'rgba(139,92,246,0.12)', color: '#a78bfa', dot: '#8b5cf6', border: 'rgba(139,92,246,0.25)', label: 'Վերլուծում' },
    completed:               { bg: 'rgba(16,185,129,0.12)', color: '#34d399', dot: '#10b981', border: 'rgba(16,185,129,0.25)', label: 'Ավարտված' },
    completed_with_warnings: { bg: 'rgba(16,185,129,0.12)', color: '#34d399', dot: '#10b981', border: 'rgba(16,185,129,0.25)', label: 'Ավարտված' },
    failed:                  { bg: 'rgba(239,68,68,0.12)',  color: '#f87171', dot: '#ef4444', border: 'rgba(239,68,68,0.25)',  label: 'Ձախողված' },
};

function StatusBadge({ status }: { status: string }) {
    const s = STATUS_MAP[status] ?? { bg: 'rgba(148,163,184,0.12)', color: '#94a3b8', dot: '#64748b', border: 'rgba(148,163,184,0.2)', label: status };
    return (
        <span style={{
            display: 'inline-flex', alignItems: 'center', gap: 5,
            padding: '3px 10px', borderRadius: 99,
            background: s.bg, color: s.color,
            border: `1px solid ${s.border}`,
            fontSize: 11, fontWeight: 700,
        }}>
            <span style={{ width: 6, height: 6, borderRadius: '50%', background: s.dot, flexShrink: 0 }} />
            {s.label}
        </span>
    );
}

// ── SkeletonList ─────────────────────────────────────────────────────────────
function SkeletonList() {
    return (
        <div style={{ padding: '16px 20px' }}>
            {[1, 2, 3, 4, 5].map(i => (
                <div key={i} style={{
                    height: 56, borderRadius: 12, marginBottom: 8,
                    background: 'linear-gradient(90deg, #1e293b 25%, #243044 50%, #1e293b 75%)',
                    backgroundSize: '200% 100%',
                    animation: 'shimmer 1.4s ease-in-out infinite',
                    animationDelay: `${(i - 1) * 80}ms`,
                }} />
            ))}
        </div>
    );
}

// ── ConfirmModal ─────────────────────────────────────────────────────────────
function ConfirmModal({ caseId, onConfirm, onCancel, loading }: {
    caseId: string;
    onConfirm: () => void;
    onCancel: () => void;
    loading: boolean;
}) {
    return (
        <div style={{
            position: 'fixed', inset: 0,
            background: 'rgba(0,0,0,0.7)',
            backdropFilter: 'blur(6px)',
            zIndex: 1000,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
        }} onClick={onCancel}>
            <div style={{
                background: '#0f172a',
                border: '1px solid rgba(255,255,255,0.1)',
                borderRadius: 20,
                padding: 36,
                width: '100%', maxWidth: 400,
                boxShadow: '0 24px 64px rgba(0,0,0,0.6)',
            }} onClick={e => e.stopPropagation()}>
                <div style={{
                    width: 56, height: 56, borderRadius: '50%',
                    background: 'rgba(239,68,68,0.12)',
                    border: '1px solid rgba(239,68,68,0.25)',
                    display: 'flex', alignItems: 'center', justifyContent: 'center',
                    margin: '0 auto 20px', fontSize: 24,
                }}>
                    🗑
                </div>
                <h2 style={{ fontSize: 20, fontWeight: 800, color: '#f1f5f9', textAlign: 'center', marginBottom: 8 }}>
                    Ջնջե՞լ դեպքը
                </h2>
                <p style={{ fontSize: 14, color: '#475569', textAlign: 'center', lineHeight: 1.6, marginBottom: 28 }}>
                    Դեպք <span style={{ fontFamily: 'monospace', fontWeight: 700, color: '#94a3b8' }}>
                        {caseId.slice(0, 8).toUpperCase()}
                    </span>-ը կջնջվի անդառնալիորեն։
                </p>
                <div style={{ display: 'flex', gap: 10 }}>
                    <button onClick={onCancel} style={{
                        flex: 1, padding: 12, borderRadius: 12,
                        border: '1px solid rgba(255,255,255,0.1)',
                        background: 'rgba(255,255,255,0.04)',
                        color: '#94a3b8', fontSize: 14, fontWeight: 600, cursor: 'pointer',
                    }}>
                        Չեղարկել
                    </button>
                    <button onClick={onConfirm} disabled={loading} style={{
                        flex: 1, padding: 12, borderRadius: 12,
                        border: 'none',
                        background: loading ? 'rgba(239,68,68,0.4)' : '#ef4444',
                        color: '#fff', fontSize: 14, fontWeight: 700,
                        cursor: loading ? 'not-allowed' : 'pointer',
                        opacity: loading ? 0.7 : 1,
                    }}>
                        {loading ? 'Ջնջում…' : 'Այո, ջնջել'}
                    </button>
                </div>
            </div>
        </div>
    );
}

// ── Main Component ───────────────────────────────────────────────────────────
export default function Cases() {
    const navigate = useNavigate();
    const [cases, setCases] = useState<DiagnosisCaseResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [activeFilter, setActiveFilter] = useState('');
    const [search, setSearch] = useState('');
    const [deleteTarget, setDeleteTarget] = useState<string | null>(null);
    const [deleteLoading, setDeleteLoading] = useState(false);

    const load = (status?: string) => {
        setLoading(true);
        setError(null);
        casesApi.list(status || undefined)
            .then(setCases)
            .catch((e: Error) => setError(e.message))
            .finally(() => setLoading(false));
    };

    useEffect(() => { load(); }, []);

    const handleFilterChange = (value: string) => {
        setActiveFilter(value);
        load(value);
    };

    const handleDelete = async () => {
        if (!deleteTarget) return;
        setDeleteLoading(true);
        try {
            await casesApi.delete(deleteTarget);
            setCases(prev => prev.filter(c => c.caseId !== deleteTarget));
            setDeleteTarget(null);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : 'Ջնջման սխալ');
        } finally {
            setDeleteLoading(false);
        }
    };

    const filtered = cases.filter(c => {
        if (!search) return true;
        const q = search.toLowerCase();
        return (
            (c.patientName ?? '').toLowerCase().includes(q) ||
            (c.patientCode ?? '').toLowerCase().includes(q) ||
            (c.doctorName ?? '').toLowerCase().includes(q)
        );
    });

    const typeLabel = (t: string) => {
        const map: Record<string, string> = {
            brain: 'Ուղեղ',
            lung: 'Թոք',
            brain_tumor: 'Ուղեղի ուռուցք',
            lung_cancer: 'Թոքերի քաղցկեղ',
        };
        return map[t] ?? t;
    };

    return (
        <>
            <style>{`
                @keyframes shimmer {
                    0%   { background-position: -200% 0 }
                    100% { background-position:  200% 0 }
                }
                @keyframes fadeUp {
                    from { opacity: 0; transform: translateY(8px) }
                    to   { opacity: 1; transform: translateY(0) }
                }
                .case-row {
                    display: grid;
                    grid-template-columns: 2fr 1.2fr 1fr 1.4fr 1fr auto;
                    align-items: center;
                    gap: 12px;
                    padding: 14px 20px;
                    border-radius: 10px;
                    cursor: pointer;
                    transition: background 0.12s, border-color 0.12s;
                    animation: fadeUp .25s ease both;
                    border: 1px solid transparent;
                    margin: 0 8px;
                }
                .case-row:hover {
                    background: rgba(255,255,255,0.04);
                    border-color: rgba(255,255,255,0.08);
                }
                .filter-btn {
                    padding: 6px 15px;
                    border-radius: 99px;
                    border: 1px solid rgba(255,255,255,0.1);
                    background: rgba(255,255,255,0.04);
                    color: #64748b;
                    font-size: 12px;
                    font-weight: 600;
                    cursor: pointer;
                    transition: all 0.15s;
                    white-space: nowrap;
                }
                .filter-btn:hover { background: rgba(255,255,255,0.07); color: #94a3b8; }
                .filter-btn.active {
                    background: rgba(14,165,233,0.2);
                    border-color: rgba(14,165,233,0.45);
                    color: #38bdf8;
                }
                .search-input {
                    padding: 8px 14px;
                    border-radius: 10px;
                    border: 1px solid rgba(255,255,255,0.1);
                    background: rgba(255,255,255,0.05);
                    color: #e2e8f0;
                    font-size: 13px;
                    width: 220px;
                    outline: none;
                    transition: border-color 0.15s;
                }
                .search-input:focus { border-color: rgba(14,165,233,0.45); }
                .search-input::placeholder { color: #334155; }
                .delete-btn {
                    padding: 5px 12px;
                    border-radius: 7px;
                    border: 1px solid rgba(239,68,68,0.25);
                    background: rgba(239,68,68,0.06);
                    color: #f87171;
                    font-size: 11px;
                    font-weight: 700;
                    cursor: pointer;
                    transition: all 0.15s;
                    white-space: nowrap;
                }
                .delete-btn:hover {
                    background: rgba(239,68,68,0.15);
                    border-color: rgba(239,68,68,0.4);
                }
                .type-badge {
                    font-size: 11px;
                    font-weight: 700;
                    color: #818cf8;
                    background: rgba(99,102,241,0.12);
                    border: 1px solid rgba(99,102,241,0.2);
                    padding: 3px 8px;
                    border-radius: 6px;
                    display: inline-block;
                }
            `}</style>

            <div className="page-container" style={{ maxWidth: 1200, paddingBottom: 48 }}>

                {/* Header */}
                <div style={{ marginBottom: 28 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 4 }}>
                        <div style={{
                            width: 36, height: 36, borderRadius: 10,
                            background: 'linear-gradient(135deg, #1d4ed8, #0ea5e9)',
                            display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 18,
                        }}></div>
                        <h1 style={{ fontSize: 24, fontWeight: 800, color: '#f1f5f9', margin: 0 }}>Դեպքեր</h1>
                    </div>
                    <p style={{ fontSize: 13, color: '#475569', margin: 0, paddingLeft: 48 }}>
                        Ախտորոշիչ դեպքերի ցուցակ
                    </p>
                </div>

                {/* Toolbar */}
                <div style={{
                    display: 'flex', alignItems: 'center',
                    justifyContent: 'space-between',
                    gap: 12, flexWrap: 'wrap', marginBottom: 16,
                }}>
                    <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                        {FILTERS.map(f => (
                            <button
                                key={f.value}
                                className={`filter-btn${activeFilter === f.value ? ' active' : ''}`}
                                onClick={() => handleFilterChange(f.value)}
                            >
                                {f.label}
                            </button>
                        ))}
                    </div>

                    <div style={{ display: 'flex', gap: 8 }}>
                        <input
                            className="search-input"
                            placeholder="Որոնել դեպք, հիվանդ…"
                            value={search}
                            onChange={e => setSearch(e.target.value)}
                        />
                        <button
                            onClick={() => navigate('/cases/new')}
                            style={{
                                padding: '8px 18px', borderRadius: 10,
                                border: 'none',
                                background: 'linear-gradient(135deg, #0284c7, #0ea5e9)',
                                color: '#fff', fontSize: 13, fontWeight: 700,
                                cursor: 'pointer', whiteSpace: 'nowrap',
                                boxShadow: '0 2px 8px rgba(14,165,233,0.3)',
                            }}
                        >
                            + Նոր դեպք
                        </button>
                    </div>
                </div>

                {/* Error */}
                {error && (
                    <div style={{
                        background: 'rgba(239,68,68,0.1)',
                        border: '1px solid rgba(239,68,68,0.3)',
                        borderRadius: 12, padding: '12px 16px',
                        color: '#f87171', fontSize: 14, marginBottom: 16,
                    }}>
                        Սխալ՝ {error}
                    </div>
                )}

                {/* Table */}
                <div style={{
                    background: 'rgba(255,255,255,0.03)',
                    border: '1px solid rgba(255,255,255,0.07)',
                    borderRadius: 16,
                    overflow: 'hidden',
                }}>
                    {/* Header row */}
                    <div style={{
                        display: 'grid',
                        gridTemplateColumns: '2fr 1.2fr 1fr 1.4fr 1fr auto',
                        gap: 12, padding: '11px 20px',
                        background: 'rgba(0,0,0,0.2)',
                        borderBottom: '1px solid rgba(255,255,255,0.06)',
                    }}>
                        {['ՀԻՎԱՆԴ', 'ԲԺԻՇԿ', 'ՏԵՍԱԿ', 'ԿԱՐԳԱՎԻՃԱԿ', 'ԱՄՍԱԹԻՎ', ''].map(h => (
                            <span key={h} style={{
                                fontSize: 10, fontWeight: 700,
                                color: '#334155', letterSpacing: '0.07em',
                            }}>
                                {h}
                            </span>
                        ))}
                    </div>

                    {/* Rows */}
                    <div style={{ padding: '8px 0' }}>
                        {loading ? (
                            <SkeletonList />
                        ) : filtered.length === 0 ? (
                            <div style={{ padding: '56px 20px', textAlign: 'center' }}>
                                <div style={{ fontSize: 36, marginBottom: 12 }}>🗂</div>
                                <div style={{ fontSize: 14, color: '#334155' }}>Դեպքեր չեն գտնվել</div>
                            </div>
                        ) : (
                            filtered.map((c, i) => (
                                <div
                                    key={c.caseId}
                                    className="case-row"
                                    style={{ animationDelay: `${i * 30}ms` }}
                                    onClick={() => navigate(`/cases/${c.caseId}`)}
                                >
                                    {/* Patient */}
                                    <div>
                                        <div style={{ fontSize: 13, fontWeight: 700, color: '#e2e8f0' }}>
                                            {c.patientCode ?? '—'}
                                        </div>
                                        <div style={{ fontSize: 12, color: '#475569', marginTop: 2 }}>
                                            {c.patientName ?? 'Անանուն'}
                                            {c.patientAge != null && ` · ${c.patientAge} տ.`}
                                        </div>
                                    </div>

                                    {/* Doctor */}
                                    <div style={{ fontSize: 13, color: '#7c8fa3', fontWeight: 500 }}>
                                        {c.doctorName ?? '—'}
                                    </div>

                                    {/* Type */}
                                    <div>
                                        <span className="type-badge">{typeLabel(c.diagnosisType)}</span>
                                    </div>

                                    {/* Status */}
                                    <StatusBadge status={c.status} />

                                    {/* Date */}
                                    <div style={{ fontSize: 11, color: '#475569', fontWeight: 500 }}>
                                        {formatDate(c.createdAt)}
                                    </div>

                                    {/* Delete */}
                                    <button
                                        className="delete-btn"
                                        onClick={e => { e.stopPropagation(); setDeleteTarget(c.caseId); }}
                                    >
                                        Ջնջել
                                    </button>
                                </div>
                            ))
                        )}
                    </div>

                    {!loading && filtered.length > 0 && (
                        <div style={{
                            padding: '11px 20px',
                            borderTop: '1px solid rgba(255,255,255,0.05)',
                            fontSize: 12, color: '#334155',
                        }}>
                            Ընդամենը {filtered.length} դեպք
                        </div>
                    )}
                </div>
            </div>

            {deleteTarget && (
                <ConfirmModal
                    caseId={deleteTarget}
                    onConfirm={handleDelete}
                    onCancel={() => setDeleteTarget(null)}
                    loading={deleteLoading}
                />
            )}
        </>
    );
}