/**
 * BRollSuggestionsModal - Modal dialog for displaying AI-generated B-Roll suggestions
 *
 * Provides a user interface for selecting from B-Roll visual suggestions for a scene.
 * Users can apply a suggestion to add visual context to their script scene.
 */

import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  makeStyles,
  Text,
  tokens,
} from '@fluentui/react-components';
import type { FC } from 'react';
import React from 'react';

const useStyles = makeStyles({
  suggestionsContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalL,
  },
  suggestionButton: {
    textAlign: 'left',
    padding: tokens.spacingVerticalM,
    justifyContent: 'flex-start',
  },
  footer: {
    marginTop: tokens.spacingVerticalL,
    display: 'flex',
    justifyContent: 'flex-end',
  },
  description: {
    color: tokens.colorNeutralForeground2,
  },
});

interface BRollSuggestionsModalProps {
  isOpen: boolean;
  sceneIndex: number;
  suggestions: string[];
  onClose: () => void;
  onApplySuggestion: (sceneIndex: number, suggestion: string) => void;
}

export const BRollSuggestionsModal: FC<BRollSuggestionsModalProps> = ({
  isOpen,
  sceneIndex,
  suggestions,
  onClose,
  onApplySuggestion,
}) => {
  const styles = useStyles();

  const handleOpenChange = (_: unknown, data: { open: boolean }) => {
    if (!data.open) {
      onClose();
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>B-Roll Suggestions for Scene {sceneIndex + 1}</DialogTitle>
          <DialogContent>
            <Text className={styles.description}>
              Select a suggestion to add visual context to this scene:
            </Text>
            <div className={styles.suggestionsContainer}>
              {suggestions.map((suggestion, index) => (
                <Button
                  key={index}
                  appearance="outline"
                  onClick={() => {
                    onApplySuggestion(sceneIndex, suggestion);
                    onClose();
                  }}
                  className={styles.suggestionButton}
                >
                  {suggestion}
                </Button>
              ))}
            </div>
            <div className={styles.footer}>
              <Button onClick={onClose}>Close</Button>
            </div>
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};
