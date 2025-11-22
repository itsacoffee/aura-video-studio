import { CheckmarkCircle24Filled } from '@fluentui/react-icons';
import type { FC } from 'react';
import './WizardStepper.css';

interface WizardStepperProps {
  currentStep: number;
  totalSteps: number;
  stepLabels?: string[];
  onStepClick?: (step: number) => void;
}

/**
 * Premium Wizard Stepper with Circular Indicators
 * Features:
 * - Circular step indicators (48px diameter)
 * - Animated progress line
 * - Gradient fills for active/completed steps
 * - Check marks for completed steps
 * - Hover states with tooltips
 * - Responsive (vertical on mobile)
 */
export const WizardStepper: FC<WizardStepperProps> = ({
  currentStep,
  totalSteps,
  stepLabels = [],
  onStepClick,
}) => {
  const steps = Array.from({ length: totalSteps }, (_, i) => i);

  const getStepState = (step: number) => {
    if (step < currentStep) return 'completed';
    if (step === currentStep) return 'active';
    return 'upcoming';
  };

  const handleStepClick = (step: number) => {
    if (onStepClick && step < currentStep) {
      onStepClick(step);
    }
  };

  return (
    <div className="wizard-stepper" role="navigation" aria-label="Wizard progress">
      <div className="wizard-stepper__track">
        {steps.map((step) => {
          const state = getStepState(step);
          const isClickable = step < currentStep;
          const label = stepLabels[step] || `Step ${step + 1}`;

          return (
            <div key={step} className="wizard-stepper__step-container">
              {/* Progress line (before step) */}
              {step > 0 && (
                <div className="wizard-stepper__line-container">
                  <div
                    className={`wizard-stepper__line ${
                      step <= currentStep ? 'wizard-stepper__line--filled' : ''
                    }`}
                  />
                </div>
              )}

              {/* Step indicator */}
              <button
                type="button"
                className={`wizard-stepper__step wizard-stepper__step--${state} ${
                  isClickable ? 'wizard-stepper__step--clickable' : ''
                }`}
                onClick={() => handleStepClick(step)}
                disabled={!isClickable}
                aria-current={state === 'active' ? 'step' : undefined}
                aria-label={`${label}${state === 'completed' ? ' (completed)' : ''}`}
                title={label}
              >
                <div className="wizard-stepper__step-inner">
                  {state === 'completed' ? (
                    <CheckmarkCircle24Filled className="wizard-stepper__checkmark" />
                  ) : (
                    <span className="wizard-stepper__step-number">{step + 1}</span>
                  )}
                </div>

                {state === 'active' && <div className="wizard-stepper__pulse-ring" />}
              </button>

              {/* Step label */}
              <div className="wizard-stepper__label">
                <span className={`wizard-stepper__label-text wizard-stepper__label-text--${state}`}>
                  {label}
                </span>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
