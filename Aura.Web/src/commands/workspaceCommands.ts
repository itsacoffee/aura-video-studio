/**
 * Command implementations for workspace layout operations
 */

import { Command } from '../services/commandHistory';
import { PanelSizes } from '../services/workspaceLayoutService';

/**
 * Command to toggle panel collapsed state
 */
export class TogglePanelCommand implements Command {
  private timestamp: Date;
  private wasCollapsed: boolean;

  constructor(
    private panel: 'properties' | 'mediaLibrary' | 'effects' | 'history',
    private getCurrentState: () => boolean,
    private toggleFunction: () => void
  ) {
    this.timestamp = new Date();
    this.wasCollapsed = getCurrentState();
  }

  execute(): void {
    this.toggleFunction();
  }

  undo(): void {
    // Toggle back if state changed
    const currentState = this.getCurrentState();
    if (currentState !== this.wasCollapsed) {
      this.toggleFunction();
    }
  }

  getDescription(): string {
    const panelNames: Record<typeof this.panel, string> = {
      properties: 'Properties Panel',
      mediaLibrary: 'Media Library',
      effects: 'Effects Panel',
      history: 'History Panel',
    };
    const action = this.wasCollapsed ? 'Expand' : 'Collapse';
    return `${action} ${panelNames[this.panel]}`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to change workspace layout
 */
export class ChangeLayoutCommand implements Command {
  private timestamp: Date;

  constructor(
    private previousLayoutId: string,
    private newLayoutId: string,
    private applyLayout: (layoutId: string) => void
  ) {
    this.timestamp = new Date();
  }

  execute(): void {
    this.applyLayout(this.newLayoutId);
  }

  undo(): void {
    this.applyLayout(this.previousLayoutId);
  }

  getDescription(): string {
    return `Change layout to ${this.newLayoutId}`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to save a new workspace layout
 */
export class SaveWorkspaceCommand implements Command {
  private timestamp: Date;
  private savedLayoutId: string | null = null;

  constructor(
    private name: string,
    private description: string,
    private panelSizes: PanelSizes,
    private saveFunction: (name: string, desc: string, sizes: PanelSizes) => { id: string },
    private deleteFunction: (layoutId: string) => void
  ) {
    this.timestamp = new Date();
  }

  execute(): void {
    const result = this.saveFunction(this.name, this.description, this.panelSizes);
    this.savedLayoutId = result.id;
  }

  undo(): void {
    if (this.savedLayoutId) {
      this.deleteFunction(this.savedLayoutId);
    }
  }

  getDescription(): string {
    return `Save workspace "${this.name}"`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to resize panel
 */
export class ResizePanelCommand implements Command {
  private timestamp: Date;

  constructor(
    private panelName: string,
    private previousSize: number,
    private newSize: number,
    private applySize: (size: number) => void
  ) {
    this.timestamp = new Date();
  }

  execute(): void {
    this.applySize(this.newSize);
  }

  undo(): void {
    this.applySize(this.previousSize);
  }

  getDescription(): string {
    return `Resize ${this.panelName}`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}
