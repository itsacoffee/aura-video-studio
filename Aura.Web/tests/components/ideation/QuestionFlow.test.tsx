import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { QuestionFlow } from '../../../src/components/ideation/QuestionFlow';
import type { ClarifyingQuestion } from '../../../src/services/ideationService';

describe('QuestionFlow', () => {
  const mockQuestions: ClarifyingQuestion[] = [
    {
      questionId: 'q1',
      question: 'What is your target audience?',
      context: 'Understanding your audience helps tailor the content',
      questionType: 'open-ended',
    },
    {
      questionId: 'q2',
      question: 'What is your primary goal?',
      context: 'Knowing the goal helps structure the video',
      questionType: 'multiple-choice',
      suggestedAnswers: ['Educate', 'Entertain', 'Sell'],
    },
  ];

  it('renders empty state when no questions provided', () => {
    const onAnswerSubmit = vi.fn();
    render(<QuestionFlow questions={[]} onAnswerSubmit={onAnswerSubmit} />);

    expect(screen.getByText('No questions available')).toBeInTheDocument();
  });

  it('renders loading state when isLoading is true', () => {
    const onAnswerSubmit = vi.fn();
    render(<QuestionFlow questions={[]} onAnswerSubmit={onAnswerSubmit} isLoading={true} />);

    expect(screen.getByText('Generating questions...')).toBeInTheDocument();
  });

  it('renders first question when questions are provided', () => {
    const onAnswerSubmit = vi.fn();
    render(<QuestionFlow questions={mockQuestions} onAnswerSubmit={onAnswerSubmit} />);

    expect(screen.getByText('What is your target audience?')).toBeInTheDocument();
    expect(
      screen.getByText('Understanding your audience helps tailor the content')
    ).toBeInTheDocument();
  });

  it('shows progress indicator with correct count', () => {
    const onAnswerSubmit = vi.fn();
    render(<QuestionFlow questions={mockQuestions} onAnswerSubmit={onAnswerSubmit} />);

    expect(screen.getByText('Answer 2 questions to refine your concept')).toBeInTheDocument();
  });
});
