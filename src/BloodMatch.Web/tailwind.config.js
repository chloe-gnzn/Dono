/** @type {import('tailwindcss').Config} */

module.exports = {
  content: [
    './Components/**/*.{razor,html,cs}',
    './Models/**/*.cs',
    './wwwroot/**/*.html',
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          50: '#FEF2F2',
          100: '#FDE2E1',
          200: '#FBC8C6',
          300: '#F79F9B',
          400: '#F46C66',
          500: '#F44336',
          600: '#D32F2F',
          700: '#C62828',
          800: '#A82121',
          900: '#8A1C1C',
        },

        aqua: {
          50: '#EEF9FA',
          100: '#D3EFF1',
          200: '#B5DADD',
          300: '#8FC4C9',
        },
        // Charcoal text.
        ink: {
          DEFAULT: '#2D2D2D',
          soft: '#5C5C5C',
          muted: '#8A8A8A',
        },
        // Small blue accent used by the illustration icons (info states).
        accent: {
          DEFAULT: '#26A5DD',
          light: '#E3F4FC',
          dark: '#12739E',
        },
      },
      fontFamily: {
        heading: ['Montserrat', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        sans: ['ui-sans-serif', 'system-ui', '-apple-system', 'Segoe UI', 'Roboto', 'Helvetica Neue', 'Arial', 'sans-serif'],
      },
      boxShadow: {
        card: '0 10px 30px -12px rgba(45, 45, 45, 0.25)',
        // Flat offset shadow like the framed card in the inspiration image.
        offset: '8px 8px 0 0 #B5DADD',
      },
    },
  },
  plugins: [],
};
