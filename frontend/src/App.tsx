import { useMemo, useState } from 'react';
import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import { getAccessToken, msal } from './auth';
import { QuizApi } from './api';
import { config } from './config';
import AuthPage from './components/AuthPage';
import QuizLibrary from './components/QuizLibrary';
import QuizBuilder from './components/QuizBuilder';
import QuizGenerator from './components/QuizGenerator';
import QuizDetail from './components/QuizDetail';
import PlayQuiz from './components/PlayQuiz';
import { ErrorMessage } from './components/Status';

type View =
  | { name: 'home' | 'build' | 'generate' }
  | { name: 'detail' | 'play'; id: string };

export default function App() {
  const authenticated = useIsAuthenticated();
  const { accounts } = useMsal();
  const [view, setView] = useState<View>({ name: 'home' });
  const [authError, setAuthError] = useState<string | null>(null);
  const [signingOut, setSigningOut] = useState(false);
  const api = useMemo(() => new QuizApi(config.apiBaseUrl, getAccessToken), []);

  async function signOut() {
    setSigningOut(true);
    setAuthError(null);
    try {
      await msal.logoutPopup();
      setView({ name: 'home' });
    } catch (error) {
      setAuthError((error as Error).message);
    } finally {
      setSigningOut(false);
    }
  }

  if (!authenticated) return <AuthPage />;
  const account = accounts[0];
  return (
    <div className="app-shell">
      <header className="app-header">
        <button
          className="brand"
          onClick={() => setView({ name: 'home' })}
          aria-label="QuizMaster home"
        >
          <span className="brand-mark">Q</span>QuizMaster
        </button>
        <div className="account">
          <span>{account.username}</span>
          <button
            className="text-button"
            disabled={signingOut}
            onClick={signOut}
          >
            {signingOut ? 'Signing out…' : 'Sign out'}
          </button>
        </div>
      </header>
      <main className="main-content">
        <ErrorMessage message={authError} />
        {view.name !== 'home' && (
          <button
            className="back-link"
            onClick={() => setView({ name: 'home' })}
          >
            ← Back to library
          </button>
        )}
        {view.name === 'home' && (
          <>
            <section className="hero">
              <div>
                <p className="eyebrow">YOUR NEXT GOOD QUESTION STARTS HERE</p>
                <h1>
                  Make a little
                  <br />
                  <em>knowledge stick.</em>
                </h1>
                <p>
                  Build a quiz, turn your notes into questions,
                  <br className="desktop-only" /> or put what you know to the
                  test.
                </p>
              </div>
              <div className="hero-actions">
                <button
                  className="button primary"
                  onClick={() => setView({ name: 'build' })}
                >
                  + Create a quiz
                </button>
                <button
                  className="button secondary"
                  onClick={() => setView({ name: 'generate' })}
                >
                  <span aria-hidden="true">✧</span> Generate from text
                </button>
                <span className="hero-note">
                  Your notes. A new way to practise.
                </span>
              </div>
              <span className="hero-decoration" aria-hidden="true">
                ?
              </span>
            </section>
            <QuizLibrary
              key={account.homeAccountId}
              api={api}
              onOpen={(id) => setView({ name: 'detail', id })}
              onPlay={(id) => setView({ name: 'play', id })}
            />
          </>
        )}
        {view.name === 'build' && (
          <QuizBuilder
            api={api}
            onCreated={(quiz) => setView({ name: 'detail', id: quiz.id })}
            onCancel={() => setView({ name: 'home' })}
          />
        )}
        {view.name === 'generate' && (
          <QuizGenerator
            api={api}
            onGenerated={(quiz) => setView({ name: 'play', id: quiz.id })}
            onCancel={() => setView({ name: 'home' })}
          />
        )}
        {view.name === 'detail' && (
          <QuizDetail
            key={view.id}
            api={api}
            quizId={view.id}
            onPlay={() => setView({ name: 'play', id: view.id })}
            onBack={() => setView({ name: 'home' })}
          />
        )}
        {view.name === 'play' && (
          <PlayQuiz
            key={view.id}
            api={api}
            quizId={view.id}
            onExit={() => setView({ name: 'home' })}
          />
        )}
      </main>
      <footer className="app-footer">
        <span>QuizMaster</span>
        <span>A little more certain, one question at a time.</span>
      </footer>
    </div>
  );
}
