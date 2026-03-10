import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import apiClient from '@/lib/axios';
import { useAuthStore } from '@/store/auth';

export default function ChangePassword() {
    const navigate  = useNavigate();
    const { doctor, setAuth, accessToken, refreshToken } = useAuthStore();
    const [password, setPassword]       = useState('');
    const [confirm, setConfirm]         = useState('');
    const [error, setError]             = useState('');
    const [loading, setLoading]         = useState(false);

    const handleSubmit = async () => {
        setError('');
        if (password.length < 8) {
            setError('Գաղտնաբառը պետք է լինի առնվազն 8 նիշ');
            return;
        }
        if (password !== confirm) {
            setError('Գաղտնաբառերը չեն համընկնում');
            return;
        }
        setLoading(true);
        try {
            await apiClient.post('/Auth/change-password', { newPassword: password });

            
            if (doctor) {
                setAuth(
                    { ...doctor, must_change_password: false },
                    accessToken!,
                    refreshToken ?? ''
                );
            }

            navigate('/dashboard', { replace: true });
        } catch {
            setError('Գաղտնաբառի փոփոխությունը ձախողվեց');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={{
            minHeight: '100vh', background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 100%)',
            display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 24,
        }}>
            <div style={{
                background: '#fff', borderRadius: 20, padding: '48px 40px',
                width: '100%', maxWidth: 440, boxShadow: '0 25px 50px rgba(0,0,0,0.3)',
            }}>
                {/* Header */}
                <div style={{ textAlign: 'center', marginBottom: 32 }}>
                    <div style={{
                        width: 56, height: 56, borderRadius: '50%',
                        background: 'linear-gradient(135deg, #f59e0b, #ef4444)',
                        display: 'flex', alignItems: 'center', justifyContent: 'center',
                        margin: '0 auto 16px', fontSize: 24,
                    }}>
                        
                    </div>
                    <h1 style={{ fontSize: 22, fontWeight: 800, color: '#0f172a', margin: '0 0 8px' }}>
                        Փոխեք գաղտնաբառը
                    </h1>
                    <p style={{ fontSize: 14, color: '#64748b', margin: 0 }}>
                        Անվտանգության համար սահմանեք նոր գաղտնաբառ
                    </p>
                    {doctor?.full_name && (
                        <p style={{ fontSize: 13, color: '#94a3b8', margin: '8px 0 0' }}>
                            {doctor.full_name}
                        </p>
                    )}
                </div>

                {error && (
                    <div style={{
                        background: '#fef2f2', border: '1px solid #fecaca',
                        borderRadius: 10, padding: '12px 16px',
                        color: '#dc2626', fontSize: 13, marginBottom: 20,
                    }}>
                        {error}
                    </div>
                )}

                <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                        <label style={{ fontSize: 13, fontWeight: 600, color: '#374151' }}>
                            Նոր գաղտնաբառ <span style={{ color: '#ef4444' }}>*</span>
                        </label>
                        <input
                            type="password"
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                            placeholder="Առնվազն 8 նիշ"
                            style={{
                                padding: '11px 14px', borderRadius: 10, fontSize: 14,
                                border: '1px solid #e2e8f0', outline: 'none', background: '#f8fafc',
                            }}
                            onFocus={e => e.currentTarget.style.borderColor = '#3b82f6'}
                            onBlur={e => e.currentTarget.style.borderColor = '#e2e8f0'}
                        />
                    </div>

                    <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                        <label style={{ fontSize: 13, fontWeight: 600, color: '#374151' }}>
                            Հաստատեք գաղտնաբառը <span style={{ color: '#ef4444' }}>*</span>
                        </label>
                        <input
                            type="password"
                            value={confirm}
                            onChange={e => setConfirm(e.target.value)}
                            placeholder="Կրկնեք գաղտնաբառը"
                            onKeyDown={e => e.key === 'Enter' && handleSubmit()}
                            style={{
                                padding: '11px 14px', borderRadius: 10, fontSize: 14,
                                border: '1px solid #e2e8f0', outline: 'none', background: '#f8fafc',
                            }}
                            onFocus={e => e.currentTarget.style.borderColor = '#3b82f6'}
                            onBlur={e => e.currentTarget.style.borderColor = '#e2e8f0'}
                        />
                    </div>

                    <button
                        onClick={handleSubmit}
                        disabled={loading}
                        style={{
                            marginTop: 8, padding: '13px', borderRadius: 12, fontSize: 15,
                            fontWeight: 700, background: loading ? '#93c5fd' : '#3b82f6',
                            color: '#fff', border: 'none', cursor: loading ? 'not-allowed' : 'pointer',
                            transition: 'all 0.2s',
                        }}
                    >
                        {loading ? 'Պահպանվում է...' : 'Պահպանել գաղտնաբառը'}
                    </button>
                </div>
            </div>
        </div>
    );
}