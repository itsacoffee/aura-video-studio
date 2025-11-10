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
  export: ExportSettings;
  rateLimits: ProviderRateLimits;
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

export interface AdvancedSettings {
  offlineMode: boolean;
  stableDiffusionUrl: string;
  ollamaUrl: string;
  ollamaModel?: string;
  enableTelemetry: boolean;
  enableCrashReports: boolean;
}

export interface ExportSettings {
  watermark: WatermarkSettings;
  namingPattern: NamingPatternSettings;
  uploadDestinations: UploadDestination[];
  defaultPreset: string;
  autoOpenOutputFolder: boolean;
  autoUploadOnComplete: boolean;
  generateThumbnail: boolean;
  generateSubtitles: boolean;
  keepIntermediateFiles: boolean;
}

export interface WatermarkSettings {
  enabled: boolean;
  type: WatermarkType;
  imagePath: string;
  text: string;
  position: WatermarkPosition;
  opacity: number;
  scale: number;
  offsetX: number;
  offsetY: number;
  fontFamily: string;
  fontSize: number;
  fontColor: string;
  enableShadow: boolean;
}

export enum WatermarkType {
  Image = 'Image',
  Text = 'Text',
}

export enum WatermarkPosition {
  TopLeft = 'TopLeft',
  TopCenter = 'TopCenter',
  TopRight = 'TopRight',
  MiddleLeft = 'MiddleLeft',
  Center = 'Center',
  MiddleRight = 'MiddleRight',
  BottomLeft = 'BottomLeft',
  BottomCenter = 'BottomCenter',
  BottomRight = 'BottomRight',
}

export interface NamingPatternSettings {
  pattern: string;
  sanitizeFilenames: boolean;
  dateFormat: string;
  timeFormat: string;
  counterStart: number;
  counterDigits: number;
  customPrefix: string;
  customSuffix: string;
  replaceSpaces: boolean;
  forceLowercase: boolean;
}

export interface UploadDestination {
  id: string;
  name: string;
  type: UploadDestinationType;
  enabled: boolean;
  localPath: string;
  host: string;
  port: number;
  username: string;
  password: string;
  remotePath: string;
  s3BucketName: string;
  s3Region: string;
  s3AccessKey: string;
  s3SecretKey: string;
  azureContainerName: string;
  azureConnectionString: string;
  googleDriveFolderId: string;
  dropboxPath: string;
  deleteAfterUpload: boolean;
  maxRetries: number;
  timeoutSeconds: number;
}

export enum UploadDestinationType {
  LocalFolder = 'LocalFolder',
  FTP = 'FTP',
  SFTP = 'SFTP',
  S3 = 'S3',
  AzureBlob = 'AzureBlob',
  GoogleDrive = 'GoogleDrive',
  Dropbox = 'Dropbox',
  Webhook = 'Webhook',
}

export interface ProviderRateLimits {
  limits: Record<string, ProviderRateLimit>;
  global: GlobalRateLimitSettings;
}

export interface ProviderRateLimit {
  providerName: string;
  enabled: boolean;
  maxRequestsPerMinute: number;
  maxRequestsPerHour: number;
  maxRequestsPerDay: number;
  maxConcurrentRequests: number;
  maxTokensPerRequest: number;
  maxTokensPerMinute: number;
  dailyCostLimit: number;
  monthlyCostLimit: number;
  exceededBehavior: RateLimitBehavior;
  priority: number;
  fallbackProvider?: string;
  retryDelayMs: number;
  maxRetries: number;
  useExponentialBackoff: boolean;
  costWarningThreshold: number;
  notifyOnLimitReached: boolean;
}

export interface GlobalRateLimitSettings {
  enabled: boolean;
  maxTotalRequestsPerMinute: number;
  maxTotalDailyCost: number;
  maxTotalMonthlyCost: number;
  globalExceededBehavior: RateLimitBehavior;
  enableCircuitBreaker: boolean;
  circuitBreakerThreshold: number;
  circuitBreakerTimeoutSeconds: number;
  enableLoadBalancing: boolean;
  loadBalancingStrategy: LoadBalancingStrategy;
}

export enum RateLimitBehavior {
  Block = 'Block',
  Queue = 'Queue',
  Fallback = 'Fallback',
  Warn = 'Warn',
}

export enum LoadBalancingStrategy {
  RoundRobin = 'RoundRobin',
  LeastLoaded = 'LeastLoaded',
  LeastCost = 'LeastCost',
  LowestLatency = 'LowestLatency',
  Priority = 'Priority',
  Random = 'Random',
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
    advancedModeEnabled: false,
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
  export: {
    watermark: {
      enabled: false,
      type: WatermarkType.Text,
      imagePath: '',
      text: '',
      position: WatermarkPosition.BottomRight,
      opacity: 0.7,
      scale: 0.1,
      offsetX: 20,
      offsetY: 20,
      fontFamily: 'Arial',
      fontSize: 24,
      fontColor: '#FFFFFF',
      enableShadow: true,
    },
    namingPattern: {
      pattern: '{project}_{date}_{time}',
      sanitizeFilenames: true,
      dateFormat: 'yyyy-MM-dd',
      timeFormat: 'HHmmss',
      counterStart: 1,
      counterDigits: 3,
      customPrefix: '',
      customSuffix: '',
      replaceSpaces: true,
      forceLowercase: false,
    },
    uploadDestinations: [],
    defaultPreset: 'YouTube1080p',
    autoOpenOutputFolder: true,
    autoUploadOnComplete: false,
    generateThumbnail: true,
    generateSubtitles: false,
    keepIntermediateFiles: false,
  },
  rateLimits: {
    limits: {},
    global: {
      enabled: true,
      maxTotalRequestsPerMinute: 100,
      maxTotalDailyCost: 50,
      maxTotalMonthlyCost: 500,
      globalExceededBehavior: RateLimitBehavior.Block,
      enableCircuitBreaker: true,
      circuitBreakerThreshold: 5,
      circuitBreakerTimeoutSeconds: 60,
      enableLoadBalancing: true,
      loadBalancingStrategy: LoadBalancingStrategy.LeastCost,
    },
  },
  advanced: {
    offlineMode: false,
    stableDiffusionUrl: 'http://127.0.0.1:7860',
    ollamaUrl: 'http://127.0.0.1:11434',
    ollamaModel: 'llama3.1:8b-q4_k_m',
    enableTelemetry: false,
    enableCrashReports: false,
  },
  version: '1.0.0',
  lastUpdated: new Date().toISOString(),
});
