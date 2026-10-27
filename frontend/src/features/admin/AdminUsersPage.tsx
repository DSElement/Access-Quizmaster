import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { meQueryKey } from '../../api/queryClient'
import { ADMIN_ROLE } from '../../api/types'
import { useDebouncedValue } from '../../lib/useDebouncedValue'
import { useAuth } from '../auth/useAuth'
import { adminKeys, deleteUser, fetchUsers, promoteUser } from './api'
import styles from './AdminUsersPage.module.css'

export function AdminUsersPage() {
  const { user: me } = useAuth()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const term = useDebouncedValue(search.trim())

  const users = useQuery({
    queryKey: adminKeys.users(term),
    queryFn: ({ signal }) => fetchUsers(term, signal),
    placeholderData: keepPreviousData,
  })

  const promote = useMutation({
    mutationFn: promoteUser,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: adminKeys.allUsers }),
  })

  const [confirmingDeleteId, setConfirmingDeleteId] = useState<string | null>(null)

  const remove = useMutation({
    mutationFn: deleteUser,
    onSuccess: () => {
      setConfirmingDeleteId(null)
      // Their quizzes are deleted with them, so the quiz lists change too.
      return queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] !== meQueryKey[0] })
    },
  })

  return (
    <section>
      <h1>Users</h1>

      <div className={styles.search}>
        <label htmlFor="user-search" className={styles.searchLabel}>
          Search
        </label>
        <input
          id="user-search"
          type="search"
          className={styles.searchInput}
          placeholder="Display name or email"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          autoComplete="off"
        />
      </div>

      {users.isPending && <p>Loading users…</p>}
      {users.isError && <p role="alert">Couldn't load users: {users.error.message}</p>}
      {promote.isError && <p role="alert">Couldn't promote the user: {promote.error.message}</p>}
      {remove.isError && <p role="alert">Couldn't delete the user: {remove.error.message}</p>}

      {users.data?.length === 0 && <p className={styles.noMatch}>No users match “{term}”.</p>}

      {users.data && users.data.length > 0 && (
        <div className={styles.tableWrap} aria-busy={users.isPlaceholderData || undefined}>
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
                const isDeleting = remove.isPending && remove.variables === user.id

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
                      {/* Admin accounts can't be deleted, so they get no actions here. */}
                      {!isAdmin &&
                        (confirmingDeleteId === user.id ? (
                          <span className={styles.confirm}>
                            <span>Delete {user.displayName} and everything they own?</span>
                            <button
                              type="button"
                              className="danger"
                              onClick={() => remove.mutate(user.id)}
                              disabled={remove.isPending}
                            >
                              {isDeleting ? 'Deleting…' : 'Yes, delete'}
                            </button>
                            <button type="button" className="secondary" onClick={() => setConfirmingDeleteId(null)}>
                              Keep
                            </button>
                          </span>
                        ) : (
                          <span className={styles.rowActions}>
                            <button
                              type="button"
                              className="secondary"
                              onClick={() => promote.mutate(user.id)}
                              disabled={promote.isPending}
                            >
                              {isPromoting ? 'Promoting…' : 'Make admin'}
                            </button>
                            <button
                              type="button"
                              className="danger"
                              onClick={() => {
                                remove.reset()
                                setConfirmingDeleteId(user.id)
                              }}
                            >
                              Delete
                            </button>
                          </span>
                        ))}
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
