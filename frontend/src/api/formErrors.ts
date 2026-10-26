import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import { ApiError } from './client'

type FieldMatcher<T extends FieldValues> = readonly Path<T>[] | ((key: string) => boolean)

export function setServerErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  fields: FieldMatcher<T>,
) {
  if (!(error instanceof ApiError)) {
    setError('root.server', { message: 'Something went wrong. Check your connection and try again.' })
    return
  }

  const isField = typeof fields === 'function' ? fields : (key: string) => fields.some((f) => f === key)
  const unmatched: string[] = []

  for (const [serverKey, messages] of Object.entries(error.fieldErrors)) {
    const key = serverKey.replace(/\[(\d+)\]/g, '.$1')
    if (isField(key)) setError(key as Path<T>, { message: messages.join(' ') })
    else unmatched.push(...messages)
  }

  if (Object.keys(error.fieldErrors).length === 0) unmatched.push(error.message)
  if (unmatched.length > 0) setError('root.server', { message: unmatched.join(' ') })
}
