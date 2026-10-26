import { api } from '../../api/client'
import type { AdminUser } from '../../api/types'

export const adminKeys = {
  users: ['admin', 'users'] as const,
}

export const fetchUsers = (signal?: AbortSignal) => api<AdminUser[]>('/admin/users', { signal })

export const promoteUser = (id: string) =>
  api<void>(`/admin/users/${encodeURIComponent(id)}/promote`, { method: 'POST' })
