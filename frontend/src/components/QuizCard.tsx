import type { ReactNode } from 'react'
import { Link } from 'react-router'
import type { QuizSummary } from '../api/types'
import { VisibilityBadge } from './Badge'
import styles from './QuizCard.module.css'

type Props = {
  quiz: QuizSummary
  showVisibility?: boolean
  showAuthor?: boolean
}

export function QuizCard({ quiz, showVisibility = false, showAuthor = false }: Props) {
  return (
    <li className={styles.card}>
      <h3 className={styles.title}>
        <Link to={`/quizzes/${quiz.id}`} className={styles.link}>
          {quiz.title}
        </Link>
      </h3>
      {quiz.description && <p className={styles.description}>{quiz.description}</p>}
      <p className={styles.meta}>
        <span>
          {quiz.questionCount} {quiz.questionCount === 1 ? 'question' : 'questions'}
        </span>
        {showAuthor && quiz.authorName && <span>by {quiz.authorName}</span>}
        {showVisibility && <VisibilityBadge visibility={quiz.visibility} />}
      </p>
    </li>
  )
}

export function QuizGrid({ children }: { children: ReactNode }) {
  return <ul className={styles.grid}>{children}</ul>
}
