/**
 * Tests for useJobProgress hook
 */

import { renderHook } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import * as jobsApi from '../../features/render/api/jobs';
import { useJobProgress } from '../useJobProgress';

// Mock the jobs API
vi.mock('../../features/render/api/jobs', () => ({
  subscribeToJobEvents: vi.fn(),
  JobEvent: {},
}));

describe('useJobProgress', () => {
  let unsubscribeMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    unsubscribeMock = vi.fn();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should not subscribe when jobId is null', () => {
    const onProgress = vi.fn();
    const subscribeToJobEventsSpy = vi.spyOn(jobsApi, 'subscribeToJobEvents');

    renderHook(() => useJobProgress(null, onProgress));

    expect(subscribeToJobEventsSpy).not.toHaveBeenCalled();
  });

  it('should subscribe when jobId is provided', () => {
    const jobId = 'test-job-123';
    const onProgress = vi.fn();
    const subscribeToJobEventsSpy = vi
      .spyOn(jobsApi, 'subscribeToJobEvents')
      .mockReturnValue(unsubscribeMock);

    renderHook(() => useJobProgress(jobId, onProgress));

    expect(subscribeToJobEventsSpy).toHaveBeenCalledWith(
      jobId,
      expect.any(Function),
      expect.any(Function)
    );
  });

  it('should call onProgress callback when event is received', () => {
    const jobId = 'test-job-123';
    const onProgress = vi.fn();
    const mockEvent: jobsApi.JobEvent = {
      type: 'step-progress',
      data: { step: 'render', progressPct: 50 },
    };

    vi.spyOn(jobsApi, 'subscribeToJobEvents').mockImplementation((_, callback) => {
      // Simulate receiving an event
      callback(mockEvent);
      return unsubscribeMock;
    });

    renderHook(() => useJobProgress(jobId, onProgress));

    expect(onProgress).toHaveBeenCalledWith(mockEvent);
  });

  it('should unsubscribe on cleanup', () => {
    const jobId = 'test-job-123';
    const onProgress = vi.fn();
    const subscribeToJobEventsSpy = vi
      .spyOn(jobsApi, 'subscribeToJobEvents')
      .mockReturnValue(unsubscribeMock);

    const { unmount } = renderHook(() => useJobProgress(jobId, onProgress));

    expect(subscribeToJobEventsSpy).toHaveBeenCalled();
    expect(unsubscribeMock).not.toHaveBeenCalled();

    unmount();

    expect(unsubscribeMock).toHaveBeenCalled();
  });

  it('should resubscribe when jobId changes', () => {
    const jobId1 = 'test-job-123';
    const jobId2 = 'test-job-456';
    const onProgress = vi.fn();
    const subscribeToJobEventsSpy = vi
      .spyOn(jobsApi, 'subscribeToJobEvents')
      .mockReturnValue(unsubscribeMock);

    const { rerender } = renderHook(({ jobId }) => useJobProgress(jobId, onProgress), {
      initialProps: { jobId: jobId1 },
    });

    expect(subscribeToJobEventsSpy).toHaveBeenCalledTimes(1);
    expect(subscribeToJobEventsSpy).toHaveBeenCalledWith(
      jobId1,
      expect.any(Function),
      expect.any(Function)
    );

    // Change jobId
    rerender({ jobId: jobId2 });

    expect(unsubscribeMock).toHaveBeenCalled();
    expect(subscribeToJobEventsSpy).toHaveBeenCalledTimes(2);
    expect(subscribeToJobEventsSpy).toHaveBeenLastCalledWith(
      jobId2,
      expect.any(Function),
      expect.any(Function)
    );
  });

  it('should handle terminal events (job-completed)', () => {
    vi.useFakeTimers();

    const jobId = 'test-job-123';
    const onProgress = vi.fn();
    const completedEvent: jobsApi.JobEvent = {
      type: 'job-completed',
      data: { output: { videoPath: '/path/to/video.mp4', sizeBytes: 1024 } },
    };

    vi.spyOn(jobsApi, 'subscribeToJobEvents').mockImplementation((_, callback) => {
      callback(completedEvent);
      return unsubscribeMock;
    });

    renderHook(() => useJobProgress(jobId, onProgress));

    expect(onProgress).toHaveBeenCalledWith(completedEvent);

    // Should unsubscribe after 1 second delay for terminal events
    expect(unsubscribeMock).not.toHaveBeenCalled();
    vi.advanceTimersByTime(1000);
    expect(unsubscribeMock).toHaveBeenCalled();

    vi.useRealTimers();
  });

  it('should handle terminal events (job-failed)', () => {
    vi.useFakeTimers();

    const jobId = 'test-job-123';
    const onProgress = vi.fn();
    const failedEvent: jobsApi.JobEvent = {
      type: 'job-failed',
      data: { errors: [{ code: 'ERR001', message: 'Test error', remediation: 'Fix it' }] },
    };

    vi.spyOn(jobsApi, 'subscribeToJobEvents').mockImplementation((_, callback) => {
      callback(failedEvent);
      return unsubscribeMock;
    });

    renderHook(() => useJobProgress(jobId, onProgress));

    expect(onProgress).toHaveBeenCalledWith(failedEvent);

    // Should unsubscribe after 1 second delay for terminal events
    expect(unsubscribeMock).not.toHaveBeenCalled();
    vi.advanceTimersByTime(1000);
    expect(unsubscribeMock).toHaveBeenCalled();

    vi.useRealTimers();
  });
});
