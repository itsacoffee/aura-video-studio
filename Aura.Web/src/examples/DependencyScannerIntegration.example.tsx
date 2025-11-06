/**
 * Example integration of DependencyScanner into FirstRunWizard
 * 
 * This file shows how to replace the existing preflight check in Step 7/8
 * with the new comprehensive DependencyScanner component.
 */

import { DependencyScanner } from '../../components/System/DependencyScanner';
import type { DependencyScanResult } from '../../types/dependency-scan';

// Add to FirstRunWizard.tsx

// Update renderStep7 or create new renderStep8 with dependency validation
const renderDependencyValidation = () => {
  const handleScanComplete = (result: DependencyScanResult) => {
    // Update wizard state based on scan result
    if (result.hasErrors) {
      dispatch({ 
        type: 'VALIDATION_FAILED',
        payload: {
          issues: result.issues,
          scanTime: result.scanTime,
        }
      });
    } else {
      dispatch({ 
        type: 'VALIDATION_SUCCESS',
        payload: {
          scanTime: result.scanTime,
        }
      });
    }
  };

  const handleFixAction = async (actionId: string, issue: DependencyIssue) => {
    switch (actionId) {
      case 'install-ffmpeg':
        // Use existing FFmpeg installation endpoint
        try {
          const response = await fetch('/api/dependencies/ffmpeg/install', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ mode: 'managed' }),
          });
          
          if (!response.ok) {
            throw new Error('FFmpeg installation failed');
          }
          
          // Rescan is handled automatically by DependencyScanner
        } catch (error: unknown) {
          const errorMessage = error instanceof Error ? error.message : 'Unknown error';
          console.error('FFmpeg installation failed:', errorMessage);
          throw error;
        }
        break;

      case 'install-ollama':
        // Open Ollama download page in new tab
        window.open('https://ollama.ai/download', '_blank');
        break;

      case 'update-ffmpeg':
        // Same as install
        await fetch('/api/dependencies/ffmpeg/install', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ mode: 'managed' }),
        });
        break;

      default:
        console.warn('Unknown fix action:', actionId);
    }
  };

  return (
    <div>
      <Title2>System Validation</Title2>
      <Text style={{ marginBottom: tokens.spacingVerticalL }}>
        We&apos;ll check your system to ensure everything is ready for video creation.
      </Text>

      <DependencyScanner
        autoScan={true}
        onScanComplete={handleScanComplete}
        onFixAction={handleFixAction}
        showRescanButton={true}
      />
    </div>
  );
};

// Update the wizard steps array to include the new validation step
const steps = [
  { title: 'Welcome', render: renderStep0 },
  { title: 'Choose Your Tier', render: renderStep1 },
  // ... other steps
  { title: 'System Validation', render: renderDependencyValidation },  // New step
  { title: 'Tutorial', render: renderStep8 },
  { title: 'Complete', render: renderStep9 },
];

// Update navigation logic to handle validation state
const handleNext = () => {
  if (state.step === VALIDATION_STEP_INDEX) {
    // Only allow proceeding if no errors or user confirms
    if (state.validationResult?.hasErrors && !state.userConfirmedProceed) {
      // Show confirmation dialog
      setShowConfirmDialog(true);
      return;
    }
  }
  
  dispatch({ type: 'SET_STEP', payload: state.step + 1 });
};

// Add confirmation dialog for proceeding with errors
const ConfirmProceedDialog = () => (
  <Dialog open={showConfirmDialog} onOpenChange={(_, data) => setShowConfirmDialog(data.open)}>
    <DialogSurface>
      <DialogBody>
        <DialogTitle>Validation Issues Detected</DialogTitle>
        <DialogContent>
          <Text>
            Some system validation checks failed. Proceeding may result in limited functionality
            or errors during video generation.
          </Text>
          <Text weight="semibold" style={{ marginTop: tokens.spacingVerticalM }}>
            Are you sure you want to continue?
          </Text>
        </DialogContent>
        <DialogActions>
          <Button appearance="secondary" onClick={() => setShowConfirmDialog(false)}>
            Go Back and Fix
          </Button>
          <Button 
            appearance="primary" 
            onClick={() => {
              dispatch({ type: 'USER_CONFIRMED_PROCEED' });
              setShowConfirmDialog(false);
              dispatch({ type: 'SET_STEP', payload: state.step + 1 });
            }}
          >
            Continue Anyway
          </Button>
        </DialogActions>
      </DialogBody>
    </DialogSurface>
  </Dialog>
);
