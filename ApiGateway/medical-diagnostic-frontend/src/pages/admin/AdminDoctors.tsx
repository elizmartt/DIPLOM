import { useEffect, useState } from 'react';
import apiClient, { getApiError } from '@/lib/axios';
import { Doctor } from '@/types';

interface CreateDoctorForm {
    email: string;
    password: string;
    full_name: string;
    specialization: string;
    hospital_affiliation: string;
}

const EMPTY_FORM: CreateDoctorForm = {
    email: '',
    password: '',
    full_name: '',
    specialization: '',
    hospital_affiliation: '',
};

export default function AdminDoctors() {
    const [doctors, setDoctors]         = useState<Doctor[]>([]);
    const [loading, setLoading]         = useState(true);
    const [showForm, setShowForm]       = useState(false);
    const [form, setForm]               = useState<CreateDoctorForm>(EMPTY_FORM);
    const [submitting, setSubmitting]   = useState(false);
    const [formError, setFormError]     = useState('');
    const [formSuccess, setFormSuccess] = useState('');
    const [listError, setListError]     = useState('');

    const loadDoctors = () => {
        setLoading(true);
        apiClient.get('/Auth/doctors')
            .then(r => {
                const data = r.data?.data ?? r.data;
                setDoctors(Array.isArray(data) ? data : []);
            })
            .catch(() => setListError('Բժիշկների ցուցակը բեռնելը ձախողվեց'))
            .finally(() => setLoading(false));
    };

    useEffect(() => { loadDoctors(); }, []);

    const handleSubmit = async () => {
        setFormError('');
        setFormSuccess('');
        if (!form.email || !form.password || !form.full_name) {
            setFormError('Լրացրեք բոլոր պարտադիր դաշտերը');
            return;
        }
        if (form.password.length < 8) {
            setFormError('Գաղտնաբառը պետք է լինի առնվազն 8 նիշ');
            return;
        }
        setSubmitting(true);
        try {
            await apiClient.post('/Auth/register-doctor', {
                email:               form.email,
                password:            form.password,
                fullName:            form.full_name,
                specialization:      form.specialization || 'General',
                hospitalAffiliation: form.hospital_affiliation || '',
            });
            setFormSuccess(`Բժիշկ «${form.full_name}» հաջողությամբ ստեղծվեց`);
            setForm(EMPTY_FORM);
            setShowForm(false);
            loadDoctors();
        } catch (err) {
            setFormError(getApiError(err).message);
        } finally {
            setSubmitting(false);
        }
    };

    const field = (label: string, key: keyof CreateDoctorForm, type = 'text', required = false) => (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            <label style={{ fontSize: 13, fontWeight: 600, color: '#374151' }}>
                {label}{required && <span style={{ color: '#ef4444' }}> *</span>}
            </label>
            <input
                type={type}
                value={form[key]}
                onChange={e => setForm(f => ({ ...f, [key]: e.target.value }))}
                style={{ padding: '9px 12px', borderRadius: 8, fontSize: 14, border: '1px solid #e2e8f0', outline: 'none', background: '#fff', color: '#0f172a' }}
                onFocus={e => e.currentTarget.style.borderColor = '#3b82f6'}
                onBlur={e => e.currentTarget.style.borderColor = '#e2e8f0'}
            />
        </div>
    );

    return (
        <div>
            {/* Header */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 28 }}>
                <div>
                    <h1 style={{ fontSize: 26, fontWeight: 800, color: '#0f172a', margin: 0 }}>Բժիշկներ</h1>
                    <p style={{ fontSize: 14, color: '#64748b', margin: '6px 0 0' }}>Համակարգի բոլոր բժիշկների կառավարում</p>
                </div>
                <button
                    onClick={() => { setShowForm(f => !f); setFormError(''); setFormSuccess(''); }}
                    style={{ padding: '10px 20px', borderRadius: 10, fontSize: 14, fontWeight: 700, background: '#3b82f6', color: '#fff', border: 'none', cursor: 'pointer' }}
                >
                    {showForm ? 'Չեղարկել' : '+ Ավելացնել բժիշկ'}
                </button>
            </div>

            {/* Success banner */}
            {formSuccess && (
                <div style={{ background: '#f0fdf4', border: '1px solid #bbf7d0', borderRadius: 10, padding: '12px 18px', color: '#16a34a', fontSize: 14, marginBottom: 20 }}>
                    ✓ {formSuccess}
                </div>
            )}

            {/* Create doctor form */}
            {showForm && (
                <div style={{ background: '#fff', borderRadius: 14, padding: '28px', boxShadow: '0 1px 4px rgba(0,0,0,0.08)', marginBottom: 28, border: '1px solid #e2e8f0' }}>
                    <h2 style={{ fontSize: 17, fontWeight: 700, color: '#0f172a', margin: '0 0 22px' }}>Նոր բժիշկի ստեղծում</h2>
                    {formError && (
                        <div style={{ background: '#fef2f2', border: '1px solid #fecaca', borderRadius: 8, padding: '10px 14px', color: '#dc2626', fontSize: 13, marginBottom: 18 }}>
                            {formError}
                        </div>
                    )}
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
                        {field('Անուն Ազգանուն', 'full_name', 'text', true)}
                        {field('Էլ. հասցե', 'email', 'email', true)}
                        {field('Գաղտնաբառ', 'password', 'password', true)}
                        {field('Մասնագիտացում', 'specialization')}
                        {field('Հիվանդանոց', 'hospital_affiliation')}
                    </div>
                    <div style={{ display: 'flex', gap: 12, marginTop: 24 }}>
                        <button
                            onClick={handleSubmit}
                            disabled={submitting}
                            style={{ padding: '10px 24px', borderRadius: 10, fontSize: 14, fontWeight: 700, background: submitting ? '#93c5fd' : '#3b82f6', color: '#fff', border: 'none', cursor: submitting ? 'not-allowed' : 'pointer' }}
                        >
                            {submitting ? 'Ստեղծվում է...' : 'Ստեղծել բժիշկ'}
                        </button>
                        <button
                            onClick={() => { setShowForm(false); setFormError(''); }}
                            style={{ padding: '10px 20px', borderRadius: 10, fontSize: 14, fontWeight: 600, background: '#f1f5f9', color: '#374151', border: 'none', cursor: 'pointer' }}
                        >
                            Չեղարկել
                        </button>
                    </div>
                </div>
            )}

            {/* Doctors table */}
            <div style={{ background: '#fff', borderRadius: 14, boxShadow: '0 1px 4px rgba(0,0,0,0.06)', overflow: 'hidden' }}>
                {listError && (
                    <div style={{ padding: '16px 24px', color: '#dc2626', fontSize: 14 }}>{listError}</div>
                )}
                {loading ? (
                    <div style={{ padding: 32 }}>
                        {Array.from({ length: 4 }).map((_, i) => (
                            <div key={i} style={{ height: 52, background: '#f1f5f9', borderRadius: 8, marginBottom: 12, animation: 'pulse 1.5s infinite' }} />
                        ))}
                    </div>
                ) : (
                    <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                        <thead>
                        <tr style={{ background: '#f8fafc', borderBottom: '1px solid #e2e8f0' }}>
                            {['Անուն', 'Էլ. հասցե', 'Մասնագիտ.', 'Հիվ.', 'Կարգ.', 'Ակտ.'].map(h => (
                                <th key={h} style={{ padding: '12px 16px', fontSize: 12, fontWeight: 700, color: '#64748b', textAlign: 'left', textTransform: 'uppercase', letterSpacing: 0.5 }}>{h}</th>
                            ))}
                        </tr>
                        </thead>
                        <tbody>
                        {doctors.length === 0 ? (
                            <tr>
                                <td colSpan={6} style={{ padding: '40px 16px', textAlign: 'center', color: '#94a3b8', fontSize: 14 }}>
                                    Բժիշկներ չեն գտնվել
                                </td>
                            </tr>
                        ) : doctors.map((doc, idx) => (
                            <tr key={doc.doctor_id} style={{ borderBottom: '1px solid #f1f5f9', background: idx % 2 === 0 ? '#fff' : '#fafafa' }}>
                                <td style={{ padding: '14px 16px' }}>
                                    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                                        <div style={{ width: 32, height: 32, borderRadius: '50%', background: 'linear-gradient(135deg,#0ea5e9,#6366f1)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                                                <span style={{ color: '#fff', fontSize: 11, fontWeight: 800 }}>
                                                    {doc.full_name?.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase()}
                                                </span>
                                        </div>
                                        <span style={{ fontSize: 14, fontWeight: 600, color: '#0f172a' }}>{doc.full_name}</span>
                                    </div>
                                </td>
                                <td style={{ padding: '14px 16px', fontSize: 13, color: '#374151' }}>{doc.email}</td>
                                <td style={{ padding: '14px 16px', fontSize: 13, color: '#374151' }}>{doc.specialization || '—'}</td>
                                <td style={{ padding: '14px 16px', fontSize: 13, color: '#374151' }}>{doc.hospital_affiliation || '—'}</td>
                                <td style={{ padding: '14px 16px' }}>
                                        <span style={{ padding: '3px 10px', borderRadius: 20, fontSize: 11, fontWeight: 700, background: doc.role === 'Admin' ? 'rgba(239,68,68,0.1)' : 'rgba(14,165,233,0.1)', color: doc.role === 'Admin' ? '#dc2626' : '#0369a1' }}>
                                            {doc.role === 'Admin' ? 'Ադմին' : 'Բժիշկ'}
                                        </span>
                                </td>
                                <td style={{ padding: '14px 16px' }}>
                                        <span style={{ padding: '3px 10px', borderRadius: 20, fontSize: 11, fontWeight: 700, background: doc.is_active ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)', color: doc.is_active ? '#059669' : '#dc2626' }}>
                                            {doc.is_active ? 'Ակտ.' : 'Անակտ.'}
                                        </span>
                                </td>
                            </tr>
                        ))}
                        </tbody>
                    </table>
                )}
            </div>
            <style>{`@keyframes pulse { 0%,100%{opacity:1} 50%{opacity:0.5} }`}</style>
        </div>
    );
}