import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { Doctor } from '@/types';

interface AuthState {
    doctor: Doctor | null;
    accessToken: string | null;
    refreshToken: string | null;
    isAuthenticated: boolean;
    _hydrated: boolean;

    setAuth: (doctor: Doctor, accessToken: string, refreshToken: string) => void;
    updateTokens: (accessToken: string, refreshToken: string) => void;
    logout: () => void;
    setHydrated: () => void;
}

export const useAuthStore = create<AuthState>()(
    persist(
        (set) => ({
            doctor: null,
            accessToken: null,
            refreshToken: null,
            isAuthenticated: false,
            _hydrated: false,

            setAuth: (doctor, accessToken, refreshToken) => {
                // Single source of truth: Zustand persist only.
                // axios interceptor reads from the store directly (see axios.ts fix).
                set({ doctor, accessToken, refreshToken, isAuthenticated: true });
            },

            updateTokens: (accessToken, refreshToken) => {
                set({ accessToken, refreshToken });
            },

            logout: () => {
                set({
                    doctor: null,
                    accessToken: null,
                    refreshToken: null,
                    isAuthenticated: false,
                });
            },

            setHydrated: () => set({ _hydrated: true }),
        }),
        {
            name: 'auth-storage',
            // _hydrated is runtime-only, never persisted
            partialize: (state) => ({
                doctor: state.doctor,
                accessToken: state.accessToken,
                refreshToken: state.refreshToken,
                isAuthenticated: state.isAuthenticated,
            }),
            onRehydrateStorage: () => (state) => {
                // Called when persist finishes loading from localStorage.
                // This is the RELIABLE way to set hydrated — no useEffect race.
                if (state) {
                    state.setHydrated();
                }
            },
        }
    )
);