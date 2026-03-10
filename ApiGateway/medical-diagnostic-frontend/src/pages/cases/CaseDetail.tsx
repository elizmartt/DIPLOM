import { useEffect, useRef, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { casesApi, DiagnosisCaseResponse, DiagnosisResults, MedicalImage } from '@/api/cases';

// ─── Design tokens ─────────────────────────────────────────────────────────────
const surface  = 'rgba(255,255,255,0.03)';
const surface2 = 'rgba(255,255,255,0.06)';
const borderC  = 'rgba(255,255,255,0.08)';
const textPri  = '#f1f5f9';
const textSec  = '#64748b';
const textMut  = '#334155';

// ─── Armenian Labels ───────────────────────────────────────────────────────────

const STATUS_COLORS: Record<string, { bg: string; text: string; border: string; dot: string }> = {
    pending:                 { bg: 'rgba(245,158,11,0.12)',  text: '#fbbf24', border: 'rgba(245,158,11,0.3)',  dot: '#f59e0b' },
    data_collection:         { bg: 'rgba(59,130,246,0.12)',  text: '#60a5fa', border: 'rgba(59,130,246,0.3)',  dot: '#3b82f6' },
    processing:              { bg: 'rgba(139,92,246,0.12)',  text: '#a78bfa', border: 'rgba(139,92,246,0.3)',  dot: '#8b5cf6' },
    analyzing:               { bg: 'rgba(139,92,246,0.12)',  text: '#a78bfa', border: 'rgba(139,92,246,0.3)',  dot: '#8b5cf6' },
    completed:               { bg: 'rgba(34,197,94,0.12)',   text: '#4ade80', border: 'rgba(34,197,94,0.3)',   dot: '#22c55e' },
    completed_with_warnings: { bg: 'rgba(34,197,94,0.12)',   text: '#4ade80', border: 'rgba(34,197,94,0.3)',   dot: '#22c55e' },
    failed:                  { bg: 'rgba(239,68,68,0.12)',   text: '#f87171', border: 'rgba(239,68,68,0.3)',   dot: '#ef4444' },
};

const STATUS_HY: Record<string, string> = {
    pending:                 'Սպասվող',
    data_collection:         'Տվյ. Հավաք',
    processing:              'Մշակում',
    analyzing:               'Վերլուծում',
    completed:               'Ավարտված',
    completed_with_warnings: 'Ավարտվել է',
    failed:                  'Ձախողված',
};

const RISK_COLORS: Record<string, { bg: string; text: string }> = {
    low:      { bg: 'rgba(34,197,94,0.12)',  text: '#4ade80' },
    medium:   { bg: 'rgba(245,158,11,0.12)', text: '#fbbf24' },
    high:     { bg: 'rgba(239,68,68,0.12)',  text: '#f87171' },
    critical: { bg: 'rgba(217,70,239,0.12)', text: '#e879f9' },
};

const RISK_HY: Record<string, string> = {
    low: 'Ցածր', medium: 'Միջին', high: 'Բարձր', critical: 'Կրիտ.'
};

const DIAG_HY: Record<string, string> = {
    normal:              'Նորմալ',
    no_cancer:           'Քաղցկեղ չկա',
    INSUFFICIENT_DATA:   'Անբավարար տվյալ',
    meningioma:          'Մենինգիոմա',
    glioma:              'Գլիոմա',
    lung_cancer:         'Թոքի քաղցկեղ',
    pituitary:           'Հիպոֆիզ ուռուցք',
    alzheimer_mild:      'Ալցհայմեր (Թույլ)',
    alzheimer_moderate:  'Ալցհայմեր (Միջին)',
    alzheimer_very_mild: 'Ալցհայմեր (Շատ Թույլ)',
    multiple_sclerosis:  'Բազմակի Սկլերոզ',
};

const TYPE_HY: Record<string, string> = {
    brain_tumor: 'Ուղեղի Ուռուցք',
    lung_cancer: 'Թոքի Քաղցկեղ',
    brain: 'Ուղեղ',
    lung: 'Թոք',
};

const REC_HY: Record<string, string> = {
    'Urgent neurosurgery referral required':                   'Շտապ նյարդաբանա-վիրաբուժի ուղեգիր',
    'Biopsy required for grading':                             'Կենսախուզում անհրաժեշտ է',
    'Oncology consultation recommended':                       'Ուռուցքաբանի խորհրդատվություն',
    'MRI with contrast recommended':                           'ՄՌՏ կոնտրաստով',
    'Neurosurgery consultation required':                      'Նյարդավիրաբույժի խորհրդատվություն',
    'Monitor for symptom progression':                         'Ախտանիշների դիտարկում',
    'Endocrinology consultation required':                     'Էնդոկրինոլոգի խորհրդատվություն',
    'Visual field testing recommended':                        'Տեսողության դաշտի հետազոտում',
    'Hormone level assessment needed':                         'Հորմոնների մակարդակի ստուգում',
    'No immediate action required':                            'Անհապաղ միջամտության կարիք չկա',
    'Routine follow-up in 12 months':                          'Կանոնավոր հետաքննություն 12 ամիս հետո',
    'Urgent oncology referral required':                       'Շտապ ուռուցքաբանի ուղեգիր',
    'CT-guided biopsy recommended':                            'Վերահսկվող բիոպսիան',
    'Pulmonology consultation required':                       'Թոքաբանի խորհդատվություն',
    'Continue routine monitoring':                             'Կանոնավոր մոնիտորինգի կարիք',
    'Follow-up in 6 months':                                   'Կրկնակի ստուգում 6 ամիս հետո',
    'Clinical review recommended':                             'Կլինիկական վերանայում',
    'Low confidence - consider additional diagnostic tests':   'Ցածր վստահություն — լրացուցիչ դիագնոստիկայի կարիք',
};

// ─── Status helpers ────────────────────────────────────────────────────────────

function isCompleted(status?: string) {
    return status === 'completed' || status === 'completed_with_warnings';
}
function isInProgress(status?: string) {
    return status === 'analyzing' || status === 'processing';
}
function diagHy(d: string) { return DIAG_HY[d] ?? d; }
function recHy(r: string)  { return REC_HY[r] ?? r; }

function formatDate(iso: string) {
    const d = new Date(iso);
    return `${String(d.getDate()).padStart(2,'0')}/${String(d.getMonth()+1).padStart(2,'0')}/${d.getFullYear()}`;
}
function extractFilename(fp: string): string {
    return fp.split('/').pop() ?? fp.split('\\').pop() ?? fp;
}

// ─── Reusable UI ───────────────────────────────────────────────────────────────

function Skeleton({ h = 80 }: { h?: number }) {
    return (
        <div style={{
            height: h, borderRadius: 12,
            background: 'linear-gradient(90deg,#1e293b 25%,#243044 50%,#1e293b 75%)',
            backgroundSize: '200% 100%', animation: 'shimmer 1.4s infinite',
        }} />
    );
}

function StatusBadge({ status }: { status: string }) {
    const s = STATUS_COLORS[status] ?? { bg: surface2, text: '#94a3b8', border: borderC, dot: '#475569' };
    return (
        <span style={{
            display: 'inline-flex', alignItems: 'center', gap: 6,
            padding: '4px 13px', borderRadius: 99,
            background: s.bg, color: s.text, border: `1px solid ${s.border}`,
            fontSize: 12, fontWeight: 700,
        }}>
            <span style={{ width: 6, height: 6, borderRadius: '50%', background: s.dot, flexShrink: 0 }} />
            {STATUS_HY[status] ?? status}
        </span>
    );
}

function Card({ title, children, accent }: { title: string; children: React.ReactNode; accent?: string }) {
    return (
        <div style={{
            background: surface,
            border: `1px solid ${borderC}`,
            borderRadius: 16, overflow: 'hidden',
            marginBottom: 20,
            borderTop: accent ? `3px solid ${accent}` : `1px solid ${borderC}`,
        }}>
            <div style={{ padding: '14px 24px', borderBottom: `1px solid ${borderC}`, fontSize: 14, fontWeight: 700, color: textPri }}>
                {title}
            </div>
            <div style={{ padding: 24 }}>{children}</div>
        </div>
    );
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
    return (
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '11px 0', borderBottom: `1px solid ${borderC}` }}>
            <span style={{ fontSize: 13, color: textSec, fontWeight: 500 }}>{label}</span>
            <span style={{ fontSize: 13, color: textPri, fontWeight: 600, textAlign: 'right' }}>{value ?? '—'}</span>
        </div>
    );
}

// ─── Image Viewer ──────────────────────────────────────────────────────────────

function ScanPanel({ src, alt, label, badge, badgeColor, badgeText }: {
    src: string; alt: string; label: string;
    badge?: string; badgeColor?: string; badgeText?: string;
}) {
    return (
        <div>
            <div style={{ fontSize: 11, fontWeight: 700, color: textSec, marginBottom: 6, display: 'flex', alignItems: 'center', gap: 6 }}>
                {badge && (
                    <span style={{ background: badgeColor ?? 'rgba(99,102,241,0.2)', color: badgeText ?? '#a5b4fc', padding: '1px 7px', borderRadius: 5, fontSize: 10, fontWeight: 800 }}>
                        {badge}
                    </span>
                )}
                {label}
            </div>
            <div style={{ borderRadius: 12, overflow: 'hidden', background: '#020817', display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 220, border: `1px solid ${borderC}` }}>
                <img src={src} alt={alt} style={{ maxWidth: '100%', maxHeight: 400, objectFit: 'contain', display: 'block' }} />
            </div>
        </div>
    );
}

interface ImageEntry { original: { url: string; label: string; scanArea: string }; overlayUrl?: string }

function ImagesSection({ caseId, status }: { caseId: string; status: string }) {
    const [items, setItems] = useState<ImageEntry[]>([]);
    const [loading, setLoading] = useState(false);
    const blobUrls = useRef<string[]>([]);
    const done = isCompleted(status);

    useEffect(() => {
        if (status === 'pending') return;
        setLoading(true);
        casesApi.getImages(caseId)
            .then(async (imgs: MedicalImage[]) => {
                const loaded: ImageEntry[] = [];
                for (const img of imgs) {
                    const filename = extractFilename(img.filePath);
                    try {
                        const url = await casesApi.getImageBlob(caseId, filename);
                        blobUrls.current.push(url);
                        let overlayUrl: string | undefined;
                        if (done) {
                            const dotIdx = filename.lastIndexOf('.');
                            const imageId = dotIdx > 0 ? filename.slice(0, dotIdx) : filename;
                            for (const name of [`overlay_${imageId}.png`, `heatmap_${imageId}.png`]) {
                                try {
                                    overlayUrl = await casesApi.getGradCam(caseId, name);
                                    blobUrls.current.push(overlayUrl);
                                    break;
                                } catch { /* try next */ }
                            }
                        }
                        loaded.push({ original: { url, label: 'Բնօրինակ', scanArea: img.scanArea }, overlayUrl });
                    } catch { /* skip */ }
                }
                setItems(loaded);
            })
            .catch(() => {})
            .finally(() => setLoading(false));
        return () => { blobUrls.current.forEach(u => URL.revokeObjectURL(u)); blobUrls.current = []; };
    }, [caseId, status]);

    if (loading) return <Card title="Բժշկական Պատկեր"><Skeleton h={300} /></Card>;
    if (!items.length) return null;

    return (
        <Card title={`Բժշկական Պատկերներ (${items.length})`}>
            {items.map((item, i) => (
                <div key={i} style={{ marginBottom: i < items.length - 1 ? 28 : 0 }}>
                    {item.overlayUrl ? (
                        <>
                            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                                <ScanPanel src={item.original.url} alt="Բնօրինակ սկան" label="Բնօրինակ Սկան"
                                           badge="ORIGINAL" badgeColor="rgba(14,165,233,0.2)" badgeText="#38bdf8" />
                                <ScanPanel src={item.overlayUrl} alt="GradCAM ծածկույթ" label="Ակտիվացման Քարտեզ"
                                           badge="GradCAM" badgeColor="rgba(139,92,246,0.2)" badgeText="#a78bfa" />
                            </div>
                            <p style={{ fontSize: 11, color: textSec, marginTop: 8, marginBottom: 0, lineHeight: 1.5 }}>
                                Կարմիր/դեղին գույնով նշված տարածքները ամենամեծ ազդեցությունն են ունեցել AI ախտորոշման վրա։
                            </p>
                        </>
                    ) : (
                        <ScanPanel src={item.original.url} alt="Բժշկական սկան" label="Բժշկական Պատկեր"
                                   badge="ORIGINAL" badgeColor="rgba(14,165,233,0.2)" badgeText="#38bdf8" />
                    )}
                </div>
            ))}
        </Card>
    );
}

// ─── Confidence Ring ───────────────────────────────────────────────────────────

function ConfidenceRing({ value }: { value: number }) {
    const r = 40;
    const circ = 2 * Math.PI * r;
    const color = value >= 0.8 ? '#22c55e' : value >= 0.55 ? '#f59e0b' : '#ef4444';
    return (
        <svg width={96} height={96} viewBox="0 0 96 96">
            <circle cx={48} cy={48} r={r} fill="none" stroke="rgba(255,255,255,0.08)" strokeWidth={10} />
            <circle cx={48} cy={48} r={r} fill="none" stroke={color} strokeWidth={10}
                    strokeDasharray={`${circ * value} ${circ}`}
                    strokeLinecap="round" transform="rotate(-90 48 48)" />
            <text x={48} y={53} textAnchor="middle" fontSize={17} fontWeight="800" fill={color}>
                {Math.round(value * 100)}%
            </text>
        </svg>
    );
}

function ProbBar({ label, value, max }: { label: string; value: number; max: number }) {
    const frac = max > 0 ? value / max : 0;
    const color = frac >= 0.7 ? '#0ea5e9' : frac >= 0.4 ? '#f59e0b' : '#475569';
    return (
        <div style={{ marginBottom: 14 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, marginBottom: 6 }}>
                <span style={{ fontWeight: 600, color: '#94a3b8' }}>{diagHy(label)}</span>
                <span style={{ fontWeight: 700, color: textSec }}>{(value * 100).toFixed(1)}%</span>
            </div>
            <div style={{ height: 7, borderRadius: 99, background: 'rgba(255,255,255,0.06)', overflow: 'hidden' }}>
                <div style={{ height: '100%', borderRadius: 99, background: color, width: `${frac * 100}%`, transition: 'width 0.6s' }} />
            </div>
        </div>
    );
}

// ─── Explanation Block ─────────────────────────────────────────────────────────

const EXPLANATION_LINES: Record<string, string> = {
    'Diagnosis:':           'Ախտորոշում:',
    'Confidence:':          'Վստահություն:',
    'Model Agreement:':     'Մոդելների համաձայնություն:',
    'Agreement Score:':     'Համաձայնության գնահատական:',
    'Contributing Models:': 'Ներդրող մոդելներ:',
    'models available':     'մոդել հասանելի',
    'models agree':         'մոդել համաձայն',
    'Imaging:':             'Պատկերագրություն:',
    'Labs:':                'Լաբ. հետազոտություն:',
    'Symptoms:':            'Ախտանիշներ:',
    'Confidence':           'Վստահություն',
    'Weight':               'Կշիռ',
    'inconclusive':         'Անորոշ',
    'class_6':              'Մենինգիոմա',
    'meningioma':           'Մենինգիոմա',
    'glioma':               'Գլիոմա',
    'normal':               'Նորմալ',
    'no_cancer':            'Քաղցկեղ չկա',
    'lung_cancer':          'Թոքի քաղցկեղ',
    'pituitary':            'Հիպոֆիզ ուռուցք',
};

function translateLine(line: string): string {
    let r = line;
    for (const [en, hy] of Object.entries(EXPLANATION_LINES)) r = r.split(en).join(hy);
    return r;
}

function ExplanationBlock({ text }: { text: string }) {
    return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            {text.split('\n').map((line, i) => {
                const trimmed = line.trim();
                if (!trimmed) return <div key={i} style={{ height: 6 }} />;
                const isSuccess = trimmed.startsWith('✓');
                const isFailure = trimmed.startsWith('✗');
                const isHeader  = trimmed.endsWith(':') && !isSuccess && !isFailure;
                const indent    = line.search(/\S/);
                const content   = isSuccess || isFailure ? translateLine(trimmed.slice(2)) : translateLine(line);
                return (
                    <div key={i} style={{ paddingLeft: indent > 0 ? 20 : 0, display: 'flex', alignItems: 'flex-start', gap: 6 }}>
                        {isSuccess && <span style={{ color: '#4ade80', fontWeight: 800, fontSize: 13, flexShrink: 0 }}>✓</span>}
                        {isFailure && <span style={{ color: '#f87171', fontWeight: 800, fontSize: 13, flexShrink: 0 }}>✗</span>}
                        <span style={{
                            fontSize: isHeader ? 11 : 13,
                            fontWeight: isHeader ? 700 : 500,
                            color: isHeader ? textPri : isSuccess ? '#4ade80' : isFailure ? '#f87171' : '#94a3b8',
                            lineHeight: 1.6,
                            letterSpacing: isHeader ? '0.05em' : undefined,
                            textTransform: isHeader ? 'uppercase' : undefined,
                        }}>
                            {content}
                        </span>
                    </div>
                );
            })}
        </div>
    );
}

// ─── AI Results Section ────────────────────────────────────────────────────────

function AIResultsSection({ caseId, status }: { caseId: string; status: string }) {
    const [results, setResults] = useState<DiagnosisResults | null>(null);
    const [loading, setLoading] = useState(false);
    const [err, setErr] = useState<string | null>(null);

    useEffect(() => {
        if (!isCompleted(status)) return;
        setLoading(true);
        casesApi.getResults(caseId)
            .then(setResults)
            .catch(e => setErr(e.message))
            .finally(() => setLoading(false));
    }, [caseId, status]);

    if (isInProgress(status)) {
        return (
            <Card title="Վերլուծություն" accent="#3b82f6">
                <div style={{ display: 'flex', alignItems: 'center', gap: 14, padding: 20, background: 'rgba(59,130,246,0.08)', border: '1px solid rgba(59,130,246,0.2)', borderRadius: 12 }}>
                    <div style={{
                        width: 22, height: 22, borderRadius: '50%',
                        border: '3px solid #3b82f6', borderTopColor: 'transparent',
                        animation: 'spin 0.8s linear infinite', flexShrink: 0,
                    }} />
                    <div>
                        <div style={{ fontSize: 14, fontWeight: 700, color: '#60a5fa' }}>Տվյալները վերլուծվում են</div>
                        <div style={{ fontSize: 12, color: textSec, marginTop: 2 }}>Արդյունքները հասանելի կլինեն ավարտից հետո</div>
                    </div>
                </div>
            </Card>
        );
    }

    if (!isCompleted(status)) return null;

    if (loading) return (
        <Card title="Ախտորոշման արդյունքներ" accent="#22c55e">
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                <Skeleton h={100} /><Skeleton h={150} /><Skeleton h={80} />
            </div>
        </Card>
    );

    if (err || !results) return (
        <Card title="Ախտորոշման արդյունքներ" accent="#ef4444">
            <div style={{ color: '#f87171', fontSize: 13, padding: '12px 0' }}>
                ⚠️ Արդյունքներ չեն գտնվել{err ? `: ${err}` : ''}
            </div>
        </Card>
    );

    const probs = results.ensembleProbabilities ?? {};
    const maxProb = Math.max(...Object.values(probs).map(Number), 0.001);
    const riskC = RISK_COLORS[results.riskLevel] ?? { bg: surface2, text: '#94a3b8' };

    const recsList: string[] = Array.isArray(results.recommendations)
        ? (results.recommendations as string[])
        : typeof results.recommendations === 'string' && results.recommendations
            ? [results.recommendations]
            : [];

    const explanation    = results.explainabilitySummary?.explanation as string | undefined;
    const agreementScore = results.explainabilitySummary?.agreement_score;
    const isReliable     = results.explainabilitySummary?.is_reliable;

    return (
        <>
            {/* ── Summary ── */}
            <Card title="Ախտորոշման արդյունքներ" accent="#22c55e">
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 14, marginBottom: 20 }}>

                    {/* Diagnosis */}
                    <div style={{ padding: 20, borderRadius: 14, background: surface2, border: `1px solid ${borderC}`, textAlign: 'center' }}>
                        <div style={{ fontSize: 10, color: textMut, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', marginBottom: 8 }}>
                            Ախտորոշում
                        </div>
                        <div style={{ fontSize: 15, fontWeight: 800, color: textPri, lineHeight: 1.3 }}>
                            {diagHy(results.finalDiagnosis)}
                        </div>
                        {isReliable !== undefined && (
                            <div style={{ marginTop: 8, fontSize: 11, fontWeight: 700, color: isReliable ? '#4ade80' : '#fbbf24' }}>
                                {isReliable ? '✓ Հուսալի' : '⚠ Լրացուցիչ հետազոտություն'}
                            </div>
                        )}
                    </div>

                    {/* Confidence */}
                    <div style={{ padding: 20, borderRadius: 14, background: 'rgba(14,165,233,0.06)', border: '1px solid rgba(14,165,233,0.15)', textAlign: 'center' }}>
                        <div style={{ fontSize: 10, color: textMut, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', marginBottom: 4 }}>
                            Վստահություն
                        </div>
                        <div style={{ display: 'flex', justifyContent: 'center' }}>
                            <ConfidenceRing value={results.overallConfidence} />
                        </div>
                    </div>

                    {/* Risk */}
                    <div style={{ padding: 20, borderRadius: 14, background: riskC.bg, border: `1px solid ${borderC}`, textAlign: 'center' }}>
                        <div style={{ fontSize: 10, color: textMut, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', marginBottom: 8 }}>
                            Ռիսկի մակարդակ
                        </div>
                        <div style={{ fontSize: 26, fontWeight: 800, color: riskC.text }}>
                            {RISK_HY[results.riskLevel] ?? results.riskLevel}
                        </div>
                        {agreementScore !== undefined && (
                            <div style={{ marginTop: 6, fontSize: 11, color: textSec }}>
                                Համաձայնություն: {(Number(agreementScore) * 100).toFixed(0)}%
                            </div>
                        )}
                    </div>
                </div>

                {results.totalProcessingTimeMs > 0 && (
                    <div style={{ fontSize: 11, color: textMut, textAlign: 'right' }}>
                        Մշակման ժամանակ: {(results.totalProcessingTimeMs / 1000).toFixed(1)} վայ.
                    </div>
                )}
            </Card>

            {/* ── Ensemble Probabilities ── */}
            {Object.keys(probs).length > 0 && (
                <Card title="Հավանականությունների բաշխում">
                    {Object.entries(probs)
                        .sort(([, a], [, b]) => Number(b) - Number(a))
                        .map(([diag, val]) => (
                            <ProbBar key={diag} label={diag} value={Number(val)} max={maxProb} />
                        ))}
                </Card>
            )}

            {/* ── Recommendations ── */}
            {recsList.length > 0 && (
                <Card title="Բժշկական առաջարկություններ" accent="#f59e0b">
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                        {recsList.map((rec, i) => (
                            <div key={i} style={{ display: 'flex', alignItems: 'flex-start', gap: 10, padding: '10px 14px', background: 'rgba(245,158,11,0.08)', borderRadius: 10, border: '1px solid rgba(245,158,11,0.2)' }}>
                                <div style={{ width: 22, height: 22, borderRadius: '50%', background: '#f59e0b', color: '#0f172a', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 11, fontWeight: 800, flexShrink: 0 }}>
                                    {i + 1}
                                </div>
                                <span style={{ fontSize: 13, color: '#fbbf24', lineHeight: 1.6 }}>{recHy(rec)}</span>
                            </div>
                        ))}
                    </div>
                </Card>
            )}

            {/* ── Explanation ── */}
            {explanation && (
                <Card title="Բացատրություն">
                    <ExplanationBlock text={explanation} />
                </Card>
            )}
        </>
    );
}

// ─── Main Page ─────────────────────────────────────────────────────────────────

export default function CaseDetail() {
    const { id: caseId } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const [caseData, setCaseData] = useState<DiagnosisCaseResponse | null>(null);
    const [loading, setLoading] = useState(true);
    const [err, setErr] = useState<string | null>(null);
    const [reportLoading, setReportLoading] = useState(false);
    const [reportErr, setReportErr] = useState<string | null>(null);
    const [reportOk, setReportOk] = useState(false);

    useEffect(() => {
        if (!caseId) return;
        casesApi.getById(caseId)
            .then(setCaseData)
            .catch(e => setErr(e.message))
            .finally(() => setLoading(false));
    }, [caseId]);

    useEffect(() => {
        if (!caseId || !isInProgress(caseData?.status)) return;
        const id = setInterval(async () => {
            try {
                const upd = await casesApi.getById(caseId);
                setCaseData(upd);
                if (!isInProgress(upd.status)) clearInterval(id);
            } catch { clearInterval(id); }
        }, 4000);
        return () => clearInterval(id);
    }, [caseId, caseData?.status]);

    const handleDownloadReport = async () => {
        if (!caseId) return;
        setReportLoading(true); setReportErr(null); setReportOk(false);
        try {
            const blob = await casesApi.downloadReport(caseId);
            const url = URL.createObjectURL(new Blob([blob], { type: 'application/pdf' }));
            const a = document.createElement('a');
            a.href = url; a.download = `diagnosis-report-${caseId.slice(0, 8)}.pdf`;
            document.body.appendChild(a); a.click(); document.body.removeChild(a);
            setTimeout(() => URL.revokeObjectURL(url), 10000);
            setReportOk(true); setTimeout(() => setReportOk(false), 4000);
        } catch {
            setReportErr('Հաշվետվությունը հնարավոր չէ բեռնել — փորձել կրկին');
        } finally { setReportLoading(false); }
    };

    const btnSecondary: React.CSSProperties = {
        padding: '8px 18px', borderRadius: 10,
        border: `1px solid ${borderC}`,
        background: surface,
        color: '#94a3b8', fontWeight: 600, fontSize: 13,
        cursor: 'pointer', display: 'inline-flex', alignItems: 'center', gap: 6,
    };

    const done = isCompleted(caseData?.status);

    if (loading) return (
        <div className="page-container" style={{ maxWidth: 1000 }}>
            <style>{`@keyframes shimmer{0%{background-position:-200% 0}100%{background-position:200% 0}}`}</style>
            <Skeleton h={60} />
            <div style={{ marginTop: 20 }}><Skeleton h={220} /></div>
            <div style={{ marginTop: 16 }}><Skeleton h={300} /></div>
        </div>
    );

    if (err || !caseData) return (
        <div className="page-container" style={{ maxWidth: 1000 }}>
            <div style={{ background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.3)', borderRadius: 12, padding: 20, color: '#f87171', fontSize: 14 }}>
                Դեպքը չի գտնվել{err ? `: ${err}` : ''}
            </div>
        </div>
    );

    return (
        <>
            <style>{`
                @keyframes shimmer { 0%{background-position:-200% 0} 100%{background-position:200% 0} }
                @keyframes spin    { to{transform:rotate(360deg)} }
            `}</style>

            <div className="page-container" style={{ maxWidth: 1000 }}>

                {/* ── Header ── */}
                <div style={{ marginBottom: 24 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap', marginBottom: 6 }}>
                        <button onClick={() => navigate('/cases')} style={btnSecondary}>← Վերադառնալ</button>
                        <h1 style={{ fontSize: 22, fontWeight: 800, color: textPri, margin: 0 }}>
                            Դեպքի մանրամասներ
                        </h1>
                        <StatusBadge status={caseData.status} />
                    </div>
                    <p style={{ fontSize: 12, color: textMut, margin: 0 }}>
                        Ստեղծված {formatDate(caseData.createdAt)}
                        {caseData.completedAt && <span>  ·  Ավարտված {formatDate(caseData.completedAt)}</span>}
                    </p>
                </div>

                {/* ── Report Banner ── */}
                {done && (
                    <div style={{
                        background: 'linear-gradient(135deg, #0c1e35, #0e2d47)',
                        border: '1px solid rgba(14,165,233,0.25)',
                        borderRadius: 14, padding: '16px 24px', marginBottom: 20,
                        display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap',
                    }}>
                        <div>
                            <div style={{ fontSize: 15, fontWeight: 800, color: textPri }}>
                                📄 Հաշվետվությունը պատրաստ է
                            </div>
                            <div style={{ fontSize: 12, color: textSec, marginTop: 2 }}>
                                Բեռնել PDF ֆորմատով
                            </div>
                        </div>
                        <button
                            onClick={handleDownloadReport}
                            disabled={reportLoading}
                            style={{
                                padding: '9px 22px', borderRadius: 10,
                                border: '1px solid rgba(14,165,233,0.4)',
                                background: reportOk ? 'rgba(34,197,94,0.2)' : 'rgba(14,165,233,0.15)',
                                color: reportOk ? '#4ade80' : '#38bdf8',
                                fontWeight: 800, fontSize: 13,
                                cursor: reportLoading ? 'not-allowed' : 'pointer',
                                display: 'inline-flex', alignItems: 'center', gap: 8,
                                transition: 'all 0.25s',
                            }}>
                            {reportOk ? '✓ Բեռնվել է' : reportLoading ? '⏳ Բեռնվում է...' : '⬇ Բեռնել հաշվետվությունը'}
                        </button>
                    </div>
                )}

                {reportErr && (
                    <div style={{ background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.3)', borderRadius: 10, padding: '10px 16px', color: '#f87171', fontSize: 13, marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        {reportErr}
                        <button onClick={() => setReportErr(null)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#f87171', fontWeight: 700 }}>✕</button>
                    </div>
                )}

                {/* ── Case Info ── */}
                <Card title="Դեպքի մասին ինֆորմացիա">
                    <Row label="Ախտորոշման տեսակ" value={TYPE_HY[caseData.diagnosisType] ?? caseData.diagnosisType} />
                    <Row label="Կարգավիճակ" value={<StatusBadge status={caseData.status} />} />
                    <Row label="Հիվանդ" value={caseData.patientName ?? 'Անանուն'} />
                    <Row label="Հիվանդի կոդ" value={caseData.patientCode ?? '—'} />
                    <Row label="Տարիք" value={caseData.patientAge != null ? `${caseData.patientAge} տարեկան` : '—'} />
                    <Row label="Բժիշկ" value={caseData.doctorName ?? '—'} />
                    {caseData.doctorNotes && (
                        <Row label="Բժշկական նշումներ" value={
                            <span style={{ maxWidth: 300, wordBreak: 'break-word', textAlign: 'right', lineHeight: 1.5 }}>
                                {caseData.doctorNotes}
                            </span>
                        } />
                    )}
                </Card>

                {/* ── Images ── */}
                <ImagesSection caseId={caseData.caseId} status={caseData.status} />

                {/* ── AI Results ── */}
                <AIResultsSection caseId={caseData.caseId} status={caseData.status} />

                {/* ── Bottom Actions ── */}
                <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end', paddingBottom: 40, flexWrap: 'wrap' }}>
                    {done && (
                        <button
                            onClick={handleDownloadReport}
                            disabled={reportLoading}
                            style={{
                                padding: '9px 22px', borderRadius: 10, border: 'none',
                                background: reportLoading ? 'rgba(148,163,184,0.2)' : 'linear-gradient(135deg,#0284c7,#0ea5e9)',
                                color: reportLoading ? '#64748b' : '#fff',
                                fontWeight: 700, fontSize: 13,
                                cursor: reportLoading ? 'not-allowed' : 'pointer',
                                display: 'inline-flex', alignItems: 'center', gap: 7,
                                boxShadow: reportLoading ? 'none' : '0 2px 8px rgba(14,165,233,0.3)',
                            }}>
                            {reportLoading ? 'Բեռնվում է...' : '⬇ Բեռնել հաշվետվություն'}
                        </button>
                    )}
                    <button onClick={() => navigate('/cases')} style={btnSecondary}>Բոլոր դեպքերը</button>
                </div>
            </div>
        </>
    );
}