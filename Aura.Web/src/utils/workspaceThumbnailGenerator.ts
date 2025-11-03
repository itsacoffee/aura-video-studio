/**
 * Workspace Thumbnail Generator
 * Generates visual thumbnail representations of workspace layouts
 */

import type { WorkspaceLayout } from '../services/workspaceLayoutService';
import {
  DEFAULT_THUMBNAIL_CONFIG,
  type WorkspaceThumbnailConfig,
} from '../types/workspaceThumbnail.types';

interface PanelRect {
  x: number;
  y: number;
  width: number;
  height: number;
  color: string;
  label: string;
  visible: boolean;
}

/**
 * Generate a thumbnail image for a workspace layout
 */
export function generateWorkspaceThumbnail(
  layout: WorkspaceLayout,
  config: WorkspaceThumbnailConfig = DEFAULT_THUMBNAIL_CONFIG
): string {
  const canvas = document.createElement('canvas');
  canvas.width = config.width;
  canvas.height = config.height;
  const ctx = canvas.getContext('2d');

  if (!ctx) {
    throw new Error('Failed to get canvas context');
  }

  // Fill background
  ctx.fillStyle = config.backgroundColor;
  ctx.fillRect(0, 0, config.width, config.height);

  // Calculate panel layout based on workspace configuration
  const panels = calculatePanelLayout(layout, config);

  // Draw each panel
  panels.forEach((panel) => {
    if (!panel.visible) {
      // Draw collapsed panel as thin bar
      drawCollapsedPanel(ctx, panel);
    } else {
      drawPanel(ctx, panel, config);
    }
  });

  // Convert to data URL
  return canvas.toDataURL('image/png');
}

/**
 * Calculate panel positions and sizes based on workspace layout
 */
function calculatePanelLayout(
  layout: WorkspaceLayout,
  config: WorkspaceThumbnailConfig
): PanelRect[] {
  const { width, height } = config;
  const { panelSizes, visiblePanels } = layout;

  const panels: PanelRect[] = [];

  // Define collapsed panel width
  const collapsedWidth = 4;
  const collapsedHeight = 4;

  // Calculate total width of left panels
  const leftPanelWidth = Math.min(
    visiblePanels.mediaLibrary ? panelSizes.mediaLibraryWidth : collapsedWidth,
    width * 0.3
  );

  // Calculate total width of right panels
  const rightPanelWidth =
    Math.min(visiblePanels.properties ? panelSizes.propertiesWidth : collapsedWidth, width * 0.3) +
    Math.min(visiblePanels.effects ? panelSizes.effectsLibraryWidth : collapsedWidth, width * 0.2) +
    Math.min(visiblePanels.history ? panelSizes.historyWidth : collapsedWidth, width * 0.2);

  // Calculate center (preview + timeline) width
  const centerWidth = width - leftPanelWidth - rightPanelWidth;

  // Calculate preview and timeline heights
  const previewHeightPercent = panelSizes.previewHeight / 100;
  const previewHeight = height * previewHeightPercent;
  const timelineHeight = height - previewHeight;

  let currentX = 0;

  // Left panel: Media Library
  const mediaLibraryWidth = visiblePanels.mediaLibrary ? leftPanelWidth : collapsedWidth;
  panels.push({
    x: currentX,
    y: 0,
    width: mediaLibraryWidth,
    height: height,
    color: config.panelColors.mediaLibrary,
    label: 'Media',
    visible: visiblePanels.mediaLibrary,
  });
  currentX += mediaLibraryWidth;

  // Center top: Preview
  panels.push({
    x: currentX,
    y: 0,
    width: centerWidth,
    height: previewHeight,
    color: config.panelColors.preview,
    label: 'Preview',
    visible: true,
  });

  // Center bottom: Timeline
  panels.push({
    x: currentX,
    y: previewHeight,
    width: centerWidth,
    height: timelineHeight,
    color: config.panelColors.timeline,
    label: 'Timeline',
    visible: true,
  });

  currentX += centerWidth;

  // Right panels stack
  const rightPanelHeight = height / 4;

  // Properties
  const propertiesWidth = visiblePanels.properties
    ? Math.min(panelSizes.propertiesWidth, width * 0.3)
    : collapsedWidth;
  panels.push({
    x: currentX,
    y: 0,
    width: propertiesWidth,
    height: visiblePanels.properties ? rightPanelHeight : collapsedHeight,
    color: config.panelColors.properties,
    label: 'Props',
    visible: visiblePanels.properties,
  });

  // Effects
  const effectsWidth = visiblePanels.effects
    ? Math.min(panelSizes.effectsLibraryWidth, width * 0.2)
    : collapsedWidth;
  panels.push({
    x: currentX,
    y: visiblePanels.properties ? rightPanelHeight : collapsedHeight,
    width: effectsWidth,
    height: visiblePanels.effects ? rightPanelHeight : collapsedHeight,
    color: config.panelColors.effects,
    label: 'FX',
    visible: visiblePanels.effects,
  });

  // History
  const historyWidth = visiblePanels.history
    ? Math.min(panelSizes.historyWidth, width * 0.2)
    : collapsedWidth;
  panels.push({
    x: currentX,
    y:
      (visiblePanels.properties ? rightPanelHeight : collapsedHeight) +
      (visiblePanels.effects ? rightPanelHeight : collapsedHeight),
    width: historyWidth,
    height: visiblePanels.history
      ? height -
        (visiblePanels.properties ? rightPanelHeight : collapsedHeight) -
        (visiblePanels.effects ? rightPanelHeight : collapsedHeight)
      : collapsedHeight,
    color: config.panelColors.history,
    label: 'History',
    visible: visiblePanels.history,
  });

  return panels;
}

/**
 * Draw a visible panel with border and label
 */
function drawPanel(
  ctx: CanvasRenderingContext2D,
  panel: PanelRect,
  config: WorkspaceThumbnailConfig
): void {
  // Draw panel background
  ctx.fillStyle = panel.color;
  ctx.fillRect(panel.x, panel.y, panel.width, panel.height);

  // Draw border
  ctx.strokeStyle = '#000000';
  ctx.lineWidth = 1;
  ctx.strokeRect(panel.x, panel.y, panel.width, panel.height);

  // Draw label if panel is large enough
  if (panel.width > 30 && panel.height > 15) {
    ctx.fillStyle = config.labelStyle.color;
    ctx.font = `${config.labelStyle.fontSize}px sans-serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(panel.label, panel.x + panel.width / 2, panel.y + panel.height / 2);
  }
}

/**
 * Draw a collapsed panel as a thin bar
 */
function drawCollapsedPanel(ctx: CanvasRenderingContext2D, panel: PanelRect): void {
  // Draw collapsed panel as darker variant
  const darkerColor = adjustColorBrightness(panel.color, -30);
  ctx.fillStyle = darkerColor;
  ctx.fillRect(panel.x, panel.y, panel.width, panel.height);

  // Draw border
  ctx.strokeStyle = '#000000';
  ctx.lineWidth = 1;
  ctx.strokeRect(panel.x, panel.y, panel.width, panel.height);
}

/**
 * Adjust color brightness
 */
function adjustColorBrightness(color: string, percent: number): string {
  const num = parseInt(color.replace('#', ''), 16);
  const amt = Math.round(2.55 * percent);
  const R = (num >> 16) + amt;
  const G = ((num >> 8) & 0x00ff) + amt;
  const B = (num & 0x0000ff) + amt;
  return (
    '#' +
    (
      0x1000000 +
      (R < 255 ? (R < 1 ? 0 : R) : 255) * 0x10000 +
      (G < 255 ? (G < 1 ? 0 : G) : 255) * 0x100 +
      (B < 255 ? (B < 1 ? 0 : B) : 255)
    )
      .toString(16)
      .slice(1)
  );
}

/**
 * Validate if a data URL is a valid thumbnail
 */
export function isValidThumbnail(dataUrl: string): boolean {
  if (!dataUrl || typeof dataUrl !== 'string') {
    return false;
  }
  return dataUrl.startsWith('data:image/');
}

/**
 * Get thumbnail file size in bytes
 */
export function getThumbnailSize(dataUrl: string): number {
  if (!isValidThumbnail(dataUrl)) {
    return 0;
  }
  // Approximate size based on base64 length
  const base64 = dataUrl.split(',')[1] || '';
  return Math.ceil((base64.length * 3) / 4);
}
