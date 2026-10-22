import {
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { QuizApi } from './api';
import type { QuizDetail, QuizResult, QuizSummary } from './types';
import QuizBuilder from './components/QuizBuilder';
import QuizGenerator from './components/QuizGenerator';
import QuizLibrary from './components/QuizLibrary';
import PlayQuiz from './components/PlayQuiz';

const detail: QuizDetail = {
  id: 'quiz',
  title: 'HTTP practice',
  description: 'HTTP questions',
  createdAt: '2026-01-01T00:00:00Z',
  canEdit: true,
  questions: [
    {
      id: 'q1',
      text: 'Which method reads data?',
      options: [
        { id: 'a1', text: 'GET' },
        { id: 'a2', text: 'POST' },
      ],
    },
    {
      id: 'q2',
      text: 'Which status means created?',
      options: [
        { id: 'b1', text: '201' },
        { id: 'b2', text: '404' },
      ],
    },
  ],
};

const grade: QuizResult = {
  totalQuestions: 2,
  correctCount: 1,
  scorePercentage: 50,
  results: detail.questions.map((question, index) => ({
    questionId: question.id,
    questionText: question.text,
    selectedOptionId: index === 0 ? 'a1' : null,
    correctOptionId: question.options[0].id,
    wasCorrect: index === 0,
    options: question.options.map((option, position) => ({
      ...option,
      isCorrect: position === 0,
    })),
  })),
};

function api(overrides: Partial<Record<keyof QuizApi, unknown>>) {
  return overrides as unknown as QuizApi;
}

describe('manual authoring', () => {
  it('keeps one correct option per question and saves the entire quiz in one request', async () => {
    const user = userEvent.setup();
    const create = vi.fn().mockResolvedValue(detail);
    const created = vi.fn();
    render(
      <QuizBuilder
        api={api({ create })}
        onCreated={created}
        onCancel={vi.fn()}
      />,
    );
    await user.type(screen.getByLabelText('Quiz title'), 'My practice quiz');
    await user.click(screen.getByRole('button', { name: /Add a question/ }));
    await user.type(
      screen.getByLabelText('Question text'),
      'Which method reads data?',
    );
    for (let index = 1; index <= 4; index++)
      await user.type(
        screen.getByLabelText(`Question 1, option ${index}`),
        `Option ${index}`,
      );
    await user.click(
      screen.getByRole('radio', {
        name: 'Mark option 1 correct for question 1',
      }),
    );
    await user.click(
      screen.getByRole('radio', {
        name: 'Mark option 3 correct for question 1',
      }),
    );
    expect(
      (
        screen.getByRole('radio', {
          name: 'Mark option 1 correct for question 1',
        }) as HTMLInputElement
      ).checked,
    ).toBe(false);
    await user.click(screen.getByRole('button', { name: 'Save quiz' }));
    await waitFor(() => expect(created).toHaveBeenCalledWith(detail));
    expect(create).toHaveBeenCalledTimes(1);
    const payload = create.mock.calls[0][0];
    expect(
      payload.questions[0].options.filter(
        (option: { isCorrect: boolean }) => option.isCorrect,
      ),
    ).toHaveLength(1);
    expect(payload.questions[0].options[2].isCorrect).toBe(true);
    expect(payload.questions[0]).not.toHaveProperty('id');
  });

  it('blocks a question without a correct answer before making a request', async () => {
    const user = userEvent.setup();
    const create = vi.fn();
    render(
      <QuizBuilder
        api={api({ create })}
        onCreated={vi.fn()}
        onCancel={vi.fn()}
      />,
    );
    await user.type(screen.getByLabelText('Quiz title'), 'My practice quiz');
    await user.click(screen.getByRole('button', { name: /Add a question/ }));
    await user.type(
      screen.getByLabelText('Question text'),
      'Which answer is correct?',
    );
    for (let index = 1; index <= 4; index++)
      await user.type(
        screen.getByLabelText(`Question 1, option ${index}`),
        `Option ${index}`,
      );
    await user.click(screen.getByRole('button', { name: 'Save quiz' }));
    expect(screen.getByRole('alert').textContent).toContain(
      'Mark exactly one correct answer',
    );
    expect(create).not.toHaveBeenCalled();
  });
});

describe('quiz play', () => {
  it('retains picks during navigation and reveals the server grade only after submission', async () => {
    const user = userEvent.setup();
    const evaluate = vi.fn().mockResolvedValue(grade);
    render(
      <PlayQuiz
        api={api({ play: vi.fn().mockResolvedValue(detail), evaluate })}
        quizId="quiz"
        onExit={vi.fn()}
      />,
    );
    await user.click(await screen.findByRole('radio', { name: 'GET' }));
    expect(screen.queryByText('Correct answer')).toBeNull();
    await user.click(screen.getByRole('button', { name: /Next question/ }));
    await user.click(screen.getByRole('button', { name: /Previous/ }));
    expect(
      (screen.getByRole('radio', { name: 'GET' }) as HTMLInputElement).checked,
    ).toBe(true);
    await user.click(screen.getByRole('button', { name: /Next question/ }));
    await user.click(
      screen.getByRole('button', { name: 'Submit for grading' }),
    );
    expect(evaluate).toHaveBeenCalledWith('quiz', [
      { questionId: 'q1', selectedOptionId: 'a1' },
      { questionId: 'q2', selectedOptionId: null },
    ]);
    expect((await screen.findByText('1 of 2 correct')).textContent).toBe(
      '1 of 2 correct',
    );
    expect(screen.getAllByText('✓ Correct answer')).toHaveLength(2);
  });

  it('keeps answers and the submit button available after a grading failure', async () => {
    const user = userEvent.setup();
    const evaluate = vi
      .fn()
      .mockRejectedValueOnce(new Error('Grading unavailable'))
      .mockResolvedValue(grade);
    render(
      <PlayQuiz
        api={api({ play: vi.fn().mockResolvedValue(detail), evaluate })}
        quizId="quiz"
        onExit={vi.fn()}
      />,
    );
    await user.click(await screen.findByRole('radio', { name: 'GET' }));
    await user.click(screen.getByRole('button', { name: /Next question/ }));
    await user.click(
      screen.getByRole('button', { name: 'Submit for grading' }),
    );
    expect((await screen.findByRole('alert')).textContent).toBe(
      'Grading unavailable',
    );
    await user.click(
      screen.getByRole('button', { name: 'Submit for grading' }),
    );
    await screen.findByText('1 of 2 correct');
    expect(evaluate.mock.calls[1][1][0].selectedOptionId).toBe('a1');
  });
});

describe('AI generation', () => {
  it('preserves source text and reports an error without switching providers or retrying automatically', async () => {
    const user = userEvent.setup();
    const generate = vi
      .fn()
      .mockRejectedValue(new Error('Ollama request failed'));
    render(
      <QuizGenerator
        api={api({ generate })}
        onGenerated={vi.fn()}
        onCancel={vi.fn()}
      />,
    );
    const source =
      'The HTTP GET method reads a representation of a resource. '.repeat(5);
    fireEvent.change(screen.getByLabelText('Source text'), {
      target: { value: source },
    });
    await user.click(screen.getByRole('button', { name: /Generate quiz/ }));
    expect((await screen.findByRole('alert')).textContent).toBe(
      'Ollama request failed',
    );
    expect(
      (screen.getByLabelText('Source text') as HTMLTextAreaElement).value,
    ).toBe(source);
    expect(generate).toHaveBeenCalledTimes(1);
    expect(generate).toHaveBeenCalledWith({
      sourceText: source.trim(),
      questionCount: 5,
    });
    expect(
      (
        screen.getByRole('button', {
          name: /Generate quiz/,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(false);
  });
});

describe('quiz management', () => {
  const summary: QuizSummary = {
    id: 'quiz',
    title: 'My quiz',
    description: null,
    createdAt: detail.createdAt,
    questionCount: 2,
    canEdit: true,
  };

  it('only exposes owner controls for owned quizzes', async () => {
    const list = vi
      .fn()
      .mockResolvedValue([
        summary,
        { ...summary, id: 'shared', title: 'Shared quiz', canEdit: false },
      ]);
    render(
      <QuizLibrary api={api({ list })} onOpen={vi.fn()} onPlay={vi.fn()} />,
    );
    await screen.findByText('Shared quiz');
    expect(
      screen.getAllByRole('button', { name: 'Edit details' }),
    ).toHaveLength(1);
    expect(screen.getAllByRole('button', { name: 'Delete' })).toHaveLength(1);
  });

  it('saves metadata and reloads the API-backed library', async () => {
    const user = userEvent.setup();
    const list = vi.fn().mockResolvedValue([summary]);
    const update = vi.fn().mockResolvedValue(undefined);
    render(
      <QuizLibrary
        api={api({ list, update })}
        onOpen={vi.fn()}
        onPlay={vi.fn()}
      />,
    );
    await user.click(
      await screen.findByRole('button', { name: 'Edit details' }),
    );
    const dialog = screen.getByRole('dialog');
    await user.clear(within(dialog).getByLabelText('Title'));
    await user.type(within(dialog).getByLabelText('Title'), 'Revised title');
    await user.click(
      within(dialog).getByRole('button', { name: 'Save changes' }),
    );
    await waitFor(() =>
      expect(update).toHaveBeenCalledWith('quiz', {
        title: 'Revised title',
        description: '',
      }),
    );
    await waitFor(() => expect(list).toHaveBeenCalledTimes(2));
  });

  it('requires confirmation before deleting a quiz', async () => {
    const user = userEvent.setup();
    const list = vi.fn().mockResolvedValue([summary]);
    const remove = vi.fn().mockResolvedValue(undefined);
    render(
      <QuizLibrary
        api={api({ list, delete: remove })}
        onOpen={vi.fn()}
        onPlay={vi.fn()}
      />,
    );
    await user.click(await screen.findByRole('button', { name: 'Delete' }));
    expect(remove).not.toHaveBeenCalled();
    await user.click(screen.getByRole('button', { name: 'Delete quiz' }));
    await waitFor(() => expect(remove).toHaveBeenCalledWith('quiz'));
    await waitFor(() => expect(list).toHaveBeenCalledTimes(2));
  });
});

describe('API boundary', () => {
  it('sends the current Microsoft token and surfaces structured server errors', async () => {
    const fetch = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(
        new Response(
          JSON.stringify({
            title: 'Quiz generation failed',
            detail: 'The model did not submit a quiz.',
          }),
          { status: 502 },
        ),
      );
    const client = new QuizApi(
      'http://localhost:5023',
      async () => 'current-token',
    );
    await expect(
      client.generate({ sourceText: 'source', questionCount: 3 }),
    ).rejects.toThrow('The model did not submit a quiz.');
    expect(fetch.mock.calls[0][1]?.headers).toEqual({
      'Content-Type': 'application/json',
      Authorization: 'Bearer current-token',
    });
    expect(fetch).toHaveBeenCalledTimes(1);
  });
});
