import { describe, it, expect, beforeEach } from 'vitest';
import { useTimelineStore } from '../state/timeline';

describe('Timeline Store', () => {
  beforeEach(() => {
    useTimelineStore.setState({
      tracks: [
        { id: 'V1', name: 'Video 1', type: 'video', clips: [] },
        { id: 'V2', name: 'Video 2', type: 'video', clips: [] },
        { id: 'A1', name: 'Audio 1', type: 'audio', clips: [] },
        { id: 'A2', name: 'Audio 2', type: 'audio', clips: [] },
      ],
      markers: [],
      overlays: [],
      currentTime: 0,
      zoom: 1.0,
      snappingEnabled: true,
      selectedClipId: null,
      selectedOverlayId: null,
    });
  });

  it('should add a clip to a track', () => {
    const store = useTimelineStore.getState();

    const clip = {
      id: 'clip1',
      sourcePath: '/video.mp4',
      sourceIn: 0,
      sourceOut: 10,
      timelineStart: 5,
      trackId: 'V1',
    };

    store.addClip('V1', clip);

    const updatedState = useTimelineStore.getState();
    const v1Track = updatedState.tracks.find((t) => t.id === 'V1');
    expect(v1Track?.clips).toHaveLength(1);
    expect(v1Track?.clips[0]).toEqual(clip);
  });

  it('should split a clip correctly', () => {
    const store = useTimelineStore.getState();

    const clip = {
      id: 'clip1',
      sourcePath: '/video.mp4',
      sourceIn: 0,
      sourceOut: 10,
      timelineStart: 5,
      trackId: 'V1',
    };

    store.addClip('V1', clip);
    store.splitClip('clip1', 8);

    const updatedState = useTimelineStore.getState();
    const v1Track = updatedState.tracks.find((t) => t.id === 'V1');
    expect(v1Track?.clips).toHaveLength(2);

    const [firstClip, secondClip] = v1Track!.clips;
    expect(firstClip.timelineStart).toBe(5);
    expect(firstClip.sourceOut).toBe(3);

    expect(secondClip.timelineStart).toBe(8);
    expect(secondClip.sourceIn).toBe(3);
    expect(secondClip.sourceOut).toBe(10);
  });

  it('should not split a clip outside its bounds', () => {
    const store = useTimelineStore.getState();

    const clip = {
      id: 'clip1',
      sourcePath: '/video.mp4',
      sourceIn: 0,
      sourceOut: 10,
      timelineStart: 5,
      trackId: 'V1',
    };

    store.addClip('V1', clip);
    store.splitClip('clip1', 20);

    const updatedState = useTimelineStore.getState();
    const v1Track = updatedState.tracks.find((t) => t.id === 'V1');
    expect(v1Track?.clips).toHaveLength(1);
  });

  it('should add and remove markers', () => {
    const store = useTimelineStore.getState();

    const marker = {
      id: 'marker1',
      title: 'Chapter 1',
      time: 10,
    };

    store.addMarker(marker);
    expect(useTimelineStore.getState().markers).toHaveLength(1);

    store.removeMarker('marker1');
    expect(useTimelineStore.getState().markers).toHaveLength(0);
  });

  it('should sort markers by time', () => {
    const store = useTimelineStore.getState();

    store.addMarker({ id: 'm2', title: 'Chapter 2', time: 20 });
    store.addMarker({ id: 'm1', title: 'Chapter 1', time: 10 });
    store.addMarker({ id: 'm3', title: 'Chapter 3', time: 30 });

    const updatedState = useTimelineStore.getState();
    expect(updatedState.markers[0].id).toBe('m1');
    expect(updatedState.markers[1].id).toBe('m2');
    expect(updatedState.markers[2].id).toBe('m3');
  });

  it('should export chapters in YouTube format', () => {
    const store = useTimelineStore.getState();

    store.addMarker({ id: 'm1', title: 'Introduction', time: 0 });
    store.addMarker({ id: 'm2', title: 'Main Content', time: 150 });
    store.addMarker({ id: 'm3', title: 'Conclusion', time: 3915 });

    const chapters = store.exportChapters();

    expect(chapters).toContain('0:00 Introduction');
    expect(chapters).toContain('2:30 Main Content');
    expect(chapters).toContain('1:05:15 Conclusion');
  });

  it('should add and update overlays', () => {
    const store = useTimelineStore.getState();

    const overlay = {
      id: 'overlay1',
      type: 'title' as const,
      text: 'Title',
      inTime: 1,
      outTime: 5,
      alignment: 'topCenter' as const,
      x: 0,
      y: 0,
      fontSize: 72,
      fontColor: 'white',
      backgroundOpacity: 0.7,
      borderWidth: 2,
    };

    store.addOverlay(overlay);
    expect(useTimelineStore.getState().overlays).toHaveLength(1);

    const updated = { ...overlay, text: 'Updated Title' };
    store.updateOverlay(updated);

    expect(useTimelineStore.getState().overlays[0].text).toBe('Updated Title');
  });

  it('should sort overlays by inTime', () => {
    const store = useTimelineStore.getState();

    const createOverlay = (id: string, inTime: number) => ({
      id,
      type: 'title' as const,
      text: 'Text',
      inTime,
      outTime: inTime + 3,
      alignment: 'topCenter' as const,
      x: 0,
      y: 0,
      fontSize: 72,
      fontColor: 'white',
      backgroundOpacity: 0.7,
      borderWidth: 2,
    });

    store.addOverlay(createOverlay('o2', 10));
    store.addOverlay(createOverlay('o1', 5));
    store.addOverlay(createOverlay('o3', 15));

    const state = useTimelineStore.getState();
    expect(state.overlays[0].id).toBe('o1');
    expect(state.overlays[1].id).toBe('o2');
    expect(state.overlays[2].id).toBe('o3');
  });

  it('should update zoom level', () => {
    const store = useTimelineStore.getState();

    store.setZoom(1.5);
    expect(useTimelineStore.getState().zoom).toBe(1.5);
  });

  it('should toggle snapping', () => {
    const store = useTimelineStore.getState();

    expect(store.snappingEnabled).toBe(true);

    store.setSnappingEnabled(false);
    expect(useTimelineStore.getState().snappingEnabled).toBe(false);
  });
});
