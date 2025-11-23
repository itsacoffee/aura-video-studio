/**
 * Configuration Gate Component
 *
 * Enforces that required settings are configured before allowing access to features.
 * Redirects users to the Setup Wizard if critical settings are missing or invalid.
 */

import { Spinner, MessageBar, MessageBarBody, Button } from '@fluentui/react-components';
import { Warning24Regular } from '@fluentui/react-icons';
import React, { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { setupApi } from '../services/api/setupApi';
import { hasCompletedFirstRun, getLocalFirstRunStatus } from '../services/firstRunService';
import { validateRequiredSettings } from '../services/settingsValidationService';

interface ConfigurationGateProps {
  children: React.ReactNode;
}

/**
 * Routes that don't require configuration to be complete
 */
const ALLOWED_ROUTES = ['/setup', '/settings', '/logs', '/health'];

/**
 * Check if a route is allowed without setup completion
 */
function isRouteAllowed(pathname: string): boolean {
  return ALLOWED_ROUTES.some((route) => pathname.startsWith(route));
}

export function ConfigurationGate({ children }: ConfigurationGateProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const [isChecking, setIsChecking] = useState(true);
  const [settingsValid, setSettingsValid] = useState(true);
  const [validationError, setValidationError] = useState<string | null>(null);

  useEffect(() => {
    async function checkConfiguration() {
      // Skip check for allowed routes
      if (isRouteAllowed(location.pathname)) {
        setIsChecking(false);
        setSettingsValid(true);
        return;
      }

      try {
        // CRITICAL FIX: Check localStorage first - if user completed wizard, trust that
        // This prevents redirect loops when backend reports incomplete due to provider health issues
        const localFirstRunStatus = getLocalFirstRunStatus();
        console.info('[ConfigurationGate] Local first-run status:', localFirstRunStatus);

        // Check system setup status from backend (primary check)
        let backendReportsIncomplete = false;
        try {
          const systemStatus = await setupApi.getSystemStatus();
          console.info('[ConfigurationGate] Backend system status:', systemStatus);
          if (!systemStatus.isComplete) {
            backendReportsIncomplete = true;
            console.warn('[ConfigurationGate] Backend reports setup incomplete');
          }
        } catch (error) {
          console.warn('[ConfigurationGate] Could not check system setup status, falling back to local check:', error);
          // If backend check fails, trust localStorage
          backendReportsIncomplete = false;
        }

        // CRITICAL FIX: Only redirect if BOTH backend AND localStorage say incomplete
        // If localStorage says completed, trust that (user completed wizard, backend may be out of sync)
        if (backendReportsIncomplete && !localFirstRunStatus) {
          console.warn('[ConfigurationGate] Both backend and localStorage indicate incomplete - redirecting to setup');
          navigate('/setup', { replace: true });
          return;
        } else if (localFirstRunStatus) {
          console.info('[ConfigurationGate] localStorage shows completed - trusting local state over backend');
          // Continue to validate settings but don't redirect
        }

        // Check if first-run is complete (secondary check for backward compatibility)
        const firstRunComplete = await hasCompletedFirstRun();

        if (!firstRunComplete && !localFirstRunStatus) {
          // Only redirect if both checks fail
          console.warn('[ConfigurationGate] First-run check failed and no local status - redirecting to setup');
          navigate('/setup', { replace: true });
          return;
        }

        // Validate required settings
        // CRITICAL FIX: Only validate if user hasn't completed wizard
        // If they completed wizard, allow access even if validation fails (they can fix in Settings)
        const validation = await validateRequiredSettings();

        if (!validation.valid && !localFirstRunStatus) {
          // Only show error if user hasn't completed wizard
          setSettingsValid(false);
          setValidationError(validation.error || 'Required settings are missing or invalid');
        } else {
          // User completed wizard OR validation passed - allow access
          setSettingsValid(true);
          setValidationError(null);
          
          if (!validation.valid && localFirstRunStatus) {
            // Log warning but don't block - user can fix in Settings
            console.warn('[ConfigurationGate] Validation failed but user completed wizard - allowing access');
          }
        }
      } catch (error: unknown) {
        console.error('Configuration check failed:', error);
        // On error, allow access but show warning
        setSettingsValid(true);
        setValidationError(null);
      } finally {
        setIsChecking(false);
      }
    }

    checkConfiguration();
  }, [location.pathname, navigate]);

  // Show loading spinner while checking
  if (isChecking) {
    return (
      <div
        style={{
          height: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          flexDirection: 'column',
          gap: '16px',
        }}
      >
        <Spinner size="large" label="Checking configuration..." />
      </div>
    );
  }

  // Show error banner if settings are invalid
  if (!settingsValid && validationError) {
    return (
      <div style={{ padding: '20px' }}>
        <MessageBar intent="error" icon={<Warning24Regular />}>
          <MessageBarBody>
            <strong>Setup Required:</strong> {validationError}
            <br />
            <Button
              appearance="primary"
              style={{ marginTop: '12px' }}
              onClick={() => navigate('/setup')}
            >
              Go to Setup
            </Button>
          </MessageBarBody>
        </MessageBar>
        {children}
      </div>
    );
  }

  // All checks passed, render children
  return <>{children}</>;
}
