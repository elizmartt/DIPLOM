/** @type {import('tailwindcss').Config} */
export default {
    darkMode: ["class"],
    content: [
        "./index.html",
        "./src/**/*.{ts,tsx,js,jsx}",
    ],
    theme: {
        extend: {
            fontFamily: {
                sans: ['"DM Sans"', 'sans-serif'],
                display: ['"Sora"', 'sans-serif'],
                mono: ['"JetBrains Mono"', 'monospace'],
            },
            colors: {
                background: "hsl(var(--background))",
                foreground: "hsl(var(--foreground))",
                card: {
                    DEFAULT: "hsl(var(--card))",
                    foreground: "hsl(var(--card-foreground))",
                },
                popover: {
                    DEFAULT: "hsl(var(--popover))",
                    foreground: "hsl(var(--popover-foreground))",
                },
                primary: {
                    DEFAULT: "hsl(var(--primary))",
                    foreground: "hsl(var(--primary-foreground))",
                },
                secondary: {
                    DEFAULT: "hsl(var(--secondary))",
                    foreground: "hsl(var(--secondary-foreground))",
                },
                muted: {
                    DEFAULT: "hsl(var(--muted))",
                    foreground: "hsl(var(--muted-foreground))",
                },
                accent: {
                    DEFAULT: "hsl(var(--accent))",
                    foreground: "hsl(var(--accent-foreground))",
                },
                destructive: {
                    DEFAULT: "hsl(var(--destructive))",
                    foreground: "hsl(var(--destructive-foreground))",
                },
                border: "hsl(var(--border))",
                input: "hsl(var(--input))",
                ring: "hsl(var(--ring))",
                medical: {
                    50: "#f0f7ff",
                    100: "#e0efff",
                    200: "#b9dcff",
                    300: "#7cc0ff",
                    400: "#36a0fc",
                    500: "#0c82ed",
                    600: "#0063cb",
                    700: "#004fa4",
                    800: "#044487",
                    900: "#0a3a70",
                    950: "#07244a",
                },
                success: {
                    DEFAULT: "#10b981",
                    light: "#d1fae5",
                },
                warning: {
                    DEFAULT: "#f59e0b",
                    light: "#fef3c7",
                },
                danger: {
                    DEFAULT: "#ef4444",
                    light: "#fee2e2",
                },
            },
            borderRadius: {
                lg: "var(--radius)",
                md: "calc(var(--radius) - 2px)",
                sm: "calc(var(--radius) - 4px)",
            },
            boxShadow: {
                card: "0 1px 3px 0 rgb(0 0 0 / 0.04), 0 1px 2px -1px rgb(0 0 0 / 0.04)",
                "card-hover": "0 4px 12px 0 rgb(0 0 0 / 0.08), 0 2px 4px -1px rgb(0 0 0 / 0.04)",
                medical: "0 0 0 3px hsl(210 100% 60% / 0.15)",
            },
            keyframes: {
                "fade-in": {
                    "0%": {opacity: "0", transform: "translateY(8px)"},
                    "100%": {opacity: "1", transform: "translateY(0)"},
                },
                "slide-in": {
                    "0%": {transform: "translateX(-100%)"},
                    "100%": {transform: "translateX(0)"},
                },
                "pulse-ring": {
                    "0%": {transform: "scale(0.95)", boxShadow: "0 0 0 0 hsl(210 100% 60% / 0.4)"},
                    "70%": {transform: "scale(1)", boxShadow: "0 0 0 8px hsl(210 100% 60% / 0)"},
                    "100%": {transform: "scale(0.95)", boxShadow: "0 0 0 0 hsl(210 100% 60% / 0)"},
                },
                shimmer: {
                    "0%": {backgroundPosition: "-200% 0"},
                    "100%": {backgroundPosition: "200% 0"},
                },
            },
            animation: {
                "fade-in": "fade-in 0.3s ease-out",
                "fade-in-slow": "fade-in 0.5s ease-out",
                "slide-in": "slide-in 0.3s ease-out",
                "pulse-ring": "pulse-ring 2s cubic-bezier(0.455, 0.03, 0.515, 0.955) infinite",
                shimmer: "shimmer 2s linear infinite",
            },
        },
    },
    plugins: [],
}
