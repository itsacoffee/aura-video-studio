import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import type { WorkspaceLayout } from '../../services/workspaceLayoutService';
import {
  generateWorkspaceThumbnail,
  isValidThumbnail,
  getThumbnailSize,
} from '../workspaceThumbnailGenerator';

describe('workspaceThumbnailGenerator', () => {
  let originalCreateElement: typeof document.createElement;

  beforeEach(() => {
    // Mock canvas for headless environment
    originalCreateElement = document.createElement;
    document.createElement = function (tagName: string) {
      if (tagName === 'canvas') {
        const canvas = originalCreateElement.call(document, 'canvas') as HTMLCanvasElement;
        const ctx = {
          fillStyle: '',
          strokeStyle: '',
          lineWidth: 0,
          font: '',
          textAlign: '',
          textBaseline: '',
          fillRect: () => {},
          strokeRect: () => {},
          fillText: () => {},
        } as unknown as CanvasRenderingContext2D;

        canvas.getContext = () => ctx;
        canvas.toDataURL = () => 'data:image/png;base64,mockImageData';
        return canvas;
      }
      return originalCreateElement.call(document, tagName);
    } as typeof document.createElement;
  });

  afterEach(() => {
    document.createElement = originalCreateElement;
  });

  const mockWorkspace: WorkspaceLayout = {
    id: 'test-workspace',
    name: 'Test Workspace',
    description: 'Test description',
    panelSizes: {
      propertiesWidth: 320,
      mediaLibraryWidth: 280,
      effectsLibraryWidth: 280,
      historyWidth: 320,
      previewHeight: 70,
    },
    visiblePanels: {
      properties: true,
      mediaLibrary: true,
      effects: true,
      history: true,
    },
  };

  describe('generateWorkspaceThumbnail', () => {
    it('should generate a valid data URL', () => {
      const result = generateWorkspaceThumbnail(mockWorkspace);
      expect(result).toContain('data:image/png;base64,');
    });

    it('should generate thumbnail with custom config', () => {
      const config = {
        width: 640,
        height: 360,
        backgroundColor: '#000000',
        panelColors: {
          mediaLibrary: '#ff0000',
          effects: '#00ff00',
          preview: '#0000ff',
          properties: '#ffff00',
          timeline: '#ff00ff',
          history: '#00ffff',
        },
        labelStyle: {
          fontSize: 12,
          color: '#ffffff',
        },
      };

      const result = generateWorkspaceThumbnail(mockWorkspace, config);
      expect(result).toContain('data:image/png;base64,');
    });

    it('should handle workspace with collapsed panels', () => {
      const collapsedWorkspace: WorkspaceLayout = {
        ...mockWorkspace,
        visiblePanels: {
          properties: false,
          mediaLibrary: false,
          effects: false,
          history: false,
        },
      };

      const result = generateWorkspaceThumbnail(collapsedWorkspace);
      expect(result).toContain('data:image/png;base64,');
    });
  });

  describe('isValidThumbnail', () => {
    it('should return true for valid data URL', () => {
      const validUrl = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUg';
      expect(isValidThumbnail(validUrl)).toBe(true);
    });

    it('should return false for invalid data URL', () => {
      expect(isValidThumbnail('not-a-valid-url')).toBe(false);
      expect(isValidThumbnail('')).toBe(false);
      expect(isValidThumbnail('http://example.com/image.png')).toBe(false);
    });

    it('should return false for non-string values', () => {
      expect(isValidThumbnail(null as unknown as string)).toBe(false);
      expect(isValidThumbnail(undefined as unknown as string)).toBe(false);
    });
  });

  describe('getThumbnailSize', () => {
    it('should calculate approximate size for valid data URL', () => {
      const dataUrl = 'data:image/png;base64,iVBORw0KGgo=';
      const size = getThumbnailSize(dataUrl);
      expect(size).toBeGreaterThan(0);
    });

    it('should return 0 for invalid data URL', () => {
      expect(getThumbnailSize('invalid-url')).toBe(0);
      expect(getThumbnailSize('')).toBe(0);
    });
  });
});
