import { describe, it, expect, beforeEach } from 'vitest';
import { useMLLabStore } from '../mlLab';
import type { AnnotatedVideo, AnnotatedFrame } from '../mlLab';

describe('ML Lab Store', () => {
  beforeEach(() => {
    // Reset the store before each test
    useMLLabStore.getState().reset();
  });

  describe('Video Management', () => {
    it('should add a video', () => {
      const video: AnnotatedVideo = {
        path: '/videos/test.mp4',
        name: 'test.mp4',
        framesExtracted: 10,
        framesAnnotated: 0,
        frames: [],
      };

      useMLLabStore.getState().addVideo(video);

      const state = useMLLabStore.getState();
      expect(state.videos).toHaveLength(1);
      expect(state.videos[0]).toEqual(video);
      expect(state.selectedVideoPath).toBe(video.path);
    });

    it('should select a video', () => {
      const video1: AnnotatedVideo = {
        path: '/videos/test1.mp4',
        name: 'test1.mp4',
        framesExtracted: 10,
        framesAnnotated: 0,
        frames: [],
      };
      const video2: AnnotatedVideo = {
        path: '/videos/test2.mp4',
        name: 'test2.mp4',
        framesExtracted: 10,
        framesAnnotated: 0,
        frames: [],
      };

      useMLLabStore.getState().addVideo(video1);
      useMLLabStore.getState().addVideo(video2);
      useMLLabStore.getState().selectVideo(video2.path);

      const state = useMLLabStore.getState();
      expect(state.selectedVideoPath).toBe(video2.path);
      expect(state.currentFrameIndex).toBe(0);
    });

    it('should remove a video', () => {
      const video: AnnotatedVideo = {
        path: '/videos/test.mp4',
        name: 'test.mp4',
        framesExtracted: 10,
        framesAnnotated: 0,
        frames: [],
      };

      useMLLabStore.getState().addVideo(video);
      useMLLabStore.getState().removeVideo(video.path);

      const state = useMLLabStore.getState();
      expect(state.videos).toHaveLength(0);
    });
  });

  describe('Frame Rating', () => {
    it('should rate a frame', () => {
      const frames: AnnotatedFrame[] = [
        {
          id: 'frame-1',
          framePath: '/frames/frame_0001.jpg',
          videoPath: '/videos/test.mp4',
          timestamp: 0,
        },
        {
          id: 'frame-2',
          framePath: '/frames/frame_0002.jpg',
          videoPath: '/videos/test.mp4',
          timestamp: 5,
        },
      ];

      const video: AnnotatedVideo = {
        path: '/videos/test.mp4',
        name: 'test.mp4',
        framesExtracted: 2,
        framesAnnotated: 0,
        frames,
      };

      useMLLabStore.getState().addVideo(video);
      useMLLabStore.getState().rateFrame(video.path, 'frame-1', 1);

      const state = useMLLabStore.getState();
      const updatedVideo = state.videos.find((v) => v.path === video.path);
      expect(updatedVideo?.frames[0].rating).toBe(1);
      expect(updatedVideo?.framesAnnotated).toBe(1);
    });

    it('should update frame annotation count', () => {
      const frames: AnnotatedFrame[] = [
        {
          id: 'frame-1',
          framePath: '/frames/frame_0001.jpg',
          videoPath: '/videos/test.mp4',
          timestamp: 0,
        },
        {
          id: 'frame-2',
          framePath: '/frames/frame_0002.jpg',
          videoPath: '/videos/test.mp4',
          timestamp: 5,
        },
      ];

      const video: AnnotatedVideo = {
        path: '/videos/test.mp4',
        name: 'test.mp4',
        framesExtracted: 2,
        framesAnnotated: 0,
        frames,
      };

      useMLLabStore.getState().addVideo(video);
      useMLLabStore.getState().rateFrame(video.path, 'frame-1', 1);
      useMLLabStore.getState().rateFrame(video.path, 'frame-2', 0);

      const state = useMLLabStore.getState();
      const updatedVideo = state.videos.find((v) => v.path === video.path);
      expect(updatedVideo?.framesAnnotated).toBe(2);
    });
  });

  describe('Training Configuration', () => {
    it('should update training config', () => {
      useMLLabStore.getState().updateTrainingConfig({
        modelName: 'my-model',
        epochsPreset: 'balanced',
      });

      const state = useMLLabStore.getState();
      expect(state.trainingConfig.modelName).toBe('my-model');
      expect(state.trainingConfig.epochsPreset).toBe('balanced');
    });

    it('should partially update training config', () => {
      useMLLabStore.getState().updateTrainingConfig({ modelName: 'first-model' });
      useMLLabStore.getState().updateTrainingConfig({ epochsPreset: 'thorough' });

      const state = useMLLabStore.getState();
      expect(state.trainingConfig.modelName).toBe('first-model');
      expect(state.trainingConfig.epochsPreset).toBe('thorough');
    });
  });

  describe('Training Job Status', () => {
    it('should update active training job', () => {
      const jobStatus = {
        jobId: 'job-123',
        state: 'Running' as const,
        progress: 45,
        createdAt: new Date().toISOString(),
      };

      useMLLabStore.getState().updateTrainingJobStatus(jobStatus);

      const state = useMLLabStore.getState();
      expect(state.activeTrainingJob).toEqual(jobStatus);
    });

    it('should move completed job to history', () => {
      const runningJob = {
        jobId: 'job-123',
        state: 'Running' as const,
        progress: 45,
        createdAt: new Date().toISOString(),
      };

      const completedJob = {
        ...runningJob,
        state: 'Completed' as const,
        progress: 100,
        completedAt: new Date().toISOString(),
      };

      useMLLabStore.getState().updateTrainingJobStatus(runningJob);

      // Verify running job is active
      let state = useMLLabStore.getState();
      expect(state.activeTrainingJob).toEqual(runningJob);
      expect(state.trainingHistory).toHaveLength(0);

      // Complete the job
      useMLLabStore.getState().updateTrainingJobStatus(completedJob);

      // Verify completed job moved to history
      state = useMLLabStore.getState();
      expect(state.activeTrainingJob).toBeUndefined();
      expect(state.trainingHistory).toHaveLength(1);
      expect(state.trainingHistory[0]).toEqual(completedJob);
    });

    it('should handle multiple jobs in history', () => {
      const job1 = {
        jobId: 'job-1',
        state: 'Completed' as const,
        progress: 100,
        createdAt: new Date().toISOString(),
        completedAt: new Date().toISOString(),
      };

      const job2 = {
        jobId: 'job-2',
        state: 'Failed' as const,
        progress: 50,
        error: 'Out of memory',
        createdAt: new Date().toISOString(),
        completedAt: new Date().toISOString(),
      };

      useMLLabStore.getState().updateTrainingJobStatus(job1);
      useMLLabStore.getState().updateTrainingJobStatus(job2);

      const state = useMLLabStore.getState();
      expect(state.trainingHistory).toHaveLength(2);
      expect(state.trainingHistory[0]).toEqual(job1);
      expect(state.trainingHistory[1]).toEqual(job2);
    });
  });

  describe('Tab Navigation', () => {
    it('should set current tab', () => {
      useMLLabStore.getState().setCurrentTab('training');

      const state = useMLLabStore.getState();
      expect(state.currentTab).toBe('training');
    });

    it('should default to annotation tab', () => {
      const state = useMLLabStore.getState();
      expect(state.currentTab).toBe('annotation');
    });
  });

  describe('Frame Navigation', () => {
    it('should set current frame index', () => {
      useMLLabStore.getState().setCurrentFrameIndex(5);

      const state = useMLLabStore.getState();
      expect(state.currentFrameIndex).toBe(5);
    });

    it('should reset frame index when selecting video', () => {
      const video: AnnotatedVideo = {
        path: '/videos/test.mp4',
        name: 'test.mp4',
        framesExtracted: 10,
        framesAnnotated: 0,
        frames: [],
      };

      useMLLabStore.getState().setCurrentFrameIndex(5);
      useMLLabStore.getState().addVideo(video);
      useMLLabStore.getState().selectVideo(video.path);

      const state = useMLLabStore.getState();
      expect(state.currentFrameIndex).toBe(0);
    });
  });

  describe('Reset', () => {
    it('should reset all state', () => {
      const video: AnnotatedVideo = {
        path: '/videos/test.mp4',
        name: 'test.mp4',
        framesExtracted: 10,
        framesAnnotated: 0,
        frames: [],
      };

      useMLLabStore.getState().addVideo(video);
      useMLLabStore.getState().setCurrentTab('training');
      useMLLabStore.getState().updateTrainingConfig({ modelName: 'test' });
      useMLLabStore.getState().reset();

      const state = useMLLabStore.getState();
      expect(state.videos).toHaveLength(0);
      expect(state.currentTab).toBe('annotation');
      expect(state.trainingConfig).toEqual({});
    });
  });
});
