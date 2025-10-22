import { describe, it, expect } from 'vitest';
import { onboardingReducer, initialOnboardingState } from '../state/onboarding';
import type { OnboardingState, OnboardingAction } from '../state/onboarding';

describe('Onboarding State Machine with Path Pickers', () => {
  it('should handle attach existing engine flow', () => {
    let state: OnboardingState = { ...initialOnboardingState };

    // Start installation for FFmpeg
    state = onboardingReducer(state, { type: 'START_INSTALL', payload: 'ffmpeg' });
    expect(state.status).toBe('installing');
    expect(state.installItems.find((i) => i.id === 'ffmpeg')?.installing).toBe(true);

    // Complete installation (simulating attach)
    state = onboardingReducer(state, { type: 'INSTALL_COMPLETE', payload: 'ffmpeg' });
    expect(state.status).toBe('installed');
    expect(state.installItems.find((i) => i.id === 'ffmpeg')?.installed).toBe(true);
    expect(state.installItems.find((i) => i.id === 'ffmpeg')?.installing).toBe(false);
  });

  it('should handle skip optional component flow', () => {
    let state: OnboardingState = { ...initialOnboardingState };

    // Mark optional item as complete (skipped)
    state = onboardingReducer(state, { type: 'INSTALL_COMPLETE', payload: 'ollama' });
    expect(state.installItems.find((i) => i.id === 'ollama')?.installed).toBe(true);
  });

  it('should handle install failure for attach', () => {
    let state: OnboardingState = { ...initialOnboardingState };

    // Start installation
    state = onboardingReducer(state, { type: 'START_INSTALL', payload: 'ffmpeg' });
    expect(state.status).toBe('installing');

    // Fail installation (e.g., invalid path)
    state = onboardingReducer(state, {
      type: 'INSTALL_FAILED',
      payload: { itemId: 'ffmpeg', error: 'Invalid installation path' },
    });
    expect(state.status).toBe('idle');
    expect(state.installItems.find((i) => i.id === 'ffmpeg')?.installing).toBe(false);
    expect(state.installItems.find((i) => i.id === 'ffmpeg')?.installed).toBe(false);
    expect(state.errors).toContain('Failed to install ffmpeg: Invalid installation path');
  });

  it('should transition through complete wizard flow with attach', () => {
    let state: OnboardingState = { ...initialOnboardingState };

    // Step 0: Select mode
    state = onboardingReducer(state, { type: 'SET_MODE', payload: 'free' });
    expect(state.mode).toBe('free');

    // Step 1: Hardware detection
    state = onboardingReducer(state, { type: 'START_HARDWARE_DETECTION' });
    expect(state.isDetectingHardware).toBe(true);

    state = onboardingReducer(state, {
      type: 'HARDWARE_DETECTED',
      payload: {
        gpu: 'Intel UHD',
        vram: 2,
        canRunSD: false,
        recommendation: 'Use Free mode',
      },
    });
    expect(state.isDetectingHardware).toBe(false);
    expect(state.hardware?.gpu).toBe('Intel UHD');

    // Step 2: Attach existing FFmpeg
    state = onboardingReducer(state, { type: 'START_INSTALL', payload: 'ffmpeg' });
    state = onboardingReducer(state, { type: 'INSTALL_COMPLETE', payload: 'ffmpeg' });
    expect(state.installItems.find((i) => i.id === 'ffmpeg')?.installed).toBe(true);

    // Step 3: Validation
    state = onboardingReducer(state, { type: 'START_VALIDATION' });
    expect(state.status).toBe('validating');

    state = onboardingReducer(state, {
      type: 'VALIDATION_SUCCESS',
      payload: {
        correlationId: 'test-123',
        report: {
          ok: true,
          stages: [
            { stage: 'Script', status: 'pass', provider: 'RuleBased', message: 'OK' },
            { stage: 'TTS', status: 'pass', provider: 'Windows', message: 'OK' },
            { stage: 'Visuals', status: 'pass', provider: 'Stock', message: 'OK' },
          ],
        },
      },
    });
    expect(state.status).toBe('valid');
    expect(state.lastValidation?.report.ok).toBe(true);

    // Mark as ready
    state = onboardingReducer(state, { type: 'MARK_READY' });
    expect(state.status).toBe('ready');
  });

  it('should handle multiple attach operations', () => {
    let state: OnboardingState = { ...initialOnboardingState };

    // Attach FFmpeg
    state = onboardingReducer(state, { type: 'START_INSTALL', payload: 'ffmpeg' });
    state = onboardingReducer(state, { type: 'INSTALL_COMPLETE', payload: 'ffmpeg' });

    // Attach Stable Diffusion
    state = onboardingReducer(state, { type: 'START_INSTALL', payload: 'stable-diffusion' });
    state = onboardingReducer(state, { type: 'INSTALL_COMPLETE', payload: 'stable-diffusion' });

    // Both should be installed
    expect(state.installItems.find((i) => i.id === 'ffmpeg')?.installed).toBe(true);
    expect(state.installItems.find((i) => i.id === 'stable-diffusion')?.installed).toBe(true);
  });

  it('should handle validation failure with retry', () => {
    let state: OnboardingState = { ...initialOnboardingState };

    // First validation fails
    state = onboardingReducer(state, { type: 'START_VALIDATION' });
    state = onboardingReducer(state, {
      type: 'VALIDATION_FAILED',
      payload: {
        correlationId: 'test-fail',
        report: {
          ok: false,
          stages: [
            {
              stage: 'Script',
              status: 'fail',
              provider: 'OpenAI',
              message: 'API key not configured',
            },
          ],
        },
      },
    });
    expect(state.status).toBe('invalid');
    expect(state.lastValidation?.failedStages).toHaveLength(1);

    // Reset and retry
    state = onboardingReducer(state, { type: 'RESET_VALIDATION' });
    expect(state.status).toBe('idle');
    expect(state.lastValidation).toBeNull();

    // Second validation succeeds
    state = onboardingReducer(state, { type: 'START_VALIDATION' });
    state = onboardingReducer(state, {
      type: 'VALIDATION_SUCCESS',
      payload: {
        correlationId: 'test-success',
        report: {
          ok: true,
          stages: [{ stage: 'Script', status: 'pass', provider: 'OpenAI', message: 'OK' }],
        },
      },
    });
    expect(state.status).toBe('valid');
  });

  it('should maintain state consistency during errors', () => {
    let state: OnboardingState = { ...initialOnboardingState };

    // Install fails for one item
    state = onboardingReducer(state, { type: 'START_INSTALL', payload: 'ffmpeg' });
    state = onboardingReducer(state, {
      type: 'INSTALL_FAILED',
      payload: { itemId: 'ffmpeg', error: 'Network error' },
    });

    // Another item installs successfully
    state = onboardingReducer(state, { type: 'START_INSTALL', payload: 'ollama' });
    state = onboardingReducer(state, { type: 'INSTALL_COMPLETE', payload: 'ollama' });

    // FFmpeg should still be not installed
    expect(state.installItems.find((i) => i.id === 'ffmpeg')?.installed).toBe(false);
    // Ollama should be installed
    expect(state.installItems.find((i) => i.id === 'ollama')?.installed).toBe(true);
    // Error should be recorded
    expect(state.errors).toHaveLength(1);
  });
});
