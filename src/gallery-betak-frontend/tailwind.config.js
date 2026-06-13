/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      fontFamily: {
        'cairo': ['"Cairo"', 'sans-serif'],
      },
      colors: {
        cyan: {
          50: '#fff1f2',
          100: '#ffe4e6',
          200: '#fecdd3',
          300: '#fda4af',
          400: '#ff85a2',
          500: '#fb7185',
          600: '#f43f5e',
          700: '#be123c',
          800: '#9f1239',
          900: '#881337',
          950: '#4c0519',
        },
        sky: {
          50: '#fff5f6',
          100: '#ffeef0',
          200: '#ffd6dc',
          300: '#ffadb7',
          400: '#ff85a2',
          500: '#fb7185',
          600: '#e11d48',
          700: '#be123c',
          800: '#9f1239',
          900: '#881337',
          950: '#4c0519',
        },
        teal: {
          50: '#fffaf0',
          100: '#fef3c7',
          200: '#fde68a',
          300: '#fcd34d',
          400: '#fbbf24',
          500: '#e5a93b',
          600: '#d4af37',
          700: '#b45309',
          800: '#92400e',
          900: '#78350f',
          950: '#451a03',
        }
      }
    },
  },
  plugins: [],
}
