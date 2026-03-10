import { useEffect, useState } from 'react';
import apiClient from '@/lib/axios';

interface SystemStats {
    totalCases: number;
    brainCases: number;
    lungCases: number;
    completedCases: number;
    pendingCases: number;
    analyzingCases: number;
    casesLast30Days: number;
}

function StatCard({ label, value, sub, accent }: {
    label: string;
    value: number | string;
    sub?: string;
    accent: string;
}) {
    return (
        <div style={{
            background: '#fff', borderRadius: 14, padding: '24px 28px',
            boxShadow: '0 1px 4px rgba(0,0,0,0.06)', borderTop: `4px solid ${accent}`,
            display: 'flex', flexDirection: 'column', gap: 4
        }}>
            <div style={{ fontSize: 13, color: '#64748b', fontWeight: 600 }}>{label}</div>
            <div style={{ fontSize: 36, fontWeight: 800, color: '#0f172a', lineHeight: 1.1 }}>{value}</div>
            {sub && <div style={{ fontSize: 12, color: '#94a3b8' }}>{sub}</div>}
        </div>
    );
}

export default function AdminDashboard() {
    const [stats, setStats]         = useState<SystemStats | null>(null);
    const [doctorCount, setDoctorCount] = useState<number | null>(null);
    const [loading, setLoading]     = useState(true);
    const [error, setError]         = useState('');

    useEffect(() => {
        Promise.all([
            apiClient.get('/statistics/overview'),
            apiClient.get('/Auth/doctors'),
        ])
            .then(([statsRes, doctorsRes]) => {
                const d = statsRes.data?.data ?? statsRes.data;
                setStats({
                    totalCases:      d.totalCases      ?? 0,
                    brainCases:      d.brainCases       ?? 0,
                    lungCases:       d.lungCases        ?? 0,
                    completedCases:  d.completedCases   ?? 0,
                    pendingCases:    d.pendingCases      ?? 0,
                    analyzingCases:  d.analyzingCases   ?? 0,
                    casesLast30Days: d.casesLast30Days  ?? 0,
                });

                const doctors = doctorsRes.data?.data ?? doctorsRes.data;
                setDoctorCount(Array.isArray(doctors) ? doctors.length : null);
            })
            .catch(() => setError('Վիճակագրությունը բեռնելը ձախողվեց'))
            .finally(() => setLoading(false));
    }, []);

    return (
        <div>
            <div style={{ marginBottom: 32 }}>
                <h1 style={{ fontSize: 26, fontWeight: 800, color: '#0f172a', margin: 0 }}>Կառավարման Վահանակ</h1>
                <p style={{ fontSize: 14, color: '#64748b', margin: '6px 0 0' }}>Համակարգի ընդհանուր վիճակ</p>
            </div>

            {error && (
                <div style={{ background: '#fef2f2', border: '1px solid #fecaca', borderRadius: 10, padding: '14px 18px', color: '#dc2626', fontSize: 14, marginBottom: 24 }}>
                    {error}
                </div>
            )}

            {loading ? (
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))', gap: 20 }}>
                    {Array.from({ length: 6 }).map((_, i) => (
                        <div key={i} style={{ height: 120, background: '#f1f5f9', borderRadius: 14, animation: 'pulse 1.5s infinite' }} />
                    ))}
                </div>
            ) : stats ? (
                <>
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))', gap: 20, marginBottom: 36 }}>
                        {doctorCount !== null && (
                            <StatCard label="Բժիշկների թիվ"   value={doctorCount}           sub="գրանցված"              accent="#3b82f6" />
                        )}
                        <StatCard label="Ընդամենը դեպք"       value={stats.totalCases}       sub={`${stats.casesLast30Days} վերջին 30 օրում`} accent="#0ea5e9" />
                        <StatCard label="Ավարտված դեպքեր"     value={stats.completedCases}   sub={`${Math.round(stats.completedCases / Math.max(stats.totalCases, 1) * 100)}%`} accent="#10b981" />
                        <StatCard label="Վերլուծման մեջ"      value={stats.analyzingCases}   sub="AI մշակում"            accent="#f59e0b" />
                        <StatCard label="Սպասող դեպքեր"       value={stats.pendingCases}     sub="նոր"                   accent="#ef4444" />
                        <StatCard label="Ուղեղի ուռուցք"      value={stats.brainCases}       sub="դեպք"                  accent="#8b5cf6" />
                        <StatCard label="Թոքի քաղցկեղ"        value={stats.lungCases}        sub="դեպք"                  accent="#ec4899" />
                    </div>

                    {/* Quick links */}
                    <div style={{ background: '#fff', borderRadius: 14, padding: '24px 28px', boxShadow: '0 1px 4px rgba(0,0,0,0.06)' }}>
                        <h2 style={{ fontSize: 16, fontWeight: 700, color: '#0f172a', margin: '0 0 16px' }}>Արագ հղումներ</h2>
                        <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
                            <a href="/admin/doctors" style={{ padding: '10px 20px', borderRadius: 10, background: '#3b82f6', color: '#fff', fontSize: 13, fontWeight: 700, textDecoration: 'none' }}>
                                Կառ. բժիշկներ
                            </a>
                            <a href="/admin/audit" style={{ padding: '10px 20px', borderRadius: 10, background: '#8b5cf6', color: '#fff', fontSize: 13, fontWeight: 700, textDecoration: 'none' }}>
                                Աուդիտ մատյան
                            </a>
                        </div>
                    </div>
                </>
            ) : null}

            <style>{`@keyframes pulse { 0%,100%{opacity:1} 50%{opacity:0.5} }`}</style>
        </div>
    );
}