import { makeStyles, tokens, Title2, Title3, Text, Button, Card } from '@fluentui/react-components';
import {
  ChevronRight24Regular,
  ChevronLeft24Regular,
  Dismiss24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';

const useStyles = makeStyles({
  overlay: {
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    zIndex: 1000,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    animation: 'fadeIn 0.3s ease-in-out',
  },
  '@keyframes fadeIn': {
    from: { opacity: 0 },
    to: { opacity: 1 },
  },
  container: {
    position: 'relative',
    width: '100%',
    height: '100%',
    padding: tokens.spacingVerticalXXL,
  },
  tooltip: {
    position: 'absolute',
    maxWidth: '400px',
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusLarge,
    boxShadow: tokens.shadow64,
    animation: 'slideIn 0.4s ease-out',
  },
  '@keyframes slideIn': {
    from: {
      opacity: 0,
      transform: 'translateY(20px)',
    },
    to: {
      opacity: 1,
      transform: 'translateY(0)',
    },
  },
  tooltipHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: tokens.spacingVerticalM,
  },
  tooltipTitle: {
    marginBottom: tokens.spacingVerticalXS,
  },
  tooltipIcon: {
    fontSize: '48px',
    marginBottom: tokens.spacingVerticalM,
    display: 'block',
  },
  tooltipDescription: {
    marginBottom: tokens.spacingVerticalL,
    lineHeight: '1.5',
  },
  tooltipFeatures: {
    listStyleType: 'none',
    padding: 0,
    margin: `${tokens.spacingVerticalM} 0`,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  tooltipFooter: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalL,
    paddingTop: tokens.spacingVerticalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  progressDots: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  dot: {
    width: '8px',
    height: '8px',
    borderRadius: '50%',
    backgroundColor: tokens.colorNeutralBackground5,
    transition: 'all 0.3s ease-in-out',
  },
  activeDot: {
    backgroundColor: tokens.colorBrandBackground,
    width: '24px',
    borderRadius: '4px',
  },
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  highlight: {
    position: 'absolute',
    border: `3px solid ${tokens.colorBrandBackground}`,
    borderRadius: tokens.borderRadiusLarge,
    animation: 'pulse 2s ease-in-out infinite',
    pointerEvents: 'none',
  },
  '@keyframes pulse': {
    '0%, 100%': {
      boxShadow: `0 0 0 0 ${tokens.colorBrandBackground}`,
    },
    '50%': {
      boxShadow: `0 0 20px 5px ${tokens.colorBrandBackground}`,
    },
  },
  skipAllButton: {
    position: 'absolute',
    top: tokens.spacingVerticalXL,
    right: tokens.spacingVerticalXL,
    zIndex: 1001,
  },
});

export interface TutorialStep {
  id: string;
  title: string;
  description: string;
  icon: string;
  features: string[];
  position: React.CSSProperties;
  highlightArea?: {
    top: string;
    left: string;
    width: string;
    height: string;
  };
}

export interface QuickTutorialProps {
  steps: TutorialStep[];
  onComplete: () => void;
  onSkip: () => void;
}

export function QuickTutorial({ steps, onComplete, onSkip }: QuickTutorialProps) {
  const styles = useStyles();
  const [currentStep, setCurrentStep] = useState(0);

  const handleNext = () => {
    if (currentStep < steps.length - 1) {
      setCurrentStep(currentStep + 1);
    } else {
      onComplete();
    }
  };

  const handleBack = () => {
    if (currentStep > 0) {
      setCurrentStep(currentStep - 1);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      onSkip();
    } else if (e.key === 'ArrowRight' || e.key === 'Enter') {
      handleNext();
    } else if (e.key === 'ArrowLeft') {
      handleBack();
    }
  };

  const currentTutorialStep = steps[currentStep];

  if (!currentTutorialStep) {
    return null;
  }

  return (
    <div
      className={styles.overlay}
      onClick={onSkip}
      onKeyDown={handleKeyDown}
      role="dialog"
      aria-modal="true"
      aria-labelledby="tutorial-title"
      tabIndex={0}
    >
      <Button
        className={styles.skipAllButton}
        appearance="subtle"
        onClick={onSkip}
        icon={<Dismiss24Regular />}
      >
        Skip Tutorial
      </Button>

      <div className={styles.container}>
        {/* Highlight Area */}
        {currentTutorialStep.highlightArea && (
          <div
            className={styles.highlight}
            style={{
              top: currentTutorialStep.highlightArea.top,
              left: currentTutorialStep.highlightArea.left,
              width: currentTutorialStep.highlightArea.width,
              height: currentTutorialStep.highlightArea.height,
            }}
          />
        )}

        {/* Tooltip */}
        <Card
          className={styles.tooltip}
          style={currentTutorialStep.position}
          onClick={(e) => e.stopPropagation()}
        >
          <div className={styles.tooltipHeader}>
            <div>
              <Title2 id="tutorial-title" className={styles.tooltipTitle}>
                {currentTutorialStep.title}
              </Title2>
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                Step {currentStep + 1} of {steps.length}
              </Text>
            </div>
          </div>

          <div className={styles.tooltipIcon}>{currentTutorialStep.icon}</div>

          <Text className={styles.tooltipDescription} size={400}>
            {currentTutorialStep.description}
          </Text>

          {currentTutorialStep.features.length > 0 && (
            <>
              <Title3 style={{ marginBottom: tokens.spacingVerticalS }}>Key Features:</Title3>
              <ul className={styles.tooltipFeatures}>
                {currentTutorialStep.features.map((feature, index) => (
                  <li key={index}>
                    <Text size={300}>✓ {feature}</Text>
                  </li>
                ))}
              </ul>
            </>
          )}

          <div className={styles.tooltipFooter}>
            <div className={styles.progressDots}>
              {steps.map((_, index) => (
                <div
                  key={index}
                  className={`${styles.dot} ${index === currentStep ? styles.activeDot : ''}`}
                />
              ))}
            </div>

            <div className={styles.buttonGroup}>
              {currentStep > 0 && (
                <Button appearance="secondary" icon={<ChevronLeft24Regular />} onClick={handleBack}>
                  Back
                </Button>
              )}
              <Button
                appearance="primary"
                icon={<ChevronRight24Regular />}
                iconPosition="after"
                onClick={handleNext}
              >
                {currentStep < steps.length - 1 ? 'Next' : 'Get Started'}
              </Button>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}

// Default tutorial steps for the main application
export const defaultTutorialSteps: TutorialStep[] = [
  {
    id: 'media-library',
    title: 'Media Library',
    description:
      'Your content hub where you can browse, import, and manage all your media assets including videos, images, and audio files.',
    icon: '📁',
    features: [
      'Drag and drop files to import',
      'Preview media before adding to timeline',
      'Organize with collections and tags',
      'Search and filter your assets',
    ],
    position: { top: '20%', left: '5%' },
    highlightArea: { top: '10%', left: '2%', width: '20%', height: '70%' },
  },
  {
    id: 'timeline',
    title: 'Timeline Editor',
    description:
      'The heart of your video editing workflow. Arrange clips, adjust timing, and orchestrate your video with precision.',
    icon: '🎬',
    features: [
      'Multi-track editing support',
      'Precise frame-by-frame control',
      'Add transitions and effects',
      'Audio waveform visualization',
    ],
    position: { bottom: '25%', left: '50%', transform: 'translateX(-50%)' } as React.CSSProperties,
    highlightArea: { top: '70%', left: '2%', width: '96%', height: '25%' },
  },
  {
    id: 'preview',
    title: 'Video Preview',
    description:
      'See your edits in real-time with the video preview window. Play, pause, and scrub through your video as you work.',
    icon: '▶️',
    features: [
      'Real-time playback preview',
      'Full-screen viewing mode',
      'Playback controls and scrubbing',
      'Quality settings adjustment',
    ],
    position: { top: '20%', right: '30%' },
    highlightArea: { top: '10%', left: '60%', width: '35%', height: '50%' },
  },
  {
    id: 'effects-panel',
    title: 'Effects & Tools',
    description:
      'Access a powerful collection of effects, transitions, filters, and AI-powered tools to enhance your videos.',
    icon: '✨',
    features: [
      'Visual effects and filters',
      'Transition library',
      'AI-powered enhancements',
      'Color grading tools',
    ],
    position: { top: '20%', right: '5%' },
    highlightArea: { top: '10%', left: '78%', width: '20%', height: '70%' },
  },
  {
    id: 'export',
    title: 'Export & Share',
    description:
      'When you&apos;re ready, export your masterpiece with optimized settings for any platform or device.',
    icon: '📤',
    features: [
      'Platform-specific presets',
      'Custom quality settings',
      'Fast rendering with GPU acceleration',
      'Direct upload to social media',
    ],
    position: { top: '10%', left: '50%', transform: 'translateX(-50%)' } as React.CSSProperties,
  },
];
