import type { FC, ReactNode } from 'react';
import { useEffect } from 'react';
import { guidedModeService } from '../../services/guidedModeService';
import { useGuidedMode } from '../../state/guidedMode';
import { ExplanationPanel } from './ExplanationPanel';
import { PromptDiffModal } from './PromptDiffModal';

export interface GuidedModeProviderProps {
  children: ReactNode;
  enabled?: boolean;
}

/**
 * Provider component that wraps pages with guided mode features
 * Adds explanation panels, prompt diff modals, and telemetry
 */
export const GuidedModeProvider: FC<GuidedModeProviderProps> = ({ children, enabled = true }) => {
  const { config, setConfig } = useGuidedMode();

  useEffect(() => {
    if (enabled && config.enabled) {
      guidedModeService
        .getConfig()
        .then((remoteConfig) => {
          setConfig(remoteConfig);
        })
        .catch((error: unknown) => {
          console.error('Failed to load guided mode config:', error);
        });
    }
  }, [enabled, config.enabled, setConfig]);

  if (!enabled || !config.enabled) {
    return <>{children}</>;
  }

  return (
    <>
      {children}
      <ExplanationPanel />
      <PromptDiffModal />
    </>
  );
};
