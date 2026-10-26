import { api } from '../../api/client'
import type { ChunkMatch, DocumentChunk, DocumentSummary } from '../../api/types'

// Same limits as DocumentFiles on the API; checking here too saves uploading a file that would be refused.
export const MAX_FILE_BYTES = 10 * 1024 * 1024
export const ACCEPTED_EXTENSIONS = ['.pdf', '.md', '.markdown', '.txt']

// How often a page re-fetches while a document is still queued or being indexed.
export const POLL_INTERVAL_MS = 2000

export const documentKeys = {
  all: ['documents'] as const,
  list: ['documents', 'list'] as const,
  detail: (id: string) => ['documents', 'detail', id] as const,
  chunks: (id: string) => ['documents', 'chunks', id] as const,
  search: (id: string, query: string) => ['documents', 'search', id, query] as const,
}

export const isIndexing = (document: DocumentSummary) =>
  document.status === 'Pending' || document.status === 'Processing'

export function findFileProblem(file: File): string | null {
  const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase()
  if (!ACCEPTED_EXTENSIONS.includes(extension)) return 'Only PDF, Markdown (.md) and plain text (.txt) files are supported.'
  if (file.size === 0) return 'The file is empty.'
  if (file.size > MAX_FILE_BYTES) return 'The file is too big. The limit is 10 MB.'
  return null
}

export const fetchDocuments = (signal?: AbortSignal) => api<DocumentSummary[]>('/documents', { signal })

export const fetchDocument = (id: string, signal?: AbortSignal) => api<DocumentSummary>(`/documents/${id}`, { signal })

export const fetchChunks = (id: string, signal?: AbortSignal) =>
  api<DocumentChunk[]>(`/documents/${id}/chunks`, { signal })

export const searchDocument = (id: string, query: string, signal?: AbortSignal) =>
  api<ChunkMatch[]>(`/documents/${id}/search?q=${encodeURIComponent(query)}`, { signal })

export function uploadDocument(file: File) {
  const form = new FormData()
  form.append('file', file)
  return api<DocumentSummary>('/documents', { method: 'POST', body: form })
}

export const deleteDocument = (id: string) => api<void>(`/documents/${id}`, { method: 'DELETE' })
