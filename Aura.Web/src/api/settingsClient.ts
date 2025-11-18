/**
 * Settings API client for managing application settings and preferences
 */

import { apiUrl } from '../config/api';

// Types
export interface UserSettings {
  version: string;
  lastUpdated: string;
  general: GeneralSettings;
  apiKeys: ApiKeysSettings;
  fileLocations: FileLocationsSettings;
  videoDefaults: VideoDefaultsSettings;
  editorPreferences: EditorPreferencesSettings;
  ui: UISettings;
  visualGeneration: VisualGenerationSettings;
  advanced: AdvancedSettings;
}

export interface GeneralSettings {
  defaultProjectSaveLocation: string;
  autosaveIntervalSeconds: number;
  autosaveEnabled: boolean;
  language: string;
  locale: string;
  theme: 'Light' | 'Dark' | 'Auto';
  startupBehavior: 'ShowDashboard' | 'ShowLastProject' | 'ShowNewProjectDialog';
  checkForUpdatesOnStartup: boolean;
  advancedModeEnabled: boolean;
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

export interface VisualGenerationSettings {
  enableNsfwDetection: boolean;
  contentSafetyLevel: string;
  variationsPerScene: number;
  enableClipScoring: boolean;
  enableQualityChecks: boolean;
  defaultAspectRatio: string;
  continuityStrength: number;
}

export interface AdvancedSettings {
  offlineMode: boolean;
  stableDiffusionUrl: string;
  ollamaUrl: string;
  ollamaModel: string;
  enableTelemetry: boolean;
  enableCrashReports: boolean;
}

export interface HardwarePerformanceSettings {
  hardwareAccelerationEnabled: boolean;
  preferredEncoder: string;
  selectedGpuId: string;
  ramAllocationMB: number;
  maxRenderingThreads: number;
  previewQuality: string;
  backgroundRenderingEnabled: boolean;
  maxCacheSizeMB: number;
  enableGpuMemoryMonitoring: boolean;
  enablePerformanceMetrics: boolean;
}

export interface ProviderConfiguration {
  openAI: OpenAIProviderSettings;
  ollama: OllamaProviderSettings;
  anthropic: AnthropicProviderSettings;
  azureOpenAI: AzureOpenAIProviderSettings;
  gemini: GeminiProviderSettings;
  elevenLabs: ElevenLabsProviderSettings;
  stableDiffusion: StableDiffusionProviderSettings;
  providerPriorityOrder: string[];
}

export interface OpenAIProviderSettings {
  enabled: boolean;
  apiKey: string;
  baseUrl: string;
  model: string;
  organizationId: string;
  projectId: string;
  timeoutSeconds: number;
  maxRetries: number;
}

export interface OllamaProviderSettings {
  enabled: boolean;
  baseUrl: string;
  model: string;
  executablePath: string;
  timeoutSeconds: number;
  autoStart: boolean;
}

export interface AnthropicProviderSettings {
  enabled: boolean;
  apiKey: string;
  model: string;
  timeoutSeconds: number;
}

export interface AzureOpenAIProviderSettings {
  enabled: boolean;
  apiKey: string;
  endpoint: string;
  deploymentName: string;
  apiVersion: string;
  timeoutSeconds: number;
}

export interface GeminiProviderSettings {
  enabled: boolean;
  apiKey: string;
  model: string;
  timeoutSeconds: number;
}

export interface ElevenLabsProviderSettings {
  enabled: boolean;
  apiKey: string;
  defaultVoiceId: string;
  timeoutSeconds: number;
}

export interface StableDiffusionProviderSettings {
  enabled: boolean;
  baseUrl: string;
  timeoutSeconds: number;
  autoStart: boolean;
}

export interface SettingsUpdateResult {
  success: boolean;
  message: string;
  errors: string[];
  warnings: string[];
}

export interface SettingsValidationResult {
  isValid: boolean;
  issues: ValidationIssue[];
}

export interface ValidationIssue {
  category: string;
  key: string;
  message: string;
  severity: 'Info' | 'Warning' | 'Error';
}

export interface ProviderTestResult {
  success: boolean;
  providerName: string;
  message: string;
  responseTimeMs: number;
  details: Record<string, string>;
}

export interface GpuDevice {
  id: string;
  name: string;
  vendor: string;
  vramMB: number;
  isDefault: boolean;
}

export interface EncoderOption {
  id: string;
  name: string;
  description: string;
  isHardwareAccelerated: boolean;
  isAvailable: boolean;
  requiredHardware: string[];
}

// API client
class SettingsClient {
  private baseUrl: string;

  constructor() {
    this.baseUrl = `${apiUrl}/settings`;
  }

  /**
   * Get all user settings
   */
  async getSettings(): Promise<UserSettings> {
    const response = await fetch(`${this.baseUrl}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to get settings: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Update user settings
   */
  async updateSettings(settings: UserSettings): Promise<SettingsUpdateResult> {
    const response = await fetch(`${this.baseUrl}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(settings),
    });

    if (!response.ok) {
      throw new Error(`Failed to update settings: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Reset settings to defaults
   */
  async resetSettings(): Promise<SettingsUpdateResult> {
    const response = await fetch(`${this.baseUrl}/reset`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to reset settings: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Get general settings section
   */
  async getGeneralSettings(): Promise<GeneralSettings> {
    const response = await fetch(`${this.baseUrl}/general`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to get general settings: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Update general settings section
   */
  async updateGeneralSettings(settings: GeneralSettings): Promise<SettingsUpdateResult> {
    const response = await fetch(`${this.baseUrl}/general`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(settings),
    });

    if (!response.ok) {
      throw new Error(`Failed to update general settings: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Validate settings
   */
  async validateSettings(settings: UserSettings): Promise<SettingsValidationResult> {
    const response = await fetch(`${this.baseUrl}/validate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(settings),
    });

    if (!response.ok) {
      throw new Error(`Failed to validate settings: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Export settings to JSON
   */
  async exportSettings(includeSecrets: boolean = false): Promise<string> {
    const response = await fetch(`${this.baseUrl}/export?includeSecrets=${includeSecrets}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to export settings: ${response.statusText}`);
    }

    return response.text();
  }

  /**
   * Import settings from JSON
   */
  async importSettings(
    json: string,
    overwriteExisting: boolean = false
  ): Promise<SettingsUpdateResult> {
    const response = await fetch(`${this.baseUrl}/import?overwriteExisting=${overwriteExisting}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(json),
    });

    if (!response.ok) {
      throw new Error(`Failed to import settings: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Get hardware performance settings
   */
  async getHardwareSettings(): Promise<HardwarePerformanceSettings> {
    const response = await fetch(`${this.baseUrl}/hardware`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to get hardware settings: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Update hardware performance settings
   */
  async updateHardwareSettings(
    settings: HardwarePerformanceSettings
  ): Promise<SettingsUpdateResult> {
    const response = await fetch(`${this.baseUrl}/hardware`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(settings),
    });

    if (!response.ok) {
      throw new Error(`Failed to update hardware settings: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Get provider configuration
   */
  async getProviderConfiguration(): Promise<ProviderConfiguration> {
    const response = await fetch(`${this.baseUrl}/providers`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to get provider configuration: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Update provider configuration
   */
  async updateProviderConfiguration(config: ProviderConfiguration): Promise<SettingsUpdateResult> {
    const response = await fetch(`${this.baseUrl}/providers`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(config),
    });

    if (!response.ok) {
      throw new Error(`Failed to update provider configuration: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Test provider connection
   */
  async testProviderConnection(providerName: string): Promise<ProviderTestResult> {
    const response = await fetch(`${this.baseUrl}/providers/${providerName}/test`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(
        `Failed to test provider connection for ${providerName}: ${response.statusText}`
      );
    }

    return response.json();
  }

  /**
   * Get available GPU devices
   */
  async getAvailableGpuDevices(): Promise<GpuDevice[]> {
    const response = await fetch(`${this.baseUrl}/hardware/gpus`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to get available GPU devices: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Get available hardware encoders
   */
  async getAvailableEncoders(): Promise<EncoderOption[]> {
    const response = await fetch(`${this.baseUrl}/hardware/encoders`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to get available encoders: ${response.statusText}`);
    }

    return response.json();
  }
}

// Export singleton instance
export const settingsClient = new SettingsClient();
