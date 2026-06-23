import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Frontend talks to backend only via API.
// In dev, /api is proxied to the ASP.NET Core backend.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})
