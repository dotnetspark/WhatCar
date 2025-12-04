import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const apiTarget = process.env['WHATCAR-API_HTTPS'] || process.env['WHATCAR-API_HTTP'];

export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true,
        secure: false
      }
    }
  },
})
