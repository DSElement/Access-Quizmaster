import type { QuizVisibility } from '../api/types'
import styles from './Badge.module.css'

const labels: Record<QuizVisibility, string> = {
  Official: 'Official',
  Private: 'Private',
  Public: 'Shared',
}

export function VisibilityBadge({ visibility }: { visibility: QuizVisibility }) {
  return <span className={`${styles.badge} ${styles[visibility]}`}>{labels[visibility]}</span>
}
