import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The SPA talks to the LedgerFlow minimal API. In dev, proxy /api to it so there are no CORS hoops.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': { target: 'http://localhost:5080', changeOrigin: true },
    },
  },
})
