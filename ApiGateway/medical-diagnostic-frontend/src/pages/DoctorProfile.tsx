import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/store/auth';
import { statisticsApi, Overview } from '@/api/statistics';

function StatCard({ label, value, color }: { label: string; value: number | string; color: string }) {
    return (
        <div style={{
            background: 'rgba(255,255,255,0.03)',
            border: '1px solid rgba(255,255,255,0.07)',
            borderRadius: 14,
            padding: '20px 24px',
            borderTop: `3px solid ${color}`,
        }}>
            <div style={{ fontSize: 32, fontWeight: 800, color: '#f1f5f9' }}>{value}</div>
            <div style={{ fontSize: 13, color: '#475569', marginTop: 4, fontWeight: 500 }}>{label}</div>
        </div>
    );
}

export default function DoctorProfile() {
    const navigate = useNavigate();
    const { doctor } = useAuthStore();

    const [stats, setStats] = useState<Pick<Overview, 'totalCases' | 'completedCases' | 'pendingCases' | 'analyzingCases'>>({
        totalCases: 0,
        completedCases: 0,
        pendingCases: 0,
        analyzingCases: 0,
    });

    useEffect(() => {
        statisticsApi.getOverview()
            .then(data => {
                setStats({
                    totalCases: data.totalCases ?? 0,
                    completedCases: data.completedCases ?? 0,
                    pendingCases: data.pendingCases ?? 0,
                    analyzingCases: data.analyzingCases ?? 0,
                });
            })
            .catch(() => {});
    }, []);

    const initials = doctor?.full_name
        ? doctor.full_name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase()
        : 'DR';

    return (
        <>
            <style>{`
                @keyframes fadeUp { from{opacity:0;transform:translateY(12px)} to{opacity:1;transform:translateY(0)} }
                .profile-section {
                    animation: fadeUp .35s ease both;
                    background: rgba(255,255,255,0.03);
                    border: 1px solid rgba(255,255,255,0.07);
                    border-radius: 16px;
                    overflow: hidden;
                }
                .info-row {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    padding: 12px 0;
                    border-bottom: 1px solid rgba(255,255,255,0.04);
                }
                .info-row:last-child { border-bottom: none; }
                .quick-action-btn {
                    padding: 10px 16px;
                    border-radius: 10px;
                    border: 1px solid rgba(255,255,255,0.08);
                    background: rgba(255,255,255,0.05);
                    color: #e2e8f0;
                    font-size: 13px;
                    font-weight: 600;
                    cursor: pointer;
                    text-align: left;
                    transition: background 0.15s, border-color 0.15s;
                }
                .quick-action-btn:hover {
                    background: rgba(14,165,233,0.15);
                    border-color: rgba(14,165,233,0.35);
                    color: #38bdf8;
                }
            `}</style>

            <div className="page-container" style={{ maxWidth: 860, paddingBottom: 60 }}>

                {/* Page header */}
                <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginBottom: 32 }}>
                    <button
                        onClick={() => navigate(-1)}
                        style={{
                            padding: '7px 14px',
                            borderRadius: 10,
                            border: '1px solid rgba(255,255,255,0.1)',
                            background: 'rgba(255,255,255,0.04)',
                            color: '#94a3b8',
                            fontSize: 13,
                            fontWeight: 600,
                            cursor: 'pointer',
                        }}
                    >
                        ← Վերադառնալ
                    </button>
                    <h1 style={{ fontSize: 24, fontWeight: 800, color: '#f1f5f9' }}>Բժշկի պրոֆիլ</h1>
                </div>

                {/* Hero */}
                <div style={{
                    background: 'linear-gradient(135deg, #0c1a2e, #0f2d4a)',
                    border: '1px solid rgba(14,165,233,0.15)',
                    borderRadius: 20,
                    padding: '36px 40px',
                    marginBottom: 20,
                    position: 'relative',
                    overflow: 'hidden',
                }}>
                    {/* Decorative blobs */}
                    <div style={{
                        position: 'absolute', top: -40, right: -40,
                        width: 200, height: 200, borderRadius: '50%',
                        background: 'rgba(14,165,233,0.08)',
                        pointerEvents: 'none',
                    }} />
                    <div style={{
                        position: 'absolute', bottom: -60, left: '40%',
                        width: 160, height: 160, borderRadius: '50%',
                        background: 'rgba(99,102,241,0.06)',
                        pointerEvents: 'none',
                    }} />

                    <div style={{ display: 'flex', alignItems: 'center', gap: 24, position: 'relative' }}>
                        <div style={{
                            width: 80, height: 80, borderRadius: '50%',
                            background: 'linear-gradient(135deg, #0284c7, #6366f1)',
                            display: 'flex', alignItems: 'center', justifyContent: 'center',
                            flexShrink: 0,
                            boxShadow: '0 0 0 4px rgba(14,165,233,0.2)',
                        }}>
                            <span style={{ color: '#fff', fontSize: 28, fontWeight: 800 }}>{initials}</span>
                        </div>

                        <div>
                            <div style={{ fontSize: 24, fontWeight: 800, color: '#f1f5f9', marginBottom: 4 }}>
                                {doctor?.full_name ?? '—'}
                            </div>
                            <div style={{ fontSize: 14, color: 'rgba(255,255,255,0.4)', marginBottom: 10 }}>
                                {doctor?.email ?? '—'}
                            </div>
                            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                                <span style={{
                                    padding: '4px 12px', borderRadius: 99,
                                    background: 'rgba(14,165,233,0.15)',
                                    border: '1px solid rgba(14,165,233,0.25)',
                                    color: '#38bdf8', fontSize: 12, fontWeight: 700,
                                }}>
                                    {doctor?.role ?? 'Բժիշկ'}
                                </span>
                                {doctor?.specialization && (
                                    <span style={{
                                        padding: '4px 12px', borderRadius: 99,
                                        background: 'rgba(99,102,241,0.15)',
                                        border: '1px solid rgba(99,102,241,0.25)',
                                        color: '#a5b4fc', fontSize: 12, fontWeight: 700,
                                    }}>
                                        {doctor.specialization}
                                    </span>
                                )}
                                {doctor?.hospital_affiliation && (
                                    <span style={{
                                        padding: '4px 12px', borderRadius: 99,
                                        background: 'rgba(255,255,255,0.06)',
                                        border: '1px solid rgba(255,255,255,0.1)',
                                        color: 'rgba(255,255,255,0.6)', fontSize: 12, fontWeight: 600,
                                    }}>
                                        {doctor.hospital_affiliation}
                                    </span>
                                )}
                            </div>
                        </div>
                    </div>
                </div>

                {/* Stats */}
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 12, marginBottom: 20 }}>
                    <StatCard label="Ընդհանուր դեպքեր" value={stats.totalCases}     color="#0ea5e9" />
                    <StatCard label="Ավարտված"          value={stats.completedCases} color="#22c55e" />
                    <StatCard label="Սպասվող"            value={stats.pendingCases}   color="#f59e0b" />
                    <StatCard label="Վերլուծում"         value={stats.analyzingCases} color="#8b5cf6" />
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>

                    {/* Account info */}
                    <div className="profile-section">
                        <div style={{
                            padding: '16px 24px',
                            borderBottom: '1px solid rgba(255,255,255,0.06)',
                            fontSize: 14, fontWeight: 700, color: '#e2e8f0',
                        }}>
                            Պրոֆիլի տվյալներ
                        </div>
                        <div style={{ padding: '8px 24px 16px' }}>
                            {[
                                { label: 'Անուն',         value: doctor?.full_name },
                                { label: 'Էլ. հասցե',    value: doctor?.email },
                                { label: 'Կարգավիճակ',   value: doctor?.role ?? 'Բժիշկ' },
                                { label: 'Մասնագիտություն', value: doctor?.specialization ?? '—' },
                                { label: 'Հիվանդանոց',   value: doctor?.hospital_affiliation ?? '—' },
                            ].map(r => (
                                <div key={r.label} className="info-row">
                                    <span style={{ fontSize: 13, color: '#475569', fontWeight: 500 }}>{r.label}</span>
                                    <span style={{
                                        fontSize: 13, color: '#cbd5e1', fontWeight: 600,
                                        maxWidth: 200, textAlign: 'right',
                                        overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap',
                                    }}>
                                        {r.value ?? '—'}
                                    </span>
                                </div>
                            ))}
                        </div>
                    </div>

                    {/* Quick actions + system info */}
                    <div className="profile-section">
                        <div style={{
                            padding: '16px 24px',
                            borderBottom: '1px solid rgba(255,255,255,0.06)',
                            fontSize: 14, fontWeight: 700, color: '#e2e8f0',
                        }}>
                            Արագ գործողություններ
                        </div>
                        <div style={{ padding: '16px 24px', display: 'flex', flexDirection: 'column', gap: 10 }}>
                            <button className="quick-action-btn" onClick={() => navigate('/audit')}>
                                📋 Պատմություն
                            </button>
                        </div>

                        <div style={{ padding: '8px 24px 16px', borderTop: '1px solid rgba(255,255,255,0.05)' }}>
                            {[
                                { label: 'Բժշկի ID',  value: doctor?.doctor_id ?? '—', mono: true },
                                { label: 'Համակարգ', value: 'Medical Diagnostic v1.0' },
                                { label: 'Միջավայր', value: 'Production' },
                            ].map(r => (
                                <div key={r.label} className="info-row">
                                    <span style={{ fontSize: 13, color: '#475569', fontWeight: 500 }}>{r.label}</span>
                                    <span style={{
                                        fontSize: r.mono ? 11 : 13,
                                        color: r.mono ? '#64748b' : '#cbd5e1',
                                        fontWeight: 600,
                                        fontFamily: r.mono ? 'monospace' : 'inherit',
                                        maxWidth: 180, textAlign: 'right',
                                        overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap',
                                    }}>
                                        {r.mono ? String(r.value).slice(0, 16) + '…' : r.value}
                                    </span>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
}