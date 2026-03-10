import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useAuthStore } from '@/store/auth';
import { authApi } from '@/api/auth';
import { getApiError } from '@/lib/axios';

const loginSchema = z.object({
    email: z.string().email('Մուտքագրեք վավեր էլ. հասցե'),
    password: z.string().min(6, 'Գաղտնաբառը պետք է լինի առնվազն 6 նիշ'),
});

type LoginForm = z.infer<typeof loginSchema>;

export default function Login() {
    const navigate = useNavigate();
    const setAuth = useAuthStore((s) => s.setAuth);
    const [serverError, setServerError] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const { register, handleSubmit, formState: { errors } } = useForm<LoginForm>({
        resolver: zodResolver(loginSchema),
    });

    const onSubmit = async (data: LoginForm) => {
        setIsLoading(true);
        setServerError('');
        try {
            const response = await authApi.login(data);
            console.log('Full response:', response);

            const { token, email, fullName, role, userId, mustChangePassword } = response.data;

            setAuth(
                {
                    doctor_id: userId,
                    email,
                    full_name: fullName,
                    role,
                    is_active: true,
                    specialization: '',
                    hospital_affiliation: '',
                    created_at: '',
                    updated_at: '',
                    must_change_password: mustChangePassword ?? false,
                },
                token,
                ''
            );

            if (mustChangePassword) {
                navigate('/change-password');
            } else {
                navigate(role === 'Admin' ? '/admin' : '/dashboard');
            }
            console.log('navigate called');
        } catch (err) {
            console.error('Login error:', err);
            const apiErr = getApiError(err);
            setServerError(apiErr.message || 'Սխալ էլ. հասցե կամ գաղտնաբառ');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="min-h-screen bg-slate-950 flex overflow-hidden">

            {/* LEFT PANEL */}
            <div className="hidden lg:flex lg:w-[52%] relative flex-col justify-center p-14 overflow-hidden">
                <div className="absolute inset-0" style={{
                    backgroundImage: `linear-gradient(rgba(14,165,233,0.06) 1px, transparent 1px), linear-gradient(90deg, rgba(14,165,233,0.06) 1px, transparent 1px)`,
                    backgroundSize: '48px 48px',
                }} />
                <div className="absolute top-[-80px] left-[-80px] w-[420px] h-[420px] rounded-full bg-sky-500/10 blur-[100px]" />
                <div className="absolute bottom-[-60px] right-[-60px] w-[320px] h-[320px] rounded-full bg-blue-600/10 blur-[80px]" />

                {/* Headline */}
                <div className="relative z-10">
                    <h1 className="font-display text-5xl font-bold text-white leading-[1.15] mb-6">
                        Բազմամոդալ<br />
                        <span className="text-transparent bg-clip-text bg-gradient-to-r from-sky-400 to-blue-500">
                            Բժշկական Ախտորոշման
                        </span><br />
                        Օգնական
                    </h1>
                    <p className="text-slate-400 text-lg leading-relaxed max-w-md">
                        Պատկերի վերլուծության, կլինիկական ախտանշանների և լաբորատոր արդյունքների համադրումը՝ ախտորոշման բարձր ճշգրտության համար։
                    </p>
                </div>
            </div>

            {/* RIGHT PANEL - Form */}
            <div className="flex-1 flex items-center justify-center p-8 relative">
                <div className="absolute inset-0 bg-slate-900/60" />
                <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[500px] h-[500px] rounded-full bg-sky-600/5 blur-[120px]" />

                <div className="relative z-10 w-full max-w-[420px] animate-fade-in">

                    {/* Mobile logo */}
                    <div className="lg:hidden flex items-center gap-3 mb-10">
                        <div className="w-9 h-9 rounded-xl bg-sky-500 flex items-center justify-center">
                            <svg viewBox="0 0 24 24" fill="none" className="w-4 h-4 text-white" stroke="currentColor" strokeWidth={2.5}>
                                <path strokeLinecap="round" strokeLinejoin="round" d="M9 3H5a2 2 0 00-2 2v4m6-6h10a2 2 0 012 2v4M9 3v18m0 0h10a2 2 0 002-2V9M9 21H5a2 2 0 01-2-2V9m0 0h18" />
                            </svg>
                        </div>
                    </div>

                    <div className="mb-8">
                        <h2 className="text-2xl font-bold text-white mb-1">Մուտք գործեք ձեր հաշիվ</h2>
                        <p className="text-slate-400 text-sm">Լրացրեք ձեր տվյալները շարունակելու համար</p>
                    </div>

                    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-5">

                        {serverError && (
                            <div className="flex items-start gap-3 bg-red-500/10 border border-red-500/20 rounded-lg px-4 py-3">
                                <svg className="w-4 h-4 text-red-400 flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
                                </svg>
                                <p className="text-red-400 text-sm">{serverError}</p>
                            </div>
                        )}

                        {/* Email */}
                        <div>
                            <label className="block text-sm font-medium text-slate-300 mb-2">Էլ. հասցե</label>
                            <div className="relative">
                                <div className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-500">
                                    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                                        <path strokeLinecap="round" strokeLinejoin="round" d="M21.75 6.75v10.5a2.25 2.25 0 01-2.25 2.25h-15a2.25 2.25 0 01-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25m19.5 0v.243a2.25 2.25 0 01-1.07 1.916l-7.5 4.615a2.25 2.25 0 01-2.36 0L3.32 8.91a2.25 2.25 0 01-1.07-1.916V6.75" />
                                    </svg>
                                </div>
                                <input
                                    {...register('email')}
                                    type="email"
                                    autoComplete="email"
                                    placeholder="dr.name@hospital.com"
                                    className={`w-full bg-slate-800/80 border rounded-lg pl-10 pr-4 py-3 text-sm text-white placeholder:text-slate-600 focus:outline-none focus:ring-2 focus:border-transparent transition-all duration-150 ${errors.email ? 'border-red-500/50 focus:ring-red-500/30' : 'border-slate-700/60 focus:ring-sky-500/40 hover:border-slate-600'}`}
                                />
                            </div>
                            {errors.email && <p className="mt-1.5 text-xs text-red-400">{errors.email.message}</p>}
                        </div>

                        {/* Password */}
                        <div>
                            <label className="block text-sm font-medium text-slate-300 mb-2">Գաղտնաբառ</label>
                            <div className="relative">
                                <div className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-500">
                                    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                                        <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
                                    </svg>
                                </div>
                                <input
                                    {...register('password')}
                                    type="password"
                                    autoComplete="current-password"
                                    placeholder="••••••••"
                                    className={`w-full bg-slate-800/80 border rounded-lg pl-10 pr-4 py-3 text-sm text-white placeholder:text-slate-600 focus:outline-none focus:ring-2 focus:border-transparent transition-all duration-150 ${errors.password ? 'border-red-500/50 focus:ring-red-500/30' : 'border-slate-700/60 focus:ring-sky-500/40 hover:border-slate-600'}`}
                                />
                            </div>
                            {errors.password && <p className="mt-1.5 text-xs text-red-400">{errors.password.message}</p>}
                        </div>

                        {/* Submit */}
                        <button
                            type="submit"
                            disabled={isLoading}
                            className="w-full mt-2 bg-sky-500 hover:bg-sky-400 disabled:bg-sky-500/50 text-white font-semibold text-sm rounded-lg py-3 px-4 flex items-center justify-center gap-2 transition-all duration-150 shadow-lg shadow-sky-500/20 hover:shadow-sky-500/30 disabled:cursor-not-allowed"
                        >
                            {isLoading ? (
                                <>
                                    <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                                    </svg>
                                    Մուտք գործում...
                                </>
                            ) : (
                                <>
                                    Մուտք գործել
                                    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                                        <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                                    </svg>
                                </>
                            )}
                        </button>
                    </form>
                </div>
            </div>
        </div>
    );
}