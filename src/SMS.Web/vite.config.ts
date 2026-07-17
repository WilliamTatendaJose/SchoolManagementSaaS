import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: 'autoUpdate',
      workbox: {
        // Never cache API calls in the service worker; the app's own IndexedDB
        // sync queue (see src/offline) handles offline tolerance for writes.
        navigateFallbackDenylist: [/^\/api\//],
      },
      manifest: {
        name: 'School Management SaaS',
        short_name: 'SchoolSMS',
        description: 'School management for Zimbabwean schools',
        theme_color: '#0f172a',
        background_color: '#0f172a',
        display: 'standalone',
        icons: [
          { src: 'pwa-192.svg', sizes: '192x192', type: 'image/svg+xml' },
          { src: 'pwa-512.svg', sizes: '512x512', type: 'image/svg+xml' },
        ],
      },
    }),
  ],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:59584',
        changeOrigin: true,
      },
    },
  },
})
