import { useState } from 'react';
import type { QuizApi } from '../api';
import type { QuizDetail } from '../types';
import { ErrorMessage } from './Status';

export default function QuizGenerator({
  api,
  onGenerated,
  onCancel,
}: {
  api: QuizApi;
  onGenerated: (quiz: QuizDetail) => void;
  onCancel: () => void;
}) {
  const [sourceText, setSourceText] = useState('');
  const [title, setTitle] = useState('');
  const [questionCount, setQuestionCount] = useState('5');
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function generate(event: React.FormEvent) {
    event.preventDefault();
    if (sourceText.trim().length < 200) {
      setError('Paste at least 200 characters of source text.');
      return;
    }
    const count = Number(questionCount);
    if (!Number.isInteger(count) || count < 3 || count > 10) {
      setError('Choose between 3 and 10 questions.');
      return;
    }
    if (title !== '' && title.trim().length < 3) {
      setError('Use at least 3 characters for the title, or leave it empty.');
      return;
    }
    setGenerating(true);
    setError(null);
    try {
      const quiz = await api.generate({
        sourceText: sourceText.trim(),
        questionCount: count,
        ...(title.trim() === '' ? {} : { title: title.trim() }),
      });
      onGenerated(quiz);
    } catch (error) {
      setError((error as Error).message);
    } finally {
      setGenerating(false);
    }
  }

  return (
    <form className="editor" onSubmit={generate}>
      <div className="page-heading">
        <p className="eyebrow">FROM NOTES TO KNOW-HOW</p>
        <h1>Give your notes a new question.</h1>
        <p>
          Paste an article, study notes, or a useful explanation. AI will turn
          the key ideas into a quiz.
        </p>
      </div>
      <fieldset className="form-fields" disabled={generating}>
        <div className="panel">
          <label className="field">
            Source text
            <textarea
              required
              minLength={200}
              maxLength={20000}
              rows={12}
              placeholder="Paste the text you want to practise…"
              value={sourceText}
              onChange={(event) => setSourceText(event.target.value)}
            />
          </label>
          <p className="field-hint">
            {sourceText.trim().length.toLocaleString()} / 20,000 characters · at
            least 200
          </p>
          <div className="form-columns">
            <label className="field">
              Quiz title <span className="optional">optional</span>
              <input
                minLength={3}
                maxLength={200}
                placeholder="Let AI suggest a title"
                value={title}
                onChange={(event) => setTitle(event.target.value)}
              />
            </label>
            <label className="field">
              Questions
              <input
                required
                type="number"
                min={3}
                max={10}
                step={1}
                value={questionCount}
                onChange={(event) => setQuestionCount(event.target.value)}
              />
            </label>
          </div>
        </div>
      </fieldset>
      {generating && (
        <p className="notice" role="status">
          <span className="spinner" aria-hidden="true" />
          Writing your quiz… This can take a few minutes on a local model.
        </p>
      )}
      <ErrorMessage message={error} />
      <div className="actions">
        <button className="button primary" disabled={generating}>
          {generating ? 'Generating…' : 'Generate quiz ↗'}
        </button>
        <button
          type="button"
          className="button ghost"
          disabled={generating}
          onClick={onCancel}
        >
          Cancel
        </button>
      </div>
      <p className="fine-print">
        Questions are generated locally from your text. Check AI-written content
        for accuracy.
      </p>
    </form>
  );
}
