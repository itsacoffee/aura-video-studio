/**
 * OpenCut Markers Store
 *
 * Manages timeline markers for annotating, organizing, and navigating video projects.
 * Supports standard markers, chapter markers, to-do markers, and beat markers.
 */

import { create } from 'zustand';
import type { Marker, MarkerType, MarkerColor } from '../types/opencut';

function generateId(): string {
  return `marker-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

interface OpenCutMarkersState {
  markers: Marker[];
  selectedMarkerId: string | null;
  visibleTypes: MarkerType[];
  filterColor: MarkerColor | null;
}

interface OpenCutMarkersActions {
  // CRUD
  addMarker: (time: number, options?: Partial<Omit<Marker, 'id' | 'time'>>) => string;
  removeMarker: (markerId: string) => void;
  updateMarker: (markerId: string, updates: Partial<Marker>) => void;
  moveMarker: (markerId: string, newTime: number) => void;

  // Selection
  selectMarker: (markerId: string | null) => void;

  // Navigation
  goToNextMarker: (currentTime: number) => Marker | null;
  goToPreviousMarker: (currentTime: number) => Marker | null;
  goToMarker: (markerId: string) => Marker | null;

  // Filtering
  setVisibleTypes: (types: MarkerType[]) => void;
  toggleTypeVisibility: (type: MarkerType) => void;
  setFilterColor: (color: MarkerColor | null) => void;

  // Bulk operations
  deleteAllMarkers: () => void;
  deleteMarkersByType: (type: MarkerType) => void;
  toggleTodoComplete: (markerId: string) => void;

  // Export
  exportChapterMarkers: () => { time: number; title: string }[];
  importMarkers: (markers: Partial<Marker>[]) => void;

  // Getters
  getMarkerById: (markerId: string) => Marker | undefined;
  getMarkersInRange: (startTime: number, endTime: number) => Marker[];
  getMarkerAtTime: (time: number, tolerance?: number) => Marker | undefined;
  getFilteredMarkers: () => Marker[];
  getTodoMarkers: () => Marker[];
  getChapterMarkers: () => Marker[];
}

export type OpenCutMarkersStore = OpenCutMarkersState & OpenCutMarkersActions;

const MARKER_COLORS: Record<MarkerType, MarkerColor> = {
  standard: 'blue',
  chapter: 'green',
  todo: 'orange',
  beat: 'purple',
};

export const useOpenCutMarkersStore = create<OpenCutMarkersStore>((set, get) => ({
  markers: [],
  selectedMarkerId: null,
  visibleTypes: ['standard', 'chapter', 'todo', 'beat'],
  filterColor: null,

  addMarker: (time, options = {}) => {
    const id = generateId();
    const type = options.type || 'standard';
    const marker: Marker = {
      id,
      time,
      type,
      color: options.color || MARKER_COLORS[type],
      name: options.name || `Marker ${get().markers.length + 1}`,
      notes: options.notes,
      duration: options.duration,
      completed: type === 'todo' ? false : undefined,
    };
    set((state) => ({
      markers: [...state.markers, marker].sort((a, b) => a.time - b.time),
    }));
    return id;
  },

  removeMarker: (markerId) => {
    set((state) => ({
      markers: state.markers.filter((m) => m.id !== markerId),
      selectedMarkerId: state.selectedMarkerId === markerId ? null : state.selectedMarkerId,
    }));
  },

  updateMarker: (markerId, updates) => {
    set((state) => ({
      markers: state.markers
        .map((m) => (m.id === markerId ? { ...m, ...updates } : m))
        .sort((a, b) => a.time - b.time),
    }));
  },

  moveMarker: (markerId, newTime) => {
    get().updateMarker(markerId, { time: Math.max(0, newTime) });
  },

  selectMarker: (markerId) => set({ selectedMarkerId: markerId }),

  goToNextMarker: (currentTime) => {
    const filtered = get().getFilteredMarkers();
    const next = filtered.find((m) => m.time > currentTime + 0.01);
    if (next) set({ selectedMarkerId: next.id });
    return next || null;
  },

  goToPreviousMarker: (currentTime) => {
    const filtered = get().getFilteredMarkers();
    const prev = [...filtered].reverse().find((m) => m.time < currentTime - 0.01);
    if (prev) set({ selectedMarkerId: prev.id });
    return prev || null;
  },

  goToMarker: (markerId) => {
    const marker = get().getMarkerById(markerId);
    if (marker) set({ selectedMarkerId: markerId });
    return marker || null;
  },

  setVisibleTypes: (types) => set({ visibleTypes: types }),

  toggleTypeVisibility: (type) => {
    set((state) => ({
      visibleTypes: state.visibleTypes.includes(type)
        ? state.visibleTypes.filter((t) => t !== type)
        : [...state.visibleTypes, type],
    }));
  },

  setFilterColor: (color) => set({ filterColor: color }),

  deleteAllMarkers: () => set({ markers: [], selectedMarkerId: null }),

  deleteMarkersByType: (type) => {
    set((state) => ({
      markers: state.markers.filter((m) => m.type !== type),
      selectedMarkerId:
        state.markers.find((m) => m.id === state.selectedMarkerId)?.type === type
          ? null
          : state.selectedMarkerId,
    }));
  },

  toggleTodoComplete: (markerId) => {
    const marker = get().getMarkerById(markerId);
    if (marker?.type === 'todo') {
      get().updateMarker(markerId, { completed: !marker.completed });
    }
  },

  exportChapterMarkers: () => {
    return get()
      .markers.filter((m) => m.type === 'chapter')
      .map((m) => ({ time: m.time, title: m.name }));
  },

  importMarkers: (markers) => {
    markers.forEach((m) => {
      if (m.time !== undefined) {
        get().addMarker(m.time, m);
      }
    });
  },

  getMarkerById: (markerId) => get().markers.find((m) => m.id === markerId),

  getMarkersInRange: (startTime, endTime) => {
    return get().markers.filter((m) => m.time >= startTime && m.time <= endTime);
  },

  getMarkerAtTime: (time, tolerance = 0.1) => {
    return get().markers.find((m) => Math.abs(m.time - time) <= tolerance);
  },

  getFilteredMarkers: () => {
    const { markers, visibleTypes, filterColor } = get();
    return markers.filter((m) => {
      if (!visibleTypes.includes(m.type)) return false;
      if (filterColor && m.color !== filterColor) return false;
      return true;
    });
  },

  getTodoMarkers: () => get().markers.filter((m) => m.type === 'todo'),

  getChapterMarkers: () => get().markers.filter((m) => m.type === 'chapter'),
}));
