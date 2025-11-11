/**
 * Configurable timeout settings for different API endpoints and operations
 * Provides centralized timeout management with sensible defaults
 */

export interface TimeoutConfig {
  default: number;
  health: number;
  auth: number;
  scriptGeneration: number;
  tts: number;
  imageGeneration: number;
  videoGeneration: number;
  videoRendering: number;
  fileUpload: number;
  fileDownload: number;
  quickOperations: number;
}

/**
 * Default timeout configurations in milliseconds
 */
export const DEFAULT_TIMEOUTS: TimeoutConfig = {
  default: 30000, // 30 seconds
  health: 5000, // 5 seconds
  auth: 10000, // 10 seconds
  scriptGeneration: 120000, // 2 minutes
  tts: 60000, // 1 minute
  imageGeneration: 180000, // 3 minutes
  videoGeneration: 300000, // 5 minutes
  videoRendering: 600000, // 10 minutes
  fileUpload: 120000, // 2 minutes
  fileDownload: 180000, // 3 minutes
  quickOperations: 5000, // 5 seconds
};

/**
 * Timeout configuration class with localStorage persistence
 */
class TimeoutConfigManager {
  private readonly STORAGE_KEY = 'aura_timeout_config';
  private config: TimeoutConfig;

  constructor() {
    this.config = this.loadConfig();
  }

  /**
   * Get timeout for a specific operation
   */
  public getTimeout(operation: keyof TimeoutConfig): number {
    return this.config[operation];
  }

  /**
   * Update timeout for a specific operation
   */
  public setTimeout(operation: keyof TimeoutConfig, value: number): void {
    if (value < 1000) {
      throw new Error('Timeout must be at least 1000ms (1 second)');
    }
    if (value > 3600000) {
      throw new Error('Timeout cannot exceed 3600000ms (1 hour)');
    }

    this.config[operation] = value;
    this.saveConfig();
  }

  /**
   * Update multiple timeouts at once
   */
  public setTimeouts(updates: Partial<TimeoutConfig>): void {
    Object.entries(updates).forEach(([key, value]) => {
      if (value !== undefined) {
        this.setTimeout(key as keyof TimeoutConfig, value);
      }
    });
  }

  /**
   * Get all current timeout configurations
   */
  public getConfig(): TimeoutConfig {
    return { ...this.config };
  }

  /**
   * Reset to default timeouts
   */
  public resetToDefaults(): void {
    this.config = { ...DEFAULT_TIMEOUTS };
    this.saveConfig();
  }

  /**
   * Reset specific timeout to default
   */
  public resetTimeout(operation: keyof TimeoutConfig): void {
    this.config[operation] = DEFAULT_TIMEOUTS[operation];
    this.saveConfig();
  }

  /**
   * Save configuration to localStorage
   */
  private saveConfig(): void {
    try {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.config));
    } catch (error) {
      console.warn('Failed to save timeout configuration:', error);
    }
  }

  /**
   * Load configuration from localStorage or use defaults
   */
  private loadConfig(): TimeoutConfig {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      if (stored) {
        const parsed = JSON.parse(stored) as Partial<TimeoutConfig>;
        return { ...DEFAULT_TIMEOUTS, ...parsed };
      }
    } catch (error) {
      console.warn('Failed to load timeout configuration, using defaults:', error);
    }
    return { ...DEFAULT_TIMEOUTS };
  }
}

export const timeoutConfig = new TimeoutConfigManager();

/**
 * Helper function to get timeout for common operations
 */
export function getOperationTimeout(
  operation:
    | 'default'
    | 'health'
    | 'auth'
    | 'script'
    | 'tts'
    | 'image'
    | 'video'
    | 'render'
    | 'upload'
    | 'download'
    | 'quick'
): number {
  const mapping: Record<string, keyof TimeoutConfig> = {
    default: 'default',
    health: 'health',
    auth: 'auth',
    script: 'scriptGeneration',
    tts: 'tts',
    image: 'imageGeneration',
    video: 'videoGeneration',
    render: 'videoRendering',
    upload: 'fileUpload',
    download: 'fileDownload',
    quick: 'quickOperations',
  };

  return timeoutConfig.getTimeout(mapping[operation] || 'default');
}
