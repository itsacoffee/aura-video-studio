import { describe, it, expect } from 'vitest';
import { onboardingReducer, initialOnboardingState, canAdvanceStep } from '../onboarding';
import type { PreflightReport } from '../providers';

describe('onboardingReducer', () => {
  it('should return initial state', () => {
    const state = initialOnboardingState;
    expect(state.step).toBe(0);
    expect(state.mode).toBe('free');
    expect(state.selectedTier).toBeNull();
    expect(state.status).toBe('idle');
    expect(state.lastValidation).toBeNull();
    expect(state.errors).toEqual([]);
    expect(state.apiKeys).toEqual({});
    expect(state.apiKeyValidationStatus).toEqual({});
    expect(state.apiKeyErrors).toEqual({});
  });

  it('should have install items with descriptions and default paths', () => {
    const state = initialOnboardingState;

    // Check ffmpeg item
    const ffmpegItem = state.installItems.find((item) => item.id === 'ffmpeg');
    expect(ffmpegItem).toBeDefined();
    expect(ffmpegItem?.description).toBeTruthy();
    expect(ffmpegItem?.defaultPath).toBeTruthy();
    expect(ffmpegItem?.defaultPath).toContain('ffmpeg');

    // Check ollama item
    const ollamaItem = state.installItems.find((item) => item.id === 'ollama');
    expect(ollamaItem).toBeDefined();
    expect(ollamaItem?.description).toBeTruthy();
    expect(ollamaItem?.defaultPath).toBeTruthy();
    expect(ollamaItem?.defaultPath).toContain('ollama');

    // Check stable-diffusion item
    const sdItem = state.installItems.find((item) => item.id === 'stable-diffusion');
    expect(sdItem).toBeDefined();
    expect(sdItem?.description).toBeTruthy();
    expect(sdItem?.defaultPath).toBeTruthy();
    expect(sdItem?.defaultPath).toContain('stable-diffusion');
  });

  it('should handle SET_STEP', () => {
    const state = onboardingReducer(initialOnboardingState, {
      type: 'SET_STEP',
      payload: 2,
    });
    expect(state.step).toBe(2);
  });

  it('should handle SET_MODE', () => {
    const state = onboardingReducer(initialOnboardingState, {
      type: 'SET_MODE',
      payload: 'local',
    });
    expect(state.mode).toBe('local');
  });

  it('should handle START_VALIDATION', () => {
    const state = onboardingReducer(initialOnboardingState, {
      type: 'START_VALIDATION',
    });
    expect(state.status).toBe('validating');
    expect(state.errors).toEqual([]);
    expect(state.lastValidation).toBeNull();
  });

  it('should handle VALIDATION_SUCCESS', () => {
    const report: PreflightReport = {
      ok: true,
      stages: [
        {
          stage: 'Script',
          status: 'pass',
          provider: 'RuleBased',
          message: 'All good',
        },
      ],
    };

    const state = onboardingReducer(initialOnboardingState, {
      type: 'VALIDATION_SUCCESS',
      payload: { report, correlationId: 'test-123' },
    });

    expect(state.status).toBe('valid');
    expect(state.lastValidation).not.toBeNull();
    expect(state.lastValidation?.correlationId).toBe('test-123');
    expect(state.lastValidation?.report).toBe(report);
    expect(state.lastValidation?.failedStages).toEqual([]);
    expect(state.errors).toEqual([]);
  });

  it('should handle VALIDATION_FAILED', () => {
    const report: PreflightReport = {
      ok: false,
      stages: [
        {
          stage: 'Script',
          status: 'fail',
          provider: 'OpenAI',
          message: 'API key not configured',
          hint: 'Add API key in Settings',
        },
        {
          stage: 'TTS',
          status: 'pass',
          provider: 'Windows',
          message: 'All good',
        },
      ],
    };

    const state = onboardingReducer(initialOnboardingState, {
      type: 'VALIDATION_FAILED',
      payload: { report, correlationId: 'test-456' },
    });

    expect(state.status).toBe('invalid');
    expect(state.lastValidation).not.toBeNull();
    expect(state.lastValidation?.correlationId).toBe('test-456');
    expect(state.lastValidation?.failedStages).toHaveLength(1);
    expect(state.lastValidation?.failedStages[0].stage).toBe('Script');
    expect(state.errors).toEqual(['API key not configured']);
  });

  it('should handle START_HARDWARE_DETECTION', () => {
    const state = onboardingReducer(initialOnboardingState, {
      type: 'START_HARDWARE_DETECTION',
    });
    expect(state.isDetectingHardware).toBe(true);
    expect(state.hardware).toBeNull();
  });

  it('should handle HARDWARE_DETECTED', () => {
    const hardware = {
      gpu: 'NVIDIA RTX 3080',
      vram: 10,
      canRunSD: true,
      recommendation: 'Your GPU can run SD!',
    };

    const state = onboardingReducer(initialOnboardingState, {
      type: 'HARDWARE_DETECTED',
      payload: hardware,
    });

    expect(state.isDetectingHardware).toBe(false);
    expect(state.hardware).toEqual(hardware);
  });

  it('should handle HARDWARE_DETECTION_FAILED', () => {
    const state = onboardingReducer(initialOnboardingState, {
      type: 'HARDWARE_DETECTION_FAILED',
      payload: 'Network error',
    });

    expect(state.isDetectingHardware).toBe(false);
    expect(state.hardware).toEqual({
      canRunSD: false,
      recommendation: 'Network error',
    });
  });

  it('should handle START_INSTALL', () => {
    const state = onboardingReducer(initialOnboardingState, {
      type: 'START_INSTALL',
      payload: 'ffmpeg',
    });

    expect(state.status).toBe('installing');
    const ffmpegItem = state.installItems.find((item) => item.id === 'ffmpeg');
    expect(ffmpegItem?.installing).toBe(true);
  });

  it('should handle INSTALL_COMPLETE', () => {
    const installingState = onboardingReducer(initialOnboardingState, {
      type: 'START_INSTALL',
      payload: 'ffmpeg',
    });

    const state = onboardingReducer(installingState, {
      type: 'INSTALL_COMPLETE',
      payload: 'ffmpeg',
    });

    expect(state.status).toBe('installed');
    const ffmpegItem = state.installItems.find((item) => item.id === 'ffmpeg');
    expect(ffmpegItem?.installing).toBe(false);
    expect(ffmpegItem?.installed).toBe(true);
  });

  it('should handle INSTALL_FAILED', () => {
    const installingState = onboardingReducer(initialOnboardingState, {
      type: 'START_INSTALL',
      payload: 'ffmpeg',
    });

    const state = onboardingReducer(installingState, {
      type: 'INSTALL_FAILED',
      payload: { itemId: 'ffmpeg', error: 'Download failed' },
    });

    expect(state.status).toBe('idle');
    const ffmpegItem = state.installItems.find((item) => item.id === 'ffmpeg');
    expect(ffmpegItem?.installing).toBe(false);
    expect(state.errors).toContain('Failed to install ffmpeg: Download failed');
  });

  it('should handle MARK_READY', () => {
    const state = onboardingReducer(initialOnboardingState, {
      type: 'MARK_READY',
    });
    expect(state.status).toBe('ready');
  });

  it('should handle RESET_VALIDATION', () => {
    const validatedState = onboardingReducer(initialOnboardingState, {
      type: 'VALIDATION_SUCCESS',
      payload: {
        report: { ok: true, stages: [] },
        correlationId: 'test-123',
      },
    });

    const state = onboardingReducer(validatedState, {
      type: 'RESET_VALIDATION',
    });

    expect(state.status).toBe('idle');
    expect(state.lastValidation).toBeNull();
    expect(state.errors).toEqual([]);
  });

  it('should handle SET_TIER', () => {
    const state = onboardingReducer(initialOnboardingState, {
      type: 'SET_TIER',
      payload: 'pro',
    });
    expect(state.selectedTier).toBe('pro');
  });

  it('should handle SET_API_KEY', () => {
    const state = onboardingReducer(initialOnboardingState, {
      type: 'SET_API_KEY',
      payload: { provider: 'openai', key: 'sk-test123' },
    });
    expect(state.apiKeys['openai']).toBe('sk-test123');
  });

  it('should handle START_API_KEY_VALIDATION', () => {
    const state = onboardingReducer(initialOnboardingState, {
      type: 'START_API_KEY_VALIDATION',
      payload: 'openai',
    });
    expect(state.apiKeyValidationStatus['openai']).toBe('validating');
    expect(state.apiKeyErrors['openai']).toBe('');
  });

  it('should handle API_KEY_VALID', () => {
    const state = onboardingReducer(initialOnboardingState, {
      type: 'API_KEY_VALID',
      payload: { provider: 'openai', accountInfo: 'Account validated' },
    });
    expect(state.apiKeyValidationStatus['openai']).toBe('valid');
    expect(state.apiKeyErrors['openai']).toBe('');
  });

  it('should handle API_KEY_INVALID', () => {
    const state = onboardingReducer(initialOnboardingState, {
      type: 'API_KEY_INVALID',
      payload: { provider: 'openai', error: 'Invalid key format' },
    });
    expect(state.apiKeyValidationStatus['openai']).toBe('invalid');
    expect(state.apiKeyErrors['openai']).toBe('Invalid key format');
  });
});

describe('canAdvanceStep', () => {
  it('should allow advance when status is valid', () => {
    expect(canAdvanceStep('valid')).toBe(true);
  });

  it('should allow advance when status is ready', () => {
    expect(canAdvanceStep('ready')).toBe(true);
  });

  it('should not allow advance when status is idle', () => {
    expect(canAdvanceStep('idle')).toBe(false);
  });

  it('should not allow advance when status is validating', () => {
    expect(canAdvanceStep('validating')).toBe(false);
  });

  it('should not allow advance when status is invalid', () => {
    expect(canAdvanceStep('invalid')).toBe(false);
  });

  it('should not allow advance when status is installing', () => {
    expect(canAdvanceStep('installing')).toBe(false);
  });
});

describe('State machine transitions', () => {
  it('should follow correct flow: idle → validating → valid → ready', () => {
    let state = initialOnboardingState;
    expect(state.status).toBe('idle');

    // Start validation
    state = onboardingReducer(state, { type: 'START_VALIDATION' });
    expect(state.status).toBe('validating');

    // Validation succeeds
    state = onboardingReducer(state, {
      type: 'VALIDATION_SUCCESS',
      payload: {
        report: { ok: true, stages: [] },
        correlationId: 'test',
      },
    });
    expect(state.status).toBe('valid');

    // Mark ready
    state = onboardingReducer(state, { type: 'MARK_READY' });
    expect(state.status).toBe('ready');
  });

  it('should follow correct flow: idle → validating → invalid', () => {
    let state = initialOnboardingState;
    expect(state.status).toBe('idle');

    // Start validation
    state = onboardingReducer(state, { type: 'START_VALIDATION' });
    expect(state.status).toBe('validating');

    // Validation fails
    state = onboardingReducer(state, {
      type: 'VALIDATION_FAILED',
      payload: {
        report: {
          ok: false,
          stages: [
            {
              stage: 'Test',
              status: 'fail',
              provider: 'TestProvider',
              message: 'Test failed',
            },
          ],
        },
        correlationId: 'test',
      },
    });
    expect(state.status).toBe('invalid');
  });

  it('should follow correct flow: idle → installing → installed', () => {
    let state = initialOnboardingState;
    expect(state.status).toBe('idle');

    // Start install
    state = onboardingReducer(state, {
      type: 'START_INSTALL',
      payload: 'ffmpeg',
    });
    expect(state.status).toBe('installing');

    // Install completes
    state = onboardingReducer(state, {
      type: 'INSTALL_COMPLETE',
      payload: 'ffmpeg',
    });
    expect(state.status).toBe('installed');
  });

  it('should handle retry after validation failure', () => {
    let state = initialOnboardingState;

    // Start validation
    state = onboardingReducer(state, { type: 'START_VALIDATION' });
    expect(state.status).toBe('validating');

    // Validation fails
    state = onboardingReducer(state, {
      type: 'VALIDATION_FAILED',
      payload: {
        report: {
          ok: false,
          stages: [
            {
              stage: 'Script',
              status: 'fail',
              provider: 'OpenAI',
              message: 'API key missing',
            },
          ],
        },
        correlationId: 'test-1',
      },
    });
    expect(state.status).toBe('invalid');

    // User retries validation
    state = onboardingReducer(state, { type: 'START_VALIDATION' });
    expect(state.status).toBe('validating');
    expect(state.errors).toEqual([]); // Errors should be cleared

    // Validation succeeds on retry
    state = onboardingReducer(state, {
      type: 'VALIDATION_SUCCESS',
      payload: {
        report: { ok: true, stages: [] },
        correlationId: 'test-2',
      },
    });
    expect(state.status).toBe('valid');
  });

  it('should preserve state when step changes', () => {
    let state = initialOnboardingState;

    // Set mode
    state = onboardingReducer(state, {
      type: 'SET_MODE',
      payload: 'pro',
    });
    expect(state.mode).toBe('pro');

    // Change step
    state = onboardingReducer(state, {
      type: 'SET_STEP',
      payload: 2,
    });
    expect(state.step).toBe(2);
    expect(state.mode).toBe('pro'); // Mode should be preserved
  });

  it('should handle install failure gracefully', () => {
    let state = initialOnboardingState;

    // Start install
    state = onboardingReducer(state, {
      type: 'START_INSTALL',
      payload: 'ffmpeg',
    });
    expect(state.status).toBe('installing');

    // Install fails
    state = onboardingReducer(state, {
      type: 'INSTALL_FAILED',
      payload: 'ffmpeg',
    });
    expect(state.status).toBe('idle'); // Should return to idle state
  });

  it('should track multiple validation attempts with correlationId', () => {
    let state = initialOnboardingState;

    // First validation
    state = onboardingReducer(state, { type: 'START_VALIDATION' });
    state = onboardingReducer(state, {
      type: 'VALIDATION_FAILED',
      payload: {
        report: { ok: false, stages: [] },
        correlationId: 'attempt-1',
      },
    });
    expect(state.lastValidation?.correlationId).toBe('attempt-1');

    // Second validation
    state = onboardingReducer(state, { type: 'START_VALIDATION' });
    state = onboardingReducer(state, {
      type: 'VALIDATION_SUCCESS',
      payload: {
        report: { ok: true, stages: [] },
        correlationId: 'attempt-2',
      },
    });
    expect(state.lastValidation?.correlationId).toBe('attempt-2');
  });
});
