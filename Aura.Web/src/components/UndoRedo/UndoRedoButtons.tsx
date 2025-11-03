/**
 * Undo/Redo toolbar buttons component
 * Displays undo and redo buttons with tooltips and disabled states
 */

import { Button, Tooltip } from '@fluentui/react-components';
import { ArrowUndo24Regular, ArrowRedo24Regular } from '@fluentui/react-icons';
import { useUndoManager } from '../../state/undoManager';

export function UndoRedoButtons() {
  const { undo, redo, canUndo, canRedo, getUndoDescription, getRedoDescription } = useUndoManager();

  const undoDescription = getUndoDescription();
  const redoDescription = getRedoDescription();

  // Platform-specific shortcut display
  const isMac = typeof navigator !== 'undefined' && navigator.platform.includes('Mac');
  const undoShortcut = isMac ? '⌘Z' : 'Ctrl+Z';
  const redoShortcut = isMac ? '⌘⇧Z' : 'Ctrl+Y';

  return (
    <div
      style={{
        display: 'flex',
        gap: '4px',
        alignItems: 'center',
      }}
    >
      <Tooltip
        content={
          canUndo && undoDescription
            ? `Undo: ${undoDescription} (${undoShortcut})`
            : `Undo (${undoShortcut})`
        }
        relationship="description"
      >
        <Button
          appearance="subtle"
          icon={<ArrowUndo24Regular />}
          disabled={!canUndo}
          onClick={undo}
          aria-label={`Undo ${undoDescription || ''}`}
          title={canUndo && undoDescription ? `Undo: ${undoDescription}` : 'Undo'}
        />
      </Tooltip>

      <Tooltip
        content={
          canRedo && redoDescription
            ? `Redo: ${redoDescription} (${redoShortcut})`
            : `Redo (${redoShortcut})`
        }
        relationship="description"
      >
        <Button
          appearance="subtle"
          icon={<ArrowRedo24Regular />}
          disabled={!canRedo}
          onClick={redo}
          aria-label={`Redo ${redoDescription || ''}`}
          title={canRedo && redoDescription ? `Redo: ${redoDescription}` : 'Redo'}
        />
      </Tooltip>
    </div>
  );
}
