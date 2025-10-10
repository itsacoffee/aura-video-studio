// Provider state management for preflight checks

export type CheckStatus = 'pass' | 'warn' | 'fail';

export interface StageCheck {
  stage: string;
  status: CheckStatus;
  provider: string;
  message: string;
  hint?: string | null;
}

export interface PreflightReport {
  ok: boolean;
  stages: StageCheck[];
}

export interface ProviderSelectionState {
  selectedProfile: string;
  preflightReport: PreflightReport | null;
  isRunningPreflight: boolean;
  lastCheckTime: Date | null;
}

export const defaultProviderState: ProviderSelectionState = {
  selectedProfile: 'Free-Only',
  preflightReport: null,
  isRunningPreflight: false,
  lastCheckTime: null,
};
