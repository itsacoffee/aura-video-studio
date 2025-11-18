import {
  Button,
  Card,
  Field,
  Textarea,
  makeStyles,
  tokens,
  Text,
  Badge,
  Spinner,
  Radio,
  RadioGroup,
} from '@fluentui/react-components';
import {
  QuestionCircleRegular,
  SendRegular,
  CheckmarkCircleRegular,
  ChatRegular,
} from '@fluentui/react-icons';
import React, { useState, useCallback } from 'react';
import type { FC } from 'react';
import type { ClarifyingQuestion } from '../../services/ideationService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    width: '100%',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  icon: {
    fontSize: '24px',
    color: tokens.colorBrandForeground1,
  },
  questionCard: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  questionHeader: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
  },
  questionContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    flex: 1,
  },
  questionText: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  context: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    fontStyle: 'italic',
  },
  answerSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalS,
  },
  suggestedAnswers: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  actions: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  completedCard: {
    backgroundColor: tokens.colorPaletteGreenBackground2,
    borderColor: tokens.colorPaletteGreenBorder2,
  },
  completedIcon: {
    color: tokens.colorPaletteGreenForeground1,
  },
  conversationFlow: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  message: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
  },
  userMessage: {
    backgroundColor: tokens.colorBrandBackground2,
    alignSelf: 'flex-end',
    maxWidth: '80%',
  },
  aiMessage: {
    backgroundColor: tokens.colorNeutralBackground3,
    alignSelf: 'flex-start',
    maxWidth: '80%',
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground3,
  },
  emptyIcon: {
    fontSize: '48px',
  },
  loadingState: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalL,
  },
});

export interface QuestionFlowProps {
  questions: ClarifyingQuestion[];
  onAnswerSubmit: (questionId: string, answer: string) => void;
  onComplete?: () => void;
  isLoading?: boolean;
  showConversation?: boolean;
}

interface AnsweredQuestion {
  questionId: string;
  question: string;
  answer: string;
}

export const QuestionFlow: FC<QuestionFlowProps> = ({
  questions,
  onAnswerSubmit,
  onComplete,
  isLoading = false,
  showConversation = true,
}) => {
  const styles = useStyles();
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [answeredQuestions, setAnsweredQuestions] = useState<AnsweredQuestion[]>([]);

  const currentQuestion = questions[currentQuestionIndex];
  const isLastQuestion = currentQuestionIndex === questions.length - 1;
  const allQuestionsAnswered = currentQuestionIndex >= questions.length;

  const handleAnswerChange = useCallback((questionId: string, value: string) => {
    setAnswers((prev) => ({ ...prev, [questionId]: value }));
  }, []);

  const handleSubmitAnswer = useCallback(() => {
    if (!currentQuestion) return;

    const answer = answers[currentQuestion.questionId] || '';
    if (!answer.trim()) return;

    setAnsweredQuestions((prev) => [
      ...prev,
      {
        questionId: currentQuestion.questionId,
        question: currentQuestion.question,
        answer,
      },
    ]);

    onAnswerSubmit(currentQuestion.questionId, answer);

    if (isLastQuestion) {
      if (onComplete) {
        onComplete();
      }
    } else {
      setCurrentQuestionIndex((prev) => prev + 1);
    }
  }, [currentQuestion, answers, isLastQuestion, onAnswerSubmit, onComplete]);

  const handleSkip = useCallback(() => {
    if (isLastQuestion && onComplete) {
      onComplete();
    } else {
      setCurrentQuestionIndex((prev) => prev + 1);
    }
  }, [isLastQuestion, onComplete]);

  if (questions.length === 0 && !isLoading) {
    return (
      <div className={styles.emptyState}>
        <ChatRegular className={styles.emptyIcon} />
        <Text size={400}>No questions available</Text>
        <Text size={300}>Start brainstorming to get AI-generated questions</Text>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className={styles.loadingState}>
        <Spinner size="medium" />
        <Text>Generating questions...</Text>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <QuestionCircleRegular className={styles.icon} />
        <div>
          <Text size={500} weight="semibold">
            Interactive Q&A
          </Text>
          <Text size={200} as="div" style={{ color: tokens.colorNeutralForeground3 }}>
            Answer {questions.length} question{questions.length !== 1 ? 's' : ''} to refine your
            concept
          </Text>
        </div>
      </div>

      {showConversation && answeredQuestions.length > 0 && (
        <div className={styles.conversationFlow}>
          {answeredQuestions.map((qa) => (
            <React.Fragment key={qa.questionId}>
              <div className={`${styles.message} ${styles.aiMessage}`}>
                <Text size={300} weight="semibold">
                  AI
                </Text>
                <Text size={200}>{qa.question}</Text>
              </div>
              <div className={`${styles.message} ${styles.userMessage}`}>
                <Text size={300} weight="semibold">
                  You
                </Text>
                <Text size={200}>{qa.answer}</Text>
              </div>
            </React.Fragment>
          ))}
        </div>
      )}

      {!allQuestionsAnswered && currentQuestion && (
        <Card className={styles.questionCard}>
          <div className={styles.questionHeader}>
            <Badge appearance="tint" color="brand">
              {currentQuestionIndex + 1} of {questions.length}
            </Badge>
            <div className={styles.questionContent}>
              <Text className={styles.questionText}>{currentQuestion.question}</Text>
              {currentQuestion.context && (
                <Text className={styles.context}>{currentQuestion.context}</Text>
              )}
            </div>
          </div>

          <div className={styles.answerSection}>
            {currentQuestion.questionType === 'multiple-choice' &&
            currentQuestion.suggestedAnswers &&
            currentQuestion.suggestedAnswers.length > 0 ? (
              <Field label="Select an option">
                <RadioGroup
                  value={answers[currentQuestion.questionId] || ''}
                  onChange={(_, data) => handleAnswerChange(currentQuestion.questionId, data.value)}
                >
                  {currentQuestion.suggestedAnswers.map((option, idx) => (
                    <Radio key={idx} value={option} label={option} />
                  ))}
                </RadioGroup>
              </Field>
            ) : currentQuestion.questionType === 'yes-no' ? (
              <Field label="Choose">
                <RadioGroup
                  value={answers[currentQuestion.questionId] || ''}
                  onChange={(_, data) => handleAnswerChange(currentQuestion.questionId, data.value)}
                >
                  <Radio value="Yes" label="Yes" />
                  <Radio value="No" label="No" />
                </RadioGroup>
              </Field>
            ) : (
              <Field label="Your answer">
                <Textarea
                  value={answers[currentQuestion.questionId] || ''}
                  onChange={(_, data) => handleAnswerChange(currentQuestion.questionId, data.value)}
                  placeholder="Type your answer here..."
                  rows={4}
                />
              </Field>
            )}

            {currentQuestion.suggestedAnswers &&
              currentQuestion.suggestedAnswers.length > 0 &&
              currentQuestion.questionType === 'open-ended' && (
                <div className={styles.suggestedAnswers}>
                  <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                    Suggestions:
                  </Text>
                  {currentQuestion.suggestedAnswers.map((suggestion, idx) => (
                    <Button
                      key={idx}
                      size="small"
                      appearance="outline"
                      onClick={() => handleAnswerChange(currentQuestion.questionId, suggestion)}
                    >
                      {suggestion}
                    </Button>
                  ))}
                </div>
              )}
          </div>

          <div className={styles.actions}>
            <Button appearance="subtle" onClick={handleSkip}>
              Skip
            </Button>
            <Button
              appearance="primary"
              icon={isLastQuestion ? <CheckmarkCircleRegular /> : <SendRegular />}
              onClick={handleSubmitAnswer}
              disabled={!answers[currentQuestion.questionId]?.trim()}
            >
              {isLastQuestion ? 'Complete' : 'Next'}
            </Button>
          </div>
        </Card>
      )}

      {allQuestionsAnswered && (
        <Card className={`${styles.questionCard} ${styles.completedCard}`}>
          <div className={styles.questionHeader}>
            <CheckmarkCircleRegular className={`${styles.icon} ${styles.completedIcon}`} />
            <div className={styles.questionContent}>
              <Text className={styles.questionText}>All questions answered!</Text>
              <Text className={styles.context}>
                Your responses will help refine the concept and generate better content.
              </Text>
            </div>
          </div>
        </Card>
      )}
    </div>
  );
};
