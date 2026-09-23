import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://127.0.0.1:5015',
      '/signin-google': 'http://127.0.0.1:5015',
      '/signin-github': 'http://127.0.0.1:5015',
    },
  },
})
