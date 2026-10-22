import { useEffect, useState } from 'react';
import type { QuizApi } from '../api';
import type { QuizSummary } from '../types';
import { ErrorMessage, Loading } from './Status';
import Dialog from './Dialog';

type Props = {
  api: QuizApi;
  onOpen: (id: string) => void;
  onPlay: (id: string) => void;
};

export default function QuizLibrary({ api, onOpen, onPlay }: Props) {
  const [quizzes, setQuizzes] = useState<QuizSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);
  const [editing, setEditing] = useState<QuizSummary | null>(null);
  const [deleting, setDeleting] = useState<QuizSummary | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);
    api
      .list(controller.signal)
      .then(setQuizzes)
      .catch((error: Error) => {
        if (!controller.signal.aborted) setError(error.message);
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [api, revision]);

  function refresh() {
    setEditing(null);
    setDeleting(null);
    setRevision((value) => value + 1);
  }

  if (loading) return <Loading>Opening the quiz library…</Loading>;
  if (error)
    return (
      <div>
        <ErrorMessage message={error} />
        <button className="button secondary" onClick={refresh}>
          Reload library
        </button>
      </div>
    );

  return (
    <>
      <div className="section-heading">
        <h2>The quiz library</h2>
        <span className="muted">{quizzes.length} quizzes</span>
      </div>
      {quizzes.length === 0 && (
        <div className="empty-state">
          <h3>A fresh page.</h3>
          <p>Create your first quiz or generate one from your notes.</p>
        </div>
      )}
      <div className="quiz-grid">
        {quizzes.map((quiz, index) => (
          <article className="quiz-card" key={quiz.id}>
            <div className="card-top">
              <span className="card-number">
                {String(index + 1).padStart(2, '0')}
              </span>
              <span className="pill">
                {quiz.canEdit ? 'YOUR QUIZ' : 'SHARED QUIZ'}
              </span>
            </div>
            <h3>{quiz.title}</h3>
            <p className="card-description">{quiz.description}</p>
            <p className="card-meta">
              {quiz.questionCount} question{quiz.questionCount === 1 ? '' : 's'}
              <span>·</span>
              {new Date(quiz.createdAt).toLocaleDateString(undefined, {
                month: 'short',
                day: 'numeric',
              })}
            </p>
            <div className="card-actions">
              <button
                className="button primary"
                onClick={() => onPlay(quiz.id)}
                disabled={quiz.questionCount === 0}
              >
                Play quiz <span aria-hidden="true">↗</span>
              </button>
              <button className="button ghost" onClick={() => onOpen(quiz.id)}>
                Details
              </button>
            </div>
            {quiz.canEdit && (
              <div className="owner-actions">
                <button onClick={() => setEditing(quiz)}>Edit details</button>
                <button
                  className="danger-text"
                  onClick={() => setDeleting(quiz)}
                >
                  Delete
                </button>
              </div>
            )}
          </article>
        ))}
      </div>
      {editing && (
        <MetadataEditor
          api={api}
          quiz={editing}
          onSaved={refresh}
          onCancel={() => setEditing(null)}
        />
      )}
      {deleting && (
        <DeleteDialog
          api={api}
          quiz={deleting}
          onDeleted={refresh}
          onCancel={() => setDeleting(null)}
        />
      )}
    </>
  );
}

function MetadataEditor({
  api,
  quiz,
  onSaved,
  onCancel,
}: {
  api: QuizApi;
  quiz: QuizSummary;
  onSaved: () => void;
  onCancel: () => void;
}) {
  const [title, setTitle] = useState(quiz.title);
  const [description, setDescription] = useState(quiz.description ?? '');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function save(event: React.FormEvent) {
    event.preventDefault();
    if (title.trim().length < 3) {
      setError('Give your quiz a title with at least 3 characters.');
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await api.update(quiz.id, {
        title: title.trim(),
        description: description.trim(),
      });
      onSaved();
    } catch (error) {
      setError((error as Error).message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog
      title="Edit quiz details"
      onCancel={() => {
        if (!saving) onCancel();
      }}
    >
      <form onSubmit={save}>
        <label className="field">
          Title
          <input
            autoFocus
            required
            minLength={3}
            maxLength={200}
            value={title}
            onChange={(event) => setTitle(event.target.value)}
          />
        </label>
        <label className="field">
          Description
          <textarea
            maxLength={2000}
            rows={3}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
        </label>
        <ErrorMessage message={error} />
        <div className="actions">
          <button className="button primary" disabled={saving}>
            {saving ? 'Saving…' : 'Save changes'}
          </button>
          <button
            type="button"
            className="button ghost"
            disabled={saving}
            onClick={onCancel}
          >
            Cancel
          </button>
        </div>
      </form>
    </Dialog>
  );
}

function DeleteDialog({
  api,
  quiz,
  onDeleted,
  onCancel,
}: {
  api: QuizApi;
  quiz: QuizSummary;
  onDeleted: () => void;
  onCancel: () => void;
}) {
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function remove() {
    setDeleting(true);
    setError(null);
    try {
      await api.delete(quiz.id);
      onDeleted();
    } catch (error) {
      setError((error as Error).message);
    } finally {
      setDeleting(false);
    }
  }

  return (
    <Dialog
      title="Delete this quiz?"
      onCancel={() => {
        if (!deleting) onCancel();
      }}
    >
      <p>“{quiz.title}” and its questions will be permanently removed.</p>
      <ErrorMessage message={error} />
      <div className="actions">
        <button className="button danger" disabled={deleting} onClick={remove}>
          {deleting ? 'Deleting…' : 'Delete quiz'}
        </button>
        <button
          autoFocus
          className="button ghost"
          disabled={deleting}
          onClick={onCancel}
        >
          Keep quiz
        </button>
      </div>
    </Dialog>
  );
}
