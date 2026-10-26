import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { Empty, ErrorNote, Loading } from '../../components/LoadState'
import { PageHeader } from '../../components/PageHeader'
import { formatDateTime } from '../../lib/format'
import { attemptKeys, fetchAttempts } from './api'
import styles from './AttemptsPage.module.css'

export function AttemptsPage() {
  const attempts = useQuery({ queryKey: attemptKeys.list, queryFn: ({ signal }) => fetchAttempts(signal) })

  return (
    <section>
      <PageHeader title="Your results" lead="Every quiz you've finished, newest first." />

      {attempts.isPending ? (
        <Loading label="Loading results…" />
      ) : attempts.isError ? (
        <ErrorNote error={attempts.error} />
      ) : attempts.data.length === 0 ? (
        <Empty>
          <p>You haven't finished a quiz yet.</p>
          <Link to="/quizzes" className="button">
            Find a quiz
          </Link>
        </Empty>
      ) : (
        <ol className={styles.list}>
          {attempts.data.map((attempt) => (
            <li key={attempt.id}>
              <Link to={`/attempts/${attempt.id}`} className={styles.row}>
                <span className={styles.title}>{attempt.quizTitle}</span>
                <span className={styles.date}>{formatDateTime(attempt.submittedAt)}</span>
                <span className={styles.score}>
                  {attempt.correctCount}/{attempt.totalQuestions}
                  <span className={styles.percent}>{attempt.scorePercentage}%</span>
                </span>
              </Link>
            </li>
          ))}
        </ol>
      )}
    </section>
  )
}
