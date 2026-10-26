import type { DocumentStatus, QuizVisibility } from '../api/types'
import styles from './Badge.module.css'

const visibilityLabels: Record<QuizVisibility, string> = {
  Official: 'Official',
  Private: 'Private',
  Public: 'Shared',
}

export function VisibilityBadge({ visibility }: { visibility: QuizVisibility }) {
  return <span className={`${styles.badge} ${styles[visibility]}`}>{visibilityLabels[visibility]}</span>
}

const statusLabels: Record<DocumentStatus, string> = {
  Pending: 'Queued',
  Processing: 'Indexing',
  Ready: 'Ready',
  Failed: 'Failed',
}

export function DocumentStatusBadge({ status }: { status: DocumentStatus }) {
  return <span className={`${styles.badge} ${styles[status]}`}>{statusLabels[status]}</span>
}
