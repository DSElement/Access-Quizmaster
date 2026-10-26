import { isRouteErrorResponse, Link, useRouteError } from 'react-router'

export function ErrorPage() {
  const error = useRouteError()

  const message = isRouteErrorResponse(error)
    ? `${error.status} ${error.statusText}`
    : error instanceof Error
      ? error.message
      : 'Unknown error'

  return (
    <section>
      <h1>Something went wrong</h1>
      <p>{message}</p>
      <Link to="/">Back to the quizzes</Link>
    </section>
  )
}
