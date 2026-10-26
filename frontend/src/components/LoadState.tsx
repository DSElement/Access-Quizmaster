import type { ReactNode } from 'react'
import { ApiError } from '../api/client'
import styles from './LoadState.module.css'

export function Loading({ label = 'Loading…' }: { label?: string }) {
  return <p className={styles.loading}>{label}</p>
}

type ErrorNoteProps = {
  error: Error
  messages?: Partial<Record<number, string>>
}

export function ErrorNote({ error, messages }: ErrorNoteProps) {
  const message = (error instanceof ApiError && messages?.[error.status]) || error.message

  return (
    <p role="alert" className={styles.error}>
      {message}
    </p>
  )
}

export function Empty({ children }: { children: ReactNode }) {
  return <div className={styles.empty}>{children}</div>
}
