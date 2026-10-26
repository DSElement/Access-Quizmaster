import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router'
import type { AttemptDetail } from '../../api/types'
import { ErrorNote, Loading } from '../../components/LoadState'
import { PageHeader } from '../../components/PageHeader'
import { formatDateTime } from '../../lib/format'
import { attemptKeys, fetchAttempt } from './api'
import styles from './AttemptPage.module.css'

export function AttemptPage() {
  const { id = '' } = useParams()
  const attempt = useQuery({
    queryKey: attemptKeys.detail(id),
    queryFn: ({ signal }) => fetchAttempt(id, signal),
  })

  if (attempt.isPending) return <Loading label="Loading result…" />
  if (attempt.isError) return <ErrorNote error={attempt.error} messages={{ 404: "This result doesn't exist." }} />

  return <AttemptResult attempt={attempt.data} />
}

function AttemptResult({ attempt }: { attempt: AttemptDetail }) {
  return (
    <article>
      <div className={styles.top}>
        <PageHeader
          eyebrow={`Result · ${formatDateTime(attempt.submittedAt)}`}
          title={attempt.quizTitle}
          lead={attempt.quizId === null ? 'This quiz has since been deleted.' : undefined}
          actions={
            <>
              <Link to="/attempts" className="button secondary">
                All results
              </Link>
              {attempt.quizId && (
                <Link to={`/quizzes/${attempt.quizId}/play`} className="button">
                  Play again
                </Link>
              )}
            </>
          }
        />
        <p className={styles.score}>
          <span className={styles.scoreLabel}>Score</span>
          <span className={styles.fraction}>
            {attempt.correctCount}/{attempt.totalQuestions}
          </span>
          <span className={styles.percent}>{attempt.scorePercentage}%</span>
        </p>
      </div>

      <ol className={styles.answers}>
        {attempt.answers.map((answer, index) => (
          <li key={answer.id} className={styles.answer} data-correct={answer.wasCorrect || undefined}>
            <h2 className={styles.question}>
              <span className={styles.number}>Q{index + 1}</span>
              <span>{answer.questionText}</span>
              <span className={styles.mark}>{answer.wasCorrect ? 'Correct' : 'Wrong'}</span>
            </h2>

            <ul className={styles.options}>
              {answer.options.map((option, optionIndex) => {
                const isPicked = optionIndex === answer.selectedOptionIndex
                const state = isPicked ? (answer.wasCorrect ? 'correct' : 'wrong') : undefined

                return (
                  <li key={optionIndex} className={styles.option} data-state={state}>
                    <span>{option}</span>
                    {isPicked && <span className={styles.tag}>your answer</span>}
                  </li>
                )
              })}
            </ul>

            {answer.selectedOptionIndex === null && <p className={styles.unanswered}>You didn't answer this one.</p>}
          </li>
        ))}
      </ol>
    </article>
  )
}
