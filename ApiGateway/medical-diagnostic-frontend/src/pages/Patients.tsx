import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { patientsApi, PatientApiResponse } from '@/api/patients';

/* ─── hardcoded tokens (safe to use anywhere) ──────────────────────── */
const surface  = '#1e293b';
const surf2    = '#273348';
const border   = '#2d3f55';
const textCol  = '#f1f5f9';
const muted    = '#64748b';
const faint    = '#94a3b8';
const accent   = '#0ea5e9';

function Skeleton() {
    return (
        <div style={{ padding: '8px 16px' }}>
            {[1,2,3,4,5,6].map(i => (
                <div key={i} style={{
                    height: 52, borderRadius: 10, marginBottom: 6,
                    background: `linear-gradient(90deg,${surface} 25%,${surf2} 50%,${surface} 75%)`,
                    backgroundSize: '200% 100%',
                    animation: 'pt-shimmer 1.4s ease-in-out infinite',
                    animationDelay: `${(i-1)*80}ms`,
                }} />
            ))}
        </div>
    );
}

function GenderBadge({ gender }: { gender: string }) {
    const map: Record<string, { label: string; color: string; bg: string }> = {
        male:   { label: 'Արական', color: '#60a5fa', bg: 'rgba(59,130,246,0.15)' },
        female: { label: 'Իգական', color: '#f472b6', bg: 'rgba(244,114,182,0.15)' },
        other:  { label: 'Այլ',    color: faint,     bg: surf2 },
    };
    const s = map[gender?.toLowerCase()] ?? map['other'];
    return (
        <span style={{ padding: '3px 10px', borderRadius: 99, background: s.bg, color: s.color, fontSize: 12, fontWeight: 600 }}>
            {s.label}
        </span>
    );
}

function StatCard({ label, value, color }: { label: string; value: number; color: string }) {
    return (
        <div
            style={{
                background: surf2,
                borderRadius: 14,
                padding: '20px 24px',
                border: `1px solid ${border}`,
                borderTop: `3px solid ${color}`,
                boxShadow: '0 2px 10px rgba(0,0,0,0.25)',
            }}
        >
            <div style={{ fontSize: 32, fontWeight: 800, color: textCol }}>
                {value}
            </div>

            <div style={{ fontSize: 13, color: muted, marginTop: 4, fontWeight: 500 }}>
                {label}
            </div>
        </div>
    );
}

/* ─── filter button ─────────────────────────────────────────────────── */
function FBtn({ label, active, onClick }: { label: string; active: boolean; onClick: () => void }) {
    return (
        <button onClick={onClick} style={{
            padding: '7px 16px', borderRadius: 99, cursor: 'pointer', fontSize: 13, fontWeight: 600,
            border: `1.5px solid ${active ? accent : border}`,
            background: active ? accent : 'transparent',
            color: active ? '#fff' : muted,
            transition: 'all 0.15s',
        }}>
            {label}
        </button>
    );
}

/* ─── page button ───────────────────────────────────────────────────── */
function PgBtn({ children, active, disabled, onClick }: { children: React.ReactNode; active?: boolean; disabled?: boolean; onClick: () => void }) {
    return (
        <button onClick={onClick} disabled={disabled} style={{
            width: 34, height: 34, borderRadius: 8, cursor: disabled ? 'not-allowed' : 'pointer',
            fontSize: 13, fontWeight: 600, display: 'flex', alignItems: 'center', justifyContent: 'center',
            border: `1.5px solid ${active ? accent : border}`,
            background: active ? accent : 'transparent',
            color: active ? '#fff' : faint,
            opacity: disabled ? 0.3 : 1,
            transition: 'all 0.15s',
        }}>
            {children}
        </button>
    );
}

export default function Patients() {
    const navigate = useNavigate();
    const [patients, setPatients] = useState<PatientApiResponse[]>([]);
    const [loading, setLoading]   = useState(true);
    const [error, setError]       = useState<string | null>(null);
    const [search, setSearch]     = useState('');
    const [genderFilter, setGenderFilter] = useState('');
    const [page, setPage]         = useState(1);
    const PER_PAGE = 15;

    useEffect(() => {
        patientsApi.list()
            .then(res => setPatients(res.data))
            .catch((e: Error) => setError(e.message))
            .finally(() => setLoading(false));
    }, []);

    const filtered = patients.filter(p => {
        const q = search.toLowerCase();
        const matchSearch = !q
            || (p.patientCode ?? '').toLowerCase().includes(q)
            || (p.firstName ?? '').toLowerCase().includes(q)
            || (p.lastName ?? '').toLowerCase().includes(q)
            || String(p.age).includes(q);
        const matchGender = !genderFilter || (p.gender ?? '').toLowerCase() === genderFilter;
        return matchSearch && matchGender;
    });

    const totalPages = Math.max(1, Math.ceil(filtered.length / PER_PAGE));
    const paginated  = filtered.slice((page-1)*PER_PAGE, page*PER_PAGE);
    const handleSearch = (v: string) => { setSearch(v); setPage(1); };
    const handleGender = (v: string) => { setGenderFilter(v === genderFilter ? '' : v); setPage(1); };
    const goTo = (p: number) => setPage(Math.min(Math.max(1, p), totalPages));

    const [hoveredRow, setHoveredRow] = useState<string | null>(null);

    return (
        <>
            <style>{`
                @keyframes pt-shimmer { 0%{background-position:-200% 0} 100%{background-position:200% 0} }
                @keyframes fadeUp { from{opacity:0;transform:translateY(10px)} to{opacity:1;transform:translateY(0)} }
            `}</style>

            <div className="page-container" style={{ maxWidth: 1100, paddingBottom: 48 }}>

                <div style={{ marginBottom: 28 }}>
                    <h1 style={{ fontSize: 26, fontWeight: 800, color: textCol }}>Հիվանդներ</h1>
                    <p style={{ fontSize: 14, color: muted, marginTop: 4 }}>Հիվանդների ամբողջական ցուցակ</p>
                </div>

                {!loading && (
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 14, marginBottom: 24 }}>
                        <StatCard label="Ընդհանուր հիվանդներ" value={patients.length} color={accent} />
                        <StatCard label="Արական" value={patients.filter(p => p.gender?.toLowerCase() === 'male').length} color="#3b82f6" />
                        <StatCard label="Իգական" value={patients.filter(p => p.gender?.toLowerCase() === 'female').length} color="#ec4899" />
                    </div>
                )}

                {error && (
                    <div style={{ background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.3)', borderRadius: 12, padding: '12px 16px', color: '#f87171', fontSize: 14, marginBottom: 20 }}>
                        Սխալ՝ {error}
                    </div>
                )}

                <div style={{ background: surface, borderRadius: 16, border: `1px solid ${border}`, overflow: 'hidden' }}>

                    {/* Toolbar */}
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 12, padding: '16px 20px', borderBottom: `1px solid ${border}` }}>
                        <div style={{ display: 'flex', gap: 8 }}>
                            <FBtn label="Բոլորը" active={genderFilter === ''} onClick={() => handleGender('')} />
                            <FBtn label="Արական" active={genderFilter === 'male'} onClick={() => handleGender('male')} />
                            <FBtn label="Իգական" active={genderFilter === 'female'} onClick={() => handleGender('female')} />
                        </div>
                        <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
                            <div style={{ position: 'relative' }}>
                                <span style={{ position: 'absolute', left: 11, top: '50%', transform: 'translateY(-50%)', fontSize: 13, color: muted, pointerEvents: 'none' }}></span>
                                <input
                                    type="text"
                                    placeholder="Որոնել կոդ, անուն..."
                                    value={search}
                                    onChange={e => handleSearch(e.target.value)}
                                    style={{
                                        padding: '8px 14px 8px 34px', borderRadius: 10,
                                        border: `1.5px solid ${border}`, background: surf2,
                                        color: textCol, fontSize: 13, width: 220, outline: 'none',
                                    }}
                                />
                            </div>
                            <button onClick={() => navigate('/patients/new')} style={{
                                padding: '9px 18px', borderRadius: 10, border: 'none',
                                background: accent, color: '#1e293b', fontSize: 13, fontWeight: 700, cursor: 'pointer',
                                display: 'flex', alignItems: 'center', gap: 6,
                                boxShadow: '0 2px 8px rgba(14,165,233,0.3)', whiteSpace: 'nowrap',
                            }}>
                                + Նոր հիվանդ
                            </button>
                        </div>
                    </div>

                    {/* Column headers */}
                    <div style={{ display: 'grid', gridTemplateColumns: '1.6fr 1.2fr 90px 130px auto', gap: 12, padding: '10px 20px', background: surf2, borderBottom: `1px solid ${border}` }}>
                        {['ԿՈԴ', 'ԱՆՈՒՆ', 'ՏԱՐԻՔ', 'ՍԵՌ', ''].map(h => (
                            <span key={h} style={{ fontSize: 11, fontWeight: 700, color: muted, letterSpacing: 0.6 }}>{h}</span>
                        ))}
                    </div>

                    {/* Rows */}
                    <div style={{ padding: '6px 0' }}>
                        {loading ? <Skeleton /> : paginated.length === 0 ? (
                            <div style={{ padding: '48px 20px', textAlign: 'center' }}>
                                <div style={{ fontSize: 32, marginBottom: 12 }}>👥</div>
                                <div style={{ fontSize: 15, color: muted }}>Հիվանդներ չեն գտնվել</div>
                            </div>
                        ) : paginated.map((p, i) => (
                            <div
                                key={p.patientId}
                                onMouseEnter={() => setHoveredRow(p.patientId)}
                                onMouseLeave={() => setHoveredRow(null)}
                                onClick={() => navigate(`/patients/${p.patientId}`)}
                                style={{
                                    display: 'grid', gridTemplateColumns: '1.6fr 1.2fr 90px 130px auto',
                                    alignItems: 'center', gap: 12, padding: '13px 20px',
                                    borderRadius: 10, cursor: 'pointer',
                                    border: `1px solid ${hoveredRow === p.patientId ? border : 'transparent'}`,
                                    background: hoveredRow === p.patientId ? surf2 : 'transparent',
                                    animation: 'fadeUp .3s ease both',
                                    animationDelay: `${i*30}ms`,
                                    transition: 'background 0.15s',
                                }}
                            >
                                <div style={{ fontSize: 13, fontWeight: 700, color: textCol, fontFamily: 'monospace' }}>{p.patientCode}</div>
                                <div style={{ fontSize: 13, fontWeight: 600, color: textCol }}>{p.firstName ?? '—'} {p.lastName ?? ''}</div>
                                <div style={{ fontSize: 13, fontWeight: 600, color: textCol }}>{p.age} տ.</div>
                                <GenderBadge gender={p.gender} />
                                <button
                                    onClick={e => { e.stopPropagation(); navigate(`/patients/${p.patientId}`); }}
                                    style={{
                                        padding: '5px 12px', borderRadius: 8, border: `1.5px solid ${border}`,
                                        background: 'transparent', color: faint, fontSize: 12, fontWeight: 600,
                                        cursor: 'pointer', whiteSpace: 'nowrap',
                                    }}
                                >
                                    Մանրամասներ →
                                </button>
                            </div>
                        ))}
                    </div>

                    {/* Pagination */}
                    {!loading && totalPages > 1 && (
                        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '14px 20px', borderTop: `1px solid ${border}` }}>
                            <span style={{ fontSize: 13, color: muted }}>
                                {filtered.length} հիվանդից {(page-1)*PER_PAGE+1}–{Math.min(page*PER_PAGE, filtered.length)}
                            </span>
                            <div style={{ display: 'flex', gap: 6 }}>
                                <PgBtn disabled={page === 1} onClick={() => goTo(page-1)}>‹</PgBtn>
                                {Array.from({ length: totalPages }, (_, i) => i+1)
                                    .filter(n => n === 1 || n === totalPages || Math.abs(n-page) <= 1)
                                    .reduce<(number|'…')[]>((acc, n, i, arr) => {
                                        if (i > 0 && n - (arr[i-1] as number) > 1) acc.push('…');
                                        acc.push(n); return acc;
                                    }, [])
                                    .map((n, i) => n === '…'
                                        ? <span key={`e${i}`} style={{ display: 'flex', alignItems: 'center', color: muted, fontSize: 13, padding: '0 2px' }}>…</span>
                                        : <PgBtn key={n} active={n === page} onClick={() => goTo(n as number)}>{n}</PgBtn>
                                    )}
                                <PgBtn disabled={page === totalPages} onClick={() => goTo(page+1)}>›</PgBtn>
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </>
    );
}