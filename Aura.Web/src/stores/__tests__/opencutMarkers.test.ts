/**
 * OpenCut Markers Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useOpenCutMarkersStore } from '../opencutMarkers';

describe('OpenCutMarkersStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useOpenCutMarkersStore.setState({
      markers: [],
      selectedMarkerId: null,
      visibleTypes: ['standard', 'chapter', 'todo', 'beat'],
      filterColor: null,
    });
  });

  describe('CRUD Operations', () => {
    it('should add a standard marker at specified time', () => {
      const { addMarker, markers } = useOpenCutMarkersStore.getState();

      const markerId = addMarker(5);

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers.length).toBe(1);
      expect(state.markers[0].time).toBe(5);
      expect(state.markers[0].type).toBe('standard');
      expect(state.markers[0].color).toBe('blue');
      expect(state.markers[0].name).toBe('Marker 1');
    });

    it('should add a chapter marker with custom options', () => {
      const { addMarker } = useOpenCutMarkersStore.getState();

      addMarker(10, { type: 'chapter', name: 'Introduction' });

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers[0].type).toBe('chapter');
      expect(state.markers[0].name).toBe('Introduction');
      expect(state.markers[0].color).toBe('green');
    });

    it('should add a to-do marker with completed=false by default', () => {
      const { addMarker } = useOpenCutMarkersStore.getState();

      addMarker(15, { type: 'todo', name: 'Fix audio sync' });

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers[0].type).toBe('todo');
      expect(state.markers[0].completed).toBe(false);
    });

    it('should remove a marker', () => {
      const { addMarker, removeMarker } = useOpenCutMarkersStore.getState();

      const markerId = addMarker(5);
      expect(useOpenCutMarkersStore.getState().markers.length).toBe(1);

      useOpenCutMarkersStore.getState().removeMarker(markerId);
      expect(useOpenCutMarkersStore.getState().markers.length).toBe(0);
    });

    it('should update a marker', () => {
      const { addMarker, updateMarker } = useOpenCutMarkersStore.getState();

      const markerId = addMarker(5, { name: 'Original' });

      useOpenCutMarkersStore.getState().updateMarker(markerId, { name: 'Updated' });

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers[0].name).toBe('Updated');
    });

    it('should move a marker to a new time', () => {
      const { addMarker, moveMarker } = useOpenCutMarkersStore.getState();

      const markerId = addMarker(5);

      useOpenCutMarkersStore.getState().moveMarker(markerId, 10);

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers[0].time).toBe(10);
    });

    it('should not allow negative time values', () => {
      const { addMarker, moveMarker } = useOpenCutMarkersStore.getState();

      const markerId = addMarker(5);

      useOpenCutMarkersStore.getState().moveMarker(markerId, -5);

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers[0].time).toBe(0);
    });

    it('should sort markers by time when adding', () => {
      const { addMarker } = useOpenCutMarkersStore.getState();

      addMarker(10);
      addMarker(5);
      addMarker(15);

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers[0].time).toBe(5);
      expect(state.markers[1].time).toBe(10);
      expect(state.markers[2].time).toBe(15);
    });
  });

  describe('Selection Operations', () => {
    it('should select a marker', () => {
      const { addMarker, selectMarker } = useOpenCutMarkersStore.getState();

      const markerId = addMarker(5);
      useOpenCutMarkersStore.getState().selectMarker(markerId);

      const state = useOpenCutMarkersStore.getState();
      expect(state.selectedMarkerId).toBe(markerId);
    });

    it('should clear selection when selecting null', () => {
      const { addMarker, selectMarker } = useOpenCutMarkersStore.getState();

      const markerId = addMarker(5);
      useOpenCutMarkersStore.getState().selectMarker(markerId);
      useOpenCutMarkersStore.getState().selectMarker(null);

      const state = useOpenCutMarkersStore.getState();
      expect(state.selectedMarkerId).toBeNull();
    });

    it('should clear selection when removing selected marker', () => {
      const { addMarker, selectMarker, removeMarker } = useOpenCutMarkersStore.getState();

      const markerId = addMarker(5);
      useOpenCutMarkersStore.getState().selectMarker(markerId);
      useOpenCutMarkersStore.getState().removeMarker(markerId);

      const state = useOpenCutMarkersStore.getState();
      expect(state.selectedMarkerId).toBeNull();
    });
  });

  describe('Navigation Operations', () => {
    it('should go to next marker', () => {
      const { addMarker, goToNextMarker } = useOpenCutMarkersStore.getState();

      addMarker(5);
      addMarker(10);
      addMarker(15);

      const nextMarker = useOpenCutMarkersStore.getState().goToNextMarker(7);

      expect(nextMarker).not.toBeNull();
      expect(nextMarker?.time).toBe(10);
      expect(useOpenCutMarkersStore.getState().selectedMarkerId).toBe(nextMarker?.id);
    });

    it('should return null when no next marker exists', () => {
      const { addMarker, goToNextMarker } = useOpenCutMarkersStore.getState();

      addMarker(5);

      const nextMarker = useOpenCutMarkersStore.getState().goToNextMarker(10);

      expect(nextMarker).toBeNull();
    });

    it('should go to previous marker', () => {
      const { addMarker, goToPreviousMarker } = useOpenCutMarkersStore.getState();

      addMarker(5);
      addMarker(10);
      addMarker(15);

      const prevMarker = useOpenCutMarkersStore.getState().goToPreviousMarker(12);

      expect(prevMarker).not.toBeNull();
      expect(prevMarker?.time).toBe(10);
    });

    it('should return null when no previous marker exists', () => {
      const { addMarker, goToPreviousMarker } = useOpenCutMarkersStore.getState();

      addMarker(10);

      const prevMarker = useOpenCutMarkersStore.getState().goToPreviousMarker(5);

      expect(prevMarker).toBeNull();
    });

    it('should go to specific marker by ID', () => {
      const { addMarker, goToMarker } = useOpenCutMarkersStore.getState();

      addMarker(5);
      const targetId = useOpenCutMarkersStore.getState().addMarker(10);
      addMarker(15);

      const marker = useOpenCutMarkersStore.getState().goToMarker(targetId);

      expect(marker).not.toBeNull();
      expect(marker?.time).toBe(10);
      expect(useOpenCutMarkersStore.getState().selectedMarkerId).toBe(targetId);
    });
  });

  describe('Filtering Operations', () => {
    it('should toggle type visibility', () => {
      const { toggleTypeVisibility, visibleTypes } = useOpenCutMarkersStore.getState();

      expect(useOpenCutMarkersStore.getState().visibleTypes).toContain('todo');

      useOpenCutMarkersStore.getState().toggleTypeVisibility('todo');

      expect(useOpenCutMarkersStore.getState().visibleTypes).not.toContain('todo');

      useOpenCutMarkersStore.getState().toggleTypeVisibility('todo');

      expect(useOpenCutMarkersStore.getState().visibleTypes).toContain('todo');
    });

    it('should set visible types', () => {
      const { setVisibleTypes } = useOpenCutMarkersStore.getState();

      useOpenCutMarkersStore.getState().setVisibleTypes(['chapter', 'beat']);

      const state = useOpenCutMarkersStore.getState();
      expect(state.visibleTypes).toEqual(['chapter', 'beat']);
    });

    it('should set filter color', () => {
      const { setFilterColor } = useOpenCutMarkersStore.getState();

      useOpenCutMarkersStore.getState().setFilterColor('red');

      expect(useOpenCutMarkersStore.getState().filterColor).toBe('red');

      useOpenCutMarkersStore.getState().setFilterColor(null);

      expect(useOpenCutMarkersStore.getState().filterColor).toBeNull();
    });

    it('should filter markers by type and color', () => {
      const { addMarker, toggleTypeVisibility, setFilterColor, getFilteredMarkers } =
        useOpenCutMarkersStore.getState();

      addMarker(5, { type: 'standard', color: 'blue' });
      addMarker(10, { type: 'chapter', color: 'green' });
      addMarker(15, { type: 'todo', color: 'orange' });

      // Filter by type
      useOpenCutMarkersStore.getState().toggleTypeVisibility('todo');
      let filtered = useOpenCutMarkersStore.getState().getFilteredMarkers();
      expect(filtered.length).toBe(2);
      expect(filtered.every((m) => m.type !== 'todo')).toBe(true);

      // Reset type filter
      useOpenCutMarkersStore.getState().toggleTypeVisibility('todo');

      // Filter by color
      useOpenCutMarkersStore.getState().setFilterColor('blue');
      filtered = useOpenCutMarkersStore.getState().getFilteredMarkers();
      expect(filtered.length).toBe(1);
      expect(filtered[0].color).toBe('blue');
    });
  });

  describe('Bulk Operations', () => {
    it('should delete all markers', () => {
      const { addMarker, deleteAllMarkers } = useOpenCutMarkersStore.getState();

      addMarker(5);
      addMarker(10);
      addMarker(15);

      expect(useOpenCutMarkersStore.getState().markers.length).toBe(3);

      useOpenCutMarkersStore.getState().deleteAllMarkers();

      expect(useOpenCutMarkersStore.getState().markers.length).toBe(0);
      expect(useOpenCutMarkersStore.getState().selectedMarkerId).toBeNull();
    });

    it('should delete markers by type', () => {
      const { addMarker, deleteMarkersByType } = useOpenCutMarkersStore.getState();

      addMarker(5, { type: 'standard' });
      addMarker(10, { type: 'chapter' });
      addMarker(15, { type: 'todo' });
      addMarker(20, { type: 'todo' });

      expect(useOpenCutMarkersStore.getState().markers.length).toBe(4);

      useOpenCutMarkersStore.getState().deleteMarkersByType('todo');

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers.length).toBe(2);
      expect(state.markers.every((m) => m.type !== 'todo')).toBe(true);
    });

    it('should toggle to-do completion', () => {
      const { addMarker, toggleTodoComplete } = useOpenCutMarkersStore.getState();

      const todoId = addMarker(5, { type: 'todo' });

      expect(useOpenCutMarkersStore.getState().markers[0].completed).toBe(false);

      useOpenCutMarkersStore.getState().toggleTodoComplete(todoId);

      expect(useOpenCutMarkersStore.getState().markers[0].completed).toBe(true);

      useOpenCutMarkersStore.getState().toggleTodoComplete(todoId);

      expect(useOpenCutMarkersStore.getState().markers[0].completed).toBe(false);
    });

    it('should not toggle completion for non-todo markers', () => {
      const { addMarker, toggleTodoComplete } = useOpenCutMarkersStore.getState();

      const markerId = addMarker(5, { type: 'standard' });

      useOpenCutMarkersStore.getState().toggleTodoComplete(markerId);

      // Should have no effect
      expect(useOpenCutMarkersStore.getState().markers[0].completed).toBeUndefined();
    });
  });

  describe('Export/Import Operations', () => {
    it('should export chapter markers', () => {
      const { addMarker, exportChapterMarkers } = useOpenCutMarkersStore.getState();

      addMarker(0, { type: 'chapter', name: 'Intro' });
      addMarker(30, { type: 'standard', name: 'Standard Marker' });
      addMarker(60, { type: 'chapter', name: 'Main Content' });
      addMarker(120, { type: 'chapter', name: 'Conclusion' });

      const chapters = useOpenCutMarkersStore.getState().exportChapterMarkers();

      expect(chapters.length).toBe(3);
      expect(chapters[0]).toEqual({ time: 0, title: 'Intro' });
      expect(chapters[1]).toEqual({ time: 60, title: 'Main Content' });
      expect(chapters[2]).toEqual({ time: 120, title: 'Conclusion' });
    });

    it('should import markers', () => {
      const { importMarkers } = useOpenCutMarkersStore.getState();

      useOpenCutMarkersStore.getState().importMarkers([
        { time: 5, name: 'Imported 1', type: 'standard' },
        { time: 10, name: 'Imported 2', type: 'chapter' },
        { time: 15, name: 'Imported 3', type: 'todo' },
      ]);

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers.length).toBe(3);
      expect(state.markers[0].name).toBe('Imported 1');
      expect(state.markers[1].name).toBe('Imported 2');
      expect(state.markers[2].name).toBe('Imported 3');
    });

    it('should skip markers without time during import', () => {
      const { importMarkers } = useOpenCutMarkersStore.getState();

      useOpenCutMarkersStore
        .getState()
        .importMarkers([
          { time: 5, name: 'Valid' },
          { name: 'Invalid - no time' } as Partial<{ time: number; name: string }>,
          { time: 10, name: 'Also Valid' },
        ]);

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers.length).toBe(2);
    });
  });

  describe('Query Operations', () => {
    it('should get marker by ID', () => {
      const { addMarker, getMarkerById } = useOpenCutMarkersStore.getState();

      const id1 = addMarker(5, { name: 'First' });
      const id2 = addMarker(10, { name: 'Second' });

      const marker = useOpenCutMarkersStore.getState().getMarkerById(id2);

      expect(marker).toBeDefined();
      expect(marker?.name).toBe('Second');
    });

    it('should return undefined for non-existent marker ID', () => {
      const { getMarkerById } = useOpenCutMarkersStore.getState();

      const marker = getMarkerById('non-existent-id');

      expect(marker).toBeUndefined();
    });

    it('should get markers in time range', () => {
      const { addMarker, getMarkersInRange } = useOpenCutMarkersStore.getState();

      addMarker(5);
      addMarker(10);
      addMarker(15);
      addMarker(20);

      const inRange = useOpenCutMarkersStore.getState().getMarkersInRange(8, 18);

      expect(inRange.length).toBe(2);
      expect(inRange[0].time).toBe(10);
      expect(inRange[1].time).toBe(15);
    });

    it('should get marker at time with tolerance', () => {
      const { addMarker, getMarkerAtTime } = useOpenCutMarkersStore.getState();

      addMarker(10);

      // Within tolerance
      let marker = useOpenCutMarkersStore.getState().getMarkerAtTime(10.05, 0.1);
      expect(marker).toBeDefined();

      // Outside tolerance
      marker = useOpenCutMarkersStore.getState().getMarkerAtTime(10.2, 0.1);
      expect(marker).toBeUndefined();
    });

    it('should get to-do markers', () => {
      const { addMarker, getTodoMarkers } = useOpenCutMarkersStore.getState();

      addMarker(5, { type: 'standard' });
      addMarker(10, { type: 'todo' });
      addMarker(15, { type: 'chapter' });
      addMarker(20, { type: 'todo' });

      const todos = useOpenCutMarkersStore.getState().getTodoMarkers();

      expect(todos.length).toBe(2);
      expect(todos.every((m) => m.type === 'todo')).toBe(true);
    });

    it('should get chapter markers', () => {
      const { addMarker, getChapterMarkers } = useOpenCutMarkersStore.getState();

      addMarker(0, { type: 'chapter' });
      addMarker(30, { type: 'standard' });
      addMarker(60, { type: 'chapter' });

      const chapters = useOpenCutMarkersStore.getState().getChapterMarkers();

      expect(chapters.length).toBe(2);
      expect(chapters.every((m) => m.type === 'chapter')).toBe(true);
    });
  });
});
