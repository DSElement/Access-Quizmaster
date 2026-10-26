import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Link, Navigate, useLocation } from 'react-router'
import { z } from 'zod'
import { setServerErrors } from '../../api/formErrors'
import { setSignedInUser } from '../../api/queryClient'
import { TextField } from '../../components/TextField'
import { login } from './api'
import styles from './AuthForm.module.css'
import { useAuth } from './useAuth'

const schema = z.object({
  email: z.email('Enter a valid email address.'),
  password: z.string().min(1, 'Enter your password.'),
  rememberMe: z.boolean(),
})

type LoginForm = z.infer<typeof schema>

export function LoginPage() {
  const { user } = useAuth()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '', rememberMe: false },
  })

  if (user) return <Navigate to={from} replace />

  const onSubmit = handleSubmit(async (values) => {
    try {
      await setSignedInUser(await login(values))
    } catch (error) {
      setServerErrors(error, setError, ['email', 'password'])
    }
  })

  return (
    <section className={styles.card}>
      <h1>Sign in</h1>
      <form className={styles.form} onSubmit={onSubmit} noValidate>
        <TextField
          label="Email"
          type="email"
          autoComplete="email"
          error={errors.email?.message}
          {...register('email')}
        />
        <TextField
          label="Password"
          type="password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <label className={styles.checkbox}>
          <input type="checkbox" {...register('rememberMe')} />
          Keep me signed in
        </label>

        {errors.root?.server && (
          <p role="alert" className={styles.serverError}>
            {errors.root.server.message}
          </p>
        )}

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
      <p className={styles.switch}>
        No account yet?{' '}
        <Link to="/register" state={location.state}>
          Create one
        </Link>
      </p>
    </section>
  )
}
