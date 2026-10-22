export type QuizSummary = {
  id: string;
  title: string;
  description: string | null;
  createdAt: string;
  questionCount: number;
  canEdit: boolean;
};

export type PlayQuestion = {
  id: string;
  text: string;
  options: { id: string; text: string }[];
};

export type QuizDetail = Omit<QuizSummary, 'questionCount'> & {
  questions: PlayQuestion[];
};
export type QuestionInput = {
  text: string;
  options: { text: string; isCorrect: boolean }[];
};
export type QuizInput = {
  title: string;
  description?: string;
  questions: QuestionInput[];
};
export type GenerateInput = {
  sourceText: string;
  title?: string;
  questionCount: number;
};
export type SubmittedAnswer = {
  questionId: string;
  selectedOptionId: string | null;
};
export type QuizResult = {
  totalQuestions: number;
  correctCount: number;
  scorePercentage: number;
  results: {
    questionId: string;
    questionText: string;
    selectedOptionId: string | null;
    correctOptionId: string;
    wasCorrect: boolean;
    options: { id: string; text: string; isCorrect: boolean }[];
  }[];
};
