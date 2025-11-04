import { describe, it, expect, beforeEach } from 'vitest';
import type { PromptDiffDto, LockedSectionDto } from '../../types/api-v1';
import { useGuidedMode } from '../guidedMode';

describe('Guided Mode State', () => {
  beforeEach(() => {
    useGuidedMode.getState().reset();
  });

  describe('Configuration', () => {
    it('should have default configuration', () => {
      const { config } = useGuidedMode.getState();

      expect(config.enabled).toBe(true);
      expect(config.experienceLevel).toBe('beginner');
      expect(config.showTooltips).toBe(true);
      expect(config.showWhyLinks).toBe(true);
      expect(config.requirePromptDiffConfirmation).toBe(true);
    });

    it('should update configuration', () => {
      const { setConfig, config: initialConfig } = useGuidedMode.getState();

      setConfig({ enabled: false });

      const { config } = useGuidedMode.getState();
      expect(config.enabled).toBe(false);
      expect(config.experienceLevel).toBe(initialConfig.experienceLevel);
    });

    it('should set experience level and adjust related settings', () => {
      const { setExperienceLevel } = useGuidedMode.getState();

      setExperienceLevel('advanced');

      const { config } = useGuidedMode.getState();
      expect(config.experienceLevel).toBe('advanced');
      expect(config.showTooltips).toBe(false);
      expect(config.showWhyLinks).toBe(false);
      expect(config.requirePromptDiffConfirmation).toBe(false);
    });
  });

  describe('Tooltips', () => {
    it('should show tooltip when enabled', () => {
      const { showTooltip, activeTooltips } = useGuidedMode.getState();

      showTooltip('test-tooltip', 'Test content', 'test-element');

      const state = useGuidedMode.getState();
      expect(state.activeTooltips.size).toBe(1);
      expect(state.activeTooltips.get('test-tooltip')).toEqual({
        id: 'test-tooltip',
        visible: true,
        content: 'Test content',
        targetElement: 'test-element',
      });
    });

    it('should not show tooltip when disabled', () => {
      const { setConfig, showTooltip } = useGuidedMode.getState();

      setConfig({ showTooltips: false });
      showTooltip('test-tooltip', 'Test content', 'test-element');

      const state = useGuidedMode.getState();
      expect(state.activeTooltips.size).toBe(0);
    });

    it('should hide specific tooltip', () => {
      const { showTooltip, hideTooltip } = useGuidedMode.getState();

      showTooltip('tooltip-1', 'Content 1', 'element-1');
      showTooltip('tooltip-2', 'Content 2', 'element-2');

      let state = useGuidedMode.getState();
      expect(state.activeTooltips.size).toBe(2);

      hideTooltip('tooltip-1');

      state = useGuidedMode.getState();
      expect(state.activeTooltips.size).toBe(1);
      expect(state.activeTooltips.has('tooltip-2')).toBe(true);
    });

    it('should hide all tooltips', () => {
      const { showTooltip, hideAllTooltips } = useGuidedMode.getState();

      showTooltip('tooltip-1', 'Content 1', 'element-1');
      showTooltip('tooltip-2', 'Content 2', 'element-2');

      hideAllTooltips();

      const state = useGuidedMode.getState();
      expect(state.activeTooltips.size).toBe(0);
    });
  });

  describe('Explanation Panel', () => {
    it('should show explanation', () => {
      const { showExplanation, explanationPanel } = useGuidedMode.getState();

      showExplanation('script', 'test content');

      const state = useGuidedMode.getState();
      expect(state.explanationPanel).toBeDefined();
      expect(state.explanationPanel?.artifactType).toBe('script');
      expect(state.explanationPanel?.content).toBe('test content');
      expect(state.explanationPanel?.visible).toBe(true);
      expect(state.explanationPanel?.loading).toBe(false);
    });

    it('should hide explanation', () => {
      const { showExplanation, hideExplanation } = useGuidedMode.getState();

      showExplanation('script', 'test content');
      hideExplanation();

      const state = useGuidedMode.getState();
      expect(state.explanationPanel).toBeNull();
    });

    it('should set explanation loading state', () => {
      const { showExplanation, setExplanationLoading } = useGuidedMode.getState();

      showExplanation('script', 'test content');
      setExplanationLoading(true);

      const state = useGuidedMode.getState();
      expect(state.explanationPanel?.loading).toBe(true);
    });

    it('should set explanation response', () => {
      const { showExplanation, setExplanationResponse } = useGuidedMode.getState();

      showExplanation('script', 'test content');

      const response = {
        success: true,
        explanation: 'Test explanation',
        keyPoints: ['Point 1', 'Point 2'],
      };

      setExplanationResponse(response);

      const state = useGuidedMode.getState();
      expect(state.explanationPanel?.response).toEqual(response);
      expect(state.explanationPanel?.loading).toBe(false);
    });
  });

  describe('Prompt Diff Modal', () => {
    it('should show prompt diff', () => {
      const { showPromptDiff } = useGuidedMode.getState();

      const promptDiff: PromptDiffDto = {
        originalPrompt: 'Original',
        modifiedPrompt: 'Modified',
        intendedOutcome: 'Test outcome',
        changes: [],
      };

      const onConfirm = () => {};
      const onCancel = () => {};

      showPromptDiff(promptDiff, onConfirm, onCancel);

      const state = useGuidedMode.getState();
      expect(state.promptDiffModal.visible).toBe(true);
      expect(state.promptDiffModal.promptDiff).toEqual(promptDiff);
    });

    it('should hide prompt diff', () => {
      const { showPromptDiff, hidePromptDiff } = useGuidedMode.getState();

      const promptDiff: PromptDiffDto = {
        originalPrompt: 'Original',
        modifiedPrompt: 'Modified',
        intendedOutcome: 'Test outcome',
        changes: [],
      };

      showPromptDiff(
        promptDiff,
        () => {},
        () => {}
      );
      hidePromptDiff();

      const state = useGuidedMode.getState();
      expect(state.promptDiffModal.visible).toBe(false);
      expect(state.promptDiffModal.promptDiff).toBeUndefined();
    });
  });

  describe('Locked Sections', () => {
    it('should lock section', () => {
      const { lockSection, getLockedSections } = useGuidedMode.getState();

      const section: LockedSectionDto = {
        startIndex: 0,
        endIndex: 5,
        content: 'Test content',
        reason: 'User locked',
      };

      lockSection('artifact-1', section);

      const locked = getLockedSections('artifact-1');
      expect(locked).toHaveLength(1);
      expect(locked[0]).toEqual(section);
    });

    it('should unlock section', () => {
      const { lockSection, unlockSection, getLockedSections } = useGuidedMode.getState();

      const section1: LockedSectionDto = {
        startIndex: 0,
        endIndex: 5,
        content: 'Section 1',
        reason: 'User locked',
      };

      const section2: LockedSectionDto = {
        startIndex: 6,
        endIndex: 10,
        content: 'Section 2',
        reason: 'User locked',
      };

      lockSection('artifact-1', section1);
      lockSection('artifact-1', section2);

      unlockSection('artifact-1', 0);

      const locked = getLockedSections('artifact-1');
      expect(locked).toHaveLength(1);
      expect(locked[0]).toEqual(section2);
    });

    it('should clear all locked sections for artifact', () => {
      const { lockSection, clearLockedSections, getLockedSections } = useGuidedMode.getState();

      const section: LockedSectionDto = {
        startIndex: 0,
        endIndex: 5,
        content: 'Test content',
        reason: 'User locked',
      };

      lockSection('artifact-1', section);
      clearLockedSections('artifact-1');

      const locked = getLockedSections('artifact-1');
      expect(locked).toHaveLength(0);
    });

    it('should manage locked sections for multiple artifacts', () => {
      const { lockSection, getLockedSections } = useGuidedMode.getState();

      const section1: LockedSectionDto = {
        startIndex: 0,
        endIndex: 5,
        content: 'Artifact 1 content',
        reason: 'User locked',
      };

      const section2: LockedSectionDto = {
        startIndex: 0,
        endIndex: 3,
        content: 'Artifact 2 content',
        reason: 'User locked',
      };

      lockSection('artifact-1', section1);
      lockSection('artifact-2', section2);

      expect(getLockedSections('artifact-1')).toHaveLength(1);
      expect(getLockedSections('artifact-2')).toHaveLength(1);
      expect(getLockedSections('artifact-3')).toHaveLength(0);
    });
  });

  describe('Completed Steps', () => {
    it('should mark step as completed', () => {
      const { markStepCompleted, isStepCompleted } = useGuidedMode.getState();

      markStepCompleted('step-1');

      expect(isStepCompleted('step-1')).toBe(true);
      expect(isStepCompleted('step-2')).toBe(false);
    });

    it('should track multiple completed steps', () => {
      const { markStepCompleted, isStepCompleted } = useGuidedMode.getState();

      markStepCompleted('step-1');
      markStepCompleted('step-2');
      markStepCompleted('step-3');

      expect(isStepCompleted('step-1')).toBe(true);
      expect(isStepCompleted('step-2')).toBe(true);
      expect(isStepCompleted('step-3')).toBe(true);
      expect(isStepCompleted('step-4')).toBe(false);
    });
  });

  describe('Reset', () => {
    it('should reset all state to defaults', () => {
      const { setConfig, showTooltip, showExplanation, lockSection, markStepCompleted, reset } =
        useGuidedMode.getState();

      setConfig({ enabled: false });
      showTooltip('test', 'content', 'element');
      showExplanation('script', 'content');
      lockSection('artifact-1', {
        startIndex: 0,
        endIndex: 5,
        content: 'Test',
        reason: 'Test',
      });
      markStepCompleted('step-1');

      reset();

      const state = useGuidedMode.getState();
      expect(state.config.enabled).toBe(true);
      expect(state.activeTooltips.size).toBe(0);
      expect(state.explanationPanel).toBeNull();
      expect(state.lockedSections.size).toBe(0);
      expect(state.completedSteps.size).toBe(0);
    });
  });
});
