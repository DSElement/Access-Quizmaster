import { useEffect, useState } from 'react';
import type { QuizApi } from '../api';
import type { QuizDetail as Detail } from '../types';
import { ErrorMessage, Loading } from './Status';

export default function QuizDetail({
  api,
  quizId,
  onPlay,
  onBack,
}: {
  api: QuizApi;
  quizId: string;
  onPlay: () => void;
  onBack: () => void;
}) {
  const [quiz, setQuiz] = useState<Detail | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    api
      .detail(quizId, controller.signal)
      .then(setQuiz)
      .catch((error: Error) => {
        if (!controller.signal.aborted) setError(error.message);
      });
    return () => controller.abort();
  }, [api, quizId]);

  if (error)
    return (
      <div>
        <ErrorMessage message={error} />
        <button className="button ghost" onClick={onBack}>
          Back to library
        </button>
      </div>
    );
  if (!quiz) return <Loading>Opening quiz…</Loading>;
  return (
    <section className="editor">
      <div className="page-heading">
        <p className="eyebrow">A QUICK LOOK</p>
        <h1>{quiz.title}</h1>
        <p>{quiz.description}</p>
      </div>
      <div className="panel">
        <div className="section-heading">
          <h2>What you’ll practise</h2>
          <span className="pill">{quiz.questions.length} QUESTIONS</span>
        </div>
        {quiz.questions.length === 0 ? (
          <p>This quiz has no questions yet.</p>
        ) : (
          <ol className="question-preview">
            {quiz.questions.map((question) => (
              <li key={question.id}>{question.text}</li>
            ))}
          </ol>
        )}
      </div>
      <div className="actions">
        <button
          className="button primary"
          disabled={quiz.questions.length === 0}
          onClick={onPlay}
        >
          Play quiz ↗
        </button>
        <button className="button ghost" onClick={onBack}>
          Back to library
        </button>
      </div>
    </section>
  );
}
