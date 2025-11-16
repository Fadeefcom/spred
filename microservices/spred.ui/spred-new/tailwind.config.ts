import type { Config } from "tailwindcss";
import * as animate from 'tailwindcss-animate';

export default {
	darkMode: ["class"],
	content: [
		"./src/**/*.{js,jsx,ts,tsx}",
		"./src/**/*.css",
		"./index.html"
	],
	prefix: "",
	theme: {
		container: {
			center: true,
			padding: '2rem',
			screens: {
				'2xl': '1400px'
			}
		},
		extend: {
			colors: {
				border: 'hsl(var(--border))',
				input: 'hsl(var(--input))',
				ring: 'hsl(var(--ring))',
				background: 'hsl(var(--background))',
				foreground: 'hsl(var(--foreground))',
				primary: {
					DEFAULT: 'hsl(var(--primary))',
					foreground: 'hsl(var(--primary-foreground))'
				},
				secondary: {
					DEFAULT: 'hsl(var(--secondary))',
					foreground: 'hsl(var(--secondary-foreground))'
				},
				destructive: {
					DEFAULT: 'hsl(var(--destructive))',
					foreground: 'hsl(var(--destructive-foreground))'
				},
				muted: {
					DEFAULT: 'hsl(var(--muted))',
					foreground: 'hsl(var(--muted-foreground))'
				},
				accent: {
					DEFAULT: 'hsl(var(--accent))',
					foreground: 'hsl(var(--accent-foreground))'
				},
				popover: {
					DEFAULT: 'hsl(var(--popover))',
					foreground: 'hsl(var(--popover-foreground))'
				},
				card: {
					DEFAULT: 'hsl(var(--card))',
					foreground: 'hsl(var(--card-foreground))'
				},
				sidebar: {
					DEFAULT: 'hsl(var(--sidebar-background))',
					foreground: 'hsl(var(--sidebar-foreground))',
					primary: 'hsl(var(--sidebar-primary))',
					'primary-foreground': 'hsl(var(--sidebar-primary-foreground))',
					accent: 'hsl(var(--sidebar-accent))',
					'accent-foreground': 'hsl(var(--sidebar-accent-foreground))',
					border: 'hsl(var(--sidebar-border))',
					ring: 'hsl(var(--sidebar-ring))'
				},
				spred: {
					black: '#000000',
					white: '#FFFFFF',
					lime: '#FFEE52',
					yellow: '#FFEE52',
					yellowdark: 'rgba(247, 249, 74, 0.5)',
					'neon-green': '#9DF57A',
					'mint-green': '#C5F0C2',
					'soft-pink': '#FABBD1',
					'vibrant-pink': '#FF00FF',
					teal: '#7FFFD4',
					'light-gray': '#F7F7F7',
					'dark-gray': '#333333',
				},
			},
			borderRadius: {
				lg: 'var(--radius)',
				md: 'calc(var(--radius) - 2px)',
				sm: 'calc(var(--radius) - 4px)'
			},
			keyframes: {
				'accordion-down': {
					from: { height: '0' },
					to: { height: 'var(--radix-accordion-content-height)' }
				},
				'accordion-up': {
					from: { height: 'var(--radix-accordion-content-height)' },
					to: { height: '0' }
				},
				'fade-in': {
					'0%': { opacity: '0', transform: 'translateY(10px)' },
					'100%': { opacity: '1', transform: 'translateY(0)' }
				},
				'fade-in-up': {
					'0%': { opacity: '0', transform: 'translateY(20px)' },
					'100%': { opacity: '1', transform: 'translateY(0)' }
				},
				'fade-in-down': {
					'0%': { opacity: '0', transform: 'translateY(-20px)' },
					'100%': { opacity: '1', transform: 'translateY(0)' }
				},
				'fade-in-left': {
					'0%': { opacity: '0', transform: 'translateX(-20px)' },
					'100%': { opacity: '1', transform: 'translateX(0)' }
				},
				'fade-in-right': {
					'0%': { opacity: '0', transform: 'translateX(20px)' },
					'100%': { opacity: '1', transform: 'translateX(0)' }
				},
				'float': {
					'0%, 100%': { transform: 'translateY(0)' },
					'50%': { transform: 'translateY(-10px)' }
				},
				'pulse-subtle': {
					'0%, 100%': { opacity: '1' },
					'50%': { opacity: '0.8' }
				},
				'scale': {
					'0%': { transform: 'scale(0.95)', opacity: '0' },
					'100%': { transform: 'scale(1)', opacity: '1' }
				},
				'blur-in': {
					'0%': { filter: 'blur(5px)', opacity: '0' },
					'100%': { filter: 'blur(0)', opacity: '1' }
				},
			},
			animation: {
				'accordion-down': 'accordion-down 0.2s ease-out',
				'accordion-up': 'accordion-up 0.2s ease-out',
				'fade-in': 'fade-in 0.7s ease-out forwards',
				'fade-in-up': 'fade-in-up 0.7s ease-out',
				'fade-in-down': 'fade-in-down 0.7s ease-out',
				'fade-in-left': 'fade-in-left 0.7s ease-out',
				'fade-in-right': 'fade-in-right 0.7s ease-out',
				'float': 'float 6s ease-in-out infinite',
				'pulse-subtle': 'pulse-subtle 4s ease-in-out infinite',
				'scale': 'scale 0.5s ease-out',
				'blur-in': 'blur-in 0.7s ease-out'
			},
			backgroundImage: {
				'hero-pattern': 'linear-gradient(to right, rgba(255, 238, 82, 0.1), rgba(127, 255, 212, 0.1))',
				'card-gradient': 'linear-gradient(135deg, rgba(255, 238, 82, 0.05) 0%, rgba(127, 255, 212, 0.05) 100%)',
				'button-gradient': 'linear-gradient(135deg, #FFEE52 0%, #7FFFD4 100%)',
			},
			fontFamily: {
				'sans': ['Inter', 'sans-serif'],
				'display': ['Clash Display', 'SF Pro Display', 'sans-serif'],
				'mono': ['Source Code Pro', 'SF Mono', 'monospace'],
			},
			boxShadow: {
				'glass': '0 4px 30px rgba(0, 0, 0, 0.1)',
				'glass-strong': '0 8px 32px rgba(0, 0, 0, 0.1)',
				'button': '0 4px 14px rgba(194, 255, 0, 0.2)',
				'button-hover': '0 6px 20px rgba(194, 255, 0, 0.4)',
			},
			backdropBlur: {
				'xs': '2px',
			},
			typography: {
				DEFAULT: {
					css: {
						maxWidth: '100%',
					},
				},
			},
			fontSize: {
				'xs-custom': ['11px', '13px'],
				'sm-custom': ['16.5px', '19.5px'],
			},
			spacing: {
				'gap-9': '9px',
				'gap-13': '13.5px',
				'px-9': '9px',
				'px-13': '13.5px',
				'py-4': '4px',
				'py-6': '6px',
			},
			width: {
				'w-11': '11px',
				'w-16': '16.5px',
			},
			height: {
				'h-11': '11px',
				'h-16': '16.5px',
			}
		}
	},
	plugins: [animate],
} satisfies Config;
