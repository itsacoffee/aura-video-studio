/**
 * Safe Mode Banner Component
 * Displays a prominent warning banner when the application is running in safe mode
 */

import {
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Button,
  Link,
} from '@fluentui/react-components';
import { Warning24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import type { FC } from 'react';
import './SafeModeBanner.css';

interface SafeModeStatus {
  enabled: boolean;
  crashCount: number;
  disabledFeatures: string[];
}

interface SafeModeBannerProps {
  onOpenDiagnostics?: () => void;
}

export const SafeModeBanner: FC<SafeModeBannerProps> = ({ onOpenDiagnostics }) => {
  const [safeModeStatus, setSafeModeStatus] = useState<SafeModeStatus | null>(null);
  const [dismissed, setDismissed] = useState(false);

  useEffect(() => {
    // Check if running in Electron
    if (window.electron) {
      // Listen for safe mode status from main process
      const unsubscribe = window.electron.on('app:safeMode', (status: SafeModeStatus) => {
        setSafeModeStatus(status);
      });

      // Also check current safe mode status
      window.electron
        .invoke('config:isSafeMode')
        .then((isSafeMode: boolean) => {
          if (isSafeMode) {
            Promise.all([window.electron.invoke('config:getCrashCount')])
              .then(([crashCount]) => {
                setSafeModeStatus({
                  enabled: true,
                  crashCount: crashCount as number,
                  disabledFeatures: [
                    'System tray (minimize to tray disabled)',
                    'Auto-updater (manual updates only)',
                    'Protocol handling (deep linking disabled)',
                  ],
                });
              })
              .catch((error: unknown) => {
                console.error('Failed to get safe mode status:', error);
              });
          }
        })
        .catch((error: unknown) => {
          console.error('Failed to check safe mode:', error);
        });

      return () => {
        if (unsubscribe) {
          unsubscribe();
        }
      };
    }
  }, []);

  if (!safeModeStatus?.enabled || dismissed) {
    return null;
  }

  return (
    <div className="safe-mode-banner-container">
      <MessageBar intent="warning" className="safe-mode-banner" icon={<Warning24Regular />}>
        <MessageBarBody>
          <MessageBarTitle>Safe Mode Active</MessageBarTitle>
          <div className="safe-mode-details">
            <p>
              The application has started in safe mode due to{' '}
              <strong>{safeModeStatus.crashCount} recent crashes</strong>.
            </p>
            <div className="disabled-features">
              <strong>Disabled features:</strong>
              <ul>
                {safeModeStatus.disabledFeatures.map((feature, index) => (
                  <li key={index}>{feature}</li>
                ))}
              </ul>
            </div>
            <p>
              Use the Diagnostics panel to identify and fix issues, or reset configuration to
              defaults. Once issues are resolved, restart the application to exit safe mode.
            </p>
          </div>
        </MessageBarBody>
        <div className="safe-mode-actions">
          {onOpenDiagnostics && (
            <Button appearance="primary" onClick={onOpenDiagnostics}>
              Open Diagnostics
            </Button>
          )}
          <Link
            onClick={() => {
              window.location.hash = '#/settings';
            }}
          >
            Settings
          </Link>
          <Button
            appearance="subtle"
            icon={<Dismiss24Regular />}
            onClick={() => setDismissed(true)}
            aria-label="Dismiss safe mode banner"
          />
        </div>
      </MessageBar>
    </div>
  );
};

export default SafeModeBanner;
