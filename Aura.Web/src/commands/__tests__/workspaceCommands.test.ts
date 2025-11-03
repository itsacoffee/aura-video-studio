/**
 * Tests for workspace command implementations
 */

import { describe, it, expect, vi } from 'vitest';
import { TogglePanelCommand, ChangeLayoutCommand, ResizePanelCommand } from '../workspaceCommands';

describe('Workspace Commands', () => {
  describe('TogglePanelCommand', () => {
    it('should toggle panel state on execute', () => {
      let collapsed = true;
      const getCurrentState = () => collapsed;
      const toggle = () => {
        collapsed = !collapsed;
      };

      const command = new TogglePanelCommand('properties', getCurrentState, toggle);

      command.execute();
      expect(collapsed).toBe(false);
    });

    it('should restore previous state on undo', () => {
      let collapsed = true;
      const getCurrentState = () => collapsed;
      const toggle = () => {
        collapsed = !collapsed;
      };

      const command = new TogglePanelCommand('properties', getCurrentState, toggle);

      command.execute();
      expect(collapsed).toBe(false);

      command.undo();
      expect(collapsed).toBe(true);
    });

    it('should have correct description', () => {
      let collapsed = true;
      const command = new TogglePanelCommand(
        'properties',
        () => collapsed,
        () => {
          collapsed = !collapsed;
        }
      );

      // Command captures state at construction time (collapsed=true means it will expand)
      expect(command.getDescription()).toBe('Expand Properties Panel');

      collapsed = false;
      const command2 = new TogglePanelCommand(
        'mediaLibrary',
        () => collapsed,
        () => {
          collapsed = !collapsed;
        }
      );

      // collapsed=false means it will collapse
      expect(command2.getDescription()).toBe('Collapse Media Library');
    });

    it('should have a timestamp', () => {
      const command = new TogglePanelCommand(
        'properties',
        () => true,
        () => {}
      );
      expect(command.getTimestamp()).toBeInstanceOf(Date);
    });
  });

  describe('ChangeLayoutCommand', () => {
    it('should apply new layout on execute', () => {
      let currentLayout = 'editing';
      const applyLayout = vi.fn((layoutId: string) => {
        currentLayout = layoutId;
      });

      const command = new ChangeLayoutCommand('editing', 'color-grading', applyLayout);

      command.execute();
      expect(applyLayout).toHaveBeenCalledWith('color-grading');
      expect(currentLayout).toBe('color-grading');
    });

    it('should restore previous layout on undo', () => {
      let currentLayout = 'editing';
      const applyLayout = vi.fn((layoutId: string) => {
        currentLayout = layoutId;
      });

      const command = new ChangeLayoutCommand('editing', 'color-grading', applyLayout);

      command.execute();
      expect(currentLayout).toBe('color-grading');

      command.undo();
      expect(applyLayout).toHaveBeenCalledWith('editing');
      expect(currentLayout).toBe('editing');
    });

    it('should have correct description', () => {
      const command = new ChangeLayoutCommand('editing', 'color-grading', () => {});
      expect(command.getDescription()).toBe('Change layout to color-grading');
    });

    it('should have a timestamp', () => {
      const command = new ChangeLayoutCommand('editing', 'color-grading', () => {});
      expect(command.getTimestamp()).toBeInstanceOf(Date);
    });
  });

  describe('ResizePanelCommand', () => {
    it('should apply new size on execute', () => {
      let size = 300;
      const applySize = vi.fn((newSize: number) => {
        size = newSize;
      });

      const command = new ResizePanelCommand('Properties Panel', 300, 400, applySize);

      command.execute();
      expect(applySize).toHaveBeenCalledWith(400);
      expect(size).toBe(400);
    });

    it('should restore previous size on undo', () => {
      let size = 300;
      const applySize = vi.fn((newSize: number) => {
        size = newSize;
      });

      const command = new ResizePanelCommand('Properties Panel', 300, 400, applySize);

      command.execute();
      expect(size).toBe(400);

      command.undo();
      expect(applySize).toHaveBeenCalledWith(300);
      expect(size).toBe(300);
    });

    it('should have correct description', () => {
      const command = new ResizePanelCommand('Properties Panel', 300, 400, () => {});
      expect(command.getDescription()).toBe('Resize Properties Panel');
    });

    it('should have a timestamp', () => {
      const command = new ResizePanelCommand('Properties Panel', 300, 400, () => {});
      expect(command.getTimestamp()).toBeInstanceOf(Date);
    });
  });
});
