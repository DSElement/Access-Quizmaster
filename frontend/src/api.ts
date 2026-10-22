import type {
  GenerateInput,
  QuizDetail,
  QuizInput,
  QuizResult,
  QuizSummary,
  SubmittedAnswer,
} from './types';

type Problem = {
  title: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

export class QuizApi {
  constructor(
    private readonly baseUrl: string,
    private readonly getToken: () => Promise<string>,
  ) {}

  private async request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const token = await this.getToken();
    const response = await fetch(`${this.baseUrl}${path}`, {
      ...init,
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
    });
    if (!response.ok) {
      if (response.status === 401)
        throw new Error(
          'Your session has expired. Sign out and sign in again.',
        );
      if (response.status === 403)
        throw new Error('Only the owner can change this quiz.');
      if (response.status === 404)
        throw new Error('This quiz no longer exists. Return to the library.');
      const problem = (await response.json()) as Problem;
      throw new Error(
        problem.errors
          ? Object.values(problem.errors).flat().join(' ')
          : (problem.detail ?? problem.title),
      );
    }
    if (response.status === 204) return undefined as T;
    return response.json() as Promise<T>;
  }

  list(signal?: AbortSignal) {
    return this.request<QuizSummary[]>('/quizzes', { signal });
  }
  detail(id: string, signal?: AbortSignal) {
    return this.request<QuizDetail>(`/quizzes/${id}`, { signal });
  }
  play(id: string, signal?: AbortSignal) {
    return this.request<QuizDetail>(`/quizzes/${id}/play`, { signal });
  }
  create(input: QuizInput) {
    return this.request<QuizDetail>('/quizzes', {
      method: 'POST',
      body: JSON.stringify(input),
    });
  }
  update(id: string, input: { title: string; description: string }) {
    return this.request<void>(`/quizzes/${id}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    });
  }
  delete(id: string) {
    return this.request<void>(`/quizzes/${id}`, { method: 'DELETE' });
  }
  generate(input: GenerateInput) {
    return this.request<QuizDetail>('/quizzes/generate', {
      method: 'POST',
      body: JSON.stringify(input),
    });
  }
  evaluate(id: string, answers: SubmittedAnswer[]) {
    return this.request<QuizResult>(`/quizzes/${id}/evaluate`, {
      method: 'POST',
      body: JSON.stringify({ answers }),
    });
  }
}
