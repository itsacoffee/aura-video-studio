/**
 * OpenCut Motion Graphics Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useMotionGraphicsStore, BUILTIN_GRAPHICS } from '../opencutMotionGraphics';

describe('MotionGraphicsStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useMotionGraphicsStore.setState({
      assets: BUILTIN_GRAPHICS,
      applied: [],
      selectedGraphicId: null,
      searchQuery: '',
      filterCategory: 'all',
      previewingAssetId: null,
    });
  });

  describe('Built-in Graphics', () => {
    it('should have built-in graphics', () => {
      const { assets } = useMotionGraphicsStore.getState();
      expect(assets.length).toBeGreaterThan(0);
      expect(assets.length).toBe(BUILTIN_GRAPHICS.length);
    });

    it('should have at least 17 built-in graphics', () => {
      const { assets } = useMotionGraphicsStore.getState();
      expect(assets.length).toBeGreaterThanOrEqual(17);
    });

    it('should have lower-thirds category graphics', () => {
      const { getAssetsByCategory } = useMotionGraphicsStore.getState();
      const lowerThirds = getAssetsByCategory('lower-thirds');
      expect(lowerThirds.length).toBe(5);
    });

    it('should have callouts category graphics', () => {
      const { getAssetsByCategory } = useMotionGraphicsStore.getState();
      const callouts = getAssetsByCategory('callouts');
      expect(callouts.length).toBe(3);
    });

    it('should have social category graphics', () => {
      const { getAssetsByCategory } = useMotionGraphicsStore.getState();
      const social = getAssetsByCategory('social');
      expect(social.length).toBe(3);
    });

    it('should have titles category graphics', () => {
      const { getAssetsByCategory } = useMotionGraphicsStore.getState();
      const titles = getAssetsByCategory('titles');
      expect(titles.length).toBe(3);
    });

    it('should have shapes category graphics', () => {
      const { getAssetsByCategory } = useMotionGraphicsStore.getState();
      const shapes = getAssetsByCategory('shapes');
      expect(shapes.length).toBe(3);
    });

    it('should get asset by ID', () => {
      const { getAsset } = useMotionGraphicsStore.getState();
      const asset = getAsset('lt-editorial-minimal');
      expect(asset).toBeDefined();
      expect(asset?.name).toBe('Editorial Minimal');
      expect(asset?.category).toBe('lower-thirds');
    });
  });

  describe('Add Graphics', () => {
    it('should add a graphic to timeline', () => {
      const { addGraphic } = useMotionGraphicsStore.getState();
      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);

      const state = useMotionGraphicsStore.getState();
      expect(state.applied.length).toBe(1);
      expect(state.applied[0].id).toBe(graphicId);
      expect(state.applied[0].assetId).toBe('lt-editorial-minimal');
      expect(state.applied[0].trackId).toBe('track-1');
      expect(state.applied[0].startTime).toBe(0);
    });

    it('should select the added graphic', () => {
      const { addGraphic } = useMotionGraphicsStore.getState();
      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);

      const state = useMotionGraphicsStore.getState();
      expect(state.selectedGraphicId).toBe(graphicId);
    });

    it('should initialize default values from customization schema', () => {
      const { addGraphic, getAsset } = useMotionGraphicsStore.getState();
      addGraphic('lt-editorial-minimal', 'track-1', 0);

      const state = useMotionGraphicsStore.getState();
      const asset = getAsset('lt-editorial-minimal');

      expect(state.applied[0].customValues['name']).toBe('John Smith');
      expect(state.applied[0].customValues['title']).toBe('Creative Director');
      expect(state.applied[0].customValues['accentColor']).toBe('#3B82F6');
    });

    it('should use asset duration as default', () => {
      const { addGraphic, getAsset } = useMotionGraphicsStore.getState();
      addGraphic('lt-editorial-minimal', 'track-1', 0);

      const state = useMotionGraphicsStore.getState();
      const asset = getAsset('lt-editorial-minimal');

      expect(state.applied[0].duration).toBe(asset?.duration);
    });

    it('should return empty string for non-existent asset', () => {
      const { addGraphic } = useMotionGraphicsStore.getState();
      const id = addGraphic('non-existent', 'track-1', 0);
      expect(id).toBe('');
      expect(useMotionGraphicsStore.getState().applied.length).toBe(0);
    });
  });

  describe('Remove Graphics', () => {
    it('should remove a graphic', () => {
      const { addGraphic, removeGraphic } = useMotionGraphicsStore.getState();

      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);
      expect(useMotionGraphicsStore.getState().applied.length).toBe(1);

      useMotionGraphicsStore.getState().removeGraphic(graphicId);
      expect(useMotionGraphicsStore.getState().applied.length).toBe(0);
    });

    it('should clear selection when removing selected graphic', () => {
      const { addGraphic, removeGraphic } = useMotionGraphicsStore.getState();

      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);
      expect(useMotionGraphicsStore.getState().selectedGraphicId).toBe(graphicId);

      useMotionGraphicsStore.getState().removeGraphic(graphicId);
      expect(useMotionGraphicsStore.getState().selectedGraphicId).toBeNull();
    });
  });

  describe('Update Graphics', () => {
    it('should update graphic properties', () => {
      const { addGraphic, updateGraphic } = useMotionGraphicsStore.getState();

      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);

      useMotionGraphicsStore.getState().updateGraphic(graphicId, {
        duration: 10,
        positionX: 25,
        positionY: 75,
      });

      const state = useMotionGraphicsStore.getState();
      expect(state.applied[0].duration).toBe(10);
      expect(state.applied[0].positionX).toBe(25);
      expect(state.applied[0].positionY).toBe(75);
    });

    it('should update single customization value', () => {
      const { addGraphic, updateGraphicValue } = useMotionGraphicsStore.getState();

      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);

      useMotionGraphicsStore.getState().updateGraphicValue(graphicId, 'name', 'Jane Doe');

      const state = useMotionGraphicsStore.getState();
      expect(state.applied[0].customValues['name']).toBe('Jane Doe');
    });

    it('should update scale', () => {
      const { addGraphic, updateGraphic } = useMotionGraphicsStore.getState();

      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);

      useMotionGraphicsStore.getState().updateGraphic(graphicId, { scale: 1.5 });

      const state = useMotionGraphicsStore.getState();
      expect(state.applied[0].scale).toBe(1.5);
    });

    it('should update opacity', () => {
      const { addGraphic, updateGraphic } = useMotionGraphicsStore.getState();

      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);

      useMotionGraphicsStore.getState().updateGraphic(graphicId, { opacity: 0.8 });

      const state = useMotionGraphicsStore.getState();
      expect(state.applied[0].opacity).toBe(0.8);
    });
  });

  describe('Duplicate Graphics', () => {
    it('should duplicate a graphic', () => {
      const { addGraphic, duplicateGraphic } = useMotionGraphicsStore.getState();

      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);
      expect(useMotionGraphicsStore.getState().applied.length).toBe(1);

      const duplicateId = useMotionGraphicsStore.getState().duplicateGraphic(graphicId);

      const state = useMotionGraphicsStore.getState();
      expect(state.applied.length).toBe(2);
      expect(duplicateId).not.toBe(graphicId);
    });

    it('should offset duplicate start time', () => {
      const { addGraphic, duplicateGraphic, getAsset } = useMotionGraphicsStore.getState();

      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);
      const original = useMotionGraphicsStore.getState().applied[0];

      useMotionGraphicsStore.getState().duplicateGraphic(graphicId);

      const state = useMotionGraphicsStore.getState();
      const duplicate = state.applied[1];

      expect(duplicate.startTime).toBe(original.startTime + original.duration + 0.5);
    });

    it('should copy custom values to duplicate', () => {
      const { addGraphic, updateGraphicValue, duplicateGraphic } =
        useMotionGraphicsStore.getState();

      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);
      useMotionGraphicsStore.getState().updateGraphicValue(graphicId, 'name', 'Test Name');

      useMotionGraphicsStore.getState().duplicateGraphic(graphicId);

      const state = useMotionGraphicsStore.getState();
      expect(state.applied[1].customValues['name']).toBe('Test Name');
    });

    it('should select the duplicate', () => {
      const { addGraphic, duplicateGraphic } = useMotionGraphicsStore.getState();

      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);
      const duplicateId = useMotionGraphicsStore.getState().duplicateGraphic(graphicId);

      expect(useMotionGraphicsStore.getState().selectedGraphicId).toBe(duplicateId);
    });
  });

  describe('Selection', () => {
    it('should select a graphic', () => {
      const { addGraphic, selectGraphic } = useMotionGraphicsStore.getState();

      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);
      addGraphic('lt-glass-morphism', 'track-1', 5);

      useMotionGraphicsStore.getState().selectGraphic(graphicId);

      expect(useMotionGraphicsStore.getState().selectedGraphicId).toBe(graphicId);
    });

    it('should get selected graphic', () => {
      const { addGraphic, getSelectedGraphic } = useMotionGraphicsStore.getState();

      const graphicId = addGraphic('lt-editorial-minimal', 'track-1', 0);

      const selected = useMotionGraphicsStore.getState().getSelectedGraphic();
      expect(selected).toBeDefined();
      expect(selected?.id).toBe(graphicId);
    });

    it('should clear selection', () => {
      const { addGraphic, selectGraphic } = useMotionGraphicsStore.getState();

      addGraphic('lt-editorial-minimal', 'track-1', 0);
      useMotionGraphicsStore.getState().selectGraphic(null);

      expect(useMotionGraphicsStore.getState().selectedGraphicId).toBeNull();
    });
  });

  describe('Search and Filter', () => {
    it('should set search query', () => {
      const { setSearchQuery } = useMotionGraphicsStore.getState();

      setSearchQuery('minimal');

      expect(useMotionGraphicsStore.getState().searchQuery).toBe('minimal');
    });

    it('should set filter category', () => {
      const { setFilterCategory } = useMotionGraphicsStore.getState();

      setFilterCategory('lower-thirds');

      expect(useMotionGraphicsStore.getState().filterCategory).toBe('lower-thirds');
    });

    it('should search assets by name', () => {
      const { searchAssets } = useMotionGraphicsStore.getState();

      const results = searchAssets('Editorial');

      expect(results.length).toBeGreaterThan(0);
      expect(results.some((a) => a.name.includes('Editorial'))).toBe(true);
    });

    it('should search assets by tag', () => {
      const { searchAssets } = useMotionGraphicsStore.getState();

      const results = searchAssets('minimal');

      expect(results.length).toBeGreaterThan(0);
      expect(results.some((a) => a.tags.includes('minimal'))).toBe(true);
    });

    it('should search assets by description', () => {
      const { searchAssets } = useMotionGraphicsStore.getState();

      const results = searchAssets('documentary');

      expect(results.length).toBeGreaterThan(0);
    });

    it('should get filtered assets', () => {
      useMotionGraphicsStore.setState({ filterCategory: 'callouts' });

      const { getFilteredAssets } = useMotionGraphicsStore.getState();
      const results = getFilteredAssets();

      expect(results.length).toBe(3);
      expect(results.every((a) => a.category === 'callouts')).toBe(true);
    });

    it('should combine category filter with search', () => {
      useMotionGraphicsStore.setState({
        filterCategory: 'lower-thirds',
        searchQuery: 'glass',
      });

      const { getFilteredAssets } = useMotionGraphicsStore.getState();
      const results = getFilteredAssets();

      expect(results.length).toBe(1);
      expect(results[0].id).toBe('lt-glass-morphism');
    });
  });

  describe('Query Operations', () => {
    it('should get graphics for a specific track', () => {
      const { addGraphic, getGraphicsForTrack } = useMotionGraphicsStore.getState();

      addGraphic('lt-editorial-minimal', 'track-1', 0);
      addGraphic('lt-glass-morphism', 'track-1', 5);
      addGraphic('co-focus-circle', 'track-2', 0);

      const track1Graphics = useMotionGraphicsStore.getState().getGraphicsForTrack('track-1');
      expect(track1Graphics.length).toBe(2);
      expect(track1Graphics.every((g) => g.trackId === 'track-1')).toBe(true);
    });

    it('should get graphics in time range', () => {
      const { addGraphic, getGraphicsInRange } = useMotionGraphicsStore.getState();

      addGraphic('lt-editorial-minimal', 'track-1', 0); // 0-5
      addGraphic('lt-glass-morphism', 'track-1', 10); // 10-15
      addGraphic('co-focus-circle', 'track-1', 20); // 20-24

      const inRange = useMotionGraphicsStore.getState().getGraphicsInRange(2, 12);

      // Should include first two (overlapping with 2-12)
      expect(inRange.length).toBe(2);
    });

    it('should return empty array for track with no graphics', () => {
      const { getGraphicsForTrack } = useMotionGraphicsStore.getState();
      const graphics = getGraphicsForTrack('non-existent');
      expect(graphics).toEqual([]);
    });
  });

  describe('Preview State', () => {
    it('should set previewing asset', () => {
      const { setPreviewingAsset } = useMotionGraphicsStore.getState();

      setPreviewingAsset('lt-editorial-minimal');

      expect(useMotionGraphicsStore.getState().previewingAssetId).toBe('lt-editorial-minimal');
    });

    it('should clear previewing asset', () => {
      const { setPreviewingAsset } = useMotionGraphicsStore.getState();

      setPreviewingAsset('lt-editorial-minimal');
      setPreviewingAsset(null);

      expect(useMotionGraphicsStore.getState().previewingAssetId).toBeNull();
    });
  });
});
