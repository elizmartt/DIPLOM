import { createBrowserRouter, Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '@/store/auth';
import AppLayout from '@/components/layout/AppLayout';
import AdminLayout from '@/components/layout/AdminLayout';
import { lazy, Suspense } from 'react';
import PageLoader from '@/components/shared/PageLoader';

// ─── Doctor pages ─────────────────────────────────────────────────────────────
const Login          = lazy(() => import('@/pages/Login'));
const ChangePassword = lazy(() => import('@/pages/ChangePassword'));
const Dashboard      = lazy(() => import('@/pages/Dashboard'));
const Patients       = lazy(() => import('@/pages/Patients'));
const PatientDetail  = lazy(() => import('@/pages/PatientDetail'));
const CreatePatient  = lazy(() => import('@/pages/CreatePatient'));
const Cases          = lazy(() => import('@/pages/Cases'));
const CaseDetail     = lazy(() => import('@/pages/cases/CaseDetail'));
const CreateCase     = lazy(() => import('@/pages/cases/CreateCase'));
const DoctorProfile  = lazy(() => import('@/pages/DoctorProfile'));
const AuditLog       = lazy(() => import('@/pages/AuditLog'));

// ─── Admin pages ──────────────────────────────────────────────────────────────
const AdminDashboard = lazy(() => import('@/pages/admin/AdminDashboard'));
const AdminDoctors   = lazy(() => import('@/pages/admin/AdminDoctors'));
const AdminAudit     = lazy(() => import('@/pages/admin/AdminAudit'));

// ─── Route guards ─────────────────────────────────────────────────────────────

function PublicRoute() {
    const isAuth = useAuthStore((s) => s.isAuthenticated);
    const doctor = useAuthStore((s) => s.doctor);

    if (isAuth && doctor) {
        if (doctor.must_change_password) return <Navigate to="/change-password" replace />;
        return <Navigate to={doctor.role === 'Admin' ? '/admin' : '/dashboard'} replace />;
    }
    return <Suspense fallback={<PageLoader />}><Outlet /></Suspense>;
}

function DoctorRoute() {
    const isAuth = useAuthStore((s) => s.isAuthenticated);
    const doctor = useAuthStore((s) => s.doctor);

    if (!isAuth || !doctor) return <Navigate to="/login" replace />;
    if (doctor.must_change_password) return <Navigate to="/change-password" replace />;
    if (doctor.role === 'Admin') return <Navigate to="/admin" replace />;
    return <Suspense fallback={<PageLoader />}><Outlet /></Suspense>;
}

function AdminRoute() {
    const isAuth = useAuthStore((s) => s.isAuthenticated);
    const doctor = useAuthStore((s) => s.doctor);

    if (!isAuth || !doctor) return <Navigate to="/login" replace />;
    if (doctor.must_change_password) return <Navigate to="/change-password" replace />;
    if (doctor.role !== 'Admin') return <Navigate to="/dashboard" replace />;
    return <Suspense fallback={<PageLoader />}><Outlet /></Suspense>;
}

function RootRedirect() {
    const isAuth = useAuthStore((s) => s.isAuthenticated);
    const doctor = useAuthStore((s) => s.doctor);

    if (!isAuth || !doctor) return <Navigate to="/login" replace />;
    if (doctor.must_change_password) return <Navigate to="/change-password" replace />;
    return <Navigate to={doctor.role === 'Admin' ? '/admin' : '/dashboard'} replace />;
}

// ─── Router ───────────────────────────────────────────────────────────────────
export const router = createBrowserRouter([
    { path: '/', element: <RootRedirect /> },

    {
        element: <PublicRoute />,
        children: [
            { path: '/login', element: <Login /> },
        ],
    },

    // Change password — accessible to any authenticated user
    {
        path: '/change-password',
        element: (
            <Suspense fallback={<PageLoader />}>
                <ChangePassword />
            </Suspense>
        ),
    },

    {
        element: <DoctorRoute />,
        children: [
            {
                element: <AppLayout />,
                children: [
                    { path: '/dashboard',            element: <Dashboard /> },
                    { path: '/patients',             element: <Patients /> },
                    { path: '/patients/new',         element: <CreatePatient /> },
                    { path: '/patients/:id',         element: <PatientDetail /> },
                    { path: '/cases',                element: <Cases /> },
                    { path: '/cases/new',            element: <CreateCase /> },
                    { path: '/cases/draft/:draftId', element: <CreateCase /> },
                    { path: '/cases/:id',            element: <CaseDetail /> },
                    { path: '/profile',              element: <DoctorProfile /> },
                    { path: '/audit',                element: <AuditLog /> },
                ],
            },
        ],
    },

    {
        element: <AdminRoute />,
        children: [
            {
                element: <AdminLayout />,
                children: [
                    { path: '/admin',         element: <AdminDashboard /> },
                    { path: '/admin/doctors', element: <AdminDoctors /> },
                    { path: '/admin/audit',   element: <AdminAudit /> },
                ],
            },
        ],
    },

    { path: '*', element: <Navigate to="/" replace /> },
]);