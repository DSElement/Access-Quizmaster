import { useMutation } from '@tanstack/react-query'
import { Link, NavLink, Outlet, useNavigate } from 'react-router'
import { setSignedInUser } from '../api/queryClient'
import { logout } from '../features/auth/api'
import { useAuth } from '../features/auth/useAuth'
import styles from './Layout.module.css'

const navClass = ({ isActive }: { isActive: boolean }) =>
  isActive ? `${styles.navLink} ${styles.active}` : styles.navLink

export function Layout() {
  const { user, isAdmin } = useAuth()
  const navigate = useNavigate()

  const signOut = useMutation({
    mutationFn: logout,
    onSuccess: () => {
      navigate('/')
      return setSignedInUser(null)
    },
  })

  return (
    <div className={styles.shell}>
      <header className={styles.header}>
        <div className={styles.bar}>
          <Link to="/quizzes" className={styles.brand}>
            Quiz<em>Master</em>
          </Link>

          <nav className={styles.nav} aria-label="Main">
            <NavLink to="/quizzes" className={navClass}>
              Quizzes
            </NavLink>
            <NavLink to="/community" className={navClass}>
              Community
            </NavLink>
            {user && (
              <NavLink to="/attempts" className={navClass}>
                Results
              </NavLink>
            )}
            {isAdmin && (
              <NavLink to="/admin/users" className={navClass}>
                Users
              </NavLink>
            )}
          </nav>

          <div className={styles.account}>
            {user ? (
              <>
                <span className={styles.userName}>{user.displayName}</span>
                <button
                  type="button"
                  className="secondary"
                  onClick={() => signOut.mutate()}
                  disabled={signOut.isPending}
                >
                  Sign out
                </button>
              </>
            ) : (
              <>
                <NavLink to="/login" className={navClass}>
                  Sign in
                </NavLink>
                <Link to="/register" className="button">
                  Create account
                </Link>
              </>
            )}
          </div>
        </div>
      </header>

      <main className={styles.main}>
        <Outlet />
      </main>
    </div>
  )
}
