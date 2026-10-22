import { useState } from 'react';
import { loginRequest, msal } from '../auth';
import { ErrorMessage } from './Status';

export default function AuthPage() {
  const [signingIn, setSigningIn] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function signIn() {
    setError(null);
    setSigningIn(true);
    try {
      const result = await msal.loginPopup(loginRequest);
      msal.setActiveAccount(result.account);
    } catch (error) {
      setError((error as Error).message);
    } finally {
      setSigningIn(false);
    }
  }

  return (
    <main className="auth-layout">
      <section className="auth-story">
        <a className="brand" href="/" aria-label="QuizMaster home">
          <span className="brand-mark">Q</span>QuizMaster
        </a>
        <div>
          <p className="eyebrow">A little practice. A clearer picture.</p>
          <h1>
            Turn what you read
            <br />
            into what you know.
          </h1>
          <p className="auth-intro">
            Build your own questions, turn notes into a quiz, and find out what
            sticks.
          </p>
        </div>
        <p className="auth-caption">
          CREATE &nbsp; / &nbsp; PRACTISE &nbsp; / &nbsp; UNDERSTAND
        </p>
      </section>
      <section className="auth-card" aria-labelledby="welcome-title">
        <span className="pill">YOUR STUDY SPACE</span>
        <h2 id="welcome-title">Ready for your next question?</h2>
        <p>Sign in to open the quiz library and start practising.</p>
        <ErrorMessage message={error} />
        <button
          className="button primary wide"
          onClick={signIn}
          disabled={signingIn}
        >
          <span className="microsoft-mark" aria-hidden="true">
            <i />
            <i />
            <i />
            <i />
          </span>
          {signingIn ? 'Opening sign-in…' : 'Sign in with Microsoft'}
        </button>
        <p className="fine-print">
          Use a personal Microsoft account. Your quizzes are saved automatically
          when you create them.
        </p>
      </section>
    </main>
  );
}
