import { useState } from 'react';
import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/store/auth';
import LogoutConfirmModal from '@/pages/LogoutConfirmmodal';

const NAV_ITEMS = [
    { to: '/admin',         label: 'Վահանակ',  end: true },
    { to: '/admin/doctors', label: 'Բժիշկներ'           },
    { to: '/admin/audit',   label: 'Աուդիտ'             },
];

export default function AdminLayout() {
    const { doctor } = useAuthStore();
    const navigate   = useNavigate();
    const [showLogout, setShowLogout] = useState(false);

    const initials = doctor?.full_name
        ? doctor.full_name.split(' ').map((w: string) => w[0]).join('').slice(0, 2).toUpperCase()
        : 'AD';

    return (
        <>
            <div style={{ display: 'flex', minHeight: '100vh', background: '#f8fafc' }}>

                {/* ── Sidebar ─────────────────────────────────────────────── */}
                <aside style={{
                    width: 220, background: '#0f172a',
                    display: 'flex', flexDirection: 'column',
                    padding: '24px 0', flexShrink: 0,
                    position: 'sticky', top: 0, height: '100vh',
                    overflowY: 'auto',
                }}>
                    {/* Brand */}
                    <div style={{ padding: '0 20px 24px', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
                        <div style={{ fontSize: 14, fontWeight: 800, color: '#fff', letterSpacing: 0.5 }}>
                            🏥 ԲԱԿ
                        </div>
                        <div style={{ fontSize: 11, color: 'rgba(255,255,255,0.35)', marginTop: 3 }}>
                            Բժշկական Ախտորոշման Կենտրոն
                        </div>
                        {/* Admin badge */}
                        <div style={{
                            marginTop: 10, display: 'inline-flex', alignItems: 'center', gap: 5,
                            background: 'rgba(239,68,68,0.15)', border: '1px solid rgba(239,68,68,0.25)',
                            borderRadius: 20, padding: '3px 9px',
                        }}>
                            <span style={{ width: 5, height: 5, borderRadius: '50%', background: '#ef4444', display: 'inline-block' }} />
                            <span style={{ fontSize: 10, color: '#fca5a5', fontWeight: 700, letterSpacing: 0.5 }}>ԱԴՄԻՆ</span>
                        </div>
                    </div>

                    {/* Nav */}
                    <nav style={{ flex: 1, padding: '16px 12px', display: 'flex', flexDirection: 'column', gap: 2 }}>
                        {NAV_ITEMS.map(item => (
                            <NavLink
                                key={item.to}
                                to={item.to}
                                end={item.end}
                                style={({ isActive }) => ({
                                    display: 'flex', alignItems: 'center', gap: 10,
                                    padding: '9px 12px', borderRadius: 10,
                                    textDecoration: 'none', fontSize: 13, fontWeight: 600,
                                    transition: 'all 0.15s',
                                    background: isActive ? 'rgba(239,68,68,0.15)' : 'transparent',
                                    color: isActive ? '#fca5a5' : 'rgba(255,255,255,0.55)',
                                })}
                            >
                                {item.label}
                            </NavLink>
                        ))}
                    </nav>

                    {/* User + Logout */}
                    <div style={{ padding: '16px 12px', borderTop: '1px solid rgba(255,255,255,0.06)', display: 'flex', flexDirection: 'column', gap: 4 }}>
                        <button
                            onClick={() => navigate('/profile')}
                            style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '9px 12px', borderRadius: 10, background: 'none', border: 'none', cursor: 'pointer', width: '100%', textAlign: 'left' }}
                        >
                            <div style={{ width: 28, height: 28, borderRadius: '50%', background: 'linear-gradient(135deg,#ef4444,#dc2626)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                                <span style={{ color: '#fff', fontSize: 11, fontWeight: 800 }}>{initials}</span>
                            </div>
                            <div style={{ overflow: 'hidden' }}>
                                <div style={{ fontSize: 12, fontWeight: 700, color: '#fff', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                    {doctor?.full_name ?? 'Ադմինիստրատոր'}
                                </div>
                                <div style={{ fontSize: 10, color: 'rgba(255,255,255,0.35)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                    {doctor?.email ?? ''}
                                </div>
                            </div>
                        </button>

                        <button
                            onClick={() => setShowLogout(true)}
                            style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '9px 12px', borderRadius: 10, background: 'none', border: 'none', cursor: 'pointer', color: 'rgba(255,255,255,0.4)', fontSize: 13, fontWeight: 600, width: '100%', textAlign: 'left', transition: 'all 0.15s' }}
                            onMouseEnter={e => { e.currentTarget.style.background = 'rgba(239,68,68,0.1)'; e.currentTarget.style.color = '#fca5a5'; }}
                            onMouseLeave={e => { e.currentTarget.style.background = 'none'; e.currentTarget.style.color = 'rgba(255,255,255,0.4)'; }}
                        >
                            Դուրս գալ
                        </button>
                    </div>
                </aside>

                {/* ── Main content — Outlet renders the child page here ────── */}
                <main style={{ flex: 1, overflowY: 'auto', padding: '32px 36px' }}>
                    <Outlet />
                </main>

            </div>

            {showLogout && <LogoutConfirmModal onCancel={() => setShowLogout(false)} />}
        </>
    );
}