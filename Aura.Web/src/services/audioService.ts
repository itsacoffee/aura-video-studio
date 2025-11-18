import apiClient from './api/apiClient';

/**
 * Service for generating and managing audio narration using OpenAI or TTS providers.
 */

export interface GenerateNarrationRequest {
  text: string;
  voice: string;
  useOpenAI?: boolean;
  openAIApiKey?: string;
  model?: string;
  useCache?: boolean;
  stream?: boolean;
}

export interface NarrationResponse {
  success: boolean;
  audioPath?: string;
  transcript?: string;
  voice?: string;
  format?: string;
  provider?: string;
  durationSeconds?: number;
  error?: string;
  correlationId?: string;
}

/**
 * Available OpenAI voices for narration
 */
export enum OpenAIVoice {
  Alloy = 'alloy',
  Echo = 'echo',
  Fable = 'fable',
  Onyx = 'onyx',
  Nova = 'nova',
  Shimmer = 'shimmer',
}

/**
 * Generate audio narration from text using OpenAI GPT-4o or fallback TTS providers.
 */
export async function generateNarration(
  request: GenerateNarrationRequest
): Promise<NarrationResponse> {
  try {
    const response = await apiClient.post<NarrationResponse>(
      '/api/audio/generate-narration',
      request
    );
    return response.data;
  } catch (error: unknown) {
    const err = error instanceof Error ? error : new Error(String(error));
    console.error('Failed to generate narration:', err);
    throw new Error(`Narration generation failed: ${err.message}`);
  }
}

/**
 * Generate audio narration with streaming response (returns audio file directly).
 */
export async function generateNarrationStream(request: GenerateNarrationRequest): Promise<Blob> {
  try {
    const response = await apiClient.post(
      '/api/audio/generate-narration',
      { ...request, stream: true },
      { responseType: 'blob' }
    );
    return response.data;
  } catch (error: unknown) {
    const err = error instanceof Error ? error : new Error(String(error));
    console.error('Failed to generate narration stream:', err);
    throw new Error(`Narration streaming failed: ${err.message}`);
  }
}

/**
 * Create an audio element for preview playback.
 */
export function createAudioPreview(audioBlob: Blob): HTMLAudioElement {
  const url = URL.createObjectURL(audioBlob);
  const audio = new Audio(url);

  // Clean up object URL when audio is loaded
  audio.addEventListener('loadeddata', () => {
    URL.revokeObjectURL(url);
  });

  return audio;
}

/**
 * Play audio narration with preview controls.
 */
export async function previewNarration(
  request: GenerateNarrationRequest,
  onProgress?: (progress: number) => void
): Promise<HTMLAudioElement> {
  const audioBlob = await generateNarrationStream(request);
  const audio = createAudioPreview(audioBlob);

  // Report progress during playback
  if (onProgress) {
    audio.addEventListener('timeupdate', () => {
      if (audio.duration > 0) {
        const progress = (audio.currentTime / audio.duration) * 100;
        onProgress(progress);
      }
    });
  }

  // Auto-play
  await audio.play();

  return audio;
}

/**
 * Get available voices for selection.
 */
export function getAvailableVoices(): Array<{ value: string; label: string; description: string }> {
  return [
    {
      value: OpenAIVoice.Alloy,
      label: 'Alloy',
      description: 'Neutral and balanced tone',
    },
    {
      value: OpenAIVoice.Echo,
      label: 'Echo',
      description: 'Clear and resonant tone',
    },
    {
      value: OpenAIVoice.Fable,
      label: 'Fable',
      description: 'Expressive and storytelling tone',
    },
    {
      value: OpenAIVoice.Onyx,
      label: 'Onyx',
      description: 'Deep and authoritative tone',
    },
    {
      value: OpenAIVoice.Nova,
      label: 'Nova',
      description: 'Energetic and engaging tone',
    },
    {
      value: OpenAIVoice.Shimmer,
      label: 'Shimmer',
      description: 'Warm and friendly tone',
    },
  ];
}

/**
 * Download audio file from narration response.
 */
export async function downloadNarrationAudio(audioPath: string, filename?: string): Promise<void> {
  try {
    const response = await fetch(audioPath);
    const blob = await response.blob();

    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || 'narration.wav';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  } catch (error: unknown) {
    const err = error instanceof Error ? error : new Error(String(error));
    console.error('Failed to download narration audio:', err);
    throw new Error(`Audio download failed: ${err.message}`);
  }
}

const audioService = {
  generateNarration,
  generateNarrationStream,
  createAudioPreview,
  previewNarration,
  getAvailableVoices,
  downloadNarrationAudio,
};

export default audioService;
