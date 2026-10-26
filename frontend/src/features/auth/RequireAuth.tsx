import { Navigate, Outlet, useLocation } from 'react-router'
import { useAuth } from './useAuth'

type Props = { role?: string }

export function RequireAuth({ role }: Props) {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) return <p>Loading…</p>

  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />

  if (role && !user.roles.includes(role)) {
    return (
      <section>
        <h1>No access</h1>
        <p>Your account doesn't have permission to see this page.</p>
      </section>
    )
  }

  return <Outlet />
}
