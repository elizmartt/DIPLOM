import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { patientsApi, PatientApiResponse } from '@/api/patients';
import { casesApi, DiagnosisCaseResponse } from '@/api/cases';

const surface  = '#1e293b';
const surf2    = '#273348';
const border   = '#2d3f55';
const textCol  = '#f1f5f9';
const muted    = '#64748b';
const faint    = '#94a3b8';
const accent   = '#0ea5e9';

function SkeletonBlock({ h = 80 }: { h?: number }) {
    return (
        <div style={{
            height: h, borderRadius: 12,
            background: `linear-gradient(90deg,${surface} 25%,${surf2} 50%,${surface} 75%)`,
            backgroundSize: '200% 100%',
            animation: 'shimmer 1.4s ease-in-out infinite',
        }} />
    );
}

function InfoRow({ label, value }: { label: string; value: React.ReactNode }) {
    return (
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '11px 0', borderBottom: `1px solid ${surf2}` }}>
            <span style={{ fontSize: 13, color: muted, fontWeight: 500 }}>{label}</span>
            <span style={{ fontSize: 13, color: textCol, fontWeight: 600 }}>{value ?? '—'}</span>
        </div>
    );
}

const STATUS_META: Record<string, { bg: string; color: string; dot: string; label: string }> = {
    pending:                 { bg: 'rgba(245,158,11,0.12)',  color: '#fbbf24', dot: '#f59e0b', label: 'Սպասվող' },
    data_collection:         { bg: 'rgba(59,130,246,0.12)',  color: '#60a5fa', dot: '#3b82f6', label: 'Տվյալների հավաք' },
    processing:              { bg: 'rgba(139,92,246,0.12)',  color: '#a78bfa', dot: '#8b5cf6', label: 'Մշակում' },
    analyzing:               { bg: 'rgba(34,197,94,0.12)',   color: '#4ade80', dot: '#22c55e', label: 'Վերլուծում' },
    completed:               { bg: 'rgba(34,197,94,0.12)',   color: '#4ade80', dot: '#16a34a', label: 'Ավարտված' },
    completed_with_warnings: { bg: 'rgba(34,197,94,0.12)',   color: '#4ade80', dot: '#16a34a', label: 'Ավարտվել է' },
    failed:                  { bg: 'rgba(239,68,68,0.12)',   color: '#f87171', dot: '#ef4444', label: 'Ձախողված' },
};

function StatusBadge({ status }: { status: string }) {
    const s = STATUS_META[status] ?? { bg: surf2, color: faint, dot: muted, label: status };
    return (
        <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5, padding: '3px 10px', borderRadius: 99, background: s.bg, color: s.color, fontSize: 12, fontWeight: 600 }}>
            <span style={{ width: 6, height: 6, borderRadius: '50%', background: s.dot }} />
            {s.label}
        </span>
    );
}

const TYPE_LABELS: Record<string, string> = {
    brain_tumor: 'Ուղեղի ուռուցք',
    lung_cancer: 'Թոքերի քաղցկեղ',
};

function StatCard({ label, value, color }: { label: string; value: React.ReactNode; color: string }) {
    return (
        <div style={{ background: surface, borderRadius: 14, padding: '20px 24px', border: `1px solid ${border}`, borderTop: `3px solid ${color}` }}>
            <div style={{ fontSize: 28, fontWeight: 800, color: textCol }}>{value}</div>
            <div style={{ fontSize: 13, color: muted, marginTop: 4, fontWeight: 500 }}>{label}</div>
        </div>
    );
}

export default function PatientDetail() {
    const { id }   = useParams<{ id: string }>();
    const navigate = useNavigate();
    const [patient, setPatient]           = useState<PatientApiResponse | null>(null);
    const [cases, setCases]               = useState<DiagnosisCaseResponse[]>([]);
    const [loading, setLoading]           = useState(true);
    const [casesLoading, setCasesLoading] = useState(true);
    const [error, setError]               = useState<string | null>(null);
    const [hoveredCase, setHoveredCase]   = useState<string | null>(null);

    useEffect(() => {
        if (!id) return;
        patientsApi.get(id)
            .then(setPatient)
            .catch((e: Error) => setError(e.message))
            .finally(() => setLoading(false));
        casesApi.list(undefined, 200)
            .then(res => setCases(res.filter(c => c.patientId === id)))
            .catch(() => setCases([]))
            .finally(() => setCasesLoading(false));
    }, [id]);

    if (loading) {
        return (
            <div className="page-container" style={{ maxWidth: 900, paddingBottom: 48 }}>
                <style>{`@keyframes shimmer{0%{background-position:-200% 0}100%{background-position:200% 0}}`}</style>
                <div style={{ marginBottom: 24 }}><SkeletonBlock h={40} /></div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                    <SkeletonBlock h={200} /><SkeletonBlock h={300} />
                </div>
            </div>
        );
    }

    if (error || !patient) {
        return (
            <div className="page-container" style={{ maxWidth: 900 }}>
                <div style={{ background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.3)', borderRadius: 12, padding: '16px 20px', color: '#f87171', fontSize: 14 }}>
                    Սխալ՝ {error ?? 'Հիվանդը չի գտնվել'}
                </div>
            </div>
        );
    }

    const completedCases = cases.filter(c => c.status === 'completed' || c.status === 'completed_with_warnings').length;
    const latestCase     = cases[0];
    const displayName    = `${patient.firstName ?? ''} ${patient.lastName ?? ''}`.trim() || 'Անանուն';
    const genderLabel    = patient.gender === 'male' ? 'Արական' : patient.gender === 'female' ? 'Իգական' : 'Այլ';

    const sectionStyle = {
        background: surface,
        borderRadius: 16,
        border: `1px solid ${border}`,
        overflow: 'hidden' as const,
        animation: 'fadeUp .35s ease both',
    };

    return (
        <>
            <style>{`
                @keyframes shimmer { 0%{background-position:-200% 0} 100%{background-position:200% 0} }
                @keyframes fadeUp  { from{opacity:0;transform:translateY(12px)} to{opacity:1;transform:translateY(0)} }
            `}</style>

            <div className="page-container" style={{ maxWidth: 900, paddingBottom: 48 }}>

                <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginBottom: 28 }}>
                    <button onClick={() => navigate('/patients')} style={{
                        padding: '7px 14px', borderRadius: 10, border: `1.5px solid ${border}`,
                        background: 'transparent', color: faint, fontSize: 13, fontWeight: 600, cursor: 'pointer',
                    }}>
                        ← Վերադառնալ
                    </button>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
                        <div style={{ width: 48, height: 48, borderRadius: '50%', background: 'linear-gradient(135deg,#0ea5e9,#6366f1)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                            <span style={{ color: '#fff', fontSize: 18, fontWeight: 800 }}>
                                {(patient.patientCode ?? 'AN').slice(0, 2).toUpperCase()}
                            </span>
                        </div>
                        <div>
                            <h1 style={{ fontSize: 22, fontWeight: 800, color: textCol, fontFamily: 'monospace' }}>{patient.patientCode}</h1>
                            <p style={{ fontSize: 13, color: muted, marginTop: 2 }}>{displayName} · {patient.age} տ. · {genderLabel}</p>
                        </div>
                    </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 14, marginBottom: 20 }}>
                    <StatCard label="Դեպքերի քանակ" value={cases.length} color={accent} />
                    <StatCard label="Ավարտված" value={completedCases} color="#22c55e" />
                    <StatCard
                        label="Վերջին դեպք"
                        color="#8b5cf6"
                        value={latestCase
                            ? new Date(latestCase.createdAt).toLocaleDateString('en-GB', {
                                day: '2-digit', month: '2-digit', year: 'numeric',
                            })
                            : '—'}
                    />
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>

                    <div style={sectionStyle}>
                        <div style={{ padding: '16px 24px', borderBottom: `1px solid ${border}`, fontSize: 14, fontWeight: 700, color: textCol }}>
                            Հիվանդի մանրամասներ
                        </div>
                        <div style={{ padding: '8px 24px 16px' }}>
                            <InfoRow label="Հիվանդի կոդ" value={<span style={{ fontFamily: 'monospace', fontWeight: 700, color: accent }}>{patient.patientCode}</span>} />
                            <InfoRow label="Անուն" value={displayName} />
                            <InfoRow label="Տարիք" value={`${patient.age} տ.`} />
                            <InfoRow label="Սեռ" value={genderLabel} />
                            <InfoRow
                                label="Ստեղծվել է"
                                value={patient.createdAt ? new Date(patient.createdAt).toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' }) : '—'}
                            />
                            <InfoRow
                                label="Թարմացվել է"
                                value={patient.updatedAt ? new Date(patient.updatedAt).toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' }) : '—'}
                            />
                        </div>
                    </div>

                    <div style={sectionStyle}>
                        <div style={{ padding: '16px 24px', borderBottom: `1px solid ${border}`, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                            <span style={{ fontSize: 14, fontWeight: 700, color: textCol }}>
                                Դեպքերի պատմություն
                                {cases.length > 0 && (
                                    <span style={{ marginLeft: 8, padding: '2px 8px', borderRadius: 99, background: surf2, color: muted, fontSize: 12, fontWeight: 700 }}>
                                        {cases.length}
                                    </span>
                                )}
                            </span>
                            <button onClick={() => navigate('/cases/new', { state: {
                                    patientId: patient.patientId, patientCode: patient.patientCode,
                                    patientName: `${patient.firstName} ${patient.lastName}`, patientAge: patient.age,
                                }})} style={{
                                padding: '6px 14px', borderRadius: 8, border: 'none',
                                background: accent, color: '#fff', fontSize: 12, fontWeight: 700, cursor: 'pointer',
                                boxShadow: '0 2px 6px rgba(14,165,233,0.25)',
                            }}>
                                + Նոր դեպք
                            </button>
                        </div>

                        <div style={{ padding: '8px 0' }}>
                            {casesLoading ? (
                                <div style={{ padding: '16px 24px', display: 'flex', flexDirection: 'column', gap: 8 }}>
                                    {[1,2,3].map(i => <SkeletonBlock key={i} h={50} />)}
                                </div>
                            ) : cases.length === 0 ? (
                                <div style={{ padding: '40px 20px', textAlign: 'center' }}>
                                    <div style={{ fontSize: 28, marginBottom: 10 }}>🗂</div>
                                    <div style={{ fontSize: 14, color: muted }}>Դեպքեր չկան</div>
                                </div>
                            ) : (
                                <>
                                    <div style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr 1fr 1fr', gap: 12, padding: '8px 20px', background: surf2, borderBottom: `1px solid ${border}` }}>
                                        {['ԴԵՊՔ', 'ՏԵՍԱԿ', 'ԿԱՐԳԱՎԻՃԱԿ', 'ԱՄՍԱԹԻՎ'].map(h => (
                                            <span key={h} style={{ fontSize: 11, fontWeight: 700, color: muted, letterSpacing: 0.5 }}>{h}</span>
                                        ))}
                                    </div>
                                    {cases.map(c => (
                                        <div
                                            key={c.caseId}
                                            onMouseEnter={() => setHoveredCase(c.caseId)}
                                            onMouseLeave={() => setHoveredCase(null)}
                                            onClick={() => navigate(`/cases/${c.caseId}`)}
                                            style={{
                                                display: 'grid', gridTemplateColumns: '1.4fr 1fr 1fr 1fr',
                                                gap: 12, alignItems: 'center', padding: '14px 20px',
                                                borderRadius: 10, cursor: 'pointer',
                                                border: `1px solid ${hoveredCase === c.caseId ? border : 'transparent'}`,
                                                background: hoveredCase === c.caseId ? surf2 : 'transparent',
                                                transition: 'background 0.15s',
                                            }}
                                        >
                                            <span style={{ fontSize: 13, fontWeight: 700, fontFamily: 'monospace', color: textCol }}>{c.caseId.slice(0,8).toUpperCase()}</span>
                                            <span style={{ fontSize: 12, fontWeight: 600, background: 'rgba(99,102,241,0.15)', color: '#a5b4fc', padding: '3px 8px', borderRadius: 6, width: 'fit-content' }}>
                                                {TYPE_LABELS[c.diagnosisType] ?? c.diagnosisType}
                                            </span>
                                            <StatusBadge status={c.status} />
                                            <span style={{ fontSize: 12, color: muted }}>
                                                {new Date(c.createdAt).toLocaleDateString('en-GB', {
                                                    day: '2-digit', month: '2-digit', year: 'numeric'
                                                })}
                                            </span>
                                        </div>
                                    ))}
                                </>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
}