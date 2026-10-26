import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router'
import { Empty, ErrorNote, Loading } from '../../components/LoadState'
import { PageHeader } from '../../components/PageHeader'
import { QuizCard, QuizGrid } from '../../components/QuizCard'
import { useAuth } from '../auth/useAuth'
import { fetchQuizzes, quizKeys } from './api'
import styles from './QuizzesPage.module.css'

export function QuizzesPage() {
  const { user, isAdmin } = useAuth()
  const [searchParams] = useSearchParams()
  const tab = searchParams.get('tab') === 'mine' ? 'mine' : 'official'
  const canLoad = tab === 'official' || user !== null

  const quizzes = useQuery({
    queryKey: quizKeys.list(tab),
    queryFn: ({ signal }) => fetchQuizzes(tab, signal),
    enabled: canLoad,
  })

  return (
    <section>
      <PageHeader
        title="Quizzes"
        lead="Official quizzes made by the QuizMaster team, and the ones you write yourself."
        actions={
          <>
            {isAdmin && tab === 'official' && (
              <Link to="/quizzes/new?official=true" className="button secondary">
                New official quiz
              </Link>
            )}
            {user && (
              <Link to="/quizzes/new" className="button">
                New quiz
              </Link>
            )}
          </>
        }
      />

      <nav className={styles.tabs} aria-label="Quiz lists">
        <Link to="/quizzes" className={styles.tab} aria-current={tab === 'official' ? 'page' : undefined}>
          Official
        </Link>
        <Link to="/quizzes?tab=mine" className={styles.tab} aria-current={tab === 'mine' ? 'page' : undefined}>
          Mine
        </Link>
      </nav>

      {!canLoad ? (
        <Empty>
          <p>Sign in to write your own quizzes and keep them here.</p>
          <Link to="/login" state={{ from: '/quizzes?tab=mine' }} className="button">
            Sign in
          </Link>
        </Empty>
      ) : quizzes.isPending ? (
        <Loading label="Loading quizzes…" />
      ) : quizzes.isError ? (
        <ErrorNote error={quizzes.error} />
      ) : quizzes.data.length === 0 ? (
        <Empty>
          {tab === 'mine' ? (
            <>
              <p>You haven't written any quizzes yet.</p>
              <Link to="/quizzes/new" className="button">
                Write your first quiz
              </Link>
            </>
          ) : (
            <p>There are no official quizzes yet.</p>
          )}
        </Empty>
      ) : (
        <QuizGrid>
          {quizzes.data.map((quiz) => (
            <QuizCard key={quiz.id} quiz={quiz} showVisibility={tab === 'mine'} />
          ))}
        </QuizGrid>
      )}
    </section>
  )
}
