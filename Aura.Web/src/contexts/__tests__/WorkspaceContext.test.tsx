/**
 * Tests for WorkspaceContext
 */

import { renderHook, act } from '@testing-library/react';
import type { ReactNode } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { DEFAULT_WORKSPACE_STATE } from '../../types/project';
import { WorkspaceProvider, useWorkspace, useWorkspaceOptional } from '../WorkspaceContext';

// Wrapper component for testing hooks with context and router
function createWrapper(initialPath = '/') {
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <MemoryRouter initialEntries={[initialPath]}>
        <WorkspaceProvider>{children}</WorkspaceProvider>
      </MemoryRouter>
    );
  };
}

describe('WorkspaceContext', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useWorkspace hook', () => {
    it('throws error when used outside provider', () => {
      // Create a wrapper without the provider
      const WrapperWithoutProvider = ({ children }: { children: ReactNode }) => (
        <MemoryRouter>{children}</MemoryRouter>
      );

      expect(() => {
        renderHook(() => useWorkspace(), { wrapper: WrapperWithoutProvider });
      }).toThrow('useWorkspace must be used within a WorkspaceProvider');
    });

    it('provides workspace context when used within provider', () => {
      const { result } = renderHook(() => useWorkspace(), {
        wrapper: createWrapper(),
      });

      expect(result.current).toBeDefined();
      expect(result.current.workspaceState).toBeDefined();
      expect(result.current.setTimelineViewport).toBeDefined();
      expect(result.current.setSelection).toBeDefined();
      expect(result.current.setPanelState).toBeDefined();
      expect(result.current.setPreviewState).toBeDefined();
      expect(result.current.setMediaLibraryState).toBeDefined();
    });

    it('provides isDirty flag', () => {
      const { result } = renderHook(() => useWorkspace(), {
        wrapper: createWrapper(),
      });

      expect(typeof result.current.isDirty).toBe('boolean');
    });
  });

  describe('useWorkspaceOptional hook', () => {
    it('returns undefined when used outside provider', () => {
      const WrapperWithoutProvider = ({ children }: { children: ReactNode }) => (
        <MemoryRouter>{children}</MemoryRouter>
      );

      const { result } = renderHook(() => useWorkspaceOptional(), {
        wrapper: WrapperWithoutProvider,
      });

      expect(result.current).toBeUndefined();
    });

    it('returns context when used within provider', () => {
      const { result } = renderHook(() => useWorkspaceOptional(), {
        wrapper: createWrapper(),
      });

      expect(result.current).toBeDefined();
      expect(result.current?.workspaceState).toBeDefined();
    });
  });

  describe('workspace state updates', () => {
    it('updates timeline viewport through context', () => {
      const { result } = renderHook(() => useWorkspace(), {
        wrapper: createWrapper(),
      });

      act(() => {
        result.current.setTimelineViewport({ zoomLevel: 200 });
      });

      expect(result.current.workspaceState.timeline.zoomLevel).toBe(200);
    });

    it('updates selection through context', () => {
      const { result } = renderHook(() => useWorkspace(), {
        wrapper: createWrapper(),
      });

      act(() => {
        result.current.setSelection({ clipIds: ['clip1', 'clip2'] });
      });

      expect(result.current.workspaceState.selection.clipIds).toEqual(['clip1', 'clip2']);
    });

    it('updates panel state through context', () => {
      const { result } = renderHook(() => useWorkspace(), {
        wrapper: createWrapper(),
      });

      act(() => {
        result.current.setPanelState({ openPanels: ['timeline'] });
      });

      expect(result.current.workspaceState.panels.openPanels).toEqual(['timeline']);
    });

    it('updates preview state through context', () => {
      const { result } = renderHook(() => useWorkspace(), {
        wrapper: createWrapper(),
      });

      act(() => {
        result.current.setPreviewState({ playheadPosition: 45.0 });
      });

      expect(result.current.workspaceState.preview.playheadPosition).toBe(45.0);
    });

    it('updates media library state through context', () => {
      const { result } = renderHook(() => useWorkspace(), {
        wrapper: createWrapper(),
      });

      act(() => {
        result.current.setMediaLibraryState({ viewMode: 'list' });
      });

      expect(result.current.workspaceState.mediaLibrary.viewMode).toBe('list');
    });
  });

  describe('workspace state restoration', () => {
    it('restores workspace state through context', () => {
      const { result } = renderHook(() => useWorkspace(), {
        wrapper: createWrapper(),
      });

      const savedState = {
        ...DEFAULT_WORKSPACE_STATE,
        timeline: {
          zoomLevel: 300,
          scrollPosition: { x: 250, y: 75 },
        },
      };

      act(() => {
        result.current.restoreWorkspaceState(savedState);
      });

      expect(result.current.workspaceState.timeline.zoomLevel).toBe(300);
      expect(result.current.workspaceState.timeline.scrollPosition).toEqual({ x: 250, y: 75 });
    });

    it('clears dirty flag after restoration', () => {
      const { result } = renderHook(() => useWorkspace(), {
        wrapper: createWrapper(),
      });

      // Make a change to set dirty
      act(() => {
        result.current.setTimelineViewport({ zoomLevel: 150 });
      });

      expect(result.current.isDirty).toBe(true);

      // Restore workspace state
      act(() => {
        result.current.restoreWorkspaceState(DEFAULT_WORKSPACE_STATE);
      });

      expect(result.current.isDirty).toBe(false);
    });
  });

  describe('getWorkspaceState', () => {
    it('returns current workspace state snapshot', () => {
      const { result } = renderHook(() => useWorkspace(), {
        wrapper: createWrapper(),
      });

      act(() => {
        result.current.setTimelineViewport({ zoomLevel: 125 });
        result.current.setSelection({ clipIds: ['test-clip'] });
      });

      const snapshot = result.current.getWorkspaceState();
      expect(snapshot.timeline.zoomLevel).toBe(125);
      expect(snapshot.selection.clipIds).toEqual(['test-clip']);
    });
  });
});
