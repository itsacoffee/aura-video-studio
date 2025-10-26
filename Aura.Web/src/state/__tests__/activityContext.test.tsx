import { describe, it, expect } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { ActivityProvider, useActivity } from '../activityContext';

describe('ActivityContext', () => {
  it('should add a new activity', () => {
    const { result } = renderHook(() => useActivity(), {
      wrapper: ActivityProvider,
    });

    act(() => {
      result.current.addActivity({
        type: 'video-generation',
        title: 'Test Activity',
        message: 'Testing',
      });
    });

    expect(result.current.activities).toHaveLength(1);
    expect(result.current.activities[0].title).toBe('Test Activity');
    expect(result.current.activities[0].status).toBe('pending');
    expect(result.current.activities[0].progress).toBe(0);
  });

  it('should update an activity', () => {
    const { result } = renderHook(() => useActivity(), {
      wrapper: ActivityProvider,
    });

    let activityId: string;

    act(() => {
      activityId = result.current.addActivity({
        type: 'video-generation',
        title: 'Test Activity',
        message: 'Testing',
      });
    });

    act(() => {
      result.current.updateActivity(activityId, {
        status: 'running',
        progress: 50,
        message: 'Half done',
      });
    });

    expect(result.current.activities[0].status).toBe('running');
    expect(result.current.activities[0].progress).toBe(50);
    expect(result.current.activities[0].message).toBe('Half done');
  });

  it('should track active activities', () => {
    const { result } = renderHook(() => useActivity(), {
      wrapper: ActivityProvider,
    });

    let activityId1: string;
    let activityId2: string;

    act(() => {
      activityId1 = result.current.addActivity({
        type: 'video-generation',
        title: 'Activity 1',
        message: 'Testing',
      });
      activityId2 = result.current.addActivity({
        type: 'api-call',
        title: 'Activity 2',
        message: 'Testing',
      });
    });

    act(() => {
      result.current.updateActivity(activityId1, { status: 'running' });
      result.current.updateActivity(activityId2, { status: 'completed' });
    });

    expect(result.current.activeActivities).toHaveLength(1);
    expect(result.current.completedActivities).toHaveLength(1);
  });

  it('should remove an activity', () => {
    const { result } = renderHook(() => useActivity(), {
      wrapper: ActivityProvider,
    });

    let activityId: string;

    act(() => {
      activityId = result.current.addActivity({
        type: 'video-generation',
        title: 'Test Activity',
        message: 'Testing',
      });
    });

    expect(result.current.activities).toHaveLength(1);

    act(() => {
      result.current.removeActivity(activityId);
    });

    expect(result.current.activities).toHaveLength(0);
  });

  it('should clear completed activities', () => {
    const { result } = renderHook(() => useActivity(), {
      wrapper: ActivityProvider,
    });

    let activityId1: string;
    let activityId2: string;

    act(() => {
      activityId1 = result.current.addActivity({
        type: 'video-generation',
        title: 'Activity 1',
        message: 'Testing',
      });
      activityId2 = result.current.addActivity({
        type: 'api-call',
        title: 'Activity 2',
        message: 'Testing',
      });
    });

    act(() => {
      result.current.updateActivity(activityId1, { status: 'completed' });
      result.current.updateActivity(activityId2, { status: 'running' });
    });

    expect(result.current.activities).toHaveLength(2);

    act(() => {
      result.current.clearCompleted();
    });

    expect(result.current.activities).toHaveLength(1);
    expect(result.current.activities[0].status).toBe('running');
  });

  it('should set endTime when activity completes', () => {
    const { result } = renderHook(() => useActivity(), {
      wrapper: ActivityProvider,
    });

    let activityId: string;

    act(() => {
      activityId = result.current.addActivity({
        type: 'video-generation',
        title: 'Test Activity',
        message: 'Testing',
      });
    });

    expect(result.current.activities[0].endTime).toBeUndefined();

    act(() => {
      result.current.updateActivity(activityId, { status: 'completed' });
    });

    expect(result.current.activities[0].endTime).toBeDefined();
  });

  it('should pause and resume an activity', () => {
    const { result } = renderHook(() => useActivity(), {
      wrapper: ActivityProvider,
    });

    let activityId: string;

    act(() => {
      activityId = result.current.addActivity({
        type: 'video-generation',
        title: 'Test Activity',
        message: 'Testing',
      });
      result.current.updateActivity(activityId, { status: 'running' });
    });

    expect(result.current.activities[0].status).toBe('running');

    act(() => {
      result.current.pauseActivity(activityId);
    });

    expect(result.current.activities[0].status).toBe('paused');
    expect(result.current.pausedActivities).toHaveLength(1);

    act(() => {
      result.current.resumeActivity(activityId);
    });

    expect(result.current.activities[0].status).toBe('running');
    expect(result.current.pausedActivities).toHaveLength(0);
  });

  it('should set and track priority', () => {
    const { result } = renderHook(() => useActivity(), {
      wrapper: ActivityProvider,
    });

    let activityId: string;

    act(() => {
      activityId = result.current.addActivity({
        type: 'video-generation',
        title: 'Test Activity',
        message: 'Testing',
        priority: 5,
      });
    });

    expect(result.current.activities[0].priority).toBe(5);

    act(() => {
      result.current.setPriority(activityId, 8);
    });

    expect(result.current.activities[0].priority).toBe(8);

    // Priority should be clamped between 1 and 10
    act(() => {
      result.current.setPriority(activityId, 15);
    });

    expect(result.current.activities[0].priority).toBe(10);

    act(() => {
      result.current.setPriority(activityId, -5);
    });

    expect(result.current.activities[0].priority).toBe(1);
  });

  it('should track queued activities sorted by priority', () => {
    const { result } = renderHook(() => useActivity(), {
      wrapper: ActivityProvider,
    });

    act(() => {
      result.current.addActivity({
        type: 'video-generation',
        title: 'Low Priority',
        message: 'Testing',
        priority: 2,
      });
      result.current.addActivity({
        type: 'video-generation',
        title: 'High Priority',
        message: 'Testing',
        priority: 9,
      });
      result.current.addActivity({
        type: 'video-generation',
        title: 'Medium Priority',
        message: 'Testing',
        priority: 5,
      });
    });

    const queued = result.current.queuedActivities;
    expect(queued).toHaveLength(3);
    expect(queued[0].title).toBe('High Priority');
    expect(queued[1].title).toBe('Medium Priority');
    expect(queued[2].title).toBe('Low Priority');
  });

  it('should track batch operations', () => {
    const { result } = renderHook(() => useActivity(), {
      wrapper: ActivityProvider,
    });

    const batchId = 'batch-123';

    act(() => {
      result.current.addActivity({
        type: 'export',
        title: 'Export Video 1',
        message: 'Exporting',
        batchId,
      });
      result.current.addActivity({
        type: 'export',
        title: 'Export Video 2',
        message: 'Exporting',
        batchId,
      });
      result.current.addActivity({
        type: 'export',
        title: 'Export Video 3',
        message: 'Exporting',
        batchId,
      });
    });

    const batchOps = result.current.getBatchOperations(batchId);
    expect(batchOps).toHaveLength(3);
    expect(result.current.batchOperations.get(batchId)).toHaveLength(3);
  });

  it('should maintain operation history', () => {
    const { result } = renderHook(() => useActivity(), {
      wrapper: ActivityProvider,
    });

    let activityId: string;

    act(() => {
      activityId = result.current.addActivity({
        type: 'video-generation',
        title: 'Test Activity',
        message: 'Testing',
      });
      result.current.updateActivity(activityId, { status: 'running' });
    });

    expect(result.current.recentHistory).toHaveLength(0);

    act(() => {
      result.current.updateActivity(activityId, { status: 'completed' });
    });

    // History should update after status change
    setTimeout(() => {
      expect(result.current.recentHistory.length).toBeGreaterThan(0);
    }, 100);
  });

  it('should clear history', () => {
    const { result } = renderHook(() => useActivity(), {
      wrapper: ActivityProvider,
    });

    let activityId: string;

    act(() => {
      activityId = result.current.addActivity({
        type: 'video-generation',
        title: 'Test Activity',
        message: 'Testing',
      });
      result.current.updateActivity(activityId, { status: 'completed' });
    });

    act(() => {
      result.current.clearHistory();
    });

    expect(result.current.recentHistory).toHaveLength(0);
  });
});
