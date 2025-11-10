/**
 * Video Generation Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useVideoGenerationStore } from '../videoGenerationStore';

describe('VideoGenerationStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useVideoGenerationStore.setState({
      activeJobs: new Map(),
      jobHistory: [],
      isGenerating: false,
      currentJobId: null,
    });
  });

  it('should start a new job', () => {
    const { startJob, activeJobs, isGenerating, currentJobId } = useVideoGenerationStore.getState();

    startJob('job-1', {
      topic: 'Test Video',
      brief: {},
      planSpec: {},
    });

    const state = useVideoGenerationStore.getState();
    expect(state.activeJobs.size).toBe(1);
    expect(state.isGenerating).toBe(true);
    expect(state.currentJobId).toBe('job-1');
  });

  it('should update job progress', () => {
    const { startJob, updateJobProgress } = useVideoGenerationStore.getState();

    startJob('job-1', {
      topic: 'Test Video',
      brief: {},
      planSpec: {},
    });

    updateJobProgress('job-1', 50, 'Processing');

    const state = useVideoGenerationStore.getState();
    const job = state.activeJobs.get('job-1');
    
    expect(job?.progress).toBe(50);
    expect(job?.stage).toBe('Processing');
    expect(job?.status).toBe('Running');
  });

  it('should complete a job successfully', () => {
    const { startJob, completeJob } = useVideoGenerationStore.getState();

    startJob('job-1', {
      topic: 'Test Video',
      brief: {},
      planSpec: {},
    });

    completeJob('job-1', '/path/to/video.mp4');

    const state = useVideoGenerationStore.getState();
    
    expect(state.activeJobs.size).toBe(0);
    expect(state.jobHistory.length).toBe(1);
    expect(state.jobHistory[0].status).toBe('Done');
    expect(state.jobHistory[0].outputPath).toBe('/path/to/video.mp4');
    expect(state.isGenerating).toBe(false);
  });

  it('should fail a job with error message', () => {
    const { startJob, failJob } = useVideoGenerationStore.getState();

    startJob('job-1', {
      topic: 'Test Video',
      brief: {},
      planSpec: {},
    });

    failJob('job-1', 'Network error');

    const state = useVideoGenerationStore.getState();
    
    expect(state.activeJobs.size).toBe(0);
    expect(state.jobHistory.length).toBe(1);
    expect(state.jobHistory[0].status).toBe('Failed');
    expect(state.jobHistory[0].errorMessage).toBe('Network error');
  });

  it('should cancel a job', () => {
    const { startJob, cancelJob } = useVideoGenerationStore.getState();

    startJob('job-1', {
      topic: 'Test Video',
      brief: {},
      planSpec: {},
    });

    cancelJob('job-1');

    const state = useVideoGenerationStore.getState();
    
    expect(state.activeJobs.size).toBe(0);
    expect(state.jobHistory.length).toBe(1);
    expect(state.jobHistory[0].status).toBe('Canceled');
  });

  it('should handle multiple concurrent jobs', () => {
    const { startJob } = useVideoGenerationStore.getState();

    startJob('job-1', { topic: 'Video 1', brief: {}, planSpec: {} });
    startJob('job-2', { topic: 'Video 2', brief: {}, planSpec: {} });
    startJob('job-3', { topic: 'Video 3', brief: {}, planSpec: {} });

    const state = useVideoGenerationStore.getState();
    
    expect(state.activeJobs.size).toBe(3);
    expect(state.isGenerating).toBe(true);
  });

  it('should maintain history limit', () => {
    const { startJob, completeJob, maxHistorySize } = useVideoGenerationStore.getState();

    // Create more jobs than history limit
    for (let i = 0; i < maxHistorySize + 10; i++) {
      startJob(`job-${i}`, { topic: `Video ${i}`, brief: {}, planSpec: {} });
      completeJob(`job-${i}`, `/path/to/video-${i}.mp4`);
    }

    const state = useVideoGenerationStore.getState();
    
    expect(state.jobHistory.length).toBe(maxHistorySize);
  });

  it('should clear job history', () => {
    const { startJob, completeJob, clearHistory } = useVideoGenerationStore.getState();

    startJob('job-1', { topic: 'Video 1', brief: {}, planSpec: {} });
    completeJob('job-1', '/path/to/video.mp4');

    clearHistory();

    const state = useVideoGenerationStore.getState();
    expect(state.jobHistory.length).toBe(0);
  });
});
