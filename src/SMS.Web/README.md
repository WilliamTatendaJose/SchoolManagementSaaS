# SMS.Web

React + TypeScript + Vite PWA frontend for the School Management SaaS. Consumes the `SMS.API` REST backend.

## Stack

- React 19 + TypeScript, Vite
- Tailwind CSS v4 (via `@tailwindcss/vite`)
- React Router for routing
- TanStack Query for server state
- Zustand (persisted to `localStorage`) for auth/session state
- Axios, with an interceptor that attaches the JWT and transparently refreshes it on a 401
- `vite-plugin-pwa` for installability

## Running locally

```bash
npm install
npm run dev
```

The dev server proxies `/api/*` to the backend at `http://localhost:59584` (see `vite.config.ts`). Start `SMS.API` separately:

```bash
dotnet run --project ../SMS.API --urls http://localhost:59584
```

On first run the API seeds a demo tenant (`Demo School`) and admin user (`admin@demoschool.com` / `Admin@123`).

## Structure

- `src/api` — Axios client (`client.ts`) and shared API response types (`types.ts`)
- `src/auth` — session store (`authStore.ts`) and the `/users/me` profile query (`useProfile.ts`) that drives permission-based nav
- `src/components/layout` — `AppShell` (sidebar + topbar), `ProtectedRoute`, and `navConfig.ts` (the permission → nav-item mapping)
- `src/pages` — route-level screens

## Adding a nav-gated screen

1. Add the entry to `navConfig.ts` with the permission code the backend expects (see `SMS.Application/Common/Security/Permissions.cs`).
2. Add a `<Route>` for it in `App.tsx`, replacing the corresponding `ComingSoonPage` placeholder.

## Auth flow

Login posts to `/api/auth/login` with a tenant, email, and password, storing the returned access/refresh tokens. `AppShell` then calls `GET /api/users/me` to populate roles/permissions, which `Sidebar` filters against. A 401 on any request triggers a single-flight refresh via `/api/auth/refresh` before retrying.
