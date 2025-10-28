/**
 * Tests for Create Wizard request cancellation
 * Verifies that cancellation infrastructure is properly integrated
 */

import { describe, it, expect } from 'vitest';
import { postCancellable, isAbortError } from '../services/api/cancellableRequests';

describe('CreateWizard - Request Cancellation Integration', () => {
  it('should have postCancellable function available', () => {
    expect(postCancellable).toBeDefined();
    expect(typeof postCancellable).toBe('function');
  });

  it('should have isAbortError function available', () => {
    expect(isAbortError).toBeDefined();
    expect(typeof isAbortError).toBe('function');
  });

  it('should correctly identify AbortError', () => {
    const abortError = new Error('Request aborted');
    abortError.name = 'AbortError';
    expect(isAbortError(abortError)).toBe(true);
  });

  it('should correctly identify CanceledError', () => {
    const canceledError = new Error('Request canceled');
    canceledError.name = 'CanceledError';
    expect(isAbortError(canceledError)).toBe(true);
  });

  it('should not identify regular errors as abort errors', () => {
    const regularError = new Error('Regular error');
    expect(isAbortError(regularError)).toBe(false);
  });

  it('postCancellable should return promise and cancel function', () => {
    const { promise, cancel } = postCancellable('https://api.test.com/test', {});
    expect(promise).toBeInstanceOf(Promise);
    expect(typeof cancel).toBe('function');

    // Cancel the request to clean up
    cancel();
  });
});

