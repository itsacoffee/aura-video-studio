// Provider state management for preflight checks

export type CheckStatus = 'pass' | 'warn' | 'fail';

export type FixActionType = 'Install' | 'Start' | 'OpenSettings' | 'SwitchToFree' | 'Help';

export interface FixAction {
  type: FixActionType;
  label: string;
  parameter?: string | null;
  description: string;
}

export interface StageCheck {
  stage: string;
  status: CheckStatus;
  provider: string;
  message: string;
  hint?: string | null;
  suggestions?: string[] | null;
  fixActions?: FixAction[] | null;
}

export interface PreflightReport {
  ok: boolean;
  stages: StageCheck[];
}

export interface ProviderSelectionState {
  selectedProfile: string;
  preflightReport: PreflightReport | null;
  isRunningPreflight: boolean;
  lastCheckTime: Date | null;
  perStageSelection?: PerStageProviderSelection;
}

export interface PerStageProviderSelection {
  script?: string;
  tts?: string;
  visuals?: string;
  upload?: string;
}

export const defaultProviderState: ProviderSelectionState = {
  selectedProfile: 'Free-Only',
  preflightReport: null,
  isRunningPreflight: false,
  lastCheckTime: null,
  perStageSelection: undefined,
};

// Provider options for each stage
export const ScriptProviders = [
  { value: 'RuleBased', label: 'RuleBased (Free, Always Available)', description: 'Template-based script generation, works offline' },
  { value: 'Ollama', label: 'Ollama (Free, Local AI)', description: 'Local AI model, requires Ollama installation' },
  { value: 'OpenAI', label: 'OpenAI (Pro)', description: 'Cloud AI (GPT-4), requires API key' },
  { value: 'AzureOpenAI', label: 'Azure OpenAI (Pro)', description: 'Microsoft Azure cloud AI, requires credentials' },
  { value: 'Gemini', label: 'Gemini (Pro)', description: 'Google Gemini cloud AI, requires API key' },
] as const;

export const TtsProviders = [
  { value: 'Windows', label: 'Windows SAPI (Free)', description: 'Built-in Windows text-to-speech, always available' },
  { value: 'Piper', label: 'Piper (Local)', description: 'Fast local TTS, works offline, requires installation' },
  { value: 'Mimic3', label: 'Mimic3 (Local)', description: 'Neural TTS, works offline, requires installation' },
  { value: 'ElevenLabs', label: 'ElevenLabs (Pro)', description: 'Premium voice synthesis, requires API key' },
  { value: 'PlayHT', label: 'Play.ht (Pro)', description: 'Cloud voice synthesis, requires API key' },
] as const;

export const VisualsProviders = [
  { value: 'Stock', label: 'Stock Images (Free)', description: 'Curated stock photos from Pexels/Pixabay/Unsplash' },
  { value: 'LocalSD', label: 'Local SD (Managed)', description: 'Stable Diffusion WebUI, requires NVIDIA GPU with 6GB+ VRAM, auto-managed' },
  { value: 'ComfyUI', label: 'ComfyUI', description: 'Advanced node-based Stable Diffusion, requires NVIDIA GPU with 8GB+ VRAM' },
  { value: 'CloudPro', label: 'Cloud Pro (Stability/Runway)', description: 'Cloud AI image generation, requires API key' },
] as const;

export const UploadProviders = [
  { value: 'Off', label: 'Off (Manual)', description: 'No automatic upload, save locally' },
  { value: 'YouTube', label: 'YouTube (Manual auth)', description: 'Upload to YouTube with OAuth' },
] as const;
