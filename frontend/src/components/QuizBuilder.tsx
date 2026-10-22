import { useState } from 'react';
import type { QuizApi } from '../api';
import type { QuizDetail } from '../types';
import { ErrorMessage } from './Status';

type DraftOption = { id: string; text: string; isCorrect: boolean };
type DraftQuestion = { id: string; text: string; options: DraftOption[] };
const newOption = (): DraftOption => ({
  id: crypto.randomUUID(),
  text: '',
  isCorrect: false,
});

export default function QuizBuilder({
  api,
  onCreated,
  onCancel,
}: {
  api: QuizApi;
  onCreated: (quiz: QuizDetail) => void;
  onCancel: () => void;
}) {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [questions, setQuestions] = useState<DraftQuestion[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function addQuestion() {
    setQuestions((items) => [
      ...items,
      {
        id: crypto.randomUUID(),
        text: '',
        options: Array.from({ length: 4 }, newOption),
      },
    ]);
  }

  function updateQuestion(
    id: string,
    update: (question: DraftQuestion) => DraftQuestion,
  ) {
    setQuestions((items) =>
      items.map((question) =>
        question.id === id ? update(question) : question,
      ),
    );
  }

  async function save(event: React.FormEvent) {
    event.preventDefault();
    if (title.trim().length < 3) {
      setError('Give your quiz a title with at least 3 characters.');
      return;
    }
    if (questions.length === 0) {
      setError('Add at least one question.');
      return;
    }
    for (const [index, question] of questions.entries()) {
      if (question.text.trim().length < 3) {
        setError(`Question ${index + 1} needs at least 3 characters.`);
        return;
      }
      if (
        question.options.length < 2 ||
        question.options.some((option) => option.text.trim().length === 0)
      ) {
        setError(
          `Question ${index + 1} needs at least two filled answer options.`,
        );
        return;
      }
      if (question.options.filter((option) => option.isCorrect).length !== 1) {
        setError(`Mark exactly one correct answer for question ${index + 1}.`);
        return;
      }
    }
    setSaving(true);
    setError(null);
    try {
      const quiz = await api.create({
        title: title.trim(),
        description: description.trim(),
        questions: questions.map((question) => ({
          text: question.text.trim(),
          options: question.options.map((option) => ({
            text: option.text.trim(),
            isCorrect: option.isCorrect,
          })),
        })),
      });
      onCreated(quiz);
    } catch (error) {
      setError((error as Error).message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <form className="editor" onSubmit={save}>
      <div className="page-heading">
        <p className="eyebrow">MAKE IT YOURS</p>
        <h1>Create a quiz</h1>
        <p>
          Start with a question. Add a few possibilities. Choose the right one.
        </p>
      </div>
      <fieldset className="form-fields" disabled={saving}>
        <div className="panel">
          <label className="field">
            Quiz title
            <input
              required
              minLength={3}
              maxLength={200}
              placeholder="e.g. A quick tour of HTTP"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
            />
          </label>
          <label className="field">
            Description <span className="optional">optional</span>
            <textarea
              maxLength={2000}
              rows={2}
              placeholder="What will this quiz help someone practise?"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
            />
          </label>
        </div>
        {questions.map((question, index) => (
          <section
            className="panel question-editor"
            key={question.id}
            aria-labelledby={`question-heading-${question.id}`}
          >
            <div className="section-heading">
              <h2 id={`question-heading-${question.id}`}>
                Question {String(index + 1).padStart(2, '0')}
              </h2>
              <button
                type="button"
                className="text-button danger-text"
                onClick={() =>
                  setQuestions((items) =>
                    items.filter((item) => item.id !== question.id),
                  )
                }
              >
                Remove question
              </button>
            </div>
            <label className="field">
              Question text
              <input
                required
                minLength={3}
                maxLength={500}
                placeholder="What would you like to ask?"
                value={question.text}
                onChange={(event) =>
                  updateQuestion(question.id, (item) => ({
                    ...item,
                    text: event.target.value,
                  }))
                }
              />
            </label>
            <fieldset className="answer-editor">
              <legend>
                Answer options{' '}
                <span className="muted">— select the correct answer</span>
              </legend>
              {question.options.map((option, optionIndex) => (
                <div className="answer-row" key={option.id}>
                  <input
                    type="radio"
                    name={`correct-${question.id}`}
                    aria-label={`Mark option ${optionIndex + 1} correct for question ${index + 1}`}
                    checked={option.isCorrect}
                    onChange={() =>
                      updateQuestion(question.id, (item) => ({
                        ...item,
                        options: item.options.map((answer) => ({
                          ...answer,
                          isCorrect: answer.id === option.id,
                        })),
                      }))
                    }
                  />
                  <input
                    required
                    maxLength={300}
                    aria-label={`Question ${index + 1}, option ${optionIndex + 1}`}
                    placeholder={`Option ${optionIndex + 1}`}
                    value={option.text}
                    onChange={(event) =>
                      updateQuestion(question.id, (item) => ({
                        ...item,
                        options: item.options.map((answer) =>
                          answer.id === option.id
                            ? { ...answer, text: event.target.value }
                            : answer,
                        ),
                      }))
                    }
                  />
                  <button
                    type="button"
                    className="remove-option"
                    aria-label={`Remove option ${optionIndex + 1} from question ${index + 1}`}
                    disabled={question.options.length <= 2}
                    onClick={() =>
                      updateQuestion(question.id, (item) => ({
                        ...item,
                        options: item.options.filter(
                          (answer) => answer.id !== option.id,
                        ),
                      }))
                    }
                  >
                    ×
                  </button>
                </div>
              ))}
              <button
                type="button"
                className="text-button"
                onClick={() =>
                  updateQuestion(question.id, (item) => ({
                    ...item,
                    options: [...item.options, newOption()],
                  }))
                }
              >
                + Add option
              </button>
            </fieldset>
          </section>
        ))}
        <button type="button" className="add-question" onClick={addQuestion}>
          + Add a question
        </button>
      </fieldset>
      <ErrorMessage message={error} />
      <div className="actions editor-footer">
        <span className="muted">
          {questions.length} question{questions.length === 1 ? '' : 's'}
        </span>
        <button className="button primary" disabled={saving}>
          {saving ? 'Saving quiz…' : 'Save quiz'}
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
  );
}
