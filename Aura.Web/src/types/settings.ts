/**
 * Comprehensive user settings type definitions
 * Mirrors backend UserSettings model
 */

export interface UserSettings {
  general: GeneralSettings;
  apiKeys: ApiKeysSettings;
  fileLocations: FileLocationsSettings;
  videoDefaults: VideoDefaultsSettings;
  editorPreferences: EditorPreferencesSettings;
  ui: UISettings;
  advanced: AdvancedSettings;
  version: string;
  lastUpdated: string;
}

export interface GeneralSettings {
  defaultProjectSaveLocation: string;
  autosaveIntervalSeconds: number;
  autosaveEnabled: boolean;
  language: string;
  locale: string;
  theme: ThemeMode;
  startupBehavior: StartupBehavior;
  checkForUpdatesOnStartup: boolean;
}

export interface ApiKeysSettings {
  openAI: string;
  anthropic: string;
  stabilityAI: string;
  elevenLabs: string;
  pexels: string;
  pixabay: string;
  unsplash: string;
  google: string;
  azure: string;
}

export interface FileLocationsSettings {
  ffmpegPath: string;
  ffprobePath: string;
  outputDirectory: string;
  tempDirectory: string;
  mediaLibraryLocation: string;
  projectsDirectory: string;
}

export interface VideoDefaultsSettings {
  defaultResolution: string;
  defaultFrameRate: number;
  defaultCodec: string;
  defaultBitrate: string;
  defaultAudioCodec: string;
  defaultAudioBitrate: string;
  defaultAudioSampleRate: number;
}

export interface EditorPreferencesSettings {
  timelineSnapEnabled: boolean;
  timelineSnapInterval: number;
  playbackQuality: string;
  generateThumbnails: boolean;
  thumbnailInterval: number;
  keyboardShortcuts: Record<string, string>;
  showWaveforms: boolean;
  showTimecode: boolean;
}

export interface UISettings {
  scale: number;
  compactMode: boolean;
  colorScheme: string;
}

export interface AdvancedSettings {
  offlineMode: boolean;
  stableDiffusionUrl: string;
  ollamaUrl: string;
  enableTelemetry: boolean;
  enableCrashReports: boolean;
}

export enum ThemeMode {
  Light = 'Light',
  Dark = 'Dark',
  Auto = 'Auto',
}

export enum StartupBehavior {
  ShowDashboard = 'ShowDashboard',
  ShowLastProject = 'ShowLastProject',
  ShowNewProjectDialog = 'ShowNewProjectDialog',
}

// Default settings factory
export const createDefaultSettings = (): UserSettings => ({
  general: {
    defaultProjectSaveLocation: '',
    autosaveIntervalSeconds: 300,
    autosaveEnabled: true,
    language: 'en-US',
    locale: 'en-US',
    theme: ThemeMode.Auto,
    startupBehavior: StartupBehavior.ShowDashboard,
    checkForUpdatesOnStartup: true,
  },
  apiKeys: {
    openAI: '',
    anthropic: '',
    stabilityAI: '',
    elevenLabs: '',
    pexels: '',
    pixabay: '',
    unsplash: '',
    google: '',
    azure: '',
  },
  fileLocations: {
    ffmpegPath: '',
    ffprobePath: '',
    outputDirectory: '',
    tempDirectory: '',
    mediaLibraryLocation: '',
    projectsDirectory: '',
  },
  videoDefaults: {
    defaultResolution: '1920x1080',
    defaultFrameRate: 30,
    defaultCodec: 'libx264',
    defaultBitrate: '5M',
    defaultAudioCodec: 'aac',
    defaultAudioBitrate: '192k',
    defaultAudioSampleRate: 44100,
  },
  editorPreferences: {
    timelineSnapEnabled: true,
    timelineSnapInterval: 1.0,
    playbackQuality: 'high',
    generateThumbnails: true,
    thumbnailInterval: 5,
    keyboardShortcuts: {},
    showWaveforms: true,
    showTimecode: true,
  },
  ui: {
    scale: 100,
    compactMode: false,
    colorScheme: 'default',
  },
  advanced: {
    offlineMode: false,
    stableDiffusionUrl: 'http://127.0.0.1:7860',
    ollamaUrl: 'http://127.0.0.1:11434',
    enableTelemetry: false,
    enableCrashReports: false,
  },
  version: '1.0.0',
  lastUpdated: new Date().toISOString(),
});
