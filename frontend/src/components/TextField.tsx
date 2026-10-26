import { useId, type ComponentProps, type ReactNode } from 'react'
import styles from './TextField.module.css'

type FieldProps = {
  label: string
  hint?: string
  error?: string
}

function Field({
  label,
  hint,
  error,
  children,
}: FieldProps & { children: (ids: { id: string; describedBy?: string }) => ReactNode }) {
  const id = useId()
  const hintId = `${id}-hint`
  const errorId = `${id}-error`
  const describedBy = [hint && hintId, error && errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className={styles.field}>
      <label htmlFor={id} className={styles.label}>
        {label}
      </label>
      {children({ id, describedBy })}
      {hint && (
        <p id={hintId} className={styles.hint}>
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} className={styles.error}>
          {error}
        </p>
      )}
    </div>
  )
}

export function TextField({ label, hint, error, ...inputProps }: ComponentProps<'input'> & FieldProps) {
  return (
    <Field label={label} hint={hint} error={error}>
      {({ id, describedBy }) => (
        <input
          id={id}
          className={styles.input}
          aria-invalid={error ? true : undefined}
          aria-describedby={describedBy}
          {...inputProps}
        />
      )}
    </Field>
  )
}

export function TextAreaField({ label, hint, error, ...textareaProps }: ComponentProps<'textarea'> & FieldProps) {
  return (
    <Field label={label} hint={hint} error={error}>
      {({ id, describedBy }) => (
        <textarea
          id={id}
          className={styles.input}
          aria-invalid={error ? true : undefined}
          aria-describedby={describedBy}
          {...textareaProps}
        />
      )}
    </Field>
  )
}
