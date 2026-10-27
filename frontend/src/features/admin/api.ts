import { api } from '../../api/client'
import type { AdminUser } from '../../api/types'

export const adminKeys = {
  allUsers: ['admin', 'users'] as const,
  users: (search: string) => ['admin', 'users', search] as const,
}

export const fetchUsers = (search: string, signal?: AbortSignal) =>
  api<AdminUser[]>(search ? `/admin/users?search=${encodeURIComponent(search)}` : '/admin/users', { signal })

export const promoteUser = (id: string) =>
  api<void>(`/admin/users/${encodeURIComponent(id)}/promote`, { method: 'POST' })

export const deleteUser = (id: string) => api<void>(`/admin/users/${encodeURIComponent(id)}`, { method: 'DELETE' })
