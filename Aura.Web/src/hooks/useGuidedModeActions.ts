import { useCallback } from 'react';
import { guidedModeService } from '../services/guidedModeService';
import { loggingService } from '../services/loggingService';
import { useGuidedMode } from '../state/guidedMode';

/**
 * Hook for common guided mode actions
 * Provides simplified interface for explaining, improving, and regenerating artifacts
 */
export function useGuidedModeActions(artifactType: string) {
  const {
    showExplanation,
    setExplanationResponse,
    setExplanationLoading,
    showPromptDiff,
    getLockedSections,
  } = useGuidedMode();

  /**
   * Explain the current artifact
   */
  const explainArtifact = useCallback(
    async (content: string, specificQuestion?: string) => {
      showExplanation(artifactType, content);
      setExplanationLoading(true);

      try {
        const response = await guidedModeService.explainArtifact({
          artifactType,
          artifactContent: content,
          specificQuestion,
        });

        setExplanationResponse(response);
      } catch (error: unknown) {
        console.error('Failed to explain artifact:', error);
        setExplanationResponse({
          success: false,
          explanation: null,
          keyPoints: null,
          errorMessage: 'Failed to generate explanation. Please try again.',
        });
      } finally {
        setExplanationLoading(false);
      }
    },
    [artifactType, showExplanation, setExplanationLoading, setExplanationResponse]
  );

  /**
   * Improve artifact with specific action
   */
  const improveArtifact = useCallback(
    async (
      content: string,
      improvementAction: string,
      artifactId: string,
      targetAudience?: string,
      onSuccess?: (improvedContent: string) => void
    ) => {
      const lockedSections = getLockedSections(artifactId);

      try {
        const response = await guidedModeService.improveArtifact({
          artifactType,
          artifactContent: content,
          improvementAction,
          targetAudience,
          lockedSections: lockedSections.length > 0 ? lockedSections : null,
        });

        if (response.success && response.improvedContent && response.promptDiff) {
          showPromptDiff(
            response.promptDiff,
            () => {
              if (response.improvedContent && onSuccess) {
                onSuccess(response.improvedContent);
              }
            },
            () => {
              loggingService.info('User cancelled improvement');
            }
          );
        } else {
          console.error('Improvement failed:', response.errorMessage);
        }
      } catch (error: unknown) {
        console.error('Failed to improve artifact:', error);
      }
    },
    [artifactType, getLockedSections, showPromptDiff]
  );

  /**
   * Regenerate artifact with constraints
   */
  const regenerateArtifact = useCallback(
    async (
      currentContent: string,
      regenerationType: string,
      artifactId: string,
      onSuccess?: (regeneratedContent: string) => void
    ) => {
      const lockedSections = getLockedSections(artifactId);

      try {
        const response = await guidedModeService.constrainedRegenerate({
          artifactType,
          currentContent,
          regenerationType,
          lockedSections: lockedSections.length > 0 ? lockedSections : null,
        });

        if (response.success && response.regeneratedContent && response.promptDiff) {
          showPromptDiff(
            response.promptDiff,
            () => {
              if (response.regeneratedContent && onSuccess) {
                onSuccess(response.regeneratedContent);
              }
            },
            () => {
              loggingService.info('User cancelled regeneration');
            }
          );
        } else {
          console.error('Regeneration failed:', response.errorMessage);
        }
      } catch (error: unknown) {
        console.error('Failed to regenerate artifact:', error);
      }
    },
    [artifactType, getLockedSections, showPromptDiff]
  );

  return {
    explainArtifact,
    improveArtifact,
    regenerateArtifact,
  };
}
