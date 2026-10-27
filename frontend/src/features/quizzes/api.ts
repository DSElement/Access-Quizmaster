import { api } from '../../api/client'
import type { EditableQuiz, PlayQuiz, QuizScope, QuizSummary, SaveQuizInput } from '../../api/types'

export const quizKeys = {
  all: ['quizzes'] as const,
  list: (scope: QuizScope) => ['quizzes', 'list', scope] as const,
  detail: (id: string) => ['quizzes', 'detail', id] as const,
  play: (id: string) => ['quizzes', 'play', id] as const,
  edit: (id: string) => ['quizzes', 'edit', id] as const,
}

export const fetchQuizzes = (scope: QuizScope, signal?: AbortSignal) =>
  api<QuizSummary[]>(`/quizzes?scope=${scope}`, { signal })

export const fetchQuiz = (id: string, signal?: AbortSignal) => api<QuizSummary>(`/quizzes/${id}`, { signal })

export const fetchPlayQuiz = (id: string, signal?: AbortSignal) => api<PlayQuiz>(`/quizzes/${id}/play`, { signal })

export const fetchEditableQuiz = (id: string, signal?: AbortSignal) =>
  api<EditableQuiz>(`/quizzes/${id}/edit`, { signal })

export const createQuiz = (input: SaveQuizInput, official: boolean) =>
  api<EditableQuiz>(official ? '/quizzes/official' : '/quizzes', { method: 'POST', body: input })

export const updateQuiz = (id: string, input: SaveQuizInput) =>
  api<EditableQuiz>(`/quizzes/${id}`, { method: 'PUT', body: input })

export const deleteQuiz = (id: string) => api<void>(`/quizzes/${id}`, { method: 'DELETE' })

export const setPublished = (id: string, published: boolean) =>
  api<void>(`/quizzes/${id}/${published ? 'publish' : 'unpublish'}`, { method: 'POST' })

export const makeOfficial = (id: string) => api<void>(`/quizzes/${id}/make-official`, { method: 'POST' })
