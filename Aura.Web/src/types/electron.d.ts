/**
 * Electron API Type Definitions for Graphics/Mica Support
 * Extends window object with Electron IPC methods for Windows 11 Mica effects
 */

export type WindowMaterial = 'mica' | 'acrylic' | 'tabbed' | 'none';

export interface DisplayInfo {
  id: number;
  scaleFactor: number;
  size: { width: number; height: number };
  workArea: { x: number; y: number; width: number; height: number };
  bounds: { x: number; y: number; width: number; height: number };
  isPrimary?: boolean;
}

export interface GraphicsElectronSettings {
  transparency: boolean;
  blurEffects: boolean;
}

export interface GraphicsAPI {
  getMaterial: () => Promise<{ current: WindowMaterial; supported: boolean }>;
  setMaterial: (effect: WindowMaterial) => Promise<boolean>;
  isMicaSupported: () => Promise<boolean>;
  getAccentColor: () => Promise<string | null>;
  getDpiInfo: () => Promise<DisplayInfo>;
  getAllDisplays: () => Promise<DisplayInfo[]>;
  applySettings: (
    settings: GraphicsElectronSettings
  ) => Promise<{ success: boolean; error?: string }>;
  onThemeChange: (callback: (data: { isDark: boolean }) => void) => () => void;
  onAccentColorChange: (callback: (data: { color: string }) => void) => () => void;
}

declare global {
  interface Window {
    electronAPI?: {
      graphics?: GraphicsAPI;
    };
  }
}

export {};
