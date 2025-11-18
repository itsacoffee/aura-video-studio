import {
  Button,
  Field,
  Input,
  Textarea,
  makeStyles,
  tokens,
  Text,
  Card,
  CardHeader,
  Badge,
  Spinner,
  type GriffelStyle,
} from '@fluentui/react-components';
import { LightbulbRegular, ArrowRightRegular, CheckmarkCircleRegular } from '@fluentui/react-icons';
import React, { useState, useCallback } from 'react';
import type { FC } from 'react';

const useStyles = makeStyles({
  wizard: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    maxWidth: '900px',
    margin: '0 auto',
    padding: tokens.spacingVerticalXL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
  },
  icon: {
    fontSize: '32px',
    color: tokens.colorBrandForeground1,
  },
  title: {
    fontSize: tokens.fontSizeHero700,
    fontWeight: tokens.fontWeightSemibold,
  },
  subtitle: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground3,
  },
  step: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  inputSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  variantsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  variantCard: {
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    borderWidth: '2px',
    borderStyle: 'solid',
    borderColor: tokens.colorNeutralStroke1,
    ':hover': {
      borderColor: tokens.colorBrandStroke1,
      boxShadow: tokens.shadow8,
    } as GriffelStyle,
  },
  variantCardSelected: {
    borderColor: tokens.colorBrandForeground1,
    backgroundColor: tokens.colorBrandBackground2Hover,
  },
  variantHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  variantContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
  },
  briefDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  strengthsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    paddingLeft: tokens.spacingHorizontalM,
  },
  actions: {
    display: 'flex',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalL,
  },
  loadingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalXL,
  },
});

export interface IdeaToBriefRequest {
  idea: string;
  targetPlatform?: string;
  audience?: string;
  language?: string;
  variantCount?: number;
  preferredApproaches?: string;
}

export interface Brief {
  topic: string;
  audience: string;
  goal: string;
  tone: string;
  language: string;
  aspect: string;
}

export interface PlanSpec {
  targetDuration: string;
  pacing: string;
  density: string;
  style: string;
}

export interface BriefVariant {
  variantId: string;
  approach: string;
  brief: Brief;
  planSpec: PlanSpec;
  explanation: string;
  suitabilityScore: number;
  strengths?: string[];
  considerations?: string[];
}

interface IdeaWizardProps {
  onComplete: (variant: BriefVariant) => void;
  onCancel?: () => void;
}

const IdeaWizard: FC<IdeaWizardProps> = ({ onComplete, onCancel }) => {
  const styles = useStyles();
  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [loading, setLoading] = useState(false);

  // Step 1: User input
  const [idea, setIdea] = useState('');
  const [platform, setPlatform] = useState('');
  const [audience, setAudience] = useState('');
  const [preferredApproaches, setPreferredApproaches] = useState('');

  // Step 2: Variants
  const [variants, setVariants] = useState<BriefVariant[]>([]);
  const [selectedVariantId, setSelectedVariantId] = useState<string | null>(null);

  const handleGenerate = useCallback(async () => {
    if (!idea.trim()) {
      return;
    }

    setLoading(true);
    try {
      const request: IdeaToBriefRequest = {
        idea: idea.trim(),
        targetPlatform: platform.trim() || undefined,
        audience: audience.trim() || undefined,
        preferredApproaches: preferredApproaches.trim() || undefined,
        variantCount: 3,
        language: 'en-US',
      };

      const response = await fetch('/api/ideation/idea-to-brief', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        throw new Error('Failed to generate brief variants');
      }

      const data = await response.json();
      setVariants(data.variants || []);
      setStep(2);
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Error generating brief variants:', errorObj.message);
      alert('Failed to generate brief variants. Please try again.');
    } finally {
      setLoading(false);
    }
  }, [idea, platform, audience, preferredApproaches]);

  const handleSelectVariant = useCallback((variantId: string) => {
    setSelectedVariantId(variantId);
  }, []);

  const handleContinue = useCallback(() => {
    const selectedVariant = variants.find((v) => v.variantId === selectedVariantId);
    if (selectedVariant) {
      onComplete(selectedVariant);
    }
  }, [variants, selectedVariantId, onComplete]);

  const renderStep1 = () => (
    <div className={styles.step}>
      <Text size={500} weight="semibold">
        Step 1: Describe Your Idea
      </Text>
      <Text>
        Share your video idea in your own words. Be as creative or specific as you like - the AI
        will interpret it and generate structured video concepts.
      </Text>

      <div className={styles.inputSection}>
        <Field label="Your Idea" required>
          <Textarea
            value={idea}
            onChange={(e) => setIdea(e.target.value)}
            placeholder="E.g., 'A video explaining blockchain to my grandma' or 'detective-style investigation of why cats knock things off tables'"
            rows={4}
          />
        </Field>

        <Field
          label="Target Platform (optional)"
          hint="YouTube, TikTok, Instagram, or describe any platform/format"
        >
          <Input
            value={platform}
            onChange={(e) => setPlatform(e.target.value)}
            placeholder="E.g., YouTube, TikTok, LinkedIn, educational website"
          />
        </Field>

        <Field label="Target Audience (optional)" hint="Describe your audience however you like">
          <Input
            value={audience}
            onChange={(e) => setAudience(e.target.value)}
            placeholder="E.g., 'curious teenagers', 'professional developers', 'parents of young kids'"
          />
        </Field>

        <Field
          label="Creative Direction (optional)"
          hint="Guide the AI on what approaches to explore"
        >
          <Textarea
            value={preferredApproaches}
            onChange={(e) => setPreferredApproaches(e.target.value)}
            placeholder="E.g., 'make one funny, one serious' or 'like a documentary' or 'mysterious and intriguing'"
            rows={2}
          />
        </Field>
      </div>

      <div className={styles.actions}>
        {onCancel && (
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        )}
        <Button
          appearance="primary"
          icon={<ArrowRightRegular />}
          iconPosition="after"
          disabled={!idea.trim() || loading}
          onClick={handleGenerate}
        >
          Generate Brief Variants
        </Button>
      </div>
    </div>
  );

  const renderStep2 = () => (
    <div className={styles.step}>
      <Text size={500} weight="semibold">
        Step 2: Choose Your Approach
      </Text>
      <Text>
        Select the brief variant that best matches your creative vision. Each offers a unique take
        on your idea.
      </Text>

      <div className={styles.variantsGrid}>
        {variants.map((variant) => (
          <Card
            key={variant.variantId}
            className={`${styles.variantCard} ${
              selectedVariantId === variant.variantId ? styles.variantCardSelected : ''
            }`}
            onClick={() => handleSelectVariant(variant.variantId)}
          >
            <CardHeader
              header={
                <div className={styles.variantHeader}>
                  <Text weight="semibold">{variant.approach}</Text>
                  {selectedVariantId === variant.variantId && (
                    <CheckmarkCircleRegular color={tokens.colorBrandForeground1} />
                  )}
                </div>
              }
              description={
                <Badge appearance="tint" color="informative">
                  Score: {Math.round(variant.suitabilityScore)}%
                </Badge>
              }
            />
            <div className={styles.variantContent}>
              <Text size={300}>{variant.explanation}</Text>

              <div className={styles.briefDetails}>
                <Text size={200}>
                  <strong>Topic:</strong> {variant.brief.topic}
                </Text>
                <Text size={200}>
                  <strong>Goal:</strong> {variant.brief.goal}
                </Text>
                <Text size={200}>
                  <strong>Tone:</strong> {variant.brief.tone}
                </Text>
                <Text size={200}>
                  <strong>Style:</strong> {variant.planSpec.style}
                </Text>
              </div>

              {variant.strengths && variant.strengths.length > 0 && (
                <div>
                  <Text size={200} weight="semibold">
                    Strengths:
                  </Text>
                  <div className={styles.strengthsList}>
                    {variant.strengths.map((strength, idx) => (
                      <Text key={idx} size={200}>
                        • {strength}
                      </Text>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </Card>
        ))}
      </div>

      <div className={styles.actions}>
        <Button appearance="secondary" onClick={() => setStep(1)}>
          Back
        </Button>
        <Button
          appearance="primary"
          icon={<CheckmarkCircleRegular />}
          iconPosition="after"
          disabled={!selectedVariantId}
          onClick={handleContinue}
        >
          Continue with Selected Brief
        </Button>
      </div>
    </div>
  );

  return (
    <div className={styles.wizard}>
      <div className={styles.header}>
        <LightbulbRegular className={styles.icon} />
        <div>
          <Text className={styles.title}>Idea to Video</Text>
          <Text className={styles.subtitle}>
            Turn your creative idea into a structured video brief
          </Text>
        </div>
      </div>

      {loading ? (
        <div className={styles.loadingContainer}>
          <Spinner size="large" />
          <Text>Generating creative brief variants...</Text>
        </div>
      ) : (
        <>
          {step === 1 && renderStep1()}
          {step === 2 && renderStep2()}
        </>
      )}
    </div>
  );
};

export default IdeaWizard;
