/**
 * OpenCut Keyframes Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useOpenCutKeyframesStore } from '../opencutKeyframes';

describe('OpenCutKeyframesStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useOpenCutKeyframesStore.setState({
      tracks: [],
      selectedKeyframeIds: [],
      copiedKeyframes: [],
    });
  });

  describe('Track Operations', () => {
    it('should create a new track when adding keyframe to new property', () => {
      const { addKeyframe, getTrackForProperty } = useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 0, 100);

      const track = getTrackForProperty('clip-1', 'opacity');
      expect(track).toBeDefined();
      expect(track?.clipId).toBe('clip-1');
      expect(track?.property).toBe('opacity');
      expect(track?.keyframes.length).toBe(1);
    });

    it('should reuse existing track when adding keyframe to same property', () => {
      const { addKeyframe, tracks } = useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 0, 100);
      addKeyframe('clip-1', 'opacity', 1, 50);

      const state = useOpenCutKeyframesStore.getState();
      expect(state.tracks.length).toBe(1);
      expect(state.tracks[0].keyframes.length).toBe(2);
    });

    it('should remove track when last keyframe is removed', () => {
      const { addKeyframe, removeKeyframe, getTrackForProperty } =
        useOpenCutKeyframesStore.getState();

      const kfId = addKeyframe('clip-1', 'opacity', 0, 100);

      let track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      expect(track).toBeDefined();

      useOpenCutKeyframesStore.getState().removeKeyframe(kfId);

      track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      expect(track).toBeUndefined();
    });

    it('should toggle track enabled state', () => {
      const { addKeyframe, toggleTrackEnabled, getTrackForProperty } =
        useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 0, 100);

      let track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      expect(track?.enabled).toBe(true);

      useOpenCutKeyframesStore.getState().toggleTrackEnabled(track!.id);

      track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      expect(track?.enabled).toBe(false);
    });

    it('should remove all tracks for a clip', () => {
      const { addKeyframe, removeTracksForClip, getTracksForClip } =
        useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 0, 100);
      addKeyframe('clip-1', 'positionX', 0, 0);
      addKeyframe('clip-2', 'opacity', 0, 100);

      let tracks = useOpenCutKeyframesStore.getState().getTracksForClip('clip-1');
      expect(tracks.length).toBe(2);

      useOpenCutKeyframesStore.getState().removeTracksForClip('clip-1');

      tracks = useOpenCutKeyframesStore.getState().getTracksForClip('clip-1');
      expect(tracks.length).toBe(0);

      // Clip-2 should still have its track
      tracks = useOpenCutKeyframesStore.getState().getTracksForClip('clip-2');
      expect(tracks.length).toBe(1);
    });
  });

  describe('Keyframe Operations', () => {
    it('should add keyframe at specified time and value', () => {
      const { addKeyframe, getTrackForProperty } = useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 1.5, 75);

      const track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      expect(track?.keyframes[0].time).toBe(1.5);
      expect(track?.keyframes[0].value).toBe(75);
      expect(track?.keyframes[0].easing).toBe('ease-out');
    });

    it('should replace keyframe at same time position', () => {
      const { addKeyframe, getTrackForProperty } = useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 1, 100);
      addKeyframe('clip-1', 'opacity', 1, 50);

      const track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      expect(track?.keyframes.length).toBe(1);
      expect(track?.keyframes[0].value).toBe(50);
    });

    it('should sort keyframes by time', () => {
      const { addKeyframe, getTrackForProperty } = useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 2, 50);
      addKeyframe('clip-1', 'opacity', 0, 100);
      addKeyframe('clip-1', 'opacity', 1, 75);

      const track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      expect(track?.keyframes[0].time).toBe(0);
      expect(track?.keyframes[1].time).toBe(1);
      expect(track?.keyframes[2].time).toBe(2);
    });

    it('should update keyframe properties', () => {
      const { addKeyframe, updateKeyframe, getTrackForProperty } =
        useOpenCutKeyframesStore.getState();

      const kfId = addKeyframe('clip-1', 'opacity', 0, 100);

      useOpenCutKeyframesStore.getState().updateKeyframe(kfId, { value: 50, easing: 'ease-in' });

      const track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      expect(track?.keyframes[0].value).toBe(50);
      expect(track?.keyframes[0].easing).toBe('ease-in');
    });

    it('should move keyframe to new time and re-sort', () => {
      const { addKeyframe, moveKeyframe, getTrackForProperty } =
        useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 0, 100);
      const kf2Id = addKeyframe('clip-1', 'opacity', 1, 50);
      addKeyframe('clip-1', 'opacity', 2, 0);

      useOpenCutKeyframesStore.getState().moveKeyframe(kf2Id, 3);

      const track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      expect(track?.keyframes[0].time).toBe(0);
      expect(track?.keyframes[1].time).toBe(2);
      expect(track?.keyframes[2].time).toBe(3);
    });

    it('should not allow negative time values', () => {
      const { addKeyframe, moveKeyframe, getTrackForProperty } =
        useOpenCutKeyframesStore.getState();

      const kfId = addKeyframe('clip-1', 'opacity', 1, 100);
      useOpenCutKeyframesStore.getState().moveKeyframe(kfId, -5);

      const track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      expect(track?.keyframes[0].time).toBe(0);
    });
  });

  describe('Value Interpolation', () => {
    it('should return undefined if no keyframes exist', () => {
      const { getValueAtTime } = useOpenCutKeyframesStore.getState();

      const value = getValueAtTime('clip-1', 'opacity', 1);
      expect(value).toBeUndefined();
    });

    it('should return first keyframe value before first keyframe', () => {
      const { addKeyframe, getValueAtTime } = useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 1, 100);
      addKeyframe('clip-1', 'opacity', 2, 50);

      const value = useOpenCutKeyframesStore.getState().getValueAtTime('clip-1', 'opacity', 0);
      expect(value).toBe(100);
    });

    it('should return last keyframe value after last keyframe', () => {
      const { addKeyframe, getValueAtTime } = useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 1, 100);
      addKeyframe('clip-1', 'opacity', 2, 50);

      const value = useOpenCutKeyframesStore.getState().getValueAtTime('clip-1', 'opacity', 5);
      expect(value).toBe(50);
    });

    it('should interpolate value between keyframes with linear easing', () => {
      const { addKeyframe, updateKeyframe, getValueAtTime } = useOpenCutKeyframesStore.getState();

      const kf1Id = addKeyframe('clip-1', 'opacity', 0, 0);
      addKeyframe('clip-1', 'opacity', 2, 100);

      // Set first keyframe to linear
      useOpenCutKeyframesStore.getState().updateKeyframe(kf1Id, { easing: 'linear' });

      const value = useOpenCutKeyframesStore.getState().getValueAtTime('clip-1', 'opacity', 1);
      expect(value).toBe(50);
    });

    it('should hold value with hold easing', () => {
      const { addKeyframe, updateKeyframe, getValueAtTime } = useOpenCutKeyframesStore.getState();

      const kf1Id = addKeyframe('clip-1', 'opacity', 0, 100);
      addKeyframe('clip-1', 'opacity', 2, 0);

      // Set first keyframe to hold
      useOpenCutKeyframesStore.getState().updateKeyframe(kf1Id, { easing: 'hold' });

      const value = useOpenCutKeyframesStore.getState().getValueAtTime('clip-1', 'opacity', 1);
      expect(value).toBe(100);
    });

    it('should return undefined when track is disabled', () => {
      const { addKeyframe, toggleTrackEnabled, getTrackForProperty, getValueAtTime } =
        useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 0, 100);
      addKeyframe('clip-1', 'opacity', 2, 0);

      const track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      useOpenCutKeyframesStore.getState().toggleTrackEnabled(track!.id);

      const value = useOpenCutKeyframesStore.getState().getValueAtTime('clip-1', 'opacity', 1);
      expect(value).toBeUndefined();
    });
  });

  describe('Selection Operations', () => {
    it('should select a keyframe', () => {
      const { addKeyframe, selectKeyframe, selectedKeyframeIds } =
        useOpenCutKeyframesStore.getState();

      const kfId = addKeyframe('clip-1', 'opacity', 0, 100);
      useOpenCutKeyframesStore.getState().selectKeyframe(kfId);

      const state = useOpenCutKeyframesStore.getState();
      expect(state.selectedKeyframeIds).toContain(kfId);
    });

    it('should add to selection when addToSelection is true', () => {
      const { addKeyframe, selectKeyframe } = useOpenCutKeyframesStore.getState();

      const kf1Id = addKeyframe('clip-1', 'opacity', 0, 100);
      const kf2Id = addKeyframe('clip-1', 'opacity', 1, 50);

      useOpenCutKeyframesStore.getState().selectKeyframe(kf1Id);
      useOpenCutKeyframesStore.getState().selectKeyframe(kf2Id, true);

      const state = useOpenCutKeyframesStore.getState();
      expect(state.selectedKeyframeIds).toContain(kf1Id);
      expect(state.selectedKeyframeIds).toContain(kf2Id);
    });

    it('should replace selection when addToSelection is false', () => {
      const { addKeyframe, selectKeyframe } = useOpenCutKeyframesStore.getState();

      const kf1Id = addKeyframe('clip-1', 'opacity', 0, 100);
      const kf2Id = addKeyframe('clip-1', 'opacity', 1, 50);

      useOpenCutKeyframesStore.getState().selectKeyframe(kf1Id);
      useOpenCutKeyframesStore.getState().selectKeyframe(kf2Id, false);

      const state = useOpenCutKeyframesStore.getState();
      expect(state.selectedKeyframeIds).not.toContain(kf1Id);
      expect(state.selectedKeyframeIds).toContain(kf2Id);
    });

    it('should select keyframes in time range', () => {
      const { addKeyframe, selectKeyframesInRange } = useOpenCutKeyframesStore.getState();

      const kf1Id = addKeyframe('clip-1', 'opacity', 0, 100);
      const kf2Id = addKeyframe('clip-1', 'opacity', 1, 75);
      const kf3Id = addKeyframe('clip-1', 'opacity', 2, 50);
      addKeyframe('clip-1', 'opacity', 5, 0);

      useOpenCutKeyframesStore.getState().selectKeyframesInRange(0.5, 2.5);

      const state = useOpenCutKeyframesStore.getState();
      expect(state.selectedKeyframeIds).not.toContain(kf1Id);
      expect(state.selectedKeyframeIds).toContain(kf2Id);
      expect(state.selectedKeyframeIds).toContain(kf3Id);
    });

    it('should clear keyframe selection', () => {
      const { addKeyframe, selectKeyframe, clearKeyframeSelection } =
        useOpenCutKeyframesStore.getState();

      const kfId = addKeyframe('clip-1', 'opacity', 0, 100);
      useOpenCutKeyframesStore.getState().selectKeyframe(kfId);
      useOpenCutKeyframesStore.getState().clearKeyframeSelection();

      const state = useOpenCutKeyframesStore.getState();
      expect(state.selectedKeyframeIds.length).toBe(0);
    });

    it('should get selected keyframes', () => {
      const { addKeyframe, selectKeyframe, getSelectedKeyframes } =
        useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 0, 100);
      const kf2Id = addKeyframe('clip-1', 'opacity', 1, 50);

      useOpenCutKeyframesStore.getState().selectKeyframe(kf2Id);

      const selected = useOpenCutKeyframesStore.getState().getSelectedKeyframes();
      expect(selected.length).toBe(1);
      expect(selected[0].value).toBe(50);
    });
  });

  describe('Clipboard Operations', () => {
    it('should copy selected keyframes', () => {
      const { addKeyframe, selectKeyframe, copySelectedKeyframes, copiedKeyframes } =
        useOpenCutKeyframesStore.getState();

      const kfId = addKeyframe('clip-1', 'opacity', 0, 100);
      useOpenCutKeyframesStore.getState().selectKeyframe(kfId);
      useOpenCutKeyframesStore.getState().copySelectedKeyframes();

      const state = useOpenCutKeyframesStore.getState();
      expect(state.copiedKeyframes.length).toBe(1);
    });

    it('should paste keyframes at offset', () => {
      const {
        addKeyframe,
        selectKeyframe,
        copySelectedKeyframes,
        pasteKeyframes,
        getTrackForProperty,
      } = useOpenCutKeyframesStore.getState();

      const kfId = addKeyframe('clip-1', 'opacity', 1, 100);
      useOpenCutKeyframesStore.getState().selectKeyframe(kfId);
      useOpenCutKeyframesStore.getState().copySelectedKeyframes();

      useOpenCutKeyframesStore.getState().pasteKeyframes('clip-2', 'opacity', 5);

      const track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-2', 'opacity');
      expect(track?.keyframes.length).toBe(1);
      expect(track?.keyframes[0].time).toBe(5);
      expect(track?.keyframes[0].value).toBe(100);
    });

    it('should delete selected keyframes', () => {
      const { addKeyframe, selectKeyframe, deleteSelectedKeyframes, getTrackForProperty } =
        useOpenCutKeyframesStore.getState();

      const kf1Id = addKeyframe('clip-1', 'opacity', 0, 100);
      const kf2Id = addKeyframe('clip-1', 'opacity', 1, 50);

      useOpenCutKeyframesStore.getState().selectKeyframe(kf1Id);
      useOpenCutKeyframesStore.getState().selectKeyframe(kf2Id, true);
      useOpenCutKeyframesStore.getState().deleteSelectedKeyframes();

      const track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      expect(track).toBeUndefined();
    });
  });

  describe('Bulk Operations', () => {
    it('should set easing for all selected keyframes', () => {
      const { addKeyframe, selectKeyframe, setEasingForSelected, getTrackForProperty } =
        useOpenCutKeyframesStore.getState();

      const kf1Id = addKeyframe('clip-1', 'opacity', 0, 100);
      const kf2Id = addKeyframe('clip-1', 'opacity', 1, 50);

      useOpenCutKeyframesStore.getState().selectKeyframe(kf1Id);
      useOpenCutKeyframesStore.getState().selectKeyframe(kf2Id, true);
      useOpenCutKeyframesStore.getState().setEasingForSelected('ease-in-out');

      const track = useOpenCutKeyframesStore.getState().getTrackForProperty('clip-1', 'opacity');
      expect(track?.keyframes[0].easing).toBe('ease-in-out');
      expect(track?.keyframes[1].easing).toBe('ease-in-out');
    });
  });

  describe('Query Operations', () => {
    it('should check if clip has keyframes', () => {
      const { addKeyframe, hasKeyframes } = useOpenCutKeyframesStore.getState();

      expect(useOpenCutKeyframesStore.getState().hasKeyframes('clip-1')).toBe(false);

      addKeyframe('clip-1', 'opacity', 0, 100);

      expect(useOpenCutKeyframesStore.getState().hasKeyframes('clip-1')).toBe(true);
      expect(useOpenCutKeyframesStore.getState().hasKeyframes('clip-1', 'opacity')).toBe(true);
      expect(useOpenCutKeyframesStore.getState().hasKeyframes('clip-1', 'positionX')).toBe(false);
    });

    it('should get keyframe at specific time', () => {
      const { addKeyframe, getKeyframeAtTime } = useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 1, 100);

      const kf = useOpenCutKeyframesStore.getState().getKeyframeAtTime('clip-1', 'opacity', 1);
      expect(kf).toBeDefined();
      expect(kf?.value).toBe(100);

      const noKf = useOpenCutKeyframesStore.getState().getKeyframeAtTime('clip-1', 'opacity', 2);
      expect(noKf).toBeUndefined();
    });

    it('should get keyframe at time with tolerance', () => {
      const { addKeyframe, getKeyframeAtTime } = useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 1, 100);

      const kf = useOpenCutKeyframesStore
        .getState()
        .getKeyframeAtTime('clip-1', 'opacity', 1.03, 0.05);
      expect(kf).toBeDefined();

      const noKf = useOpenCutKeyframesStore
        .getState()
        .getKeyframeAtTime('clip-1', 'opacity', 1.1, 0.05);
      expect(noKf).toBeUndefined();
    });

    it('should get adjacent keyframes', () => {
      const { addKeyframe, getAdjacentKeyframes } = useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 0, 100);
      addKeyframe('clip-1', 'opacity', 2, 50);
      addKeyframe('clip-1', 'opacity', 4, 0);

      const { prev, next } = useOpenCutKeyframesStore
        .getState()
        .getAdjacentKeyframes('clip-1', 'opacity', 3);

      expect(prev?.time).toBe(2);
      expect(next?.time).toBe(4);
    });

    it('should get all keyframes for a clip', () => {
      const { addKeyframe, getKeyframesForClip } = useOpenCutKeyframesStore.getState();

      addKeyframe('clip-1', 'opacity', 0, 100);
      addKeyframe('clip-1', 'positionX', 1, 50);
      addKeyframe('clip-2', 'opacity', 0, 75);

      const keyframes = useOpenCutKeyframesStore.getState().getKeyframesForClip('clip-1');
      expect(keyframes.length).toBe(2);
    });
  });
});
