import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Link, Navigate, useLocation } from 'react-router'
import { z } from 'zod'
import { setServerErrors } from '../../api/formErrors'
import { setSignedInUser } from '../../api/queryClient'
import { TextField } from '../../components/TextField'
import { register as registerAccount } from './api'
import styles from './AuthForm.module.css'
import { useAuth } from './useAuth'

const schema = z
  .object({
    email: z.email('Enter a valid email address.'),
    displayName: z.string().trim().min(2, 'Use at least 2 characters.').max(100, 'Use at most 100 characters.'),
    password: z.string().min(8, 'Use at least 8 characters.'),
    confirmPassword: z.string(),
  })
  .refine((values) => values.password === values.confirmPassword, {
    path: ['confirmPassword'],
    message: "The passwords don't match.",
  })

type RegisterForm = z.infer<typeof schema>

export function RegisterPage() {
  const { user } = useAuth()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', displayName: '', password: '', confirmPassword: '' },
  })

  if (user) return <Navigate to={from} replace />

  const onSubmit = handleSubmit(async ({ email, displayName, password }) => {
    try {
      await setSignedInUser(await registerAccount({ email, displayName, password }))
    } catch (error) {
      setServerErrors(error, setError, ['email', 'displayName', 'password'])
    }
  })

  return (
    <section className={styles.card}>
      <h1>Create an account</h1>
      <form className={styles.form} onSubmit={onSubmit} noValidate>
        <TextField
          label="Email"
          type="email"
          autoComplete="email"
          error={errors.email?.message}
          {...register('email')}
        />
        <TextField
          label="Display name"
          autoComplete="nickname"
          hint="Shown next to the quizzes you share."
          error={errors.displayName?.message}
          {...register('displayName')}
        />
        <TextField
          label="Password"
          type="password"
          autoComplete="new-password"
          hint="At least 8 characters, with an uppercase letter, a lowercase letter, a digit and a symbol."
          error={errors.password?.message}
          {...register('password')}
        />
        <TextField
          label="Confirm password"
          type="password"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...register('confirmPassword')}
        />

        {errors.root?.server && (
          <p role="alert" className={styles.serverError}>
            {errors.root.server.message}
          </p>
        )}

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creating account…' : 'Create account'}
        </button>
      </form>
      <p className={styles.switch}>
        Already have an account?{' '}
        <Link to="/login" state={location.state}>
          Sign in
        </Link>
      </p>
    </section>
  )
}
