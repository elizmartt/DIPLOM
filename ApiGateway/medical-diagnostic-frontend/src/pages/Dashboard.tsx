import { useEffect, useRef, useState } from 'react';
import {
    statisticsApi,
    Overview,
    DiagnosisItem,
    ModelAccuracy,
} from '@/api/statistics';

// ── Label / colour maps ────────────────────────────────────────────────────
const diagnosisLabel: Record<string, string> = {
    normal:            'Նորմալ',
    no_cancer:         'Քաղցկեղ չկա',
    INSUFFICIENT_DATA: 'Անբավարար տվյալ',
    class_6:           'Գլիոմա',
    meningioma:        'Մենինգիոմա',
    glioma:            'Գլիոմա',
    lung_cancer:       'Թոքի քաղցկեղ',
    pituitary:         'Հիպոֆիզ ուռուցք',
};

const diagnosisColor: Record<string, string> = {
    normal:            '#10b981',
    no_cancer:         '#34d399',
    INSUFFICIENT_DATA: '#94a3b8',
    class_6:           '#f59e0b',
    meningioma:        '#f97316',
    glioma:            '#ef4444',
    lung_cancer:       '#dc2626',
    pituitary:         '#8b5cf6',
};

// ── Animated counter hook ──────────────────────────────────────────────────
function useCountUp(target: number, duration = 900) {
    const [val, setVal] = useState(0);
    useEffect(() => {
        if (!target) return;
        let start: number | null = null;
        const step = (ts: number) => {
            if (!start) start = ts;
            const progress = Math.min((ts - start) / duration, 1);
            const ease = 1 - Math.pow(1 - progress, 3);
            setVal(Math.round(ease * target));
            if (progress < 1) requestAnimationFrame(step);
        };
        requestAnimationFrame(step);
    }, [target, duration]);
    return val;
}

// ── Skeleton ───────────────────────────────────────────────────────────────
function Skeleton({ h = 20, w = '100%', r = 8 }: { h?: number; w?: string | number; r?: number }) {
    return (
        <div style={{
            height: h, width: w, borderRadius: r,
            background: 'linear-gradient(90deg,#1e2a3a 25%,#243040 50%,#1e2a3a 75%)',
            backgroundSize: '200% 100%',
            animation: 'shimmer 1.4s infinite',
        }} />
    );
}

// ── Radial progress ring ───────────────────────────────────────────────────
function RingChart({ value, color, size = 80 }: { value: number; color: string; size?: number }) {
    const r = (size - 12) / 2;
    const circ = 2 * Math.PI * r;
    const dash = (value / 100) * circ;
    return (
        <svg width={size} height={size} style={{ transform: 'rotate(-90deg)' }}>
            <circle cx={size / 2} cy={size / 2} r={r} fill="none" stroke="#1e2a3a" strokeWidth={6} />
            <circle
                cx={size / 2} cy={size / 2} r={r} fill="none"
                stroke={color} strokeWidth={6}
                strokeDasharray={`${dash} ${circ}`}
                strokeLinecap="round"
                style={{ transition: 'stroke-dasharray 1s cubic-bezier(.4,0,.2,1)' }}
            />
        </svg>
    );
}

// ── Horizontal bar ─────────────────────────────────────────────────────────
function Bar({ pct, color, animate }: { pct: number; color: string; animate: boolean }) {
    return (
        <div style={{ height: 8, background: '#1e2a3a', borderRadius: 99, overflow: 'hidden' }}>
            <div style={{
                width: animate ? `${pct}%` : '0%',
                height: '100%',
                background: `linear-gradient(90deg, ${color}aa, ${color})`,
                borderRadius: 99,
                transition: 'width 1s cubic-bezier(.4,0,.2,1)',
            }} />
        </div>
    );
}

// ── Model accuracy card ────────────────────────────────────────────────────
const MODELS = [
    { key: 'imaging',   label: 'Պատկերների վերլուծություն', acc: 91.8, color: '#38bdf8',  },
    { key: 'clinical',  label: 'Կլինիկական ախտանիշներ', acc: 85.8, color: '#a78bfa', },
    { key: 'lab',       label: 'Լաբ. արդյունքներ',      acc: 88.0, color: '#fb923c', },
    { key: 'ensemble',  label: 'Համախմբված մոդել',      acc: 94.2, color: '#4ade80',  },
];

function ModelCard({ label, acc, color, delay }: {
    label: string; acc: number; color: string; delay: number;
}) {
    const [visible, setVisible] = useState(false);
    useEffect(() => { const t = setTimeout(() => setVisible(true), delay); return () => clearTimeout(t); }, [delay]);

    return (
        <div style={{
            background: 'linear-gradient(135deg, #131e2e 0%, #1a2538 100%)',
            borderRadius: 16,
            padding: '20px 22px',
            border: `1px solid ${color}22`,
            display: 'flex',
            alignItems: 'center',
            gap: 16,
            opacity: visible ? 1 : 0,
            transform: visible ? 'translateY(0)' : 'translateY(12px)',
            transition: 'opacity 0.5s ease, transform 0.5s ease',
        }}>
            <div style={{ position: 'relative', flexShrink: 0 }}>
                <RingChart value={visible ? acc : 0} color={color} size={72} />
                <div style={{
                    position: 'absolute', inset: 0,
                    display: 'flex', alignItems: 'center', justifyContent: 'center',
                    fontSize: 11, fontWeight: 800, color,
                }}>
                    {visible ? `${acc}%` : ''}
                </div>
            </div>
            <div>
                <div style={{ fontSize: 11, color: '#475569', fontWeight: 600, marginBottom: 4, textTransform: 'uppercase', letterSpacing: '0.08em' }}>
                     {label}
                </div>
                <div style={{ fontSize: 26, fontWeight: 800, color: '#f1f5f9', letterSpacing: '-0.02em' }}>
                    {visible ? `${acc}%` : '—'}
                </div>
                <div style={{ fontSize: 12, color: '#475569', marginTop: 3 }}>Ճշգրտություն</div>
            </div>
        </div>
    );
}

// ── Stat card ──────────────────────────────────────────────────────────────
function StatCard({ label, value, sub, accent, delay }: {
    label: string; value: number; sub?: string; accent: string; delay: number;
}) {
    const [visible, setVisible] = useState(false);
    useEffect(() => { const t = setTimeout(() => setVisible(true), delay); return () => clearTimeout(t); }, [delay]);
    const displayed = useCountUp(visible ? value : 0);

    return (
        <div style={{
            background: 'linear-gradient(135deg, #131e2e 0%, #1a2538 100%)',
            borderRadius: 20,
            padding: '28px 26px',
            border: `1px solid ${accent}22`,
            position: 'relative',
            overflow: 'hidden',
            opacity: visible ? 1 : 0,
            transform: visible ? 'translateY(0)' : 'translateY(16px)',
            transition: 'opacity 0.6s ease, transform 0.6s ease, box-shadow 0.2s ease',
            cursor: 'default',
        }}
             onMouseEnter={e => { (e.currentTarget as HTMLDivElement).style.boxShadow = `0 0 0 1px ${accent}44, 0 8px 32px ${accent}18`; }}
             onMouseLeave={e => { (e.currentTarget as HTMLDivElement).style.boxShadow = 'none'; }}
        >
            {/* glow blob */}
            <div style={{
                position: 'absolute', top: -40, right: -40,
                width: 120, height: 120, borderRadius: '50%',
                background: `radial-gradient(circle, ${accent}18 0%, transparent 70%)`,
                pointerEvents: 'none',
            }} />
            {/* top accent line */}
            <div style={{ position: 'absolute', top: 0, left: 0, right: 0, height: 3, background: `linear-gradient(90deg, transparent, ${accent}, transparent)` }} />

            <div style={{ fontSize: 28, marginBottom: 12 }}></div>
            <div style={{ fontSize: 13, color: '#64748b', fontWeight: 600, marginBottom: 8 }}>{label}</div>
            <div style={{ fontSize: 48, fontWeight: 900, color: '#f1f5f9', letterSpacing: '-0.03em', lineHeight: 1 }}>
                {displayed}
            </div>
            {sub && <div style={{ fontSize: 12, color: '#475569', marginTop: 10, fontWeight: 500 }}>{sub}</div>}
        </div>
    );
}

// ── Donut chart (SVG) ──────────────────────────────────────────────────────
function DonutChart({ data }: { data: DiagnosisItem[] }) {
    const size = 180;
    const r = 66;
    const cx = size / 2, cy = size / 2;
    const circ = 2 * Math.PI * r;

    const total = data.reduce((s, d) => s + d.count, 0);
    let offset = 0;

    const segments = data.map(d => {
        const frac = d.count / total;
        const len = frac * circ;
        const gap = 3;
        const seg = { d, frac, offset, len: Math.max(len - gap, 0) };
        offset += len;
        return seg;
    });

    const [hovered, setHovered] = useState<string | null>(null);

    return (
        <div style={{ display: 'flex', alignItems: 'center', gap: 32, flexWrap: 'wrap' }}>
            <div style={{ position: 'relative', flexShrink: 0 }}>
                <svg width={size} height={size} style={{ transform: 'rotate(-90deg)' }}>
                    <circle cx={cx} cy={cy} r={r} fill="none" stroke="#1e2a3a" strokeWidth={22} />
                    {segments.map(({ d, offset: off, len }) => {
                        const color = diagnosisColor[d.diagnosis] ?? '#64748b';
                        const isHov = hovered === d.diagnosis;
                        return (
                            <circle
                                key={d.diagnosis}
                                cx={cx} cy={cy} r={r}
                                fill="none"
                                stroke={color}
                                strokeWidth={isHov ? 28 : 22}
                                strokeDasharray={`${len} ${circ}`}
                                strokeDashoffset={-off}
                                strokeLinecap="butt"
                                style={{ transition: 'stroke-width 0.2s ease, opacity 0.2s ease', opacity: hovered && !isHov ? 0.3 : 1, cursor: 'pointer' }}
                                onMouseEnter={() => setHovered(d.diagnosis)}
                                onMouseLeave={() => setHovered(null)}
                            />
                        );
                    })}
                </svg>
                <div style={{
                    position: 'absolute', inset: 0,
                    display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
                }}>
                    <div style={{ fontSize: 28, fontWeight: 900, color: '#f1f5f9' }}>{total}</div>
                    <div style={{ fontSize: 11, color: '#475569', fontWeight: 600 }}>ԸՆԴՀԱՆՈՒՐ</div>
                </div>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 10, flex: 1, minWidth: 160 }}>
                {data.map(d => {
                    const color = diagnosisColor[d.diagnosis] ?? '#64748b';
                    const label = diagnosisLabel[d.diagnosis] ?? d.diagnosis;
                    const isHov = hovered === d.diagnosis;
                    return (
                        <div key={d.diagnosis}
                             style={{ display: 'flex', alignItems: 'center', gap: 10, cursor: 'pointer', opacity: hovered && !isHov ? 0.4 : 1, transition: 'opacity 0.2s' }}
                             onMouseEnter={() => setHovered(d.diagnosis)}
                             onMouseLeave={() => setHovered(null)}
                        >
                            <div style={{ width: 10, height: 10, borderRadius: 3, background: color, flexShrink: 0 }} />
                            <div style={{ flex: 1, fontSize: 13, color: '#94a3b8', fontWeight: 500 }}>{label}</div>
                            <div style={{ fontSize: 13, fontWeight: 700, color: '#f1f5f9' }}>{d.count}</div>
                            <div style={{ fontSize: 11, color: '#475569', width: 36, textAlign: 'right' }}>{d.percentage}%</div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
}

// ── Main Dashboard ─────────────────────────────────────────────────────────
export default function Dashboard() {
    const [overview, setOverview]   = useState<Overview | null>(null);
    const [diagnoses, setDiagnoses] = useState<DiagnosisItem[] | null>(null);
    const [modelAcc, setModelAcc]   = useState<ModelAccuracy | null>(null);
    const [loading, setLoading]     = useState(true);
    const [error, setError]         = useState<string | null>(null);
    const [barsVisible, setBarsVisible] = useState(false);
    const barsRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        Promise.all([
            statisticsApi.getOverview(),
            statisticsApi.getDiagnoses(),
            statisticsApi.getModelAccuracy(),
        ])
            .then(([ov, dx, ma]) => { setOverview(ov); setDiagnoses(dx); setModelAcc(ma); })
            .catch((e: Error) => setError(e.message))
            .finally(() => setLoading(false));
    }, []);

    // Trigger bar animations on scroll into view
    useEffect(() => {
        const el = barsRef.current;
        if (!el) return;
        const obs = new IntersectionObserver(([entry]) => { if (entry.isIntersecting) setBarsVisible(true); }, { threshold: 0.2 });
        obs.observe(el);
        return () => obs.disconnect();
    }, [diagnoses]);

    const hour = new Date().getHours();
    const today = new Date().toLocaleDateString('en-GB');
    return (
        <>
            <style>{`
                @keyframes shimmer { 0%{background-position:-200% 0} 100%{background-position:200% 0} }
                @keyframes fadeUp { from{opacity:0;transform:translateY(20px)} to{opacity:1;transform:none} }
                @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.5} }
                .dash-root { background: #0c1525; min-height: 100vh; }
            `}</style>

            <div className="page-container dash-root" style={{ maxWidth: 1240, background: '#0c1525' }}>

                {/* ── Header ── */}
                <div style={{ marginBottom: 44, animation: 'fadeUp 0.6s ease both' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 16 }}>
                        <div>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 8 }}>
                                <div style={{ width: 8, height: 8, borderRadius: '50%', background: '#10b981', animation: 'pulse 2s infinite' }} />
                                <span style={{ fontSize: 13, color: '#475569', fontWeight: 600, letterSpacing: '0.06em', textTransform: 'uppercase' }}>
                                </span>
                            </div>
                            <h1 style={{ fontSize: 36, fontWeight: 900, color: '#f1f5f9', letterSpacing: '-0.03em', margin: 0 }}>
                                Ախտորոշիչ Վահանակ
                            </h1>
                            <p style={{ color: '#475569', fontSize: 14, marginTop: 8, fontWeight: 500 }}>
                                Աջակցող բժշկական ախտորոշման համակարգ · {today}
                            </p>
                        </div>

                        {/* Live indicator */}
                        <div style={{
                            display: 'flex', alignItems: 'center', gap: 8,
                            background: '#131e2e', border: '1px solid #1e3a2e',
                            borderRadius: 12, padding: '10px 16px',
                        }}>
                            <div style={{ width: 7, height: 7, borderRadius: '50%', background: '#10b981', animation: 'pulse 1.5s infinite' }} />
                            <span style={{ fontSize: 13, color: '#10b981', fontWeight: 700 }}>Համակարգը ակտիվ է</span>
                        </div>
                    </div>
                </div>

                {/* ── Error ── */}
                {error && (
                    <div style={{ background: '#1a0e0e', border: '1px solid #ef444444', borderRadius: 12, padding: '14px 18px', color: '#f87171', fontSize: 14, marginBottom: 24 }}>
                         Սխալ՝ {error}
                    </div>
                )}

                {/* ── Stat Cards ── */}
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: 18, marginBottom: 28 }}>
                    {loading ? [1,2,3,4].map(i => (
                        <div key={i} style={{ background: '#131e2e', borderRadius: 20, padding: 28 }}>
                            <Skeleton h={28} w="30%" r={6} /><div style={{marginTop:14}}><Skeleton h={48} w="50%" r={8} /></div>
                        </div>
                    )) : overview && (<>
                        <StatCard label="Ընդհանուր դեպքեր"  value={overview.totalCases}     sub={`+${overview.casesLast30Days} վերջին 30 օրում`} accent="#38bdf8"  delay={0} />
                        <StatCard label="Ավարտված դեպքեր"   value={overview.completedCases}  sub={overview.totalCases ? `${Math.round(overview.completedCases/overview.totalCases*100)}% ավարտված` : undefined} accent="#4ade80" delay={80} />
                        <StatCard label="Ուղեղի ուռուցք"    value={overview.brainCases}      accent="#a78bfa"  delay={160} />
                        <StatCard label="Թոքի ախտաբանություն" value={overview.lungCases}     accent="#fb923c"  delay={240} />
                    </>)}
                </div>

                {/* ── Two-column: Donut + Model accuracy ── */}
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 18, marginBottom: 28 }}>

                    {/* Donut */}
                    <div style={{
                        background: 'linear-gradient(135deg, #131e2e 0%, #1a2538 100%)',
                        borderRadius: 20, padding: 28,
                        border: '1px solid #1e2a3a',
                        animation: 'fadeUp 0.6s ease 0.3s both',
                    }}>
                        <div style={{ fontSize: 13, color: '#475569', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.08em', marginBottom: 24 }}>
                             Ախտորոշումների բաշխում
                        </div>
                        {loading ? (
                            <div style={{ display: 'flex', gap: 24, alignItems: 'center' }}>
                                <Skeleton h={180} w={180} r={90} />
                                <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 12 }}>
                                    {[1,2,3,4].map(i => <Skeleton key={i} h={18} />)}
                                </div>
                            </div>
                        ) : diagnoses ? <DonutChart data={diagnoses} /> : null}
                    </div>

                    {/* Brain vs Lung split */}
                    <div style={{
                        background: 'linear-gradient(135deg, #131e2e 0%, #1a2538 100%)',
                        borderRadius: 20, padding: 28,
                        border: '1px solid #1e2a3a',
                        animation: 'fadeUp 0.6s ease 0.4s both',
                    }}>
                        <div style={{ fontSize: 13, color: '#475569', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.08em', marginBottom: 24 }}>
                            Ախտորոշումների բաշխում
                        </div>
                        {loading ? (
                            <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
                                {[1,2].map(i => <div key={i}><Skeleton h={14} w="50%" r={4} /><div style={{marginTop:8}}><Skeleton h={10} /></div></div>)}
                            </div>
                        ) : overview ? (() => {
                            const total = overview.brainCases + overview.lungCases || 1;
                            const brainPct = Math.round(overview.brainCases / total * 100);
                            const lungPct  = 100 - brainPct;
                            return (
                                <div style={{ display: 'flex', flexDirection: 'column', gap: 28 }}>
                                    {/* Combined stacked bar */}
                                    <div>
                                        <div style={{ height: 32, borderRadius: 8, overflow: 'hidden', display: 'flex', marginBottom: 12 }}>
                                            <div style={{ width: `${brainPct}%`, background: 'linear-gradient(90deg,#7c3aed,#a78bfa)', transition: 'width 1s ease', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                                                {brainPct > 12 && <span style={{ fontSize: 11, fontWeight: 800, color: '#fff' }}>{brainPct}%</span>}
                                            </div>
                                            <div style={{ flex: 1, background: 'linear-gradient(90deg,#ea580c,#fb923c)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                                                {lungPct > 12 && <span style={{ fontSize: 11, fontWeight: 800, color: '#fff' }}>{lungPct}%</span>}
                                            </div>
                                        </div>
                                        <div style={{ display: 'flex', gap: 20 }}>
                                            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                                <div style={{ width: 12, height: 12, borderRadius: 3, background: '#a78bfa' }} />
                                                <span style={{ fontSize: 13, color: '#94a3b8' }}>Ուղեղ · <strong style={{ color: '#f1f5f9' }}>{overview.brainCases}</strong></span>
                                            </div>
                                            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                                <div style={{ width: 12, height: 12, borderRadius: 3, background: '#fb923c' }} />
                                                <span style={{ fontSize: 13, color: '#94a3b8' }}>Թոք · <strong style={{ color: '#f1f5f9' }}>{overview.lungCases}</strong></span>
                                            </div>
                                        </div>
                                    </div>

                                    {/* Completion rate gauge */}
                                    <div>
                                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                                            <span style={{ fontSize: 13, color: '#64748b', fontWeight: 600 }}>Ավարտման մակարդակ</span>
                                            <span style={{ fontSize: 13, fontWeight: 800, color: '#4ade80' }}>
                                                {overview.totalCases ? Math.round(overview.completedCases / overview.totalCases * 100) : 0}%
                                            </span>
                                        </div>
                                        <div style={{ height: 12, background: '#1e2a3a', borderRadius: 99, overflow: 'hidden' }}>
                                            <div style={{
                                                width: `${overview.totalCases ? Math.round(overview.completedCases / overview.totalCases * 100) : 0}%`,
                                                height: '100%',
                                                background: 'linear-gradient(90deg, #059669, #4ade80)',
                                                borderRadius: 99,
                                                transition: 'width 1.2s cubic-bezier(.4,0,.2,1)',
                                            }} />
                                        </div>
                                        <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 6, fontSize: 12, color: '#334155' }}>
                                            <span>{overview.completedCases} ավարտված</span>
                                            <span>{overview.totalCases - overview.completedCases} ընթացքի մեջ</span>
                                        </div>
                                    </div>

                                    {/* Last 30 days callout */}
                                    <div style={{ background: '#0c1525', borderRadius: 12, padding: '14px 18px', border: '1px solid #1e2a3a', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                                        <span style={{ fontSize: 13, color: '#64748b', fontWeight: 500 }}>Վերջին 30 օրում</span>
                                        <span style={{ fontSize: 22, fontWeight: 900, color: '#38bdf8' }}>{overview.casesLast30Days}</span>
                                    </div>
                                </div>
                            );
                        })() : null}
                    </div>
                </div>

                {/* ── Model accuracy ── */}
                <div style={{
                    background: 'linear-gradient(135deg, #131e2e 0%, #1a2538 100%)',
                    borderRadius: 20, padding: 28,
                    border: '1px solid #1e2a3a',
                    marginBottom: 28,
                    animation: 'fadeUp 0.6s ease 0.5s both',
                }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24, flexWrap: 'wrap', gap: 12 }}>
                        <div style={{ fontSize: 13, color: '#475569', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.08em' }}>
                            ⚡ Մոդելի Ճշգրտություն
                        </div>
                        <div style={{ fontSize: 12, color: '#475569', fontWeight: 500 }}>
                            Ensemble fusion · Կշիռներ՝ 40 / 30 / 30
                        </div>
                    </div>
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: 14 }}>
                        {MODELS.map((m, i) => (
                            <ModelCard key={m.key} label={m.label} acc={m.acc} color={m.color}  delay={i * 100} />
                        ))}
                    </div>
                </div>

                {/* ── Diagnosis bar breakdown ── */}
                <div ref={barsRef} style={{
                    background: 'linear-gradient(135deg, #131e2e 0%, #1a2538 100%)',
                    borderRadius: 20, padding: 28,
                    border: '1px solid #1e2a3a',
                    animation: 'fadeUp 0.6s ease 0.6s both',
                    marginBottom: 28,
                }}>
                    <div style={{ fontSize: 13, color: '#475569', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.08em', marginBottom: 24 }}>
                        📈 Ախտորոշման Դետալ
                    </div>

                    {loading ? (
                        <div style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
                            {[1,2,3,4].map(i => <div key={i}><Skeleton h={14} w="45%" r={4} /><div style={{marginTop:8}}><Skeleton h={8} /></div></div>)}
                        </div>
                    ) : diagnoses ? (
                        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
                            {diagnoses.map((d, i) => {
                                const color = diagnosisColor[d.diagnosis] ?? '#64748b';
                                const label = diagnosisLabel[d.diagnosis] ?? d.diagnosis;
                                return (
                                    <div key={d.diagnosis} style={{ animationDelay: `${i * 60}ms` }}>
                                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                                            <span style={{ display: 'flex', alignItems: 'center', gap: 10, fontSize: 14, color: '#94a3b8', fontWeight: 600 }}>
                                                <span style={{ width: 10, height: 10, borderRadius: 3, background: color, display: 'inline-block' }} />
                                                {label}
                                            </span>
                                            <span style={{ fontSize: 14, fontWeight: 800, color: color }}>
                                                {d.count} <span style={{ fontSize: 11, color: '#334155', fontWeight: 600 }}>({d.percentage}%)</span>
                                            </span>
                                        </div>
                                        <Bar pct={d.percentage} color={color} animate={barsVisible} />
                                    </div>
                                );
                            })}
                        </div>
                    ) : null}
                </div>

            </div>
        </>
    );
}