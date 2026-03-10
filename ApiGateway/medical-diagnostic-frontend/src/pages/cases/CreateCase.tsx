import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { casesApi } from '@/api/cases';
import { patientsApi } from '@/api/patients';

// ─── Types ────────────────────────────────────────────────────────────────────

interface PatientState {
    patientId: string;
    patientCode: string;
    patientName: string;
    patientAge: number;
}

type Step = 'info' | 'data' | 'done';

interface BrainClinicalForm {
    age: string;
    gender: 'M' | 'F';
    headache_severity: number;
    headache_frequency: 'never' | 'occasional' | 'weekly' | 'daily';
    seizures: boolean;
    vision_problems: boolean;
    motor_weakness: boolean;
    speech_difficulty: boolean;
    nausea: boolean;
    cognitive_decline: boolean;
    personality_changes: boolean;
    balance_issues: boolean;
    memory_problems: boolean;
    confusion: boolean;
    numbness: boolean;
    tremor: boolean;
    gait_disturbance: boolean;
}

interface LungClinicalForm {
    age: string;
    gender: 0 | 1;
    smoking_status: 0 | 1 | 2;
    pack_years: string;
    persistent_cough: boolean;
    coughing_blood: boolean;
    chest_pain: boolean;
    shortness_of_breath: boolean;
    wheezing: boolean;
    hoarseness: boolean;
    weight_loss: boolean;
    bone_pain: boolean;
    fatigue: boolean;
    family_history: boolean;
    copd: boolean;
    asbestos_exposure: boolean;
}

interface BrainLabForm {
    age: string; gender: 'M' | 'F';
    S100B: string; GFAP: string; NSE: string;
    WBC: string; RBC: string; Hemoglobin: string; Platelets: string;
    Glucose: string; BUN: string; Creatinine: string;
    ALT: string; AST: string; CRP: string; ESR: string;
    Amyloid_Beta: string; Tau_Protein: string;
    Oligoclonal_Bands: boolean; IgG_Index: string;
}

interface LungLabForm {
    age: string; gender: 0 | 1;
    cea: string; nse: string; cyfra_21_1: string; scc: string; progrp: string;
    wbc: string; hemoglobin: string; platelets: string;
    ldh: string; albumin: string; alp: string; calcium: string;
    crp: string; esr: string; ferritin: string;
}

// ─── Initial state factories ──────────────────────────────────────────────────

const initBrainClinical = (): BrainClinicalForm => ({
    age: '', gender: 'M', headache_severity: 0, headache_frequency: 'never',
    seizures: false, vision_problems: false, motor_weakness: false,
    speech_difficulty: false, nausea: false, cognitive_decline: false,
    personality_changes: false, balance_issues: false, memory_problems: false,
    confusion: false, numbness: false, tremor: false, gait_disturbance: false,
});

const initLungClinical = (): LungClinicalForm => ({
    age: '', gender: 1, smoking_status: 0, pack_years: '',
    persistent_cough: false, coughing_blood: false, chest_pain: false,
    shortness_of_breath: false, wheezing: false, hoarseness: false,
    weight_loss: false, bone_pain: false, fatigue: false,
    family_history: false, copd: false, asbestos_exposure: false,
});

const initBrainLab = (): BrainLabForm => ({
    age: '', gender: 'M',
    S100B: '', GFAP: '', NSE: '', WBC: '', RBC: '', Hemoglobin: '',
    Platelets: '', Glucose: '', BUN: '', Creatinine: '', ALT: '', AST: '',
    CRP: '', ESR: '', Amyloid_Beta: '', Tau_Protein: '',
    Oligoclonal_Bands: false, IgG_Index: '',
});

const initLungLab = (): LungLabForm => ({
    age: '', gender: 1,
    cea: '', nse: '', cyfra_21_1: '', scc: '', progrp: '',
    wbc: '', hemoglobin: '', platelets: '', ldh: '', albumin: '',
    alp: '', calcium: '', crp: '', esr: '', ferritin: '',
});

// ─── Tabs ─────────────────────────────────────────────────────────────────────

const TABS = [
    { id: 'image',    label: 'Պատկեր' },
    { id: 'symptoms', label: 'Ախտանիշներ' },
    { id: 'lab',      label: 'Լաբ. Հետազոտություն' },
];

// ─── Done Step ────────────────────────────────────────────────────────────────

interface DoneStepProps {
    caseId: string;
    onNavigate: (to: string) => void;
    btnPrimary: (disabled?: boolean) => React.CSSProperties;
    btnSecondary: React.CSSProperties;
}

function DoneStep({ caseId, onNavigate, btnPrimary, btnSecondary }: DoneStepProps) {
    const [countdown, setCountdown] = useState(5);
    useEffect(() => {
        if (countdown <= 0) { onNavigate(`/cases/${caseId}`); return; }
        const t = setTimeout(() => setCountdown(c => c - 1), 1000);
        return () => clearTimeout(t);
    }, [countdown, caseId, onNavigate]);

    return (
        <div className="page-container" style={{ maxWidth: 600, textAlign: 'center', paddingTop: 60 }}>
            <div style={{ fontSize: 72, marginBottom: 20 }}>✅</div>
            <h2 style={{ fontSize: 24, fontWeight: 800, color: '#f1f5f9', marginBottom: 8 }}>
                Վերլուծությունը սկսվել է
            </h2>
            <p style={{ fontSize: 14, color: '#64748b', marginBottom: 8, lineHeight: 1.6 }}>
                Տվյալները մշակվել է, արդյունքները կստանաք ավարտից հետո
            </p>
            <p style={{ fontSize: 13, color: '#475569', marginBottom: 32 }}>
                Ավտոմատ անցում {countdown} վայրկյանից
            </p>
            <div style={{ display: 'flex', gap: 12, justifyContent: 'center', flexWrap: 'wrap' }}>
                <button onClick={() => onNavigate(`/cases/${caseId}`)} style={btnPrimary()}>
                    Տեսնել
                </button>
                <button onClick={() => onNavigate('/cases')} style={btnSecondary}>
                    Բոլոր դեպքերը
                </button>
            </div>
            <div style={{ marginTop: 32, height: 4, borderRadius: 99, background: '#1e293b', overflow: 'hidden', maxWidth: 300, margin: '32px auto 0' }}>
                <div style={{ height: '100%', borderRadius: 99, background: 'linear-gradient(90deg, #0ea5e9, #38bdf8)', width: `${((5 - countdown) / 5) * 100}%`, transition: 'width 0.9s linear' }} />
            </div>
        </div>
    );
}

// ─── Main Component ───────────────────────────────────────────────────────────

export default function CreateCase() {
    const navigate = useNavigate();
    const location = useLocation();
    const preloaded = location.state as PatientState | null;

    const [patientCode, setPatientCode] = useState('');
    const [diagnosisType, setDiagnosisType] = useState('brain_tumor');
    const [notes, setNotes] = useState('');
    const [resolvedPatient, setResolvedPatient] = useState<PatientState | null>(preloaded);
    const [lookingUp, setLookingUp] = useState(false);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [step, setStep] = useState<Step>('info');
    const [createdCaseId, setCreatedCaseId] = useState<string | null>(null);
    const [activeTab, setActiveTab] = useState<'image' | 'symptoms' | 'lab'>('image');

    const [imageFile, setImageFile] = useState<File | null>(null);
    const [scanArea, setScanArea] = useState('brain');
    const [imageLoading, setImageLoading] = useState(false);
    const [imageSuccess, setImageSuccess] = useState(false);

    const [brainClinical, setBrainClinical] = useState<BrainClinicalForm>(initBrainClinical);
    const [lungClinical, setLungClinical] = useState<LungClinicalForm>(initLungClinical);
    const [symptomsLoading, setSymptomsLoading] = useState(false);
    const [symptomsSuccess, setSymptomsSuccess] = useState(false);

    const [brainLab, setBrainLab] = useState<BrainLabForm>(initBrainLab);
    const [lungLab, setLungLab] = useState<LungLabForm>(initLungLab);
    const [labName, setLabName] = useState('');
    const [labDate, setLabDate] = useState(new Date().toISOString().split('T')[0]);
    const [labLoading, setLabLoading] = useState(false);
    const [labSuccess, setLabSuccess] = useState(false);

    // ─── Handlers ─────────────────────────────────────────────────────────────

    const handlePatientCodeBlur = async () => {
        const code = patientCode.trim();
        if (!code) { setResolvedPatient(null); return; }
        setLookingUp(true); setError(null);
        try {
            const p = await patientsApi.getByCode(code);
            setResolvedPatient({ patientId: p.patientId, patientCode: p.patientCode, patientName: `${p.firstName} ${p.lastName}`, patientAge: p.age ?? 0 });
        } catch {
            setResolvedPatient(null);
            setError('Նշված կոդով հիվանդ չի գտնվել');
        } finally { setLookingUp(false); }
    };

    const handleCreateCase = async () => {
        setError(null);
        let patient = resolvedPatient;
        if (!patient) {
            const code = patientCode.trim();
            if (!code) { setError('Խնդրում ենք լրացնել հիվանդի կոդը'); return; }
            setLookingUp(true);
            try {
                const p = await patientsApi.getByCode(code);
                patient = { patientId: p.patientId, patientCode: p.patientCode, patientName: `${p.firstName} ${p.lastName}`, patientAge: p.age ?? 0 };
                setResolvedPatient(patient);
            } catch {
                setLookingUp(false);
                setError('Նշված կոդով հիվանդ չի գտնվել');
                return;
            }
            setLookingUp(false);
        }
        setLoading(true);
        try {
            const created = await casesApi.create({ patientId: patient.patientId, diagnosisType, doctorNotes: notes || undefined });
            if (!created?.caseId) { setError('Դեպքի ID-ն չի ստեղծվել։ Կրկնել կրկին։'); return; }
            setCreatedCaseId(created.caseId);
            setStep('data');
            setScanArea(diagnosisType.includes('lung') ? 'lung' : 'brain');
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : 'Ստեղծման սխալ');
        } finally { setLoading(false); }
    };

    const handleUploadImage = async () => {
        if (!createdCaseId || !imageFile) return;
        setImageLoading(true);
        try {
            await casesApi.uploadImage(createdCaseId, imageFile, scanArea);
            setImageSuccess(true); setImageFile(null);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : 'Պատկերի վերբեռնման սխալ');
        } finally { setImageLoading(false); }
    };

    const handleSubmitSymptoms = async () => {
        if (!createdCaseId) return;
        setSymptomsLoading(true); setError(null);
        try {
            if (diagnosisType === 'brain_tumor') {
                const f = brainClinical;
                const ageNum = parseFloat(f.age);
                if (!f.age || isNaN(ageNum) || ageNum < 1 || ageNum > 120) {
                    setError('Մուտքգրված է ամվավեր տարիք (1–120)'); setSymptomsLoading(false); return;
                }
                const familyHistory: Record<string, unknown> = {
                    age: ageNum,
                    gender_M: f.gender === 'M' ? 1 : 0,
                    headache_severity: f.headache_severity,
                    headache_frequency_never:      f.headache_frequency === 'never'      ? 1 : 0,
                    headache_frequency_occasional: f.headache_frequency === 'occasional' ? 1 : 0,
                    headache_frequency_weekly:     f.headache_frequency === 'weekly'     ? 1 : 0,
                    seizures:            f.seizures            ? 1 : 0,
                    vision_problems:     f.vision_problems     ? 1 : 0,
                    motor_weakness:      f.motor_weakness      ? 1 : 0,
                    speech_difficulty:   f.speech_difficulty   ? 1 : 0,
                    nausea:              f.nausea              ? 1 : 0,
                    cognitive_decline:   f.cognitive_decline   ? 1 : 0,
                    personality_changes: f.personality_changes ? 1 : 0,
                    balance_issues:      f.balance_issues      ? 1 : 0,
                    memory_problems:     f.memory_problems     ? 1 : 0,
                    confusion:           f.confusion           ? 1 : 0,
                    numbness:            f.numbness            ? 1 : 0,
                    tremor:              f.tremor              ? 1 : 0,
                    gait_disturbance:    f.gait_disturbance    ? 1 : 0,
                };
                await casesApi.submitSymptoms(createdCaseId, { symptoms: [], smokingHistory: false, familyHistory });
            } else {
                const f = lungClinical;
                const ageNum = parseFloat(f.age);
                if (!f.age || isNaN(ageNum) || ageNum < 1 || ageNum > 120) {
                    setError('Մուտքագրված է անվավեր տարիք'); setSymptomsLoading(false); return;
                }
                const familyHistory: Record<string, unknown> = {
                    age: ageNum,
                    gender:          f.gender,
                    smoking_status:  f.smoking_status,
                    pack_years:      f.pack_years ? parseFloat(f.pack_years) : 0,
                    persistent_cough:    f.persistent_cough    ? 1 : 0,
                    coughing_blood:      f.coughing_blood      ? 1 : 0,
                    chest_pain:          f.chest_pain          ? 1 : 0,
                    shortness_of_breath: f.shortness_of_breath ? 1 : 0,
                    wheezing:            f.wheezing            ? 1 : 0,
                    hoarseness:          f.hoarseness          ? 1 : 0,
                    weight_loss:         f.weight_loss         ? 1 : 0,
                    bone_pain:           f.bone_pain           ? 1 : 0,
                    fatigue:             f.fatigue             ? 1 : 0,
                    family_history:      f.family_history      ? 1 : 0,
                    copd:                f.copd                ? 1 : 0,
                    asbestos_exposure:   f.asbestos_exposure   ? 1 : 0,
                };
                await casesApi.submitSymptoms(createdCaseId, {
                    symptoms: [], smokingHistory: f.smoking_status > 0, familyHistory,
                });
            }
            setSymptomsSuccess(true);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : 'Ախտանիշների ուղարկման սխալ');
        } finally { setSymptomsLoading(false); }
    };

    const handleSubmitLab = async () => {
        if (!createdCaseId) return;
        setLabLoading(true); setError(null);
        try {
            let testResults: Record<string, unknown>;
            const n = (v: string) => (v === '' ? 0 : parseFloat(v));

            if (diagnosisType === 'brain_tumor') {
                const f = brainLab;
                if (!f.age) { setError('տարիքը պարտադիր է'); setLabLoading(false); return; }
                testResults = {
                    age: n(f.age), gender_M: f.gender === 'M' ? 1 : 0,
                    S100B: n(f.S100B), GFAP: n(f.GFAP), NSE: n(f.NSE),
                    WBC: n(f.WBC), RBC: n(f.RBC), Hemoglobin: n(f.Hemoglobin),
                    Platelets: n(f.Platelets), Glucose: n(f.Glucose), BUN: n(f.BUN),
                    Creatinine: n(f.Creatinine), ALT: n(f.ALT), AST: n(f.AST),
                    CRP: n(f.CRP), ESR: n(f.ESR),
                    Amyloid_Beta: n(f.Amyloid_Beta), Tau_Protein: n(f.Tau_Protein),
                    Oligoclonal_Bands: f.Oligoclonal_Bands ? 1 : 0,
                    IgG_Index: n(f.IgG_Index),
                };
            } else {
                const f = lungLab;
                if (!f.age) { setError('տարիքը պարտադիր է'); setLabLoading(false); return; }
                testResults = {
                    age: n(f.age), gender: f.gender,
                    cea: n(f.cea), nse: n(f.nse), cyfra_21_1: n(f.cyfra_21_1),
                    scc: n(f.scc), progrp: n(f.progrp), wbc: n(f.wbc),
                    hemoglobin: n(f.hemoglobin), platelets: n(f.platelets),
                    ldh: n(f.ldh), albumin: n(f.albumin), alp: n(f.alp),
                    calcium: n(f.calcium), crp: n(f.crp), esr: n(f.esr),
                    ferritin: n(f.ferritin),
                };
            }
            await casesApi.submitLabTests(createdCaseId, {
                testDate: new Date(labDate).toISOString(),
                labName: labName || 'Լաբորատորիա',
                testResults,
            });
            setLabSuccess(true);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : 'Լաբարատոր տվյալների ուղարկման սխալ');
        } finally { setLabLoading(false); }
    };

    const handleAnalyze = async () => {
        if (!createdCaseId) return;
        setLoading(true);
        try {
            await casesApi.triggerAnalysis(createdCaseId, {
                includeImaging: imageSuccess,
                includeClinical: symptomsSuccess,
                includeLaboratory: labSuccess,
            });
            setStep('done');
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : 'Վերլուծության գործարկման սխալ');
        } finally { setLoading(false); }
    };

    // ─── Dark Styles ──────────────────────────────────────────────────────────

    const card: React.CSSProperties = {
        background: 'rgba(255,255,255,0.03)',
        border: '1px solid rgba(255,255,255,0.07)',
        borderRadius: 16,
        padding: 24,
    };

    const lbl: React.CSSProperties = {
        fontSize: 11,
        fontWeight: 700,
        display: 'block',
        marginBottom: 5,
        color: '#475569',
        textTransform: 'uppercase',
        letterSpacing: '0.05em',
    };

    const inp: React.CSSProperties = {
        width: '100%',
        padding: '8px 11px',
        borderRadius: 8,
        border: '1px solid rgba(255,255,255,0.1)',
        fontSize: 13,
        boxSizing: 'border-box',
        background: 'rgba(255,255,255,0.05)',
        color: '#e2e8f0',
        outline: 'none',
    };

    const btnPrimary = (disabled = false): React.CSSProperties => ({
        padding: '9px 20px',
        borderRadius: 10,
        border: 'none',
        background: disabled ? 'rgba(148,163,184,0.2)' : 'linear-gradient(135deg, #0284c7, #0ea5e9)',
        color: disabled ? '#475569' : '#fff',
        fontWeight: 700,
        fontSize: 13,
        cursor: disabled ? 'not-allowed' : 'pointer',
        opacity: disabled ? 0.6 : 1,
        boxShadow: disabled ? 'none' : '0 2px 8px rgba(14,165,233,0.3)',
    });

    const btnSecondary: React.CSSProperties = {
        padding: '9px 18px',
        borderRadius: 10,
        border: '1px solid rgba(255,255,255,0.1)',
        background: 'rgba(255,255,255,0.04)',
        color: '#94a3b8',
        fontWeight: 600,
        fontSize: 13,
        cursor: 'pointer',
    };

    const successBadge: React.CSSProperties = {
        display: 'inline-flex',
        alignItems: 'center',
        gap: 6,
        padding: '4px 12px',
        borderRadius: 99,
        background: 'rgba(16,185,129,0.12)',
        border: '1px solid rgba(16,185,129,0.3)',
        color: '#34d399',
        fontSize: 12,
        fontWeight: 700,
    };

    const sectionTitle: React.CSSProperties = {
        fontSize: 10,
        fontWeight: 800,
        color: '#334155',
        textTransform: 'uppercase',
        letterSpacing: '0.07em',
        marginBottom: 12,
        marginTop: 4,
        paddingBottom: 6,
        borderBottom: '1px solid rgba(255,255,255,0.05)',
    };

    const checkRow: React.CSSProperties = {
        display: 'flex',
        alignItems: 'center',
        gap: 8,
        padding: '7px 10px',
        borderRadius: 8,
        cursor: 'pointer',
        userSelect: 'none',
        transition: 'background 0.1s',
    };

    // ─── Done ─────────────────────────────────────────────────────────────────
    if (step === 'done') {
        return <DoneStep caseId={createdCaseId!} onNavigate={navigate} btnPrimary={btnPrimary} btnSecondary={btnSecondary} />;
    }

    // ─── Data collection step ─────────────────────────────────────────────────
    if (step === 'data') {
        const isBrain = diagnosisType === 'brain_tumor';

        const BrainSymptomsPanel = () => {
            const f = brainClinical;
            const set = (k: keyof BrainClinicalForm, v: unknown) =>
                setBrainClinical(prev => ({ ...prev, [k]: v }));

            const boolFields: { key: keyof BrainClinicalForm; label: string }[] = [
                { key: 'seizures',            label: 'Ցնցումներ' },
                { key: 'vision_problems',     label: 'Տեսողության խանգարումներ' },
                { key: 'motor_weakness',      label: 'Շարժողական թուլություն' },
                { key: 'speech_difficulty',   label: 'Խոսքի խանգարում' },
                { key: 'nausea',              label: 'Սրտխառնոց' },
                { key: 'cognitive_decline',   label: 'Կոգնիտիվ անկում' },
                { key: 'personality_changes', label: 'Անձնային փոփոխություններ' },
                { key: 'balance_issues',      label: 'Հավասարակշռության խանգարում' },
                { key: 'memory_problems',     label: 'Հիշողության խանգարումներ' },
                { key: 'confusion',           label: 'Շփոթված վիճակ' },
                { key: 'numbness',            label: 'Թմրածություն' },
                { key: 'tremor',              label: 'Դող' },
                { key: 'gait_disturbance',    label: 'Քայլվածքի խանգարում' },
            ];

            return (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
                    <div>
                        <div style={sectionTitle}>Դեմոգրաֆիա</div>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                            <div>
                                <label style={lbl}>Տարիք <span style={{ color: '#f87171' }}>*</span></label>
                                <input type="number" min={1} max={120} value={f.age}
                                       onChange={e => set('age', e.target.value)}
                                       style={inp} placeholder="20–85" />
                            </div>
                            <div>
                                <label style={lbl}>Սեռ <span style={{ color: '#f87171' }}>*</span></label>
                                <select value={f.gender} onChange={e => set('gender', e.target.value as 'M' | 'F')} style={inp}>
                                    <option value="M">Արական</option>
                                    <option value="F">Իգական</option>
                                </select>
                            </div>
                        </div>
                    </div>

                    <div>
                        <div style={sectionTitle}>Գլխացավ</div>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                            <div>
                                <label style={lbl}>Ինտենսիվություն (0–5) <span style={{ color: '#f87171' }}>*</span></label>
                                <input type="number" min={0} max={5} step={1} value={f.headache_severity}
                                       onChange={e => set('headache_severity', parseInt(e.target.value) || 0)}
                                       style={inp} placeholder="0–5" />
                                <div style={{ fontSize: 11, color: '#334155', marginTop: 3 }}>0 = Ցածր, 5 = Բարձր</div>
                            </div>
                            <div>
                                <label style={lbl}>Հաճախականություն <span style={{ color: '#f87171' }}>*</span></label>
                                <select value={f.headache_frequency}
                                        onChange={e => set('headache_frequency', e.target.value as BrainClinicalForm['headache_frequency'])}
                                        style={inp}>
                                    <option value="never">Երբեք</option>
                                    <option value="occasional">Երբեմն</option>
                                    <option value="weekly">Շաբաթական</option>
                                    <option value="daily">Ամեն օր</option>
                                </select>
                            </div>
                        </div>
                    </div>

                    <div>
                        <div style={sectionTitle}>Ախտանիշներ</div>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                            {boolFields.map(({ key, label }) => (
                                <label key={key} style={checkRow} onClick={() => set(key, !f[key])}>
                                    <input type="checkbox" checked={!!f[key]} onChange={() => set(key, !f[key])}
                                           style={{ width: 15, height: 15, accentColor: '#0ea5e9', cursor: 'pointer' }} />
                                    <span style={{ fontSize: 13, color: '#94a3b8' }}>{label}</span>
                                </label>
                            ))}
                        </div>
                    </div>
                </div>
            );
        };

        const LungSymptomsPanel = () => {
            const f = lungClinical;
            const set = (k: keyof LungClinicalForm, v: unknown) =>
                setLungClinical(prev => ({ ...prev, [k]: v }));

            const boolFields: { key: keyof LungClinicalForm; label: string }[] = [
                { key: 'persistent_cough',    label: 'Մշտական հազ' },
                { key: 'coughing_blood',      label: 'Հազ արյունով' },
                { key: 'chest_pain',          label: 'Կրծքավանդակային ցավ' },
                { key: 'shortness_of_breath', label: 'Շնչահեղձություն' },
                { key: 'wheezing',            label: 'Ծանր շնչառություն' },
                { key: 'hoarseness',          label: 'Ձայնի կոպտություն' },
                { key: 'weight_loss',         label: 'Քաշի կորուստ' },
                { key: 'bone_pain',           label: 'Ոսկրային ցավ' },
                { key: 'fatigue',             label: 'Հոգնածություն' },
                { key: 'family_history',      label: 'Ընտանեկան պատմություն (քաղցկեղ)' },
                { key: 'copd',                label: 'Թոքերի քրոնիկ օբստրուկտիվ հիվանդություն' },
                { key: 'asbestos_exposure',   label: 'Ազբեստի առկայություն' },
            ];

            return (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
                    <div>
                        <div style={sectionTitle}>Դեմոգրաֆիա</div>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                            <div>
                                <label style={lbl}>Տարիք <span style={{ color: '#f87171' }}>*</span></label>
                                <input type="number" min={1} max={120} value={f.age}
                                       onChange={e => set('age', e.target.value)}
                                       style={inp} placeholder="20–90" />
                            </div>
                            <div>
                                <label style={lbl}>Սեռ <span style={{ color: '#f87171' }}>*</span></label>
                                <select value={f.gender}
                                        onChange={e => set('gender', parseInt(e.target.value) as 0 | 1)}
                                        style={inp}>
                                    <option value={1}>Արական</option>
                                    <option value={0}>Իգական</option>
                                </select>
                            </div>
                        </div>
                    </div>

                    <div>
                        <div style={sectionTitle}>Ծխախոտի պատմություն</div>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                            <div>
                                <label style={lbl}>Ծխելու պատմություն <span style={{ color: '#f87171' }}>*</span></label>
                                <select value={f.smoking_status}
                                        onChange={e => set('smoking_status', parseInt(e.target.value) as 0 | 1 | 2)}
                                        style={inp}>
                                    <option value={0}>Երբեք (0)</option>
                                    <option value={1}>Նախկինում (1)</option>
                                    <option value={2}>Ներկայում (2)</option>
                                </select>
                            </div>
                            <div>
                                <label style={lbl}>Քանակ</label>
                                <input type="number" min={0} max={100} step={0.5} value={f.pack_years}
                                       onChange={e => set('pack_years', e.target.value)}
                                       style={inp} placeholder="0+" />
                                <div style={{ fontSize: 11, color: '#334155', marginTop: 3 }}>Ծխախոտի քանակ</div>
                            </div>
                        </div>
                    </div>

                    <div>
                        <div style={sectionTitle}>Ախտանիշներ և ռիսկային ֆակտորներ</div>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                            {boolFields.map(({ key, label }) => (
                                <label key={key} style={checkRow} onClick={() => set(key, !f[key])}>
                                    <input type="checkbox" checked={!!f[key]} onChange={() => set(key, !f[key])}
                                           style={{ width: 15, height: 15, accentColor: '#0ea5e9', cursor: 'pointer' }} />
                                    <span style={{ fontSize: 13, color: '#94a3b8' }}>{label}</span>
                                </label>
                            ))}
                        </div>
                    </div>
                </div>
            );
        };

        const BrainLabPanel = () => {
            const f = brainLab;
            const set = (k: keyof BrainLabForm, v: unknown) =>
                setBrainLab(prev => ({ ...prev, [k]: v }));

            type LabField = { key: keyof BrainLabForm; label: string; unit: string; min: number; max: number; step: number };
            const numFields: LabField[] = [
                { key: 'S100B',        label: 'S100B սպիտակուց',                         unit: 'մկգ/լ',   min: 0, max: 5,    step: 0.01 },
                { key: 'GFAP',         label: 'GFAP սպիտակուց',                          unit: 'մկգ/լ',   min: 0, max: 10,   step: 0.01 },
                { key: 'NSE',          label: 'Նեյրոն-հատուկ էնոլազա (NSE)',              unit: 'նգ/մլ',   min: 0, max: 100,  step: 0.1  },
                { key: 'WBC',          label: 'Լեյկոցիտներ (WBC)',                        unit: '×10⁹/լ',  min: 0, max: 50,   step: 0.1  },
                { key: 'RBC',          label: 'Էրիթրոցիտներ (RBC)',                       unit: '×10¹²/լ', min: 0, max: 10,   step: 0.01 },
                { key: 'Hemoglobin',   label: 'Հեմոգլոբին',                               unit: 'գ/դլ',    min: 0, max: 25,   step: 0.1  },
                { key: 'Platelets',    label: 'Թրոմբոցիտներ',                             unit: '×10⁹/լ',  min: 0, max: 1000, step: 1    },
                { key: 'Glucose',      label: 'Արյան գլյուկոզա',                          unit: 'մգ/դլ',   min: 0, max: 500,  step: 0.1  },
                { key: 'BUN',          label: 'Միզանյութի ազոտ (BUN)',                    unit: 'մգ/դլ',   min: 0, max: 100,  step: 0.1  },
                { key: 'Creatinine',   label: 'Կրեատինին',                                unit: 'մգ/դլ',   min: 0, max: 20,   step: 0.01 },
                { key: 'ALT',          label: 'ԱԼՏ (Ալանին-ամինոտրանսֆերազա)',            unit: 'ու/լ',    min: 0, max: 1000, step: 0.1  },
                { key: 'AST',          label: 'ԱՍՏ (Ասպարտատ-ամինոտրանսֆերազա)',         unit: 'ու/լ',    min: 0, max: 1000, step: 0.1  },
                { key: 'CRP',          label: 'C-ռեակտիվ սպիտակուց (CRP)',                unit: 'մգ/լ',    min: 0, max: 200,  step: 0.1  },
                { key: 'ESR',          label: 'Էրիթ. նստեցման արագություն (ESR)',         unit: 'մմ/հր',   min: 0, max: 150,  step: 1    },
                { key: 'Amyloid_Beta', label: 'Ամիլոիդ բետա (Aβ)',                        unit: 'պգ/մլ',   min: 0, max: 3000, step: 1    },
                { key: 'Tau_Protein',  label: 'Tau սպիտակուց',                             unit: 'պգ/մլ',   min: 0, max: 3000, step: 1    },
                { key: 'IgG_Index',    label: 'IgG ինդեքս',                                unit: 'ռ',       min: 0, max: 5,    step: 0.01 },
            ];

            const renderFields = (fields: LabField[]) => (
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
                    {fields.map(({ key, label, unit, min, max, step }) => (
                        <div key={key}>
                            <label style={lbl}>{label} <span style={{ color: '#334155', fontWeight: 400 }}>({unit})</span></label>
                            <input type="number" min={min} max={max} step={step}
                                   value={(f as unknown as Record<string, unknown>)[key] as string}
                                   onChange={e => set(key, e.target.value)}
                                   style={inp} placeholder={`${min}–${max}`} />
                        </div>
                    ))}
                </div>
            );

            return (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
                    <div>
                        <div style={sectionTitle}>Դեմոգրաֆիա</div>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                            <div>
                                <label style={lbl}>Տարիք <span style={{ color: '#f87171' }}>*</span></label>
                                <input type="number" min={1} max={120} value={f.age}
                                       onChange={e => set('age', e.target.value)} style={inp} placeholder="20–85" />
                            </div>
                            <div>
                                <label style={lbl}>Սեռ <span style={{ color: '#f87171' }}>*</span></label>
                                <select value={f.gender} onChange={e => set('gender', e.target.value as 'M' | 'F')} style={inp}>
                                    <option value="M">Արական</option>
                                    <option value="F">Իգական</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    <div>
                        <div style={sectionTitle}>Նեյրոնոլոգական կենսաբանական նյութեր</div>
                        {renderFields(numFields.slice(0, 3))}
                    </div>
                    <div>
                        <div style={sectionTitle}>Արյան ընդհանուր պարամետրեր (CBC)</div>
                        {renderFields(numFields.slice(3, 7))}
                    </div>
                    <div>
                        <div style={sectionTitle}>Քիմիական պանելներ</div>
                        {renderFields(numFields.slice(7, 14))}
                    </div>
                    <div>
                        <div style={sectionTitle}>Ալցհեյմեր կենսաբանական նյութեր</div>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
                            {numFields.slice(14).map(({ key, label, unit, min, max, step }) => (
                                <div key={key}>
                                    <label style={lbl}>{label} <span style={{ color: '#334155', fontWeight: 400 }}>({unit})</span></label>
                                    <input type="number" min={min} max={max} step={step}
                                           value={(f as unknown as Record<string, unknown>)[key] as string}
                                           onChange={e => set(key, e.target.value)}
                                           style={inp} placeholder={`${min}–${max}`} />
                                </div>
                            ))}
                            <div>
                                <label style={lbl}>Օլիգոկլոնալ բոնդ (OB)</label>
                                <label style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '9px 11px', border: '1px solid rgba(255,255,255,0.1)', borderRadius: 8, cursor: 'pointer', background: 'rgba(255,255,255,0.04)' }}>
                                    <input type="checkbox" checked={f.Oligoclonal_Bands}
                                           onChange={e => set('Oligoclonal_Bands', e.target.checked)}
                                           style={{ width: 16, height: 16, accentColor: '#0ea5e9' }} />
                                    <span style={{ fontSize: 13, color: '#94a3b8' }}>Դրական</span>
                                </label>
                            </div>
                        </div>
                    </div>
                </div>
            );
        };

        const LungLabPanel = () => {
            const f = lungLab;
            const set = (k: keyof LungLabForm, v: unknown) =>
                setLungLab(prev => ({ ...prev, [k]: v }));

            type LabField = { key: keyof LungLabForm; label: string; unit: string; min: number; max: number; step: number };
            const numFields: LabField[] = [
                { key: 'cea',        label: 'CEA (Կարցինոէմբրիոնալ անտիգեն)',           unit: 'ng/mL',  min: 0, max: 200,  step: 0.01 },
                { key: 'nse',        label: 'NSE (Նեյրոն-հատուկ էնոլազա)',               unit: 'ng/mL',  min: 0, max: 200,  step: 0.1  },
                { key: 'cyfra_21_1', label: 'CYFRA 21-1 (Ցիտոկերատին 19)',              unit: 'ng/mL',  min: 0, max: 100,  step: 0.01 },
                { key: 'scc',        label: 'SCC (Թիթեղաբջջային կարցինոմայի անտիգեն)', unit: 'ng/mL',  min: 0, max: 50,   step: 0.01 },
                { key: 'progrp',     label: 'ProGRP (Պրոգաստրին-ազատող պեպտիդ)',        unit: 'pg/mL',  min: 0, max: 2000, step: 1    },
                { key: 'wbc',        label: 'Լեյկոցիտներ (WBC)',                         unit: '×10⁹/L', min: 0, max: 50,   step: 0.1  },
                { key: 'hemoglobin', label: 'Հեմոգլոբին',                                 unit: 'g/dL',   min: 0, max: 25,   step: 0.1  },
                { key: 'platelets',  label: 'Թրոմբոցիտներ',                               unit: '×10⁹/L', min: 0, max: 1000, step: 1    },
                { key: 'ldh',        label: 'LDH (Լակտատ դեհիդրոգենազա)',                unit: 'U/L',    min: 0, max: 3000, step: 1    },
                { key: 'albumin',    label: 'Ալբումին',                                   unit: 'g/dL',   min: 0, max: 10,   step: 0.01 },
                { key: 'alp',        label: 'ALP (Ալկալային ֆոսֆատազա)',                 unit: 'U/L',    min: 0, max: 1000, step: 1    },
                { key: 'calcium',    label: 'Կալցիում',                                   unit: 'mg/dL',  min: 0, max: 20,   step: 0.01 },
                { key: 'crp',        label: 'C-ռեակտիվ սպիտակուց (CRP)',                 unit: 'mg/L',   min: 0, max: 200,  step: 0.1  },
                { key: 'esr',        label: 'Էրիթ. նստեցման արագություն (ESR)',          unit: 'mm/hr',  min: 0, max: 150,  step: 1    },
                { key: 'ferritin',   label: 'Ֆերիտին',                                    unit: 'ng/mL',  min: 0, max: 2000, step: 1    },
            ];

            const sections = [
                { title: 'Ուռուցքային մարկերներ', fields: numFields.slice(0, 5) },
                { title: 'Արյան ընդհանուր քննություն (CBC)', fields: numFields.slice(5, 8) },
                { title: 'Կենսաքիմիական հետազոտություններ', fields: numFields.slice(8) },
            ];

            return (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
                    <div>
                        <div style={sectionTitle}>Դեմոգրաֆիա</div>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                            <div>
                                <label style={lbl}>Տարիք <span style={{ color: '#f87171' }}>*</span></label>
                                <input type="number" min={1} max={120} value={f.age}
                                       onChange={e => set('age', e.target.value)} style={inp} placeholder="20–90" />
                            </div>
                            <div>
                                <label style={lbl}>Սեռ <span style={{ color: '#f87171' }}>*</span></label>
                                <select value={f.gender}
                                        onChange={e => set('gender', parseInt(e.target.value) as 0 | 1)} style={inp}>
                                    <option value={1}>Արական</option>
                                    <option value={0}>Իգական</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    {sections.map(sec => (
                        <div key={sec.title}>
                            <div style={sectionTitle}>{sec.title}</div>
                            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
                                {sec.fields.map(({ key, label, unit, min, max, step }) => (
                                    <div key={key}>
                                        <label style={lbl}>{label} <span style={{ color: '#334155', fontWeight: 400 }}>({unit})</span></label>
                                        <input type="number" min={min} max={max} step={step}
                                               value={(f as unknown as Record<string, unknown>)[key] as string}
                                               onChange={e => set(key, e.target.value)}
                                               style={inp} placeholder={`${min}–${max}`} />
                                    </div>
                                ))}
                            </div>
                        </div>
                    ))}
                </div>
            );
        };

        return (
            <>
                <style>{`
                    .cc-inp-dark { color-scheme: dark; }
                    .cc-inp-dark input, .cc-inp-dark select, .cc-inp-dark textarea {
                        color-scheme: dark;
                    }
                    input[type=number]::-webkit-inner-spin-button,
                    input[type=number]::-webkit-outer-spin-button { opacity: 0.3; }
                    input::placeholder { color: #334155 !important; }
                    select option { background: #1e293b; color: #e2e8f0; }
                    input[type=date]::-webkit-calendar-picker-indicator { filter: invert(0.5); }
                `}</style>
                <div className="cc-inp-dark page-container" style={{ maxWidth: 820 }}>
                    <div style={{ marginBottom: 20 }}>
                        <h1 style={{ fontSize: 22, fontWeight: 800, marginBottom: 4, color: '#f1f5f9' }}>Տվյալների հավաքագրում</h1>
                        <p style={{ fontSize: 13, color: '#475569' }}>Դեպքը ավելացվել է</p>
                    </div>

                    {/* Patient badge */}
                    <div style={{ ...card, marginBottom: 16, padding: '12px 20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <div>
                            <div style={{ fontSize: 13, fontWeight: 700, color: '#e2e8f0' }}>{resolvedPatient?.patientName}</div>
                            <div style={{ fontSize: 12, color: '#475569', marginTop: 2 }}>
                                {resolvedPatient?.patientCode} · {isBrain ? 'Ուղեղի ուռուցք' : 'Թոքերի քաղցկեղ'}
                            </div>
                        </div>
                        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                            {imageSuccess    && <span style={successBadge}>✓ Պատկեր</span>}
                            {symptomsSuccess && <span style={successBadge}>✓ Ախտանիշներ</span>}
                            {labSuccess      && <span style={successBadge}>✓ Լաբ. հետազոտություն</span>}
                        </div>
                    </div>

                    {/* Error */}
                    {error && (
                        <div style={{ background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.3)', borderRadius: 12, padding: '12px 16px', color: '#f87171', fontSize: 14, marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <span>Սխալ՝ {error}</span>
                            <button onClick={() => setError(null)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#f87171', fontWeight: 700, fontSize: 16 }}>✕</button>
                        </div>
                    )}

                    {/* Tab bar */}
                    <div style={{ display: 'flex', gap: 4, marginBottom: 16, background: 'rgba(0,0,0,0.3)', borderRadius: 12, padding: 4, border: '1px solid rgba(255,255,255,0.06)' }}>
                        {TABS.map(t => (
                            <button key={t.id} onClick={() => setActiveTab(t.id as typeof activeTab)}
                                    style={{
                                        flex: 1, padding: '8px 0', borderRadius: 8, border: 'none',
                                        fontSize: 13, fontWeight: 600, cursor: 'pointer',
                                        background: activeTab === t.id ? 'rgba(255,255,255,0.07)' : 'transparent',
                                        color: activeTab === t.id ? '#e2e8f0' : '#475569',
                                        boxShadow: activeTab === t.id ? '0 1px 4px rgba(0,0,0,0.3)' : 'none',
                                        transition: 'all 0.15s',
                                    }}>
                                {t.label}
                                {(t.id === 'image' && imageSuccess) || (t.id === 'symptoms' && symptomsSuccess) || (t.id === 'lab' && labSuccess)
                                    ? <span style={{ marginLeft: 6, color: '#34d399' }}>✓</span> : ''}
                            </button>
                        ))}
                    </div>

                    {/* Image Tab */}
                    {activeTab === 'image' && (
                        <div style={{ ...card, display: 'flex', flexDirection: 'column', gap: 16 }}>
                            <div>
                                <label style={lbl}>Սկանավորման շրջան</label>
                                <select value={scanArea} onChange={e => setScanArea(e.target.value)} style={inp}>
                                    <option value="brain">Ուղեղ</option>
                                    <option value="lung">Թոքեր</option>
                                </select>
                            </div>
                            <div>
                                <label style={lbl}>Բժշկական պատկեր (.jpg, .png, .dcm)</label>
                                <input type="file" accept=".jpg,.jpeg,.png,.dcm"
                                       onChange={e => { setImageFile(e.target.files?.[0] ?? null); setImageSuccess(false); }}
                                       style={{ ...inp, padding: '7px 11px', color: '#94a3b8' }} />
                                {imageFile && <div style={{ fontSize: 12, color: '#475569', marginTop: 4 }}>{imageFile.name} · {(imageFile.size / 1024).toFixed(0)} ԿԲ</div>}
                            </div>
                            {imageSuccess
                                ? <div style={successBadge}>✓ Պատկերը հաջողությամբ բեռնվել է</div>
                                : <button onClick={handleUploadImage} disabled={!imageFile || imageLoading} style={btnPrimary(!imageFile || imageLoading)}>
                                    {imageLoading ? 'Բեռնվում է…' : 'Բեռնել պատկերը'}
                                </button>
                            }
                        </div>
                    )}

                    {/* Symptoms Tab */}
                    {activeTab === 'symptoms' && (
                        <div style={{ ...card }}>
                            {isBrain ? <BrainSymptomsPanel /> : <LungSymptomsPanel />}
                            <div style={{ marginTop: 20 }}>
                                {symptomsSuccess
                                    ? <div style={successBadge}>✓ Ախտանիշները հաջողությամբ ուղարկվել են</div>
                                    : <button onClick={handleSubmitSymptoms} disabled={symptomsLoading} style={btnPrimary(symptomsLoading)}>
                                        {symptomsLoading ? 'Ուղարկվում է…' : 'Ուղարկել ախտանիշները'}
                                    </button>
                                }
                            </div>
                        </div>
                    )}

                    {/* Lab Tab */}
                    {activeTab === 'lab' && (
                        <div style={{ ...card, display: 'flex', flexDirection: 'column', gap: 16 }}>
                            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                                <div>
                                    <label style={lbl}>Լաբորատորիայի անվանում</label>
                                    <input value={labName} onChange={e => setLabName(e.target.value)}
                                           style={inp} placeholder="MED-LAB" />
                                </div>
                                <div>
                                    <label style={lbl}>Հետազոտության ամսաթիվ</label>
                                    <input value={labDate} onChange={e => setLabDate(e.target.value)}
                                           style={inp} type="date" />
                                </div>
                            </div>
                            {isBrain ? <BrainLabPanel /> : <LungLabPanel />}
                            <div style={{ marginTop: 8 }}>
                                {labSuccess
                                    ? <div style={successBadge}>✓ Լաբ. տվյալները հաջողությամբ ուղարկվել են</div>
                                    : <button onClick={handleSubmitLab} disabled={labLoading} style={btnPrimary(labLoading)}>
                                        {labLoading ? 'Ուղարկվում է…' : 'Ուղարկել լաբ. արդյունքները'}
                                    </button>
                                }
                            </div>
                        </div>
                    )}

                    {/* Footer */}
                    <div style={{ display: 'flex', gap: 10, marginTop: 24, justifyContent: 'space-between' }}>
                        <button onClick={() => navigate('/cases')} style={btnSecondary}>Ավարտել հետո</button>
                        <button
                            onClick={handleAnalyze}
                            disabled={loading || (!imageSuccess && !symptomsSuccess && !labSuccess)}
                            style={btnPrimary(loading || (!imageSuccess && !symptomsSuccess && !labSuccess))}>
                            {loading ? 'Գործարկվում է…' : 'Գործարկել վերլուծություն'}
                        </button>
                    </div>
                </div>
            </>
        );
    }

    // ─── Info step ────────────────────────────────────────────────────────────
    return (
        <>
            <style>{`
                .cc-inp-dark input, .cc-inp-dark select, .cc-inp-dark textarea {
                    color-scheme: dark;
                }
                select option { background: #1e293b; color: #e2e8f0; }
                input::placeholder, textarea::placeholder { color: #334155 !important; }
                input[type=date]::-webkit-calendar-picker-indicator { filter: invert(0.5); }
            `}</style>
            <div className="cc-inp-dark page-container" style={{ maxWidth: 800 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 6 }}>
                    <div style={{
                        width: 36, height: 36, borderRadius: 10,
                        background: 'linear-gradient(135deg, #1d4ed8, #0ea5e9)',
                        display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 18,
                    }}>➕</div>
                    <h1 style={{ fontSize: 24, fontWeight: 800, margin: 0, color: '#f1f5f9' }}>Նոր դեպք</h1>
                </div>
                <p style={{ fontSize: 13, color: '#475569', marginBottom: 24, paddingLeft: 48 }}>Լրացրեք տվյալները</p>

                {error && (
                    <div style={{ background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.3)', borderRadius: 12, padding: '12px 16px', color: '#f87171', fontSize: 14, marginBottom: 20 }}>
                        Սխալ՝ {error}
                    </div>
                )}

                <div style={{
                    background: 'rgba(255,255,255,0.03)',
                    border: '1px solid rgba(255,255,255,0.07)',
                    borderRadius: 16,
                    padding: 24,
                    display: 'flex',
                    flexDirection: 'column',
                    gap: 18,
                }}>
                    {resolvedPatient ? (
                        <div>
                            <label style={{ fontSize: 11, fontWeight: 700, display: 'block', marginBottom: 6, color: '#475569', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Հիվանդ</label>
                            <div style={{ padding: '12px 16px', borderRadius: 12, background: 'rgba(16,185,129,0.08)', border: '1px solid rgba(16,185,129,0.25)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                <div>
                                    <div style={{ fontSize: 14, fontWeight: 700, color: '#34d399' }}>✓ {resolvedPatient.patientName}</div>
                                    <div style={{ fontSize: 12, color: '#475569', marginTop: 2 }}>{resolvedPatient.patientCode} · {resolvedPatient.patientAge} տ.</div>
                                </div>
                                {!preloaded && (
                                    <button onClick={() => { setResolvedPatient(null); setPatientCode(''); }}
                                            style={{ padding: '4px 12px', borderRadius: 8, border: '1px solid rgba(255,255,255,0.1)', background: 'rgba(255,255,255,0.05)', fontSize: 12, cursor: 'pointer', color: '#94a3b8' }}>
                                        Փոխել
                                    </button>
                                )}
                            </div>
                        </div>
                    ) : (
                        <div>
                            <label style={{ fontSize: 11, fontWeight: 700, display: 'block', marginBottom: 6, color: '#475569', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Հիվանդի կոդ</label>
                            <input value={patientCode}
                                   onChange={e => { setPatientCode(e.target.value); setResolvedPatient(null); }}
                                   onBlur={handlePatientCodeBlur}
                                   style={{ width: '100%', padding: '9px 12px', borderRadius: 10, border: '1px solid rgba(255,255,255,0.1)', fontSize: 14, boxSizing: 'border-box' as const, background: 'rgba(255,255,255,0.05)', color: '#e2e8f0', outline: 'none' }}
                                   placeholder="օր.՝ PAT-20260303-001" />
                            {lookingUp && <div style={{ fontSize: 12, color: '#475569', marginTop: 4 }}>Փնտրվում է…</div>}
                        </div>
                    )}

                    <div>
                        <label style={{ fontSize: 11, fontWeight: 700, display: 'block', marginBottom: 6, color: '#475569', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Ախտորոշման տեսակ</label>
                        <select value={diagnosisType} onChange={e => setDiagnosisType(e.target.value)}
                                style={{ width: '100%', padding: '9px 12px', borderRadius: 10, border: '1px solid rgba(255,255,255,0.1)', fontSize: 14, boxSizing: 'border-box' as const, background: 'rgba(255,255,255,0.05)', color: '#e2e8f0', outline: 'none' }}>
                            <option value="brain_tumor">Ուղեղի ուռուցք</option>
                            <option value="lung_cancer">Թոքի քաղցկեղ</option>
                        </select>
                    </div>

                    <div>
                        <label style={{ fontSize: 11, fontWeight: 700, display: 'block', marginBottom: 6, color: '#475569', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Բժշկական նշումներ</label>
                        <textarea value={notes} onChange={e => setNotes(e.target.value)}
                                  style={{ width: '100%', padding: '9px 12px', borderRadius: 10, border: '1px solid rgba(255,255,255,0.1)', fontSize: 14, minHeight: 90, resize: 'vertical', boxSizing: 'border-box' as const, background: 'rgba(255,255,255,0.05)', color: '#e2e8f0', outline: 'none' }}
                                  placeholder="Լրացուցիչ տեղեկություն" />
                    </div>

                    <div style={{ display: 'flex', gap: 10 }}>
                        <button onClick={() => navigate(-1)} style={btnSecondary}>Չեղարկել</button>
                        <button onClick={handleCreateCase} disabled={loading || lookingUp}
                                style={btnPrimary(loading || lookingUp)}>
                            {loading ? 'Ստեղծվում է…' : 'Ստեղծել և շարունակել'}
                        </button>
                    </div>
                </div>
            </div>
        </>
    );
}