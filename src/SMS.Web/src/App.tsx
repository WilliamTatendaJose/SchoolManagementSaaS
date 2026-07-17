import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { ProtectedRoute } from './components/layout/ProtectedRoute'
import { navItems } from './components/layout/navConfig'
import { ComingSoonPage } from './pages/ComingSoonPage'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route path="/dashboard" element={<DashboardPage />} />
          {navItems
            .filter((item) => item.path !== '/dashboard')
            .map((item) => (
              <Route
                key={item.path}
                path={item.path}
                element={<ComingSoonPage title={item.label} icon={item.icon} />}
              />
            ))}
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}
