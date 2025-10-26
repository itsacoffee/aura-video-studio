import type { TimelineClip, Track, ChapterMarker, TextOverlay } from '../../state/timeline';

/**
 * Test data factory for creating TimelineClip objects
 */
export function createMockTimelineClip(overrides?: Partial<TimelineClip>): TimelineClip {
  return {
    id: 'clip-1',
    sourcePath: '/path/to/video.mp4',
    sourceIn: 0,
    sourceOut: 10,
    timelineStart: 0,
    trackId: 'track-1',
    ...overrides,
  };
}

/**
 * Test data factory for creating Track objects
 */
export function createMockTrack(overrides?: Partial<Track>): Track {
  return {
    id: 'track-1',
    name: 'Video Track 1',
    type: 'video',
    clips: [],
    muted: false,
    solo: false,
    volume: 100,
    pan: 0,
    locked: false,
    height: 80,
    ...overrides,
  };
}

/**
 * Test data factory for creating ChapterMarker objects
 */
export function createMockChapterMarker(overrides?: Partial<ChapterMarker>): ChapterMarker {
  return {
    id: 'marker-1',
    title: 'Chapter 1',
    time: 0,
    ...overrides,
  };
}

/**
 * Test data factory for creating TextOverlay objects
 */
export function createMockTextOverlay(overrides?: Partial<TextOverlay>): TextOverlay {
  return {
    id: 'overlay-1',
    type: 'title',
    text: 'Sample Title',
    inTime: 0,
    outTime: 5,
    alignment: 'middleCenter',
    x: 0,
    y: 0,
    fontSize: 48,
    fontColor: '#FFFFFF',
    backgroundColor: '#000000',
    backgroundOpacity: 0.5,
    borderWidth: 0,
    borderColor: '#000000',
    ...overrides,
  };
}

/**
 * Test data factory for creating multiple clips at once
 */
export function createMockClips(
  count: number,
  baseOverrides?: Partial<TimelineClip>
): TimelineClip[] {
  return Array.from({ length: count }, (_, index) =>
    createMockTimelineClip({
      ...baseOverrides,
      id: `clip-${index + 1}`,
      timelineStart: index * 10,
    })
  );
}

/**
 * Test data factory for creating multiple tracks at once
 */
export function createMockTracks(count: number, baseOverrides?: Partial<Track>): Track[] {
  return Array.from({ length: count }, (_, index) =>
    createMockTrack({
      ...baseOverrides,
      id: `track-${index + 1}`,
      name: `Track ${index + 1}`,
    })
  );
}
