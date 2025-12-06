/**
 * OpenCut Layout Store Tests
 *
 * Tests for the layout persistence and panel management store.
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useOpenCutLayoutStore, LAYOUT_CONSTANTS } from '../opencutLayout';

describe('OpenCutLayoutStore', () => {
  beforeEach(() => {
    // Reset store to default state before each test
    useOpenCutLayoutStore.setState({
      leftPanelWidth: LAYOUT_CONSTANTS.leftPanel.defaultWidth,
      leftPanelCollapsed: false,
      leftPanelPreviousWidth: LAYOUT_CONSTANTS.leftPanel.defaultWidth,
      rightPanelWidth: LAYOUT_CONSTANTS.rightPanel.defaultWidth,
      rightPanelCollapsed: false,
      rightPanelPreviousWidth: LAYOUT_CONSTANTS.rightPanel.defaultWidth,
      timelineHeight: LAYOUT_CONSTANTS.timeline.defaultHeight,
    });
  });

  describe('Layout Constants', () => {
    it('should have valid left panel constraints', () => {
      expect(LAYOUT_CONSTANTS.leftPanel.minWidth).toBe(200);
      expect(LAYOUT_CONSTANTS.leftPanel.maxWidth).toBe(600);
      expect(LAYOUT_CONSTANTS.leftPanel.collapsedWidth).toBe(48);
      expect(LAYOUT_CONSTANTS.leftPanel.defaultWidth).toBe(280);
    });

    it('should have valid right panel constraints', () => {
      expect(LAYOUT_CONSTANTS.rightPanel.minWidth).toBe(250);
      expect(LAYOUT_CONSTANTS.rightPanel.maxWidth).toBe(700);
      expect(LAYOUT_CONSTANTS.rightPanel.collapsedWidth).toBe(48);
      expect(LAYOUT_CONSTANTS.rightPanel.defaultWidth).toBe(300);
    });

    it('should have valid timeline constraints', () => {
      expect(LAYOUT_CONSTANTS.timeline.minHeight).toBe(180);
      expect(LAYOUT_CONSTANTS.timeline.maxHeight).toBe(500);
      expect(LAYOUT_CONSTANTS.timeline.defaultHeight).toBe(280);
    });
  });

  describe('Left Panel Operations', () => {
    it('should set left panel width within bounds', () => {
      const { setLeftPanelWidth } = useOpenCutLayoutStore.getState();

      setLeftPanelWidth(400);

      const state = useOpenCutLayoutStore.getState();
      expect(state.leftPanelWidth).toBe(400);
      expect(state.leftPanelPreviousWidth).toBe(400);
      expect(state.leftPanelCollapsed).toBe(false);
    });

    it('should clamp left panel width to minimum', () => {
      const { setLeftPanelWidth } = useOpenCutLayoutStore.getState();

      setLeftPanelWidth(100); // Below minimum

      const state = useOpenCutLayoutStore.getState();
      expect(state.leftPanelWidth).toBe(LAYOUT_CONSTANTS.leftPanel.minWidth);
    });

    it('should clamp left panel width to maximum', () => {
      const { setLeftPanelWidth } = useOpenCutLayoutStore.getState();

      setLeftPanelWidth(800); // Above maximum

      const state = useOpenCutLayoutStore.getState();
      expect(state.leftPanelWidth).toBe(LAYOUT_CONSTANTS.leftPanel.maxWidth);
    });

    it('should collapse left panel', () => {
      const { setLeftPanelWidth, setLeftPanelCollapsed } = useOpenCutLayoutStore.getState();

      setLeftPanelWidth(350);
      setLeftPanelCollapsed(true);

      const state = useOpenCutLayoutStore.getState();
      expect(state.leftPanelCollapsed).toBe(true);
      expect(state.leftPanelWidth).toBe(LAYOUT_CONSTANTS.leftPanel.collapsedWidth);
      expect(state.leftPanelPreviousWidth).toBe(350);
    });

    it('should expand left panel to previous width', () => {
      const { setLeftPanelWidth, setLeftPanelCollapsed } = useOpenCutLayoutStore.getState();

      setLeftPanelWidth(350);
      setLeftPanelCollapsed(true);
      setLeftPanelCollapsed(false);

      const state = useOpenCutLayoutStore.getState();
      expect(state.leftPanelCollapsed).toBe(false);
      expect(state.leftPanelWidth).toBe(350);
    });

    it('should toggle left panel', () => {
      const { toggleLeftPanel } = useOpenCutLayoutStore.getState();

      toggleLeftPanel();
      expect(useOpenCutLayoutStore.getState().leftPanelCollapsed).toBe(true);

      toggleLeftPanel();
      expect(useOpenCutLayoutStore.getState().leftPanelCollapsed).toBe(false);
    });
  });

  describe('Right Panel Operations', () => {
    it('should set right panel width within bounds', () => {
      const { setRightPanelWidth } = useOpenCutLayoutStore.getState();

      setRightPanelWidth(500);

      const state = useOpenCutLayoutStore.getState();
      expect(state.rightPanelWidth).toBe(500);
      expect(state.rightPanelPreviousWidth).toBe(500);
      expect(state.rightPanelCollapsed).toBe(false);
    });

    it('should clamp right panel width to minimum', () => {
      const { setRightPanelWidth } = useOpenCutLayoutStore.getState();

      setRightPanelWidth(100); // Below minimum

      const state = useOpenCutLayoutStore.getState();
      expect(state.rightPanelWidth).toBe(LAYOUT_CONSTANTS.rightPanel.minWidth);
    });

    it('should clamp right panel width to maximum', () => {
      const { setRightPanelWidth } = useOpenCutLayoutStore.getState();

      setRightPanelWidth(900); // Above maximum

      const state = useOpenCutLayoutStore.getState();
      expect(state.rightPanelWidth).toBe(LAYOUT_CONSTANTS.rightPanel.maxWidth);
    });

    it('should collapse right panel', () => {
      const { setRightPanelWidth, setRightPanelCollapsed } = useOpenCutLayoutStore.getState();

      setRightPanelWidth(450);
      setRightPanelCollapsed(true);

      const state = useOpenCutLayoutStore.getState();
      expect(state.rightPanelCollapsed).toBe(true);
      expect(state.rightPanelWidth).toBe(LAYOUT_CONSTANTS.rightPanel.collapsedWidth);
      expect(state.rightPanelPreviousWidth).toBe(450);
    });

    it('should expand right panel to previous width', () => {
      const { setRightPanelWidth, setRightPanelCollapsed } = useOpenCutLayoutStore.getState();

      setRightPanelWidth(450);
      setRightPanelCollapsed(true);
      setRightPanelCollapsed(false);

      const state = useOpenCutLayoutStore.getState();
      expect(state.rightPanelCollapsed).toBe(false);
      expect(state.rightPanelWidth).toBe(450);
    });

    it('should toggle right panel', () => {
      const { toggleRightPanel } = useOpenCutLayoutStore.getState();

      toggleRightPanel();
      expect(useOpenCutLayoutStore.getState().rightPanelCollapsed).toBe(true);

      toggleRightPanel();
      expect(useOpenCutLayoutStore.getState().rightPanelCollapsed).toBe(false);
    });
  });

  describe('Timeline Operations', () => {
    it('should set timeline height within bounds', () => {
      const { setTimelineHeight } = useOpenCutLayoutStore.getState();

      setTimelineHeight(350);

      const state = useOpenCutLayoutStore.getState();
      expect(state.timelineHeight).toBe(350);
    });

    it('should clamp timeline height to minimum', () => {
      const { setTimelineHeight } = useOpenCutLayoutStore.getState();

      setTimelineHeight(100); // Below minimum

      const state = useOpenCutLayoutStore.getState();
      expect(state.timelineHeight).toBe(LAYOUT_CONSTANTS.timeline.minHeight);
    });

    it('should clamp timeline height to maximum', () => {
      const { setTimelineHeight } = useOpenCutLayoutStore.getState();

      setTimelineHeight(600); // Above maximum

      const state = useOpenCutLayoutStore.getState();
      expect(state.timelineHeight).toBe(LAYOUT_CONSTANTS.timeline.maxHeight);
    });
  });

  describe('Reset Layout', () => {
    it('should reset all layout values to defaults', () => {
      const {
        setLeftPanelWidth,
        setLeftPanelCollapsed,
        setRightPanelWidth,
        setRightPanelCollapsed,
        setTimelineHeight,
        resetLayout,
      } = useOpenCutLayoutStore.getState();

      // Modify all values
      setLeftPanelWidth(500);
      setLeftPanelCollapsed(true);
      setRightPanelWidth(600);
      setRightPanelCollapsed(true);
      setTimelineHeight(400);

      // Reset
      resetLayout();

      const state = useOpenCutLayoutStore.getState();
      expect(state.leftPanelWidth).toBe(LAYOUT_CONSTANTS.leftPanel.defaultWidth);
      expect(state.leftPanelCollapsed).toBe(false);
      expect(state.rightPanelWidth).toBe(LAYOUT_CONSTANTS.rightPanel.defaultWidth);
      expect(state.rightPanelCollapsed).toBe(false);
      expect(state.timelineHeight).toBe(LAYOUT_CONSTANTS.timeline.defaultHeight);
    });
  });
});
