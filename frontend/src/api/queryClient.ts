import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query'
import { ApiError } from './client'
import type { Me } from './types'

export const meQueryKey = ['me'] as const

function signOutOnUnauthorized(error: Error) {
  if (error instanceof ApiError && error.status === 401) {
    queryClient.setQueryData<Me | null>(meQueryKey, null)
  }
}

export const queryClient = new QueryClient({
  queryCache: new QueryCache({ onError: signOutOnUnauthorized }),
  mutationCache: new MutationCache({ onError: signOutOnUnauthorized }),
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: (failureCount, error) =>
        !(error instanceof ApiError && error.status < 500) && failureCount < 2,
    },
  },
})

let currentUserId: string | null | undefined

queryClient.getQueryCache().subscribe((event) => {
  if (event.type !== 'updated' || event.query.queryKey[0] !== meQueryKey[0]) return

  const me = event.query.state.data as Me | null | undefined
  if (me === undefined) return

  const userId = me?.id ?? null
  if (currentUserId !== undefined && userId !== currentUserId) {
    void queryClient.resetQueries({ predicate: (query) => query.queryKey[0] !== meQueryKey[0] })
  }
  currentUserId = userId
})

export async function setSignedInUser(me: Me | null) {
  await queryClient.cancelQueries({ queryKey: meQueryKey })
  queryClient.setQueryData<Me | null>(meQueryKey, me)
}
