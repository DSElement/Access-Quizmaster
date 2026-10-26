import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ADMIN_ROLE } from '../../api/types'
import { useAuth } from '../auth/useAuth'
import { adminKeys, fetchUsers, promoteUser } from './api'
import styles from './AdminUsersPage.module.css'

export function AdminUsersPage() {
  const { user: me } = useAuth()
  const queryClient = useQueryClient()

  const users = useQuery({
    queryKey: adminKeys.users,
    queryFn: ({ signal }) => fetchUsers(signal),
  })

  const promote = useMutation({
    mutationFn: promoteUser,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: adminKeys.users }),
  })

  return (
    <section>
      <h1>Users</h1>

      {users.isPending && <p>Loading users…</p>}
      {users.isError && <p role="alert">Couldn't load users: {users.error.message}</p>}
      {promote.isError && <p role="alert">Couldn't promote the user: {promote.error.message}</p>}

      {users.data && (
        <div className={styles.tableWrap}>
          <table className={styles.table}>
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Email</th>
                <th scope="col">Role</th>
                <th scope="col">
                  <span className="visually-hidden">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {users.data.map((user) => {
                const isAdmin = user.roles.includes(ADMIN_ROLE)
                const isPromoting = promote.isPending && promote.variables === user.id

                return (
                  <tr key={user.id}>
                    <td>
                      {user.displayName}
                      {user.id === me?.id && <span className={styles.you}> (you)</span>}
                    </td>
                    <td>{user.email}</td>
                    <td>
                      <span className={isAdmin ? styles.adminBadge : styles.userBadge}>
                        {isAdmin ? 'Admin' : 'User'}
                      </span>
                    </td>
                    <td className={styles.actions}>
                      {!isAdmin && (
                        <button
                          type="button"
                          className="secondary"
                          onClick={() => promote.mutate(user.id)}
                          disabled={promote.isPending}
                        >
                          {isPromoting ? 'Promoting…' : 'Make admin'}
                        </button>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
