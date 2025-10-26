import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useJobState } from '../jobState';

describe('useJobState - State Transitions and Guards', () => {
  beforeEach(() => {
    // Reset to initial state before each test
    useJobState.setState({
      currentJobId: null,
      status: 'idle',
      progress: 0,
      message: '',
    });

    // Clear console warnings
    vi.spyOn(console, 'warn').mockImplementation(() => {});
  });

  describe('State Initialization', () => {
    it('should initialize with idle state', () => {
      const state = useJobState.getState();
      expect(state.currentJobId).toBeNull();
      expect(state.status).toBe('idle');
      expect(state.progress).toBe(0);
      expect(state.message).toBe('');
    });
  });

  describe('Setting a New Job', () => {
    it('should allow setting a job when idle', () => {
      const state = useJobState.getState();
      state.setJob('job-123');

      const updatedState = useJobState.getState();
      expect(updatedState.currentJobId).toBe('job-123');
      expect(updatedState.status).toBe('running');
      expect(updatedState.progress).toBe(0);
      expect(updatedState.message).toBe('Starting job...');
    });

    it('should prevent setting a job when another is already running', () => {
      const state = useJobState.getState();

      // Start first job
      state.setJob('job-1');

      // Try to start second job
      state.setJob('job-2');

      const updatedState = useJobState.getState();
      expect(updatedState.currentJobId).toBe('job-1'); // Should not change
      expect(console.warn).toHaveBeenCalledWith('Cannot start new job: a job is already running');
    });

    it('should allow setting a new job after previous one completed', () => {
      const state = useJobState.getState();

      // Complete first job
      state.setJob('job-1');
      state.setStatus('completed');

      // Start new job
      state.setJob('job-2');

      const updatedState = useJobState.getState();
      expect(updatedState.currentJobId).toBe('job-2');
      expect(updatedState.status).toBe('running');
    });

    it('should allow setting a new job after previous one failed', () => {
      const state = useJobState.getState();

      // Fail first job
      state.setJob('job-1');
      state.setStatus('failed');

      // Start new job
      state.setJob('job-2');

      const updatedState = useJobState.getState();
      expect(updatedState.currentJobId).toBe('job-2');
      expect(updatedState.status).toBe('running');
    });
  });

  describe('Updating Progress', () => {
    it('should allow progress updates when job is running', () => {
      const state = useJobState.getState();
      state.setJob('job-1');

      state.updateProgress(50, 'Processing...');

      const updatedState = useJobState.getState();
      expect(updatedState.progress).toBe(50);
      expect(updatedState.message).toBe('Processing...');
    });

    it('should prevent progress updates when job is idle', () => {
      const state = useJobState.getState();

      state.updateProgress(50, 'Processing...');

      const updatedState = useJobState.getState();
      expect(updatedState.progress).toBe(0); // Should not change
      expect(console.warn).toHaveBeenCalledWith('Cannot update progress: job is idle');
    });

    it('should prevent progress updates when job is completed', () => {
      const state = useJobState.getState();
      state.setJob('job-1');
      state.setStatus('completed');

      state.updateProgress(75, 'Should not update');

      const updatedState = useJobState.getState();
      expect(updatedState.progress).toBe(0); // Should remain at initial value
      expect(console.warn).toHaveBeenCalledWith('Cannot update progress: job is completed');
    });
  });

  describe('State Transitions', () => {
    it('should allow transition from idle to running', () => {
      const state = useJobState.getState();
      state.setJob('job-1'); // This sets status to running

      const updatedState = useJobState.getState();
      expect(updatedState.status).toBe('running');
    });

    it('should allow transition from running to completed', () => {
      const state = useJobState.getState();
      state.setJob('job-1');
      state.setStatus('completed');

      const updatedState = useJobState.getState();
      expect(updatedState.status).toBe('completed');
    });

    it('should allow transition from running to failed', () => {
      const state = useJobState.getState();
      state.setJob('job-1');
      state.setStatus('failed');

      const updatedState = useJobState.getState();
      expect(updatedState.status).toBe('failed');
    });

    it('should prevent transition from completed to running', () => {
      const state = useJobState.getState();
      state.setJob('job-1');
      state.setStatus('completed');

      // Try to set back to running
      state.setStatus('running');

      const updatedState = useJobState.getState();
      expect(updatedState.status).toBe('completed'); // Should not change
      expect(console.warn).toHaveBeenCalledWith(
        'Invalid state transition from completed to running'
      );
    });

    it('should prevent transition from idle to completed', () => {
      const state = useJobState.getState();

      state.setStatus('completed');

      const updatedState = useJobState.getState();
      expect(updatedState.status).toBe('idle'); // Should not change
      expect(console.warn).toHaveBeenCalledWith('Invalid state transition from idle to completed');
    });

    it('should allow transition from completed to idle via clearJob', () => {
      const state = useJobState.getState();
      state.setJob('job-1');
      state.setStatus('completed');
      state.clearJob();

      const updatedState = useJobState.getState();
      expect(updatedState.status).toBe('idle');
      expect(updatedState.currentJobId).toBeNull();
    });
  });

  describe('Clearing Job', () => {
    it('should allow clearing when job is completed', () => {
      const state = useJobState.getState();
      state.setJob('job-1');
      state.setStatus('completed');
      state.clearJob();

      const updatedState = useJobState.getState();
      expect(updatedState.currentJobId).toBeNull();
      expect(updatedState.status).toBe('idle');
      expect(updatedState.progress).toBe(0);
      expect(updatedState.message).toBe('');
    });

    it('should allow clearing when job is failed', () => {
      const state = useJobState.getState();
      state.setJob('job-1');
      state.setStatus('failed');
      state.clearJob();

      const updatedState = useJobState.getState();
      expect(updatedState.status).toBe('idle');
    });

    it('should prevent clearing when job is running', () => {
      const state = useJobState.getState();
      state.setJob('job-1');

      state.clearJob();

      const updatedState = useJobState.getState();
      expect(updatedState.currentJobId).toBe('job-1'); // Should not change
      expect(updatedState.status).toBe('running'); // Should not change
      expect(console.warn).toHaveBeenCalledWith('Cannot clear job while it is still running');
    });
  });

  describe('canStartNewJob', () => {
    it('should return true when idle', () => {
      const state = useJobState.getState();
      expect(state.canStartNewJob()).toBe(true);
    });

    it('should return false when running', () => {
      const state = useJobState.getState();
      state.setJob('job-1');
      expect(state.canStartNewJob()).toBe(false);
    });

    it('should return true when completed', () => {
      const state = useJobState.getState();
      state.setJob('job-1');
      state.setStatus('completed');
      expect(state.canStartNewJob()).toBe(true);
    });

    it('should return true when failed', () => {
      const state = useJobState.getState();
      state.setJob('job-1');
      state.setStatus('failed');
      expect(state.canStartNewJob()).toBe(true);
    });
  });

  describe('Complete Job Flow', () => {
    it('should handle complete job lifecycle correctly', () => {
      let state = useJobState.getState();

      // Start job
      expect(state.canStartNewJob()).toBe(true);
      state.setJob('job-1');

      state = useJobState.getState();
      expect(state.status).toBe('running');
      expect(state.canStartNewJob()).toBe(false);

      // Update progress
      state.updateProgress(25, 'Script generation');
      state = useJobState.getState();
      expect(state.progress).toBe(25);

      state.updateProgress(50, 'Image generation');
      state = useJobState.getState();
      expect(state.progress).toBe(50);

      state.updateProgress(75, 'Video composition');
      state = useJobState.getState();
      expect(state.progress).toBe(75);

      // Complete job
      state.setStatus('completed');
      state = useJobState.getState();
      expect(state.status).toBe('completed');
      expect(state.canStartNewJob()).toBe(true);

      // Clear job
      state.clearJob();
      state = useJobState.getState();
      expect(state.status).toBe('idle');
      expect(state.currentJobId).toBeNull();
    });

    it('should handle failed job lifecycle correctly', () => {
      let state = useJobState.getState();

      // Start job
      state.setJob('job-1');
      state.updateProgress(25, 'Processing');

      // Job fails
      state.setStatus('failed');
      state = useJobState.getState();
      expect(state.status).toBe('failed');
      expect(state.canStartNewJob()).toBe(true);

      // Clear and retry
      state.clearJob();
      state = useJobState.getState();
      expect(state.status).toBe('idle');

      // Start new job
      state.setJob('job-2');
      state = useJobState.getState();
      expect(state.currentJobId).toBe('job-2');
      expect(state.status).toBe('running');
    });
  });
});
