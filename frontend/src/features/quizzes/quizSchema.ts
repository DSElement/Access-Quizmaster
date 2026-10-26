import { z } from 'zod'
import type { EditableQuiz, QuizDraft, SaveQuizInput } from '../../api/types'

export const MAX_QUESTIONS = 50
export const MIN_OPTIONS = 2
export const MAX_OPTIONS = 6

const optionSchema = z.object({
  text: z.string().trim().min(1, 'Write the answer.').max(300, 'Keep answers under 300 characters.'),
  isCorrect: z.boolean(),
})

const questionSchema = z.object({
  text: z.string().trim().min(3, 'Write the question (at least 3 characters).').max(500, 'Keep questions under 500 characters.'),
  options: z
    .array(optionSchema)
    .min(MIN_OPTIONS, `Add at least ${MIN_OPTIONS} answers.`)
    .max(MAX_OPTIONS, `Use at most ${MAX_OPTIONS} answers.`)
    .refine((options) => options.filter((o) => o.isCorrect).length === 1, 'Mark which answer is correct.')
    .refine(
      (options) => new Set(options.map((o) => o.text.trim().toLowerCase())).size === options.length,
      'Two answers are the same.',
    ),
  // Not shown in the form: the document passage a generated question came from, kept through edits.
  sourceChunkId: z.string().nullable(),
})

export const quizFormSchema = z.object({
  title: z.string().trim().min(3, 'Give the quiz a title (at least 3 characters).').max(200, 'Keep the title under 200 characters.'),
  description: z.string().trim().max(2000, 'Keep the description under 2000 characters.'),
  questions: z
    .array(questionSchema)
    .min(1, 'Add at least one question.')
    .max(MAX_QUESTIONS, `A quiz can have at most ${MAX_QUESTIONS} questions.`),
})

export type QuizFormValues = z.infer<typeof quizFormSchema>

export const emptyQuestion = (): QuizFormValues['questions'][number] => ({
  text: '',
  sourceChunkId: null,
  options: Array.from({ length: 4 }, () => ({ text: '', isCorrect: false })),
})

export const emptyQuiz = (): QuizFormValues => ({
  title: '',
  description: '',
  questions: [emptyQuestion()],
})

export const toFormValues = (quiz: EditableQuiz): QuizFormValues => ({
  title: quiz.title,
  description: quiz.description ?? '',
  questions: quiz.questions.map((q) => ({
    text: q.text,
    sourceChunkId: q.sourceChunkId,
    options: q.options.map((o) => ({ text: o.text, isCorrect: o.isCorrect })),
  })),
})

export const draftToFormValues = (draft: QuizDraft): QuizFormValues => ({
  title: draft.title,
  description: draft.description ?? '',
  questions: draft.questions,
})

export const toSaveInput = (values: QuizFormValues): SaveQuizInput => ({
  title: values.title,
  description: values.description || null,
  questions: values.questions,
})

export const isQuizFormField = (key: string) =>
  /^(title|description|questions(\.\d+(\.(text|options(\.\d+\.text)?))?)?)$/.test(key)
