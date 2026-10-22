import { useEffect, useState } from 'react';
import type { QuizApi } from '../api';
import type { QuizDetail, QuizResult } from '../types';
import { ErrorMessage, Loading } from './Status';

export default function PlayQuiz({
  api,
  quizId,
  onExit,
}: {
  api: QuizApi;
  quizId: string;
  onExit: () => void;
}) {
  const [quiz, setQuiz] = useState<QuizDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [current, setCurrent] = useState(0);
  const [picks, setPicks] = useState<Record<string, string>>({});
  const [result, setResult] = useState<QuizResult | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setLoadError(null);
    setCurrent(0);
    setPicks({});
    setResult(null);
    setSubmitError(null);
    api
      .play(quizId, controller.signal)
      .then(setQuiz)
      .catch((error: Error) => {
        if (!controller.signal.aborted) setLoadError(error.message);
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [api, quizId]);

  async function submit() {
    if (!quiz) return;
    setSubmitting(true);
    setSubmitError(null);
    try {
      const answers = quiz.questions.map((question) => ({
        questionId: question.id,
        selectedOptionId: picks[question.id] ?? null,
      }));
      setResult(await api.evaluate(quiz.id, answers));
    } catch (error) {
      setSubmitError((error as Error).message);
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) return <Loading>Getting your questions ready…</Loading>;
  if (loadError)
    return (
      <div>
        <ErrorMessage message={loadError} />
        <button className="button ghost" onClick={onExit}>
          Back to library
        </button>
      </div>
    );
  if (!quiz) return null;
  if (quiz.questions.length === 0)
    return (
      <div className="empty-state">
        <h1>{quiz.title}</h1>
        <p>This quiz has no questions yet.</p>
        <button className="button primary" onClick={onExit}>
          Back to library
        </button>
      </div>
    );

  if (result)
    return (
      <section className="editor">
        <div className="page-heading">
          <p className="eyebrow">PRACTICE, MEET PROGRESS</p>
          <h1>Here’s how you did.</h1>
          <p>{quiz.title}</p>
        </div>
        <div className="score-panel">
          <strong>
            {result.scorePercentage}
            <span>%</span>
          </strong>
          <div>
            <h2>
              {result.correctCount} of {result.totalQuestions} correct
            </h2>
            <p>A closer look at what you knew and what to revisit.</p>
          </div>
        </div>
        <ol className="results-list">
          {result.results.map((question, index) => (
            <li className="panel" key={question.questionId}>
              <div className="section-heading">
                <span className="eyebrow">
                  QUESTION {String(index + 1).padStart(2, '0')}
                </span>
                <span
                  className={`result-label ${question.wasCorrect ? 'correct' : 'incorrect'}`}
                >
                  {question.wasCorrect
                    ? 'Correct'
                    : question.selectedOptionId === null
                      ? 'Unanswered'
                      : 'To revisit'}
                </span>
              </div>
              <h2 className="result-question">{question.questionText}</h2>
              <ul className="result-options">
                {question.options.map((option) => (
                  <li
                    key={option.id}
                    className={
                      option.isCorrect
                        ? 'correct'
                        : option.id === question.selectedOptionId
                          ? 'incorrect'
                          : ''
                    }
                  >
                    <span>{option.text}</span>
                    <span className="result-note">
                      {option.isCorrect
                        ? '✓ Correct answer'
                        : option.id === question.selectedOptionId
                          ? 'Your answer'
                          : ''}
                    </span>
                  </li>
                ))}
              </ul>
            </li>
          ))}
        </ol>
        <button className="button primary" onClick={onExit}>
          Back to library
        </button>
      </section>
    );

  const question = quiz.questions[current];
  const last = current === quiz.questions.length - 1;
  return (
    <section className="player">
      <div className="player-heading">
        <span className="eyebrow">{quiz.title}</span>
        <button className="text-button" disabled={submitting} onClick={onExit}>
          Quit quiz
        </button>
      </div>
      <div className="progress-label">
        <span>
          Question {current + 1} of {quiz.questions.length}
        </span>
        <span>{Object.keys(picks).length} answered</span>
      </div>
      <progress
        max={quiz.questions.length}
        value={current + 1}
        aria-label="Quiz progress"
      />
      <div className="panel play-panel">
        <h1>{question.text}</h1>
        <fieldset className="play-options" disabled={submitting}>
          <legend className="visually-hidden">Choose one answer</legend>
          {question.options.map((option, index) => (
            <label
              key={option.id}
              className={`play-option ${picks[question.id] === option.id ? 'selected' : ''}`}
            >
              <input
                type="radio"
                name={`answer-${question.id}`}
                checked={picks[question.id] === option.id}
                onChange={() =>
                  setPicks((values) => ({
                    ...values,
                    [question.id]: option.id,
                  }))
                }
              />
              <span className="option-letter" aria-hidden="true">
                {String.fromCharCode(65 + index)}
              </span>
              <span>{option.text}</span>
            </label>
          ))}
        </fieldset>
        <ErrorMessage message={submitError} />
        <div className="actions player-actions">
          <button
            className="button ghost"
            disabled={current === 0 || submitting}
            onClick={() => setCurrent((value) => value - 1)}
          >
            ← Previous
          </button>
          {last ? (
            <button
              className="button primary"
              disabled={submitting}
              onClick={submit}
            >
              {submitting ? 'Grading…' : 'Submit for grading'}
            </button>
          ) : (
            <button
              className="button primary"
              onClick={() => setCurrent((value) => value + 1)}
            >
              Next question →
            </button>
          )}
        </div>
      </div>
      <p className="fine-print">
        You can go back to change an answer before submitting. Unanswered
        questions count as incorrect.
      </p>
    </section>
  );
}
