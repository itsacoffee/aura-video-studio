import { describe, it, expect, beforeEach } from 'vitest';
import { useTimelineStore } from '../state/timeline';

describe('Timeline Multi-Select', () => {
  beforeEach(() => {
    useTimelineStore.setState({
      tracks: [
        { id: 'V1', name: 'Video 1', type: 'video', clips: [] },
        { id: 'A1', name: 'Audio 1', type: 'audio', clips: [] },
      ],
      markers: [],
      overlays: [],
      currentTime: 0,
      zoom: 50,
      snappingEnabled: true,
      selectedClipId: null,
      selectedClipIds: [],
      selectedOverlayId: null,
      rippleEditMode: false,
      magneticTimelineEnabled: false,
    });
  });

  it('should select multiple clips individually', () => {
    const store = useTimelineStore.getState();

    const clip1 = {
      id: 'clip1',
      sourcePath: '/video1.mp4',
      sourceIn: 0,
      sourceOut: 10,
      timelineStart: 0,
      trackId: 'V1',
    };

    const clip2 = {
      id: 'clip2',
      sourcePath: '/video2.mp4',
      sourceIn: 0,
      sourceOut: 10,
      timelineStart: 10,
      trackId: 'V1',
    };

    store.addClip('V1', clip1);
    store.addClip('V1', clip2);

    store.setSelectedClipIds(['clip1', 'clip2']);

    const state = useTimelineStore.getState();
    expect(state.selectedClipIds).toEqual(['clip1', 'clip2']);
    expect(state.selectedClipId).toBe('clip1');
  });

  it('should toggle clip selection', () => {
    const store = useTimelineStore.getState();

    const clip1 = {
      id: 'clip1',
      sourcePath: '/video1.mp4',
      sourceIn: 0,
      sourceOut: 10,
      timelineStart: 0,
      trackId: 'V1',
    };

    store.addClip('V1', clip1);
    store.setSelectedClipIds(['clip1']);

    store.toggleClipSelection('clip1');
    expect(useTimelineStore.getState().selectedClipIds).toEqual([]);

    store.toggleClipSelection('clip1');
    expect(useTimelineStore.getState().selectedClipIds).toEqual(['clip1']);
  });

  it('should select range of clips', () => {
    const store = useTimelineStore.getState();

    const clips = [
      {
        id: 'clip1',
        sourcePath: '/video1.mp4',
        sourceIn: 0,
        sourceOut: 5,
        timelineStart: 0,
        trackId: 'V1',
      },
      {
        id: 'clip2',
        sourcePath: '/video2.mp4',
        sourceIn: 0,
        sourceOut: 5,
        timelineStart: 5,
        trackId: 'V1',
      },
      {
        id: 'clip3',
        sourcePath: '/video3.mp4',
        sourceIn: 0,
        sourceOut: 5,
        timelineStart: 10,
        trackId: 'V1',
      },
    ];

    clips.forEach((clip) => store.addClip('V1', clip));

    store.selectClipRange('clip1', 'clip3');

    const state = useTimelineStore.getState();
    expect(state.selectedClipIds).toEqual(['clip1', 'clip2', 'clip3']);
  });

  it('should clear selection', () => {
    const store = useTimelineStore.getState();

    store.setSelectedClipIds(['clip1', 'clip2']);
    store.clearSelection();

    const state = useTimelineStore.getState();
    expect(state.selectedClipIds).toEqual([]);
    expect(state.selectedClipId).toBeNull();
  });

  it('should delete multiple clips at once', () => {
    const store = useTimelineStore.getState();

    const clips = [
      {
        id: 'clip1',
        sourcePath: '/video1.mp4',
        sourceIn: 0,
        sourceOut: 5,
        timelineStart: 0,
        trackId: 'V1',
      },
      {
        id: 'clip2',
        sourcePath: '/video2.mp4',
        sourceIn: 0,
        sourceOut: 5,
        timelineStart: 5,
        trackId: 'V1',
      },
      {
        id: 'clip3',
        sourcePath: '/video3.mp4',
        sourceIn: 0,
        sourceOut: 5,
        timelineStart: 10,
        trackId: 'V1',
      },
    ];

    clips.forEach((clip) => store.addClip('V1', clip));

    store.removeClips(['clip1', 'clip3']);

    const state = useTimelineStore.getState();
    const v1Track = state.tracks.find((t) => t.id === 'V1');
    expect(v1Track?.clips).toHaveLength(1);
    expect(v1Track?.clips[0].id).toBe('clip2');
    expect(state.selectedClipIds).toEqual([]);
  });

  it('should ripple delete multiple clips', () => {
    const store = useTimelineStore.getState();

    const clips = [
      {
        id: 'clip1',
        sourcePath: '/video1.mp4',
        sourceIn: 0,
        sourceOut: 5,
        timelineStart: 0,
        trackId: 'V1',
      },
      {
        id: 'clip2',
        sourcePath: '/video2.mp4',
        sourceIn: 0,
        sourceOut: 5,
        timelineStart: 5,
        trackId: 'V1',
      },
      {
        id: 'clip3',
        sourcePath: '/video3.mp4',
        sourceIn: 0,
        sourceOut: 5,
        timelineStart: 10,
        trackId: 'V1',
      },
    ];

    clips.forEach((clip) => store.addClip('V1', clip));

    store.rippleDeleteClips(['clip1']);

    const state = useTimelineStore.getState();
    const v1Track = state.tracks.find((t) => t.id === 'V1');
    expect(v1Track?.clips).toHaveLength(2);
    expect(v1Track?.clips[0].id).toBe('clip2');
    expect(v1Track?.clips[0].timelineStart).toBe(0);
    expect(v1Track?.clips[1].id).toBe('clip3');
    expect(v1Track?.clips[1].timelineStart).toBe(5);
  });

  it('should enable and disable ripple edit mode', () => {
    const store = useTimelineStore.getState();

    expect(store.rippleEditMode).toBe(false);

    store.setRippleEditMode(true);
    expect(useTimelineStore.getState().rippleEditMode).toBe(true);

    store.setRippleEditMode(false);
    expect(useTimelineStore.getState().rippleEditMode).toBe(false);
  });

  it('should enable and disable magnetic timeline', () => {
    const store = useTimelineStore.getState();

    expect(store.magneticTimelineEnabled).toBe(false);

    store.setMagneticTimelineEnabled(true);
    expect(useTimelineStore.getState().magneticTimelineEnabled).toBe(true);

    store.setMagneticTimelineEnabled(false);
    expect(useTimelineStore.getState().magneticTimelineEnabled).toBe(false);
  });

  it('should configure snap settings', () => {
    const store = useTimelineStore.getState();

    store.setSnapConfig({
      thresholdMs: 200,
      snapToAudioPeaks: false,
    });

    const state = useTimelineStore.getState();
    expect(state.snapConfig.thresholdMs).toBe(200);
    expect(state.snapConfig.snapToAudioPeaks).toBe(false);
    expect(state.snapConfig.snapToClips).toBe(true);
  });
});
