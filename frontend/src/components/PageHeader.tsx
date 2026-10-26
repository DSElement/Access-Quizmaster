import type { ReactNode } from 'react'
import styles from './PageHeader.module.css'

type Props = {
  title: string
  eyebrow?: ReactNode
  lead?: ReactNode
  actions?: ReactNode
}

export function PageHeader({ title, eyebrow, lead, actions }: Props) {
  return (
    <header className={styles.header}>
      <div className={styles.text}>
        {eyebrow && <p className={styles.eyebrow}>{eyebrow}</p>}
        <h1>{title}</h1>
        {lead && <p className={styles.lead}>{lead}</p>}
      </div>
      {actions && <div className={styles.actions}>{actions}</div>}
    </header>
  )
}
