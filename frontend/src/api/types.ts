export const ADMIN_ROLE = 'Admin'

export type Me = {
  id: string
  email: string
  displayName: string
  roles: string[]
}

export type AdminUser = Me

export type QuizVisibility = 'Official' | 'Private' | 'Public'

export type QuizScope = 'official' | 'mine' | 'community'

export type QuizSummary = {
  id: string
  title: string
  description: string | null
  visibility: QuizVisibility
  authorName: string | null
  questionCount: number
  createdAt: string
  updatedAt: string
  canEdit: boolean
  canDelete: boolean
  canPublish: boolean
}

export type PlayQuiz = {
  id: string
  title: string
  questions: { id: string; text: string; options: { id: string; text: string }[] }[]
}

export type EditableQuiz = {
  id: string
  title: string
  description: string | null
  visibility: QuizVisibility
  sourceDocumentId: string | null
  questions: {
    id: string
    text: string
    sourceChunkId: string | null
    options: { id: string; text: string; isCorrect: boolean }[]
  }[]
}

export type SaveQuizInput = {
  title: string
  description: string | null
  sourceDocumentId?: string | null
  questions: { text: string; sourceChunkId?: string | null; options: { text: string; isCorrect: boolean }[] }[]
}

// A generated quiz that isn't saved yet; it opens in the quiz builder for review.
export type QuizDraft = {
  title: string
  description: string | null
  sourceDocumentId: string
  sourceDocumentName: string
  questions: { text: string; sourceChunkId: string; options: { text: string; isCorrect: boolean }[] }[]
}

export type SubmittedAnswer = { questionId: string; selectedOptionId: string | null }

export type AttemptSummary = {
  id: string
  quizId: string | null
  quizTitle: string
  submittedAt: string
  correctCount: number
  totalQuestions: number
  scorePercentage: number
}

export type DocumentStatus = 'Pending' | 'Processing' | 'Ready' | 'Failed'

export type DocumentSummary = {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  status: DocumentStatus
  error: string | null
  chunkCount: number
  uploadedAt: string
  processedAt: string | null
  progress: IndexingStep | null
}

// Done and total count chunks; both are 0 while the document is still being read.
export type IndexingStep = { stage: 'Reading' | 'Embedding' | 'RateLimited'; done: number; total: number }

export type DocumentChunk = { id: string; index: number; text: string }

export type ChunkMatch = { chunkId: string; index: number; text: string; similarity: number }

export type AttemptDetail = AttemptSummary & {
  answers: {
    id: string
    questionText: string
    options: string[]
    selectedOptionIndex: number | null
    wasCorrect: boolean
  }[]
}
