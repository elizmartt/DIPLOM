export default function PageLoader() {
    return (
        <div style={{
            minHeight: '100vh', display: 'flex', alignItems: 'center',
            justifyContent: 'center', background: '#f8fafc',
        }}>
            <style>{`
                @keyframes spin { to { transform: rotate(360deg) } }
                @keyframes pulse { 0%, 100% { opacity: 0.4; transform: scale(0.95) } 50% { opacity: 1; transform: scale(1) } }
            `}</style>
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 16 }}>
                <div style={{ position: 'relative', width: 48, height: 48 }}>
                    <div style={{
                        width: 48, height: 48, borderRadius: '50%',
                        border: '3px solid #e0f2fe',
                        borderTopColor: '#0ea5e9',
                        animation: 'spin 0.8s linear infinite',
                    }} />
                    <div style={{
                        position: 'absolute', inset: 0,
                        display: 'flex', alignItems: 'center', justifyContent: 'center',
                    }}>
                        <div style={{
                            width: 16, height: 16, borderRadius: '50%',
                            background: 'rgba(14,165,233,0.2)',
                            animation: 'pulse 1.2s ease-in-out infinite',
                        }} />
                    </div>
                </div>
                <p style={{ fontSize: 13, color: '#94a3b8', fontWeight: 500 }}>Բեռնվում է...</p>
            </div>
        </div>
    );
}