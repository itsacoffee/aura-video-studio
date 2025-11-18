import {
  makeStyles,
  tokens,
  Text,
  Button,
  Field,
  Input,
  Textarea,
  Radio,
  RadioGroup,
  Card,
  ProgressBar,
  Spinner,
} from '@fluentui/react-components';
import {
  ArrowRightRegular,
  ArrowLeftRegular,
  CheckmarkCircleRegular,
  DocumentRegular,
  LightbulbRegular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import React, { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { QuestionFlow } from '../../components/ideation/QuestionFlow';
import { ErrorState } from '../../components/Loading';
import {
  ideationService,
  type BriefRequirements,
  type ClarifyingQuestion,
} from '../../services/ideationService';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1000px',
    margin: '0 auto',
    minHeight: '100vh',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalXL,
  },
  icon: {
    fontSize: '32px',
    color: tokens.colorBrandForeground1,
  },
  title: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightSemibold,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
  progressSection: {
    marginBottom: tokens.spacingVerticalXL,
  },
  progressHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  stepCard: {
    padding: tokens.spacingVerticalXL,
    marginBottom: tokens.spacingVerticalL,
  },
  stepHeader: {
    marginBottom: tokens.spacingVerticalL,
  },
  stepTitle: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXS,
  },
  stepDescription: {
    color: tokens.colorNeutralForeground3,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  actions: {
    display: 'flex',
    justifyContent: 'space-between',
    marginTop: tokens.spacingVerticalL,
    gap: tokens.spacingHorizontalM,
  },
  summaryCard: {
    padding: tokens.spacingVerticalXL,
  },
  summarySection: {
    marginBottom: tokens.spacingVerticalL,
  },
  summaryLabel: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground3,
    marginBottom: tokens.spacingVerticalXXS,
  },
  summaryValue: {
    fontSize: tokens.fontSizeBase400,
  },
  completionCard: {
    padding: tokens.spacingVerticalXXL,
    textAlign: 'center',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalM,
  },
  completionIcon: {
    fontSize: '64px',
    color: tokens.colorPaletteGreenForeground1,
  },
  completionActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
  loadingState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalXXXL,
  },
});

interface Step {
  id: number;
  title: string;
  description: string;
}

const steps: Step[] = [
  {
    id: 1,
    title: 'Topic & Purpose',
    description: 'What is your video about and what do you want to achieve?',
  },
  {
    id: 2,
    title: 'Audience & Tone',
    description: 'Who is your target audience and what tone should the video have?',
  },
  {
    id: 3,
    title: 'Video Details',
    description: 'Technical details about your video',
  },
  {
    id: 4,
    title: 'AI Questions',
    description: 'Answer clarifying questions to improve your brief',
  },
  {
    id: 5,
    title: 'Review & Complete',
    description: 'Review your brief and start creating',
  },
];

export const BriefBuilder: FC = () => {
  const styles = useStyles();
  const navigate = useNavigate();

  const [currentStep, setCurrentStep] = useState(1);
  const [brief, setBrief] = useState<Partial<BriefRequirements>>({});
  const [questions, setQuestions] = useState<ClarifyingQuestion[]>([]);
  const [isLoadingQuestions, setIsLoadingQuestions] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const progress = (currentStep / steps.length) * 100;

  const updateBrief = useCallback((updates: Partial<BriefRequirements>) => {
    setBrief((prev) => ({ ...prev, ...updates }));
  }, []);

  const handleNext = useCallback(async () => {
    if (currentStep === 3) {
      setIsLoadingQuestions(true);
      setError(null);

      try {
        const response = await ideationService.expandBrief({
          projectId: crypto.randomUUID(),
          currentBrief: brief as BriefRequirements,
        });

        if (response.success && response.questions) {
          setQuestions(response.questions);
        }
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to generate questions';
        setError(errorMessage);
      } finally {
        setIsLoadingQuestions(false);
      }
    }

    setCurrentStep((prev) => Math.min(prev + 1, steps.length));
  }, [currentStep, brief]);

  const handleBack = useCallback(() => {
    setCurrentStep((prev) => Math.max(prev - 1, 1));
  }, []);

  const handleAnswerQuestion = useCallback((questionId: string, answer: string) => {
    // Store answer for later processing
    console.info('Question answered:', { questionId, answer });
  }, []);

  const handleComplete = useCallback(() => {
    navigate('/ideation', {
      state: { brief },
    });
  }, [navigate, brief]);

  const handleGenerateVideo = useCallback(() => {
    navigate('/create', {
      state: { brief },
    });
  }, [navigate, brief]);

  const canProceed = (): boolean => {
    switch (currentStep) {
      case 1:
        return !!(brief.topic && brief.goal);
      case 2:
        return !!(brief.audience && brief.tone);
      case 3:
        return !!(brief.durationSeconds && brief.platform);
      default:
        return true;
    }
  };

  if (error) {
    return (
      <div className={styles.container}>
        <ErrorState
          title="Error"
          message={error}
          onRetry={() => {
            setError(null);
            setCurrentStep(3);
          }}
        />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <DocumentRegular className={styles.icon} />
        <div>
          <Text className={styles.title}>Brief Builder</Text>
          <div>
            <Text className={styles.subtitle}>
              Create a comprehensive video brief with AI assistance
            </Text>
          </div>
        </div>
      </div>

      <div className={styles.progressSection}>
        <div className={styles.progressHeader}>
          <Text size={300} weight="semibold">
            Step {currentStep} of {steps.length}
          </Text>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            {Math.round(progress)}% complete
          </Text>
        </div>
        <ProgressBar value={progress / 100} color="brand" thickness="large" />
      </div>

      {currentStep === 1 && (
        <Card className={styles.stepCard}>
          <div className={styles.stepHeader}>
            <Text className={styles.stepTitle}>{steps[0].title}</Text>
            <Text className={styles.stepDescription}>{steps[0].description}</Text>
          </div>
          <div className={styles.form}>
            <Field label="Topic" required>
              <Input
                value={brief.topic || ''}
                onChange={(_, data) => updateBrief({ topic: data.value })}
                placeholder="e.g., How to train a neural network"
              />
            </Field>
            <Field label="Goal" required>
              <Textarea
                value={brief.goal || ''}
                onChange={(_, data) => updateBrief({ goal: data.value })}
                placeholder="What do you want to achieve with this video?"
                rows={3}
              />
            </Field>
            <Field label="Keywords (optional)">
              <Input
                value={brief.keywords?.join(', ') || ''}
                onChange={(_, data) =>
                  updateBrief({ keywords: data.value.split(',').map((k) => k.trim()) })
                }
                placeholder="Comma-separated keywords for SEO"
              />
            </Field>
          </div>
          <div className={styles.actions}>
            <Button
              appearance="subtle"
              icon={<ArrowLeftRegular />}
              onClick={() => navigate('/ideation')}
            >
              Cancel
            </Button>
            <Button
              appearance="primary"
              icon={<ArrowRightRegular />}
              iconPosition="after"
              onClick={handleNext}
              disabled={!canProceed()}
            >
              Next
            </Button>
          </div>
        </Card>
      )}

      {currentStep === 2 && (
        <Card className={styles.stepCard}>
          <div className={styles.stepHeader}>
            <Text className={styles.stepTitle}>{steps[1].title}</Text>
            <Text className={styles.stepDescription}>{steps[1].description}</Text>
          </div>
          <div className={styles.form}>
            <Field label="Target Audience" required>
              <Input
                value={brief.audience || ''}
                onChange={(_, data) => updateBrief({ audience: data.value })}
                placeholder="e.g., Beginner programmers, Business professionals"
              />
            </Field>
            <Field label="Tone" required>
              <RadioGroup
                value={brief.tone || ''}
                onChange={(_, data) => updateBrief({ tone: data.value })}
              >
                <Radio value="Professional" label="Professional" />
                <Radio value="Casual" label="Casual" />
                <Radio value="Humorous" label="Humorous" />
                <Radio value="Educational" label="Educational" />
                <Radio value="Inspirational" label="Inspirational" />
              </RadioGroup>
            </Field>
          </div>
          <div className={styles.actions}>
            <Button appearance="subtle" icon={<ArrowLeftRegular />} onClick={handleBack}>
              Back
            </Button>
            <Button
              appearance="primary"
              icon={<ArrowRightRegular />}
              iconPosition="after"
              onClick={handleNext}
              disabled={!canProceed()}
            >
              Next
            </Button>
          </div>
        </Card>
      )}

      {currentStep === 3 && (
        <Card className={styles.stepCard}>
          <div className={styles.stepHeader}>
            <Text className={styles.stepTitle}>{steps[2].title}</Text>
            <Text className={styles.stepDescription}>{steps[2].description}</Text>
          </div>
          <div className={styles.form}>
            <Field label="Duration (seconds)" required>
              <Input
                type="number"
                value={brief.durationSeconds?.toString() || ''}
                onChange={(_, data) => updateBrief({ durationSeconds: parseInt(data.value) || 60 })}
                placeholder="60"
              />
            </Field>
            <Field label="Platform" required>
              <RadioGroup
                value={brief.platform || ''}
                onChange={(_, data) => updateBrief({ platform: data.value })}
              >
                <Radio value="YouTube" label="YouTube" />
                <Radio value="TikTok" label="TikTok" />
                <Radio value="Instagram" label="Instagram" />
                <Radio value="LinkedIn" label="LinkedIn" />
                <Radio value="General" label="General / Multiple platforms" />
              </RadioGroup>
            </Field>
          </div>
          <div className={styles.actions}>
            <Button appearance="subtle" icon={<ArrowLeftRegular />} onClick={handleBack}>
              Back
            </Button>
            <Button
              appearance="primary"
              icon={<ArrowRightRegular />}
              iconPosition="after"
              onClick={handleNext}
              disabled={!canProceed()}
            >
              Next
            </Button>
          </div>
        </Card>
      )}

      {currentStep === 4 && (
        <Card className={styles.stepCard}>
          <div className={styles.stepHeader}>
            <Text className={styles.stepTitle}>{steps[3].title}</Text>
            <Text className={styles.stepDescription}>{steps[3].description}</Text>
          </div>
          {isLoadingQuestions ? (
            <div className={styles.loadingState}>
              <Spinner size="extra-large" />
              <Text size={400}>Generating questions...</Text>
            </div>
          ) : (
            <>
              <QuestionFlow
                questions={questions}
                onAnswerSubmit={handleAnswerQuestion}
                onComplete={handleNext}
                showConversation={false}
              />
              <div className={styles.actions}>
                <Button appearance="subtle" icon={<ArrowLeftRegular />} onClick={handleBack}>
                  Back
                </Button>
                <Button
                  appearance="primary"
                  icon={<ArrowRightRegular />}
                  iconPosition="after"
                  onClick={handleNext}
                >
                  Skip Questions
                </Button>
              </div>
            </>
          )}
        </Card>
      )}

      {currentStep === 5 && (
        <>
          <Card className={styles.summaryCard}>
            <div style={{ marginBottom: tokens.spacingVerticalL }}>
              <Text size={500} weight="semibold">
                Your Brief Summary
              </Text>
            </div>
            <div className={styles.summarySection}>
              <Text className={styles.summaryLabel}>Topic</Text>
              <Text className={styles.summaryValue}>{brief.topic}</Text>
            </div>
            <div className={styles.summarySection}>
              <Text className={styles.summaryLabel}>Goal</Text>
              <Text className={styles.summaryValue}>{brief.goal}</Text>
            </div>
            <div className={styles.summarySection}>
              <Text className={styles.summaryLabel}>Audience</Text>
              <Text className={styles.summaryValue}>{brief.audience}</Text>
            </div>
            <div className={styles.summarySection}>
              <Text className={styles.summaryLabel}>Tone</Text>
              <Text className={styles.summaryValue}>{brief.tone}</Text>
            </div>
            <div className={styles.summarySection}>
              <Text className={styles.summaryLabel}>Duration</Text>
              <Text className={styles.summaryValue}>{brief.durationSeconds} seconds</Text>
            </div>
            <div className={styles.summarySection}>
              <Text className={styles.summaryLabel}>Platform</Text>
              <Text className={styles.summaryValue}>{brief.platform}</Text>
            </div>
            {brief.keywords && brief.keywords.length > 0 && (
              <div className={styles.summarySection}>
                <Text className={styles.summaryLabel}>Keywords</Text>
                <Text className={styles.summaryValue}>{brief.keywords.join(', ')}</Text>
              </div>
            )}
          </Card>

          <Card className={styles.completionCard}>
            <CheckmarkCircleRegular className={styles.completionIcon} />
            <Text size={600} weight="semibold">
              Your brief is complete!
            </Text>
            <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
              You can now explore concepts or start generating your video
            </Text>
            <div className={styles.completionActions}>
              <Button appearance="outline" icon={<LightbulbRegular />} onClick={handleComplete}>
                Explore Concepts
              </Button>
              <Button appearance="primary" onClick={handleGenerateVideo}>
                Generate Video
              </Button>
            </div>
          </Card>

          <div className={styles.actions}>
            <Button appearance="subtle" icon={<ArrowLeftRegular />} onClick={handleBack}>
              Back
            </Button>
            <div />
          </div>
        </>
      )}
    </div>
  );
};

export default BriefBuilder;
