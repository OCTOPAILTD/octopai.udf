/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        'cloudera-blue': '#0047AB',
        'cloudera-green': '#00C896',
        'cloudera-orange': '#FF6B35',
      }
    },
  },
  plugins: [],
}

