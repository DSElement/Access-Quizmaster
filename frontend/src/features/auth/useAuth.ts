import { useQuery } from '@tanstack/react-query'
import { meQueryKey } from '../../api/queryClient'
import { ADMIN_ROLE } from '../../api/types'
import { fetchMe } from './api'

export function useAuth() {
  const { data, isPending } = useQuery({ queryKey: meQueryKey, queryFn: ({ signal }) => fetchMe(signal) })
  const user = data ?? null

  return {
    user,
    isLoading: isPending,
    isAdmin: user?.roles.includes(ADMIN_ROLE) ?? false,
  }
}
