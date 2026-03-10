import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { patientsApi } from '@/api/patients';

const surface  = '#1e293b';
const surf2    = '#273348';
const border   = '#2d3f55';
const textCol  = '#f1f5f9';
const muted    = '#64748b';
const faint    = '#94a3b8';
const accent   = '#0ea5e9';

const GENDER_OPTIONS = [
    { value: 'male',   label: 'Արական', icon: '♂' },
    { value: 'female', label: 'Իգական', icon: '♀' },
    { value: 'other',  label: 'Այլ',    icon: '⚬' },
];

function todayCodePreview() {
    const d = new Date();
    return `PAT-${d.getFullYear()}${String(d.getMonth()+1).padStart(2,'0')}${String(d.getDate()).padStart(2,'0')}-???`;
}

function FormSection({ title, children, delay = 0 }: { title: string; children: React.ReactNode; delay?: number }) {
    return (
        <div style={{ background: surface, borderRadius: 16, border: `1px solid ${border}`, overflow: 'hidden', animation: 'fadeUp .35s ease both', animationDelay: `${delay}ms` }}>
            <div style={{ padding: '13px 24px', borderBottom: `1px solid ${border}`, fontSize: 11, fontWeight: 700, color: muted, letterSpacing: 1, textTransform: 'uppercase' as const }}>
                {title}
            </div>
            <div style={{ padding: '20px 24px' }}>{children}</div>
        </div>
    );
}

export default function CreatePatient() {
    const navigate = useNavigate();
    const [firstName, setFirstName] = useState('');
    const [lastName,  setLastName]  = useState('');
    const [age,       setAge]       = useState('');
    const [gender,    setGender]    = useState('male');
    const [submitting, setSubmitting] = useState(false);
    const [error, setError]           = useState('');
    const [success, setSuccess]       = useState<{ patientCode: string; patientId: string } | null>(null);
    const [focusedField, setFocusedField] = useState<string | null>(null);

    const valid = !!(firstName.trim() && lastName.trim() && age && Number(age) > 0 && Number(age) < 130);

    const handleSubmit = async () => {
        if (!valid) { setError('Լրացրեք բոլոր պարտադիր դաշտերը'); return; }
        setSubmitting(true); setError('');
        try {
            const created = await patientsApi.create({ firstName: firstName.trim(), lastName: lastName.trim(), age: Number(age), gender });
            setSuccess({ patientCode: created.patientCode, patientId: created.patientId });
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : 'Սխալ');
        } finally { setSubmitting(false); }
    };

    const inputStyle = (id: string) => ({
        width: '100%', padding: '10px 14px', borderRadius: 10,
        border: `1.5px solid ${focusedField === id ? accent : border}`,
        background: surf2, color: textCol, fontSize: 14, outline: 'none',
        boxSizing: 'border-box' as const, transition: 'border-color 0.15s',
    });

    /* Success */
    if (success) {
        return (
            <>
                <style>{`
                    @keyframes fadeUp { from{opacity:0;transform:translateY(16px)} to{opacity:1;transform:translateY(0)} }
                    @keyframes popIn  { 0%{transform:scale(.7);opacity:0} 60%{transform:scale(1.08)} 100%{transform:scale(1);opacity:1} }
                `}</style>
                <div className="page-container" style={{ maxWidth: 540, paddingBottom: 60 }}>
                    <div style={{ background: surface, borderRadius: 20, padding: '48px 40px', border: `1px solid ${border}`, textAlign: 'center', animation: 'fadeUp .4s ease' }}>
                        <div style={{
                            width: 72, height: 72, borderRadius: '50%',
                            background: 'linear-gradient(135deg,#22c55e,#16a34a)',
                            display: 'flex', alignItems: 'center', justifyContent: 'center',
                            margin: '0 auto 20px', fontSize: 30, color: '#fff',
                            animation: 'popIn .5s cubic-bezier(.34,1.56,.64,1) both', animationDelay: '100ms',
                        }}>✓</div>
                        <h2 style={{ fontSize: 22, fontWeight: 800, color: textCol, marginBottom: 8 }}>Հիվանդը ստեղծված է</h2>
                        <p style={{ color: muted, fontSize: 14, marginBottom: 28 }}>Նոր հիվանդի քարտը հաջողությամբ ստեղծվեց</p>
                        <div style={{ background: surf2, border: `1.5px solid ${border}`, borderRadius: 12, padding: '14px 24px', marginBottom: 32, fontFamily: 'monospace', fontSize: 22, fontWeight: 800, color: accent, letterSpacing: 1 }}>
                            {success.patientCode}
                        </div>
                        <div style={{ display: 'flex', gap: 12, justifyContent: 'center' }}>
                            <button onClick={() => navigate(`/patients/${success.patientId}`)} style={{
                                padding: '11px 24px', borderRadius: 12, border: 'none',
                                background: accent, color: '#fff', fontSize: 14, fontWeight: 700, cursor: 'pointer',
                                boxShadow: '0 4px 12px rgba(14,165,233,0.25)',
                            }}>
                                Դիտել հիվանդին →
                            </button>
                            <button onClick={() => { setSuccess(null); setFirstName(''); setLastName(''); setAge(''); setGender('male'); }} style={{
                                padding: '11px 24px', borderRadius: 12, border: `1.5px solid ${border}`,
                                background: 'transparent', color: faint, fontSize: 14, fontWeight: 600, cursor: 'pointer',
                            }}>
                                + Նոր հիվանդ
                            </button>
                        </div>
                    </div>
                </div>
            </>
        );
    }

    return (
        <>
            <style>{`
                @keyframes fadeUp { from{opacity:0;transform:translateY(12px)} to{opacity:1;transform:translateY(0)} }
                @keyframes spin   { to{transform:rotate(360deg)} }
            `}</style>

            <div className="page-container" style={{ maxWidth: 600, paddingBottom: 60 }}>

                {/* Header */}
                <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginBottom: 28 }}>
                    <button onClick={() => navigate('/patients')} style={{
                        padding: '7px 14px', borderRadius: 10, border: `1.5px solid ${border}`,
                        background: 'transparent', color: faint, fontSize: 13, fontWeight: 600, cursor: 'pointer',
                    }}>
                        ← Հետ
                    </button>
                    <div>
                        <h1 style={{ fontSize: 24, fontWeight: 800, color: textCol }}>Նոր հիվանդ</h1>
                        <p style={{ fontSize: 13, color: muted, marginTop: 2 }}>Հիվանդի տվյալների ստեղծում</p>
                    </div>
                </div>

                {error && (
                    <div style={{ background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.3)', borderRadius: 12, padding: '12px 16px', color: '#f87171', fontSize: 14, marginBottom: 20 }}>
                        {error}
                    </div>
                )}

                <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>

                    {/* Code preview */}
                    <FormSection title="Հիվանդի կոդ" delay={0}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                            <div style={{ flex: 1, padding: '10px 16px', borderRadius: 10, border: `1.5px dashed ${border}`, background: surf2, fontFamily: 'monospace', fontSize: 15, fontWeight: 800, color: muted, letterSpacing: 1 }}>
                                {todayCodePreview()}
                            </div>
                            <span style={{ padding: '5px 12px', borderRadius: 99, background: 'rgba(14,165,233,0.1)', color: accent, fontSize: 12, fontWeight: 700, whiteSpace: 'nowrap' }}>
                                ⚙ Ավտոմատ
                            </span>
                        </div>
                        <p style={{ fontSize: 11, color: muted, marginTop: 8, marginBottom: 0 }}>
                            Կոդը կստեղծվի ավտոմատ՝ PAT-YYYYMMDD-NNN ձևաչափով
                        </p>
                    </FormSection>

                    {/* Name */}
                    <FormSection title="Անձնական տվյալներ" delay={60}>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
                            <div>
                                <label style={{ fontSize: 12, fontWeight: 600, color: faint, marginBottom: 6, display: 'block' }}>Անուն *</label>
                                <input
                                    style={inputStyle('firstName')}
                                    placeholder="Արամ, Աննա..."
                                    value={firstName}
                                    onChange={e => setFirstName(e.target.value)}
                                    onFocus={() => setFocusedField('firstName')}
                                    onBlur={() => setFocusedField(null)}
                                />
                            </div>
                            <div>
                                <label style={{ fontSize: 12, fontWeight: 600, color: faint, marginBottom: 6, display: 'block' }}>Ազգանուն *</label>
                                <input
                                    style={inputStyle('lastName')}
                                    placeholder="Պետրոսյան..."
                                    value={lastName}
                                    onChange={e => setLastName(e.target.value)}
                                    onFocus={() => setFocusedField('lastName')}
                                    onBlur={() => setFocusedField(null)}
                                />
                            </div>
                        </div>
                    </FormSection>

                    {/* Age */}
                    <FormSection title="Տարիք" delay={120}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
                            <input
                                type="number" min="0" max="130" placeholder="25"
                                style={{ ...inputStyle('age'), maxWidth: 140 }}
                                value={age}
                                onChange={e => setAge(e.target.value)}
                                onFocus={() => setFocusedField('age')}
                                onBlur={() => setFocusedField(null)}
                            />
                            <span style={{ fontSize: 14, color: muted }}>տարի</span>
                            {age && Number(age) > 0 && (
                                <span style={{ padding: '5px 12px', borderRadius: 99, background: 'rgba(14,165,233,0.1)', color: accent, fontSize: 13, fontWeight: 700 }}>
                                    {Number(age)} տ.
                                </span>
                            )}
                        </div>
                    </FormSection>

                    {/* Gender */}
                    <FormSection title="Սեռ" delay={180}>
                        <div style={{ display: 'flex', gap: 10 }}>
                            {GENDER_OPTIONS.map(g => (
                                <div key={g.value} onClick={() => setGender(g.value)} style={{
                                    border: `1.5px solid ${gender === g.value ? accent : border}`,
                                    borderRadius: 12, padding: '14px 16px', cursor: 'pointer',
                                    display: 'flex', alignItems: 'center', gap: 10, flex: 1,
                                    background: gender === g.value ? 'rgba(14,165,233,0.08)' : 'transparent',
                                    transition: 'all 0.15s',
                                }}>
                                    <span style={{ fontSize: 18 }}>{g.icon}</span>
                                    <span style={{ fontSize: 14, fontWeight: 600, color: gender === g.value ? accent : faint }}>{g.label}</span>
                                </div>
                            ))}
                        </div>
                    </FormSection>

                    {/* Live preview */}
                    {(firstName || lastName) && (
                        <div style={{ background: 'rgba(14,165,233,0.06)', borderRadius: 14, padding: '16px 20px', border: '1px solid rgba(14,165,233,0.2)', animation: 'fadeUp .2s ease' }}>
                            <div style={{ fontSize: 11, fontWeight: 700, color: accent, letterSpacing: 1, marginBottom: 12, textTransform: 'uppercase' }}>Նախադիտում</div>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                                <div style={{ width: 44, height: 44, borderRadius: '50%', background: 'linear-gradient(135deg,#0ea5e9,#6366f1)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                                    <span style={{ color: '#fff', fontSize: 16, fontWeight: 800 }}>
                                        {(firstName[0] ?? '?').toUpperCase()}{(lastName[0] ?? '').toUpperCase()}
                                    </span>
                                </div>
                                <div>
                                    <div style={{ fontSize: 15, fontWeight: 700, color: textCol }}>{firstName} {lastName}</div>
                                    <div style={{ fontSize: 12, color: muted, marginTop: 2 }}>
                                        {age ? `${age} տ.` : '—'} · {GENDER_OPTIONS.find(g => g.value === gender)?.label}
                                    </div>
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Submit */}
                    <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end', paddingTop: 4 }}>
                        <button onClick={() => navigate('/patients')} style={{
                            padding: '11px 24px', borderRadius: 12, border: `1.5px solid ${border}`,
                            background: 'transparent', color: faint, fontSize: 14, fontWeight: 600, cursor: 'pointer',
                        }}>
                            Չեղարկել
                        </button>
                        <button onClick={handleSubmit} disabled={submitting || !valid} style={{
                            padding: '11px 32px', borderRadius: 12, border: 'none',
                            background: !valid || submitting ? 'rgba(14,165,233,0.3)' : accent,
                            color: '#fff', fontSize: 14, fontWeight: 700,
                            cursor: !valid || submitting ? 'not-allowed' : 'pointer',
                            display: 'flex', alignItems: 'center', gap: 8,
                            boxShadow: valid && !submitting ? '0 4px 12px rgba(14,165,233,0.25)' : 'none',
                            transition: 'all 0.15s',
                        }}>
                            {submitting && (
                                <div style={{ width: 14, height: 14, borderRadius: '50%', border: '2px solid rgba(255,255,255,0.4)', borderTopColor: '#fff', animation: 'spin .8s linear infinite' }} />
                            )}
                            {submitting ? 'Ստեղծվում է...' : 'Ստեղծել հիվանդ'}
                        </button>
                    </div>
                </div>
            </div>
        </>
    );
}