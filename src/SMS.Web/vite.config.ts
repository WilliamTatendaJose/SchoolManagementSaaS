import path from 'node:path'
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
        // Never cache API calls in the service worker. Attendance marking tolerates
        // offline writes via its own IndexedDB queue (src/offline/attendanceQueue.ts,
        // flushed by useAttendanceSync) rather than a service-worker background sync -
        // that queue is currently attendance-only, not a generic write queue for every
        // endpoint.
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
    // node_modules can get hoisted up to the repo root (SMS.Web lives inside a larger
    // .NET solution, not its own workspace root), which Vite's default fs boundary
    // doesn't include — without this, deps hoisted above this project's root 404.
    fs: {
      allow: [path.resolve(__dirname, '..', '..')],
    },
    proxy: {
      // Overridable so the same config works running natively (API on localhost)
      // and inside docker-compose (API reachable by its service name on the
      // compose network, where "localhost" would mean the container itself).
      '/api': {
        target: process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:59584',
        changeOrigin: true,
      },
    },
  },
})
