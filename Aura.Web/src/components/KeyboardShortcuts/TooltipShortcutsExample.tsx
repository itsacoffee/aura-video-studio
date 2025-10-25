import { Button } from '@fluentui/react-components';
import {
  Play24Regular,
  Save24Regular,
  FolderOpen24Regular,
  Settings24Regular,
} from '@fluentui/react-icons';
import { Tooltip } from '../Tooltip';

/**
 * Example component demonstrating how to use tooltips with keyboard shortcuts
 */
export function TooltipShortcutsExample() {
  return (
    <div style={{ display: 'flex', gap: '16px', padding: '20px' }}>
      <Tooltip content="Play or pause the video" shortcut="Space">
        <Button icon={<Play24Regular />}>Play</Button>
      </Tooltip>

      <Tooltip content="Save the current project" shortcut="Ctrl+S">
        <Button icon={<Save24Regular />}>Save</Button>
      </Tooltip>

      <Tooltip content="Open an existing project" shortcut="Ctrl+O">
        <Button icon={<FolderOpen24Regular />}>Open</Button>
      </Tooltip>

      <Tooltip content="Open settings panel" shortcut="Ctrl+,">
        <Button icon={<Settings24Regular />}>Settings</Button>
      </Tooltip>
    </div>
  );
}
