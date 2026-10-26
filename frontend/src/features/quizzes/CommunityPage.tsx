import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { Empty, ErrorNote, Loading } from '../../components/LoadState'
import { PageHeader } from '../../components/PageHeader'
import { QuizCard, QuizGrid } from '../../components/QuizCard'
import { useAuth } from '../auth/useAuth'
import { fetchQuizzes, quizKeys } from './api'

export function CommunityPage() {
  const { user } = useAuth()
  const quizzes = useQuery({
    queryKey: quizKeys.list('community'),
    queryFn: ({ signal }) => fetchQuizzes('community', signal),
  })

  return (
    <section>
      <PageHeader title="Community" lead="Quizzes other people wrote and decided to share." />

      {quizzes.isPending ? (
        <Loading label="Loading quizzes…" />
      ) : quizzes.isError ? (
        <ErrorNote error={quizzes.error} />
      ) : quizzes.data.length === 0 ? (
        <Empty>
          <p>Nobody has shared a quiz yet.</p>
          {user && (
            <Link to="/quizzes?tab=mine" className="button secondary">
              Share one of yours
            </Link>
          )}
        </Empty>
      ) : (
        <QuizGrid>
          {quizzes.data.map((quiz) => (
            <QuizCard key={quiz.id} quiz={quiz} showAuthor />
          ))}
        </QuizGrid>
      )}
    </section>
  )
}
