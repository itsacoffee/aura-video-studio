/**
 * Test Data Factories
 *
 * This directory contains factory functions for creating test data objects.
 * These factories help maintain consistency across tests and make it easy to
 * create test data with sensible defaults while allowing for customization.
 *
 * ## Usage
 *
 * ```typescript
 * import { createMockTimelineClip, createMockTrack } from '@/test/factories/timelineFactories';
 *
 * // Create a clip with default values
 * const clip = createMockTimelineClip();
 *
 * // Create a clip with custom values
 * const customClip = createMockTimelineClip({
 *   id: 'custom-clip-1',
 *   timelineStart: 30,
 * });
 *
 * // Create multiple clips
 * const clips = createMockClips(5, { trackId: 'track-1' });
 * ```
 *
 * ## Available Factories
 *
 * ### Timeline Factories (timelineFactories.ts)
 * - `createMockTimelineClip()` - Create a timeline clip
 * - `createMockTrack()` - Create a track
 * - `createMockChapterMarker()` - Create a chapter marker
 * - `createMockTextOverlay()` - Create a text overlay
 * - `createMockClips(count)` - Create multiple clips
 * - `createMockTracks(count)` - Create multiple tracks
 *
 * ### Project Factories (projectFactories.ts)
 * - `createMockBrief()` - Create a project brief
 * - `createMockPlanSpec()` - Create a plan specification
 * - `createMockVoiceSpec()` - Create a voice specification
 * - `createMockBrandKitConfig()` - Create brand kit configuration
 * - `createMockCaptionsConfig()` - Create captions configuration
 * - `createMockStockSourcesConfig()` - Create stock sources configuration
 *
 * ### System Factories (systemFactories.ts)
 * - `createMockHardwareCapabilities()` - Create hardware capabilities
 * - `createMockRenderJob()` - Create a render job
 * - `createMockProfile()` - Create a profile
 * - `createMockDownloadItem()` - Create a download item
 * - `createMockRenderJobs(count)` - Create multiple render jobs
 *
 * ## Best Practices
 *
 * 1. **Use factories instead of inline objects** - This ensures consistency and makes tests easier to maintain
 * 2. **Only override what you need** - Let the factory provide sensible defaults
 * 3. **Add new factories as needed** - If you find yourself creating the same test data repeatedly, add a factory
 * 4. **Keep factories simple** - Factories should be straightforward and predictable
 * 5. **Document special cases** - If a factory has special behavior, document it
 */

export * from './timelineFactories';
export * from './projectFactories';
export * from './systemFactories';
