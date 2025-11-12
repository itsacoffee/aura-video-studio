import { makeStyles, tokens, Button, Text } from '@fluentui/react-components';
import { Checkmark16Regular, SaveRegular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    alignItems: 'center',
    position: 'relative',
  },
  header: {
    width: '100%',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  stepsContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'center',
    alignItems: 'center',
    width: '100%',
    maxWidth: '600px',
  },
  step: {
    flex: 1,
    height: '4px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: '2px',
    position: 'relative',
    cursor: 'pointer',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    '::after': {
      content: '""',
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      borderRadius: '2px',
      opacity: 0,
      transition: 'opacity 0.3s ease',
    },
    ':hover::after': {
      opacity: 0.1,
      backgroundColor: tokens.colorBrandBackground,
    },
  },
  stepInactive: {
    cursor: 'default',
    ':hover::after': {
      opacity: 0,
    },
  },
  stepActive: {
    background: 'linear-gradient(90deg, #00D4FF 0%, #0EA5E9 50%, #FF6B35 100%)',
    height: '6px',
    boxShadow: `0 0 12px rgba(0, 212, 255, 0.5)`,
  },
  stepCompleted: {
    backgroundColor: tokens.colorPaletteGreenBackground2,
    cursor: 'pointer',
    height: '5px',
  },
  stepLabel: {
    position: 'absolute',
    top: '12px',
    left: '50%',
    transform: 'translateX(-50%)',
    whiteSpace: 'nowrap',
    fontSize: tokens.fontSizeBase200,
    transition: 'all 0.2s ease',
  },
  stepLabelActive: {
    transform: 'translateX(-50%) scale(1.1)',
    fontWeight: tokens.fontWeightSemibold,
  },
  checkmark: {
    position: 'absolute',
    top: '-10px',
    left: '50%',
    transform: 'translateX(-50%)',
    color: tokens.colorPaletteGreenForeground1,
    animation: 'checkmarkBounce 0.5s cubic-bezier(0.68, -0.55, 0.265, 1.55)',
  },
  '@keyframes checkmarkBounce': {
    '0%': {
      transform: 'translateX(-50%) scale(0)',
      opacity: 0,
    },
    '50%': {
      transform: 'translateX(-50%) scale(1.2)',
    },
    '100%': {
      transform: 'translateX(-50%) scale(1)',
      opacity: 1,
    },
  },
});

export interface WizardProgressProps {
  currentStep: number;
  totalSteps: number;
  stepLabels: string[];
  onStepClick?: (step: number) => void;
  onSaveAndExit?: () => void;
}

export function WizardProgress({
  currentStep,
  totalSteps,
  stepLabels,
  onStepClick,
  onSaveAndExit,
}: WizardProgressProps) {
  const styles = useStyles();

  const handleStepClick = (index: number) => {
    // Only allow clicking on completed steps
    if (index < currentStep && onStepClick) {
      onStepClick(index);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div style={{ width: '120px' }}>{/* Spacer for alignment */}</div>
        <Text weight="semibold">
          Step {currentStep + 1} of {totalSteps}
        </Text>
        {onSaveAndExit && (
          <Button appearance="subtle" icon={<SaveRegular />} onClick={onSaveAndExit} size="small">
            Save and Exit
          </Button>
        )}
      </div>

      <div className={styles.stepsContainer}>
        {Array.from({ length: totalSteps }).map((_, index) => {
          const isCompleted = index < currentStep;
          const isActive = index === currentStep;
          const canClick = isCompleted && onStepClick;

          return (
            <div
              key={index}
              className={`${styles.step} ${
                isActive ? styles.stepActive : ''
              } ${isCompleted ? styles.stepCompleted : ''} ${!canClick ? styles.stepInactive : ''}`}
              onClick={() => handleStepClick(index)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  handleStepClick(index);
                }
              }}
              role={canClick ? 'button' : 'progressbar'}
              tabIndex={canClick ? 0 : -1}
              aria-label={stepLabels[index]}
              aria-current={isActive ? 'step' : undefined}
            >
              {isCompleted && (
                <div className={styles.checkmark}>
                  <Checkmark16Regular />
                </div>
              )}
              <div className={`${styles.stepLabel} ${isActive ? styles.stepLabelActive : ''}`}>
                <Text size={100} weight={isActive ? 'semibold' : 'regular'}>
                  {stepLabels[index]}
                </Text>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
