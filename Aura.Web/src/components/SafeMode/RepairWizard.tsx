/**
 * Repair Wizard Component
 * Guides the user through fixing issues and recovering from safe mode
 */

import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  ProgressBar,
  Text,
  Title3,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import type { FC } from 'react';
import './RepairWizard.css';

interface RepairWizardProps {
  open: boolean;
  onClose: () => void;
}

interface RepairStep {
  id: string;
  title: string;
  description: string;
  status: 'pending' | 'running' | 'success' | 'error' | 'skipped';
  errorMessage?: string;
}

export const RepairWizard: FC<RepairWizardProps> = ({ open, onClose }) => {
  const [currentStep, setCurrentStep] = useState(0);
  const [steps, setSteps] = useState<RepairStep[]>([
    {
      id: 'diagnostics',
      title: 'Run Diagnostics',
      description: 'Checking system health and configuration',
      status: 'pending',
    },
    {
      id: 'ffmpeg',
      title: 'Verify FFmpeg',
      description: 'Checking FFmpeg installation',
      status: 'pending',
    },
    {
      id: 'api',
      title: 'Check API Connection',
      description: 'Verifying backend service is responding',
      status: 'pending',
    },
    {
      id: 'providers',
      title: 'Validate Providers',
      description: 'Checking provider configuration',
      status: 'pending',
    },
    {
      id: 'config',
      title: 'Verify Configuration',
      description: 'Checking configuration file integrity',
      status: 'pending',
    },
  ]);

  const [isRepairing, setIsRepairing] = useState(false);

  const updateStepStatus = (
    stepId: string,
    status: RepairStep['status'],
    errorMessage?: string
  ) => {
    setSteps((prevSteps) =>
      prevSteps.map((step) => (step.id === stepId ? { ...step, status, errorMessage } : step))
    );
  };

  const processStep = async (step: RepairStep, index: number) => {
    setCurrentStep(index);
    updateStepStatus(step.id, 'running');

    try {
      const checkMethod = `diagnostics:check${step.id.charAt(0).toUpperCase() + step.id.slice(1)}`;
      const result = (await window.electron!.invoke(checkMethod)) as {
        status: string;
        message: string;
        canFix: boolean;
      };

      if (result.status === 'ok') {
        updateStepStatus(step.id, 'success');
      } else if (result.status === 'warning' && result.canFix) {
        const fixMethod = `diagnostics:fix${step.id.charAt(0).toUpperCase() + step.id.slice(1)}`;
        try {
          await window.electron!.invoke(fixMethod);
          updateStepStatus(step.id, 'success');
        } catch (fixError: unknown) {
          updateStepStatus(
            step.id,
            'error',
            fixError instanceof Error ? fixError.message : String(fixError)
          );
        }
      } else if (result.status === 'warning') {
        updateStepStatus(step.id, 'skipped', result.message);
      } else {
        updateStepStatus(step.id, 'error', result.message);
      }
    } catch (error: unknown) {
      updateStepStatus(step.id, 'error', error instanceof Error ? error.message : String(error));
    }

    await new Promise((resolve) => setTimeout(resolve, 500));
  };

  const runRepair = async () => {
    if (!window.electron) {
      return;
    }

    setIsRepairing(true);

    for (let i = 0; i < steps.length; i++) {
      await processStep(steps[i], i);
    }

    setIsRepairing(false);
    setCurrentStep(steps.length);
  };

  const handleResetConfig = async () => {
    if (!window.electron) {
      return;
    }

    const confirmed = window.confirm(
      'This will delete your configuration file and restart the application.\n\n' +
        'All settings will be reset to defaults. Are you sure?'
    );

    if (confirmed) {
      try {
        await window.electron.invoke('config:deleteAndRestart');
      } catch (error: unknown) {
        alert(
          `Failed to reset configuration: ${error instanceof Error ? error.message : String(error)}`
        );
      }
    }
  };

  const getStepIcon = (status: RepairStep['status']) => {
    switch (status) {
      case 'success':
        return <CheckmarkCircle24Regular className="step-icon-success" />;
      case 'error':
        return <ErrorCircle24Regular className="step-icon-error" />;
      case 'skipped':
        return <Warning24Regular className="step-icon-warning" />;
      default:
        return null;
    }
  };

  const progressPercent = currentStep === steps.length ? 100 : (currentStep / steps.length) * 100;

  const allStepsComplete = currentStep >= steps.length;
  const hasErrors = steps.some((s) => s.status === 'error');

  return (
    <Dialog open={open} onOpenChange={(event, data) => data.open === false && onClose()}>
      <DialogSurface className="repair-wizard">
        <DialogBody>
          <DialogTitle>System Repair Wizard</DialogTitle>
          <DialogContent className="repair-content">
            <div className="repair-progress">
              <Text>
                {isRepairing
                  ? `Repairing... Step ${currentStep + 1} of ${steps.length}`
                  : allStepsComplete
                    ? hasErrors
                      ? 'Repair completed with errors'
                      : 'Repair completed successfully'
                    : 'Ready to begin repair'}
              </Text>
              <ProgressBar value={progressPercent} max={100} />
            </div>

            <div className="repair-steps">
              {steps.map((step, index) => (
                <div
                  key={step.id}
                  className={`repair-step ${step.status} ${index === currentStep ? 'current' : ''}`}
                >
                  <div className="step-header">
                    {getStepIcon(step.status)}
                    <Title3>{step.title}</Title3>
                  </div>
                  <Text>{step.description}</Text>
                  {step.errorMessage && <Text className="step-error">{step.errorMessage}</Text>}
                </div>
              ))}
            </div>

            {allStepsComplete && hasErrors && (
              <div className="repair-options">
                <Text weight="semibold">Some issues could not be automatically fixed.</Text>
                <Text>
                  You can reset the configuration to defaults and restart the application. This will
                  clear all settings and may resolve persistent issues.
                </Text>
              </div>
            )}
          </DialogContent>
          <DialogActions>
            {!isRepairing && !allStepsComplete && (
              <Button appearance="primary" onClick={runRepair}>
                Start Repair
              </Button>
            )}
            {allStepsComplete && hasErrors && (
              <Button appearance="primary" onClick={handleResetConfig}>
                Reset Configuration
              </Button>
            )}
            <Button appearance="secondary" onClick={onClose} disabled={isRepairing}>
              {allStepsComplete ? 'Close' : 'Cancel'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};

export default RepairWizard;
