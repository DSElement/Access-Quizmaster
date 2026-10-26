import styles from './Spinner.module.css'

// Decorative: whatever shows it should also say in words what's happening.
export function Spinner() {
  return <span className={styles.spinner} aria-hidden="true" />
}
