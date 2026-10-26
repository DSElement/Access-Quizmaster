import { createBrowserRouter, Navigate } from 'react-router'
import { ADMIN_ROLE } from './api/types'
import { Layout } from './components/Layout'
import { AdminUsersPage } from './features/admin/AdminUsersPage'
import { AttemptPage } from './features/attempts/AttemptPage'
import { AttemptsPage } from './features/attempts/AttemptsPage'
import { PlayPage } from './features/attempts/PlayPage'
import { LoginPage } from './features/auth/LoginPage'
import { RegisterPage } from './features/auth/RegisterPage'
import { RequireAuth } from './features/auth/RequireAuth'
import { CommunityPage } from './features/quizzes/CommunityPage'
import { QuizDetailPage } from './features/quizzes/QuizDetailPage'
import { QuizEditorPage } from './features/quizzes/QuizEditorPage'
import { QuizzesPage } from './features/quizzes/QuizzesPage'
import { ErrorPage } from './routes/ErrorPage'
import { NotFoundPage } from './routes/NotFoundPage'

export const router = createBrowserRouter([
  {
    element: <Layout />,
    children: [
      {
        errorElement: <ErrorPage />,
        children: [
          { index: true, element: <Navigate to="/quizzes" replace /> },
          { path: 'login', element: <LoginPage /> },
          { path: 'register', element: <RegisterPage /> },
          { path: 'quizzes', element: <QuizzesPage /> },
          { path: 'community', element: <CommunityPage /> },
          { path: 'quizzes/:id', element: <QuizDetailPage /> },
          {
            element: <RequireAuth />,
            children: [
              { path: 'quizzes/new', element: <QuizEditorPage /> },
              { path: 'quizzes/:id/edit', element: <QuizEditorPage /> },
              { path: 'quizzes/:id/play', element: <PlayPage /> },
              { path: 'attempts', element: <AttemptsPage /> },
              { path: 'attempts/:id', element: <AttemptPage /> },
            ],
          },
          {
            element: <RequireAuth role={ADMIN_ROLE} />,
            children: [{ path: 'admin/users', element: <AdminUsersPage /> }],
          },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
])
