/**
 * OpenCut Layout Store
 *
 * Manages the layout state for the OpenCut video editor including:
 * - Panel widths and collapsed states
 * - Timeline height
 * - Persistent layout storage with localStorage
 * - Layout reset functionality
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

/**
 * Layout sizing constants following the design specification.
 * These define the constraints for panel resizing.
 */
export const LAYOUT_CONSTANTS = {
  /** Left panel (Media) constraints */
  leftPanel: {
    minWidth: 200,
    maxWidth: 600,
    collapsedWidth: 48,
    defaultWidth: 320,
  },
  /** Right panel (Properties) constraints */
  rightPanel: {
    minWidth: 250,
    maxWidth: 700,
    collapsedWidth: 48,
    defaultWidth: 340,
  },
  /** Timeline constraints */
  timeline: {
    minHeight: 200,
    maxHeight: 640,
    defaultHeight: 380,
  },
  /** Animation settings */
  animation: {
    collapseDuration: 200,
    collapseEasing: 'ease-out',
  },
} as const;

export interface LayoutState {
  /** Width of the left (Media) panel */
  leftPanelWidth: number;
  /** Whether the left panel is collapsed to icon-only state */
  leftPanelCollapsed: boolean;
  /** Width of the left panel before it was collapsed (for restore) */
  leftPanelPreviousWidth: number;

  /** Width of the right (Properties) panel */
  rightPanelWidth: number;
  /** Whether the right panel is collapsed to icon-only state */
  rightPanelCollapsed: boolean;
  /** Width of the right panel before it was collapsed (for restore) */
  rightPanelPreviousWidth: number;

  /** Height of the timeline panel */
  timelineHeight: number;

  /** Actions */
  setLeftPanelWidth: (width: number) => void;
  setLeftPanelCollapsed: (collapsed: boolean) => void;
  toggleLeftPanel: () => void;
  setRightPanelWidth: (width: number) => void;
  setRightPanelCollapsed: (collapsed: boolean) => void;
  toggleRightPanel: () => void;
  setTimelineHeight: (height: number) => void;
  resetLayout: () => void;
}

const defaultState = {
  leftPanelWidth: LAYOUT_CONSTANTS.leftPanel.defaultWidth,
  leftPanelCollapsed: false,
  leftPanelPreviousWidth: LAYOUT_CONSTANTS.leftPanel.defaultWidth,
  rightPanelWidth: LAYOUT_CONSTANTS.rightPanel.defaultWidth,
  rightPanelCollapsed: false,
  rightPanelPreviousWidth: LAYOUT_CONSTANTS.rightPanel.defaultWidth,
  timelineHeight: LAYOUT_CONSTANTS.timeline.defaultHeight,
};

export const useOpenCutLayoutStore = create<LayoutState>()(
  persist(
    (set, get) => ({
      ...defaultState,

      setLeftPanelWidth: (width: number) => {
        const clampedWidth = Math.max(
          LAYOUT_CONSTANTS.leftPanel.minWidth,
          Math.min(LAYOUT_CONSTANTS.leftPanel.maxWidth, width)
        );
        set({
          leftPanelWidth: clampedWidth,
          leftPanelPreviousWidth: clampedWidth,
          leftPanelCollapsed: false,
        });
      },

      setLeftPanelCollapsed: (collapsed: boolean) => {
        const state = get();
        if (collapsed) {
          set({
            leftPanelCollapsed: true,
            leftPanelPreviousWidth: state.leftPanelWidth,
            leftPanelWidth: LAYOUT_CONSTANTS.leftPanel.collapsedWidth,
          });
        } else {
          set({
            leftPanelCollapsed: false,
            leftPanelWidth: state.leftPanelPreviousWidth,
          });
        }
      },

      toggleLeftPanel: () => {
        const state = get();
        state.setLeftPanelCollapsed(!state.leftPanelCollapsed);
      },

      setRightPanelWidth: (width: number) => {
        const clampedWidth = Math.max(
          LAYOUT_CONSTANTS.rightPanel.minWidth,
          Math.min(LAYOUT_CONSTANTS.rightPanel.maxWidth, width)
        );
        set({
          rightPanelWidth: clampedWidth,
          rightPanelPreviousWidth: clampedWidth,
          rightPanelCollapsed: false,
        });
      },

      setRightPanelCollapsed: (collapsed: boolean) => {
        const state = get();
        if (collapsed) {
          set({
            rightPanelCollapsed: true,
            rightPanelPreviousWidth: state.rightPanelWidth,
            rightPanelWidth: LAYOUT_CONSTANTS.rightPanel.collapsedWidth,
          });
        } else {
          set({
            rightPanelCollapsed: false,
            rightPanelWidth: state.rightPanelPreviousWidth,
          });
        }
      },

      toggleRightPanel: () => {
        const state = get();
        state.setRightPanelCollapsed(!state.rightPanelCollapsed);
      },

      setTimelineHeight: (height: number) => {
        const clampedHeight = Math.max(
          LAYOUT_CONSTANTS.timeline.minHeight,
          Math.min(LAYOUT_CONSTANTS.timeline.maxHeight, height)
        );
        set({ timelineHeight: clampedHeight });
      },

      resetLayout: () => {
        set(defaultState);
      },
    }),
    {
      name: 'opencut-layout',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        leftPanelWidth: state.leftPanelWidth,
        leftPanelCollapsed: state.leftPanelCollapsed,
        leftPanelPreviousWidth: state.leftPanelPreviousWidth,
        rightPanelWidth: state.rightPanelWidth,
        rightPanelCollapsed: state.rightPanelCollapsed,
        rightPanelPreviousWidth: state.rightPanelPreviousWidth,
        timelineHeight: state.timelineHeight,
      }),
    }
  )
);

export default useOpenCutLayoutStore;
