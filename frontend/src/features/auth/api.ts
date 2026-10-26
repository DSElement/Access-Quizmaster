import { api, ApiError } from '../../api/client'
import type { Me } from '../../api/types'

export type LoginInput = { email: string; password: string; rememberMe: boolean }
export type RegisterInput = { email: string; displayName: string; password: string }

export async function fetchMe(signal?: AbortSignal): Promise<Me | null> {
  try {
    return await api<Me>('/auth/me', { signal })
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) return null
    throw error
  }
}

export const login = (input: LoginInput) => api<Me>('/auth/login', { method: 'POST', body: input })

export const register = (input: RegisterInput) => api<Me>('/auth/register', { method: 'POST', body: input })

export const logout = () => api<void>('/auth/logout', { method: 'POST' })
