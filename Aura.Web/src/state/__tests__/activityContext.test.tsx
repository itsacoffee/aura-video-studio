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
});
