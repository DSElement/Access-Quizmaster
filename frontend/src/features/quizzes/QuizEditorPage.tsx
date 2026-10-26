import { useQuery } from '@tanstack/react-query'
import { useLocation, useParams, useSearchParams } from 'react-router'
import type { QuizDraft } from '../../api/types'
import { ErrorNote, Loading } from '../../components/LoadState'
import { PageHeader } from '../../components/PageHeader'
import { useAuth } from '../auth/useAuth'
import { createQuiz, fetchEditableQuiz, quizKeys, updateQuiz } from './api'
import { QuizForm } from './QuizForm'
import { draftToFormValues, emptyQuiz, toFormValues } from './quizSchema'

export function QuizEditorPage() {
  const { id } = useParams()
  return id ? <EditQuiz id={id} /> : <NewQuiz />
}

function NewQuiz() {
  const { isAdmin } = useAuth()
  const [searchParams] = useSearchParams()
  const location = useLocation()
  const official = isAdmin && searchParams.get('official') === 'true'

  // A generated draft arrives through the navigation state (see the document page).
  const draft = (location.state as { draft?: QuizDraft } | null)?.draft

  if (draft) {
    return (
      <section>
        <PageHeader
          eyebrow="Generated draft"
          title="Review the quiz"
          lead={`Written by AI from ${draft.sourceDocumentName}. Check every question and answer before saving: it can get things wrong.`}
        />
        <QuizForm
          key={location.key}
          defaultValues={draftToFormValues(draft)}
          submitLabel="Save quiz"
          cancelTo={`/documents/${draft.sourceDocumentId}`}
          save={(input) => createQuiz({ ...input, sourceDocumentId: draft.sourceDocumentId }, false)}
        />
      </section>
    )
  }

  return (
    <section>
      <PageHeader
        eyebrow={official ? 'Official quiz' : 'New quiz'}
        title="Write a quiz"
        lead={
          official
            ? 'Official quizzes are visible to everyone and only admins can change them.'
            : 'New quizzes are private. You can share them with the community once they are ready.'
        }
      />
      <QuizForm
        key={location.key}
        defaultValues={emptyQuiz()}
        submitLabel="Create quiz"
        cancelTo={official ? '/quizzes' : '/quizzes?tab=mine'}
        save={(input) => createQuiz(input, official)}
      />
    </section>
  )
}

function EditQuiz({ id }: { id: string }) {
  const quiz = useQuery({
    queryKey: quizKeys.edit(id),
    queryFn: ({ signal }) => fetchEditableQuiz(id, signal),
    gcTime: 0,
    refetchOnWindowFocus: false,
  })

  return (
    <section>
      <PageHeader eyebrow="Editing" title={quiz.data?.title ?? 'Edit quiz'} />

      {quiz.isPending ? (
        <Loading label="Loading quiz…" />
      ) : quiz.isError ? (
        <ErrorNote
          error={quiz.error}
          messages={{
            403: "You can't edit this quiz.",
            404: "This quiz doesn't exist, or it isn't shared with you.",
          }}
        />
      ) : (
        <QuizForm
          defaultValues={toFormValues(quiz.data)}
          submitLabel="Save changes"
          cancelTo={`/quizzes/${id}`}
          save={(input) => updateQuiz(id, input)}
        />
      )}
    </section>
  )
}
