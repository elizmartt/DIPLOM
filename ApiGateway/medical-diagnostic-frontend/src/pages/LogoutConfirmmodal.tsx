
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/store/auth';
import { authApi } from '@/api/auth';

interface LogoutConfirmModalProps {
    onCancel: () => void;
}

export default function LogoutConfirmModal({ onCancel }: LogoutConfirmModalProps) {
    const navigate = useNavigate();
    const { logout } = useAuthStore();

    const handleLogout = async () => {
        try { await authApi.logout(); } catch { /* ignore */ }
        logout();
        navigate('/login');
    };

    return (
        <>
            <style>{`
                @keyframes backdropIn{from{opacity:0}to{opacity:1}}
                @keyframes modalIn{from{opacity:0;transform:scale(0.95) translateY(8px)}to{opacity:1;transform:scale(1) translateY(0)}}
                .logout-backdrop{position:fixed;inset:0;background:rgba(15,23,42,0.5);backdrop-filter:blur(4px);z-index:1000;display:flex;align-items:center;justify-content:center;animation:backdropIn .15s ease}
                .logout-modal{background:#fff;border-radius:20px;padding:36px;width:100%;max-width:400px;box-shadow:0 24px 64px rgba(0,0,0,0.18);animation:modalIn .2s ease}
            `}</style>
            <div className="logout-backdrop" onClick={onCancel}>
                <div className="logout-modal" onClick={e => e.stopPropagation()}>
                    <div style={{ width:56,height:56,borderRadius:'50%',background:'#fef2f2',display:'flex',alignItems:'center',justifyContent:'center',margin:'0 auto 20px',fontSize:24 }}>🚪</div>
                    <h2 style={{ fontSize:20,fontWeight:800,color:'#0f172a',textAlign:'center',marginBottom:8 }}>Դուրս գա՞լ համակարգից</h2>
                    <p style={{ fontSize:14,color:'#64748b',textAlign:'center',lineHeight:1.6,marginBottom:28 }}>
                        Դուրս գալով՝ կբացվեք համակարգից և կպահանջվի նախ մուտք գործել։
                    </p>
                    <div style={{ display:'flex',gap:10 }}>
                        <button onClick={onCancel}
                                style={{ flex:1,padding:'12px',borderRadius:12,border:'1px solid #e2e8f0',background:'#fff',color:'#374151',fontSize:14,fontWeight:600,cursor:'pointer' }}>
                            Մնալ
                        </button>
                        <button onClick={handleLogout}
                                style={{ flex:1,padding:'12px',borderRadius:12,border:'none',background:'#ef4444',color:'#fff',fontSize:14,fontWeight:700,cursor:'pointer' }}>
                            Այո, դուրս գալ
                        </button>
                    </div>
                </div>
            </div>
        </>
    );
}