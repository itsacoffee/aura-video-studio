/**
 * OpenCut Markers Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useOpenCutMarkersStore } from '../opencutMarkers';
import type { MarkerType } from '../../types/opencut';

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
    it('should add a marker at specified time', () => {
      const { addMarker, markers } = useOpenCutMarkersStore.getState();

      const id = addMarker(5.5, { name: 'Test Marker' });

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers.length).toBe(1);
      expect(state.markers[0].id).toBe(id);
      expect(state.markers[0].time).toBe(5.5);
      expect(state.markers[0].name).toBe('Test Marker');
      expect(state.markers[0].type).toBe('standard');
      expect(state.markers[0].color).toBe('blue');
    });

    it('should add a chapter marker with green color', () => {
      const { addMarker } = useOpenCutMarkersStore.getState();

      addMarker(10, { type: 'chapter', name: 'Chapter 1' });

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers[0].type).toBe('chapter');
      expect(state.markers[0].color).toBe('green');
    });

    it('should add a todo marker with completed property', () => {
      const { addMarker } = useOpenCutMarkersStore.getState();

      addMarker(15, { type: 'todo', name: 'Review this' });

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers[0].type).toBe('todo');
      expect(state.markers[0].color).toBe('orange');
      expect(state.markers[0].completed).toBe(false);
    });

    it('should remove a marker', () => {
      const { addMarker, removeMarker } = useOpenCutMarkersStore.getState();

      const id = addMarker(5);
      expect(useOpenCutMarkersStore.getState().markers.length).toBe(1);

      removeMarker(id);
      expect(useOpenCutMarkersStore.getState().markers.length).toBe(0);
    });

    it('should update a marker', () => {
      const { addMarker, updateMarker } = useOpenCutMarkersStore.getState();

      const id = addMarker(5, { name: 'Original' });
      updateMarker(id, { name: 'Updated', color: 'red' });

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers[0].name).toBe('Updated');
      expect(state.markers[0].color).toBe('red');
    });

    it('should move a marker to new time', () => {
      const { addMarker, moveMarker } = useOpenCutMarkersStore.getState();

      const id = addMarker(5);
      moveMarker(id, 10);

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers[0].time).toBe(10);
    });

    it('should not allow negative time when moving', () => {
      const { addMarker, moveMarker } = useOpenCutMarkersStore.getState();

      const id = addMarker(5);
      moveMarker(id, -5);

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers[0].time).toBe(0);
    });

    it('should keep markers sorted by time', () => {
      const { addMarker } = useOpenCutMarkersStore.getState();

      addMarker(10, { name: 'Third' });
      addMarker(5, { name: 'Second' });
      addMarker(1, { name: 'First' });

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers[0].name).toBe('First');
      expect(state.markers[1].name).toBe('Second');
      expect(state.markers[2].name).toBe('Third');
    });
  });

  describe('Selection', () => {
    it('should select a marker', () => {
      const { addMarker, selectMarker } = useOpenCutMarkersStore.getState();

      const id = addMarker(5);
      selectMarker(id);

      expect(useOpenCutMarkersStore.getState().selectedMarkerId).toBe(id);
    });

    it('should deselect when selecting null', () => {
      const { addMarker, selectMarker } = useOpenCutMarkersStore.getState();

      const id = addMarker(5);
      selectMarker(id);
      selectMarker(null);

      expect(useOpenCutMarkersStore.getState().selectedMarkerId).toBeNull();
    });

    it('should clear selection when selected marker is removed', () => {
      const { addMarker, selectMarker, removeMarker } = useOpenCutMarkersStore.getState();

      const id = addMarker(5);
      selectMarker(id);
      removeMarker(id);

      expect(useOpenCutMarkersStore.getState().selectedMarkerId).toBeNull();
    });
  });

  describe('Navigation', () => {
    it('should go to next marker', () => {
      const { addMarker, goToNextMarker } = useOpenCutMarkersStore.getState();

      addMarker(5, { name: 'First' });
      addMarker(10, { name: 'Second' });
      addMarker(15, { name: 'Third' });

      const next = goToNextMarker(7);
      expect(next?.name).toBe('Second');
      expect(useOpenCutMarkersStore.getState().selectedMarkerId).toBe(next?.id);
    });

    it('should return null when no next marker exists', () => {
      const { addMarker, goToNextMarker } = useOpenCutMarkersStore.getState();

      addMarker(5, { name: 'Only' });

      const next = goToNextMarker(10);
      expect(next).toBeNull();
    });

    it('should go to previous marker', () => {
      const { addMarker, goToPreviousMarker } = useOpenCutMarkersStore.getState();

      addMarker(5, { name: 'First' });
      addMarker(10, { name: 'Second' });
      addMarker(15, { name: 'Third' });

      const prev = goToPreviousMarker(12);
      expect(prev?.name).toBe('Second');
    });

    it('should return null when no previous marker exists', () => {
      const { addMarker, goToPreviousMarker } = useOpenCutMarkersStore.getState();

      addMarker(10, { name: 'Only' });

      const prev = goToPreviousMarker(5);
      expect(prev).toBeNull();
    });

    it('should go to specific marker', () => {
      const { addMarker, goToMarker } = useOpenCutMarkersStore.getState();

      const id = addMarker(5, { name: 'Target' });
      addMarker(10, { name: 'Other' });

      const marker = goToMarker(id);
      expect(marker?.name).toBe('Target');
      expect(useOpenCutMarkersStore.getState().selectedMarkerId).toBe(id);
    });
  });

  describe('Filtering', () => {
    it('should set visible types', () => {
      const { setVisibleTypes } = useOpenCutMarkersStore.getState();

      setVisibleTypes(['standard', 'chapter']);

      expect(useOpenCutMarkersStore.getState().visibleTypes).toEqual(['standard', 'chapter']);
    });

    it('should toggle type visibility', () => {
      const { toggleTypeVisibility } = useOpenCutMarkersStore.getState();

      toggleTypeVisibility('beat');

      let state = useOpenCutMarkersStore.getState();
      expect(state.visibleTypes).not.toContain('beat');

      toggleTypeVisibility('beat');

      state = useOpenCutMarkersStore.getState();
      expect(state.visibleTypes).toContain('beat');
    });

    it('should filter markers by type', () => {
      const { addMarker, setVisibleTypes, getFilteredMarkers } = useOpenCutMarkersStore.getState();

      addMarker(5, { type: 'standard' });
      addMarker(10, { type: 'chapter' });
      addMarker(15, { type: 'todo' });

      setVisibleTypes(['standard', 'chapter']);

      const filtered = useOpenCutMarkersStore.getState().getFilteredMarkers();
      expect(filtered.length).toBe(2);
      expect(filtered.some((m) => m.type === 'todo')).toBe(false);
    });

    it('should filter markers by color', () => {
      const { addMarker, setFilterColor, getFilteredMarkers } = useOpenCutMarkersStore.getState();

      addMarker(5, { color: 'blue' });
      addMarker(10, { color: 'red' });
      addMarker(15, { color: 'blue' });

      setFilterColor('blue');

      const filtered = useOpenCutMarkersStore.getState().getFilteredMarkers();
      expect(filtered.length).toBe(2);
    });
  });

  describe('Bulk Operations', () => {
    it('should delete all markers', () => {
      const { addMarker, deleteAllMarkers } = useOpenCutMarkersStore.getState();

      addMarker(5);
      addMarker(10);
      addMarker(15);

      deleteAllMarkers();

      expect(useOpenCutMarkersStore.getState().markers.length).toBe(0);
    });

    it('should delete markers by type', () => {
      const { addMarker, deleteMarkersByType } = useOpenCutMarkersStore.getState();

      addMarker(5, { type: 'standard' });
      addMarker(10, { type: 'chapter' });
      addMarker(15, { type: 'chapter' });

      deleteMarkersByType('chapter');

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers.length).toBe(1);
      expect(state.markers[0].type).toBe('standard');
    });

    it('should toggle todo completion', () => {
      const { addMarker, toggleTodoComplete } = useOpenCutMarkersStore.getState();

      const id = addMarker(5, { type: 'todo' });

      expect(useOpenCutMarkersStore.getState().markers[0].completed).toBe(false);

      toggleTodoComplete(id);

      expect(useOpenCutMarkersStore.getState().markers[0].completed).toBe(true);

      toggleTodoComplete(id);

      expect(useOpenCutMarkersStore.getState().markers[0].completed).toBe(false);
    });

    it('should not toggle completion for non-todo markers', () => {
      const { addMarker, toggleTodoComplete } = useOpenCutMarkersStore.getState();

      const id = addMarker(5, { type: 'standard' });

      toggleTodoComplete(id);

      expect(useOpenCutMarkersStore.getState().markers[0].completed).toBeUndefined();
    });
  });

  describe('Export/Import', () => {
    it('should export chapter markers', () => {
      const { addMarker, exportChapterMarkers } = useOpenCutMarkersStore.getState();

      addMarker(5, { type: 'chapter', name: 'Intro' });
      addMarker(30, { type: 'chapter', name: 'Main Content' });
      addMarker(60, { type: 'standard', name: 'Note' });

      const chapters = useOpenCutMarkersStore.getState().exportChapterMarkers();
      expect(chapters.length).toBe(2);
      expect(chapters[0]).toEqual({ time: 5, title: 'Intro' });
      expect(chapters[1]).toEqual({ time: 30, title: 'Main Content' });
    });

    it('should import markers', () => {
      const { importMarkers } = useOpenCutMarkersStore.getState();

      importMarkers([
        { time: 5, name: 'First', type: 'standard' },
        { time: 10, name: 'Second', type: 'chapter' },
      ]);

      const state = useOpenCutMarkersStore.getState();
      expect(state.markers.length).toBe(2);
    });

    it('should ignore markers without time during import', () => {
      const { importMarkers } = useOpenCutMarkersStore.getState();

      importMarkers([
        { time: 5, name: 'Valid' },
        { name: 'Invalid - no time' } as { time?: number; name: string },
      ]);

      expect(useOpenCutMarkersStore.getState().markers.length).toBe(1);
    });
  });

  describe('Getters', () => {
    it('should get marker by id', () => {
      const { addMarker, getMarkerById } = useOpenCutMarkersStore.getState();

      const id = addMarker(5, { name: 'Target' });

      const marker = useOpenCutMarkersStore.getState().getMarkerById(id);
      expect(marker?.name).toBe('Target');
    });

    it('should get markers in range', () => {
      const { addMarker, getMarkersInRange } = useOpenCutMarkersStore.getState();

      addMarker(5, { name: 'First' });
      addMarker(15, { name: 'Second' });
      addMarker(25, { name: 'Third' });

      const markers = useOpenCutMarkersStore.getState().getMarkersInRange(10, 20);
      expect(markers.length).toBe(1);
      expect(markers[0].name).toBe('Second');
    });

    it('should get marker at time with tolerance', () => {
      const { addMarker, getMarkerAtTime } = useOpenCutMarkersStore.getState();

      addMarker(10, { name: 'Target' });

      const marker = useOpenCutMarkersStore.getState().getMarkerAtTime(10.05, 0.1);
      expect(marker?.name).toBe('Target');

      const noMarker = useOpenCutMarkersStore.getState().getMarkerAtTime(10.5, 0.1);
      expect(noMarker).toBeUndefined();
    });

    it('should get todo markers', () => {
      const { addMarker, getTodoMarkers } = useOpenCutMarkersStore.getState();

      addMarker(5, { type: 'todo', name: 'Todo 1' });
      addMarker(10, { type: 'standard', name: 'Standard' });
      addMarker(15, { type: 'todo', name: 'Todo 2' });

      const todos = useOpenCutMarkersStore.getState().getTodoMarkers();
      expect(todos.length).toBe(2);
    });

    it('should get chapter markers', () => {
      const { addMarker, getChapterMarkers } = useOpenCutMarkersStore.getState();

      addMarker(5, { type: 'chapter', name: 'Chapter 1' });
      addMarker(10, { type: 'standard', name: 'Note' });
      addMarker(15, { type: 'chapter', name: 'Chapter 2' });

      const chapters = useOpenCutMarkersStore.getState().getChapterMarkers();
      expect(chapters.length).toBe(2);
    });
  });
});
