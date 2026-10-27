import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import type { QuizSummary } from '../../api/types'
import { VisibilityBadge } from '../../components/Badge'
import { ErrorNote, Loading } from '../../components/LoadState'
import { PageHeader } from '../../components/PageHeader'
import { formatDate, plural } from '../../lib/format'
import { useAuth } from '../auth/useAuth'
import { attemptKeys } from '../attempts/api'
import { deleteQuiz, fetchQuiz, makeOfficial, quizKeys, setPublished } from './api'
import styles from './QuizDetailPage.module.css'

export function QuizDetailPage() {
  const { id = '' } = useParams()
  const quiz = useQuery({ queryKey: quizKeys.detail(id), queryFn: ({ signal }) => fetchQuiz(id, signal) })

  if (quiz.isPending) return <Loading label="Loading quiz…" />
  if (quiz.isError) {
    return <ErrorNote error={quiz.error} messages={{ 404: "This quiz doesn't exist, or it isn't shared with you." }} />
  }

  return <QuizDetail quiz={quiz.data} />
}

function QuizDetail({ quiz }: { quiz: QuizSummary }) {
  const { user } = useAuth()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [confirmingOfficial, setConfirmingOfficial] = useState(false)

  const publish = useMutation({
    mutationFn: (published: boolean) => setPublished(quiz.id, published),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: quizKeys.all }),
  })

  // One-way: the quiz loses its owner, so it moves from "Mine" to the official list.
  const promote = useMutation({
    mutationFn: () => makeOfficial(quiz.id),
    onSuccess: () => {
      setConfirmingOfficial(false)
      return queryClient.invalidateQueries({ queryKey: quizKeys.all })
    },
  })

  const remove = useMutation({
    mutationFn: () => deleteQuiz(quiz.id),
    onSuccess: () => {
      navigate(afterDeletePath(quiz, user?.id), { replace: true })
      for (const key of [quizKeys.detail(quiz.id), quizKeys.play(quiz.id), quizKeys.edit(quiz.id)]) {
        queryClient.removeQueries({ queryKey: key })
      }
      void queryClient.invalidateQueries({ queryKey: ['quizzes', 'list'] })
      void queryClient.invalidateQueries({ queryKey: attemptKeys.all })
    },
  })

  const isShared = quiz.visibility === 'Public'
  const author = quiz.visibility === 'Official' ? 'QuizMaster team' : quiz.authorName

  return (
    <article>
      <PageHeader
        eyebrow={
          <span className={styles.eyebrow}>
            <VisibilityBadge visibility={quiz.visibility} />
            {author && <span>by {author}</span>}
          </span>
        }
        title={quiz.title}
        lead={quiz.description}
      />

      <dl className={styles.facts}>
        <div>
          <dt>Questions</dt>
          <dd>{quiz.questionCount}</dd>
        </div>
        <div>
          <dt>Updated</dt>
          <dd>{formatDate(quiz.updatedAt)}</dd>
        </div>
      </dl>

      <div className={styles.play}>
        <Link to={`/quizzes/${quiz.id}/play`} className="button">
          {user ? 'Start the quiz' : 'Sign in to play'}
        </Link>
        <span className={styles.playHint}>{plural(quiz.questionCount, 'question')}</span>
      </div>

      {(quiz.canEdit || quiz.canPublish || quiz.canDelete || quiz.canMakeOfficial) && (
        <section className={styles.manage} aria-labelledby="manage-heading">
          <h2 id="manage-heading" className={styles.manageHeading}>
            Manage
          </h2>

          {quiz.canPublish && (
            <p className={styles.shareNote}>
              {isShared
                ? 'Shared: everyone can find this quiz on the Community page.'
                : 'Private: only you can see this quiz.'}
            </p>
          )}

          <div className={styles.actions}>
            {quiz.canEdit && (
              <Link to={`/quizzes/${quiz.id}/edit`} className="button secondary">
                Edit
              </Link>
            )}
            {quiz.canPublish && (
              <button
                type="button"
                className="secondary"
                onClick={() => publish.mutate(!isShared)}
                disabled={publish.isPending}
              >
                {isShared ? 'Make private' : 'Share with community'}
              </button>
            )}
            {quiz.canMakeOfficial &&
              (confirmingOfficial ? (
                <span className={styles.confirm}>
                  <span>Make official? You won't own it anymore, and this can't be undone.</span>
                  <button type="button" onClick={() => promote.mutate()} disabled={promote.isPending}>
                    {promote.isPending ? 'Saving…' : 'Yes, make official'}
                  </button>
                  <button type="button" className="secondary" onClick={() => setConfirmingOfficial(false)}>
                    Cancel
                  </button>
                </span>
              ) : (
                <button type="button" className="secondary" onClick={() => setConfirmingOfficial(true)}>
                  Make official
                </button>
              ))}
            {quiz.canDelete &&
              (confirmingDelete ? (
                <span className={styles.confirm}>
                  <span>Delete for good?</span>
                  <button type="button" className="danger" onClick={() => remove.mutate()} disabled={remove.isPending}>
                    {remove.isPending ? 'Deleting…' : 'Yes, delete'}
                  </button>
                  <button type="button" className="secondary" onClick={() => setConfirmingDelete(false)}>
                    Keep it
                  </button>
                </span>
              ) : (
                <button type="button" className="danger" onClick={() => setConfirmingDelete(true)}>
                  Delete
                </button>
              ))}
          </div>

          {publish.isError && <ErrorNote error={publish.error} />}
          {promote.isError && <ErrorNote error={promote.error} />}
          {remove.isError && <ErrorNote error={remove.error} />}
        </section>
      )}
    </article>
  )
}

function afterDeletePath(quiz: QuizSummary, userId: string | undefined) {
  if (quiz.visibility === 'Official') return '/quizzes'
  if (quiz.canPublish) return '/quizzes?tab=mine'
  return userId ? '/community' : '/quizzes'
}
