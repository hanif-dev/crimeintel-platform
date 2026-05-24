/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{vue,ts,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        mono: ['"JetBrains Mono"', 'monospace'],
        display: ['"Space Grotesk"', 'sans-serif'],
        body: ['"DM Sans"', 'sans-serif'],
      },
      colors: {
        ink: {
          950: '#050810',
          900: '#080d1a',
          800: '#0d1526',
          700: '#131e35',
          600: '#1c2b4a',
        },
        signal: {
          DEFAULT: '#00e5ff',
          dim: '#00b8cc',
        },
        threat: {
          critical: '#ff2d55',
          high: '#ff6b35',
          medium: '#ffd60a',
          low: '#34c759',
        },
      },
    },
  },
  plugins: [],
}
