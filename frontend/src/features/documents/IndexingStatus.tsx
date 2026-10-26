import type { DocumentSummary } from '../../api/types'
import styles from './IndexingStatus.module.css'

// A label and a progress bar for a document that is queued or being indexed.
// The bar fills while chunks are embedded; before that (queued, reading the file) it just moves.
export function IndexingStatus({ document }: { document: DocumentSummary }) {
  const step = document.progress
  const counting = step !== null && step.stage !== 'Reading' && step.total > 0
  const percent = counting ? Math.round((step.done / step.total) * 100) : null

  const label =
    document.status === 'Pending'
      ? 'Waiting to be indexed'
      : step?.stage === 'Embedding'
        ? `Embedding chunks: ${step.done} of ${step.total}`
        : step?.stage === 'RateLimited'
          ? `Waiting for the AI service's rate limit (${step.done} of ${step.total} chunks done)`
          : 'Reading the document'

  return (
    <div className={styles.status}>
      <span className={styles.label}>{label}</span>
      <div
        className={styles.track}
        role="progressbar"
        aria-label="Indexing progress"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percent ?? undefined}
      >
        {percent === null ? (
          <div className={styles.moving} />
        ) : (
          <div className={styles.fill} style={{ width: `${percent}%` }} />
        )}
      </div>
    </div>
  )
}
