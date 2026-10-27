import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { setServerErrors } from '../../api/formErrors'
import { meQueryKey, setSignedInUser } from '../../api/queryClient'
import type { Me } from '../../api/types'
import { TextField } from '../../components/TextField'
import { updateProfile } from './api'
import styles from './AuthForm.module.css'
import { useAuth } from './useAuth'

const schema = z.object({
  displayName: z.string().trim().min(2, 'Use at least 2 characters.').max(100, 'Use at most 100 characters.'),
})

type ProfileForm = z.infer<typeof schema>

export function AccountPage() {
  const { user } = useAuth()
  // RequireAuth only renders this page once the user is known.
  return user ? <ProfileCard key={user.id} user={user} /> : null
}

function ProfileCard({ user }: { user: Me }) {
  const queryClient = useQueryClient()
  const [saved, setSaved] = useState(false)

  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<ProfileForm>({
    resolver: zodResolver(schema),
    defaultValues: { displayName: user.displayName },
  })

  const onSubmit = handleSubmit(async (values) => {
    setSaved(false)
    try {
      const me = await updateProfile(values)
      await setSignedInUser(me)
      reset({ displayName: me.displayName })
      setSaved(true)
      // Quiz cards and the admin user list show display names, so refetch them with the new one.
      void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] !== meQueryKey[0] })
    } catch (error) {
      setServerErrors(error, setError, ['displayName'])
    }
  })

  return (
    <section className={styles.card}>
      <h1>Your account</h1>
      <p className={styles.note}>Signed in as {user.email}</p>

      <form className={styles.form} onSubmit={onSubmit} noValidate>
        <TextField
          label="Display name"
          autoComplete="nickname"
          hint="Shown next to the quizzes you share."
          error={errors.displayName?.message}
          {...register('displayName')}
        />

        {errors.root?.server && (
          <p role="alert" className={styles.serverError}>
            {errors.root.server.message}
          </p>
        )}
        {saved && !isDirty && (
          <p role="status" className={styles.saved}>
            Display name saved.
          </p>
        )}

        <button type="submit" disabled={isSubmitting || !isDirty}>
          {isSubmitting ? 'Saving…' : 'Save'}
        </button>
      </form>
    </section>
  )
}
