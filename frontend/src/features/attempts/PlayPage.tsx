import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import type { PlayQuiz } from '../../api/types'
import { ErrorNote, Loading } from '../../components/LoadState'
import { PageHeader } from '../../components/PageHeader'
import { plural } from '../../lib/format'
import { shuffle } from '../../lib/shuffle'
import { fetchPlayQuiz, quizKeys } from '../quizzes/api'
import { attemptKeys, submitAttempt } from './api'
import styles from './PlayPage.module.css'

export function PlayPage() {
  const { id = '' } = useParams()
  const quiz = useQuery({
    queryKey: quizKeys.play(id),
    queryFn: ({ signal }) => fetchPlayQuiz(id, signal),
    gcTime: 0,
    refetchOnWindowFocus: false,
  })

  if (quiz.isPending) return <Loading label="Loading quiz…" />
  if (quiz.isError) {
    return <ErrorNote error={quiz.error} messages={{ 404: "This quiz doesn't exist, or it isn't shared with you." }} />
  }

  return <PlayQuizForm quiz={quiz.data} />
}

function PlayQuizForm({ quiz }: { quiz: PlayQuiz }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [questions] = useState(() => shuffle(quiz.questions))
  const [selected, setSelected] = useState<Record<string, string>>({})

  const answeredCount = questions.filter((q) => selected[q.id]).length
  const unanswered = questions.length - answeredCount

  const submit = useMutation({
    mutationFn: () =>
      submitAttempt(
        quiz.id,
        questions.map((q) => ({ questionId: q.id, selectedOptionId: selected[q.id] ?? null })),
      ),
    onSuccess: (attempt) => {
      void queryClient.invalidateQueries({ queryKey: attemptKeys.list })
      navigate(`/attempts/${attempt.id}`)
    },
  })

  return (
    <section>
      <PageHeader eyebrow="Playing" title={quiz.title} lead={`${plural(questions.length, 'question')} in random order. Pick one answer for each.`} />

      <form
        onSubmit={(event) => {
          event.preventDefault()
          submit.mutate()
        }}
      >
        <ol className={styles.questions}>
          {questions.map((question, index) => (
            <li key={question.id}>
              <fieldset className={styles.question}>
                <legend className={styles.legend}>
                  <span className={styles.number}>Q{index + 1}</span>
                  <span className={styles.questionText}>{question.text}</span>
                </legend>
                <div className={styles.options}>
                  {question.options.map((option) => (
                    <label key={option.id} className={styles.option}>
                      <input
                        type="radio"
                        name={question.id}
                        value={option.id}
                        checked={selected[question.id] === option.id}
                        onChange={() => setSelected((current) => ({ ...current, [question.id]: option.id }))}
                      />
                      <span>{option.text}</span>
                    </label>
                  ))}
                </div>
              </fieldset>
            </li>
          ))}
        </ol>

        <div className={styles.footer}>
          {submit.isError && (
            <div className={styles.submitError}>
              <ErrorNote
                error={submit.error}
                messages={{
                  400: 'This quiz was changed while you were playing. Reload the page to get the latest version.',
                  404: 'This quiz was deleted or made private while you were playing.',
                }}
              />
            </div>
          )}
          <span className={styles.progress} aria-live="polite">
            {answeredCount} of {questions.length} answered
            {unanswered > 0 && <span className={styles.hint}> · unanswered questions count as wrong</span>}
          </span>
          <Link to={`/quizzes/${quiz.id}`} className="button secondary">
            Leave
          </Link>
          <button type="submit" disabled={submit.isPending}>
            {submit.isPending ? 'Checking…' : 'Submit answers'}
          </button>
        </div>
      </form>
    </section>
  )
}
