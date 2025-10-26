import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useJobsStore } from '../jobs';

// Mock fetch
global.fetch = vi.fn();

describe('useJobsStore', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useJobsStore.setState({
      activeJob: null,
      jobs: [],
      loading: false,
      polling: false,
    });
  });

  it('should initialize with default values', () => {
    const state = useJobsStore.getState();
    expect(state.activeJob).toBeNull();
    expect(state.jobs).toEqual([]);
    expect(state.loading).toBe(false);
    expect(state.polling).toBe(false);
  });

  it('should set active job', () => {
    const mockJob = {
      id: 'job-1',
      stage: 'Script',
      status: 'Running' as const,
      percent: 50,
      artifacts: [],
      logs: [],
      startedAt: new Date().toISOString(),
    };

    const state = useJobsStore.getState();
    state.setActiveJob(mockJob);

    const updatedState = useJobsStore.getState();
    expect(updatedState.activeJob).toEqual(mockJob);
  });

  it('should handle job creation', async () => {
    const mockJobId = 'new-job-id';
    (global.fetch as unknown as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ jobId: mockJobId }),
    });

    const state = useJobsStore.getState();
    const jobId = await state.createJob(
      { topic: 'Test' },
      { targetDuration: 'PT3M' },
      { voiceName: 'test' },
      { res: { width: 1920, height: 1080 } }
    );

    expect(jobId).toBe(mockJobId);
    expect(global.fetch).toHaveBeenCalledWith('/api/jobs', expect.any(Object));
  });

  it('should handle job listing', async () => {
    const mockJobs = [
      {
        id: 'job-1',
        stage: 'Complete',
        status: 'Done',
        percent: 100,
        artifacts: [],
        logs: [],
        startedAt: new Date().toISOString(),
      },
    ];

    (global.fetch as unknown as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ jobs: mockJobs }),
    });

    const state = useJobsStore.getState();
    await state.listJobs();

    const updatedState = useJobsStore.getState();
    expect(updatedState.jobs).toEqual(mockJobs);
  });
});
