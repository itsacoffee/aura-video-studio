import React, { useState } from 'react';
import {
  Button,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
} from '@fluentui/react-components';
import { DeleteRegular, WarningRegular } from '@fluentui/react-icons';
import { StorageManager } from '../../services/StorageManager';

/**
 * ResetButton - Provides UI to completely reset the application
 * 
 * Shows a confirmation dialog before clearing all application data
 * including settings, cached credentials, and other stored state.
 */
export const ResetButton: React.FC = () => {
  const [open, setOpen] = useState(false);
  const [resetting, setResetting] = useState(false);

  const handleReset = async () => {
    setResetting(true);
    try {
      // Clear all storage
      StorageManager.clearAll();
      
      // Wait a moment for storage to clear
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // Reset application
      await StorageManager.resetApplication();
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Failed to reset application:', errorObj.message);
      // Fallback to reload
      window.location.reload();
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => setOpen(data.open)}>
      <DialogTrigger disableButtonEnhancement>
        <Button
          appearance="secondary"
          icon={<DeleteRegular />}
          disabled={resetting}
        >
          Reset Application
        </Button>
      </DialogTrigger>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>
            <WarningRegular style={{ color: 'var(--colorPaletteRedForeground1)' }} />
            {' '}
            Reset Application?
          </DialogTitle>
          <DialogContent>
            <p>This will completely reset the application and delete all data including:</p>
            <ul>
              <li>All settings and preferences</li>
              <li>Cached API keys and credentials</li>
              <li>Recent projects and history</li>
              <li>Downloaded models and assets</li>
            </ul>
            <p><strong>This action cannot be undone!</strong></p>
          </DialogContent>
          <DialogActions>
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="secondary" disabled={resetting}>Cancel</Button>
            </DialogTrigger>
            <Button 
              appearance="primary" 
              onClick={handleReset}
              disabled={resetting}
              style={{ backgroundColor: 'var(--colorPaletteRedBackground3)' }}
            >
              {resetting ? 'Resetting...' : 'Reset Everything'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};
