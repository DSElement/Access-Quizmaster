import { api } from '../../api/client'
import type { AttemptDetail, AttemptSummary, SubmittedAnswer } from '../../api/types'

export const attemptKeys = {
  all: ['attempts'] as const,
  list: ['attempts', 'list'] as const,
  detail: (id: string) => ['attempts', 'detail', id] as const,
}

export const fetchAttempts = (signal?: AbortSignal) => api<AttemptSummary[]>('/attempts', { signal })

export const fetchAttempt = (id: string, signal?: AbortSignal) => api<AttemptDetail>(`/attempts/${id}`, { signal })

export const submitAttempt = (quizId: string, answers: SubmittedAnswer[]) =>
  api<AttemptDetail>(`/quizzes/${quizId}/attempts`, { method: 'POST', body: { answers } })
