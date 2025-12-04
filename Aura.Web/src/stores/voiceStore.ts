import { create } from 'zustand';
import { get as apiGet, post as apiPost, del as apiDel } from '@/services/api/apiClient';

/**
 * Voice cloning quality levels.
 */
export type VoiceCloneQuality = 'Instant' | 'Standard' | 'Professional';

/**
 * Voice provider types.
 */
export type VoiceProvider =
  | 'ElevenLabs'
  | 'Azure'
  | 'PlayHT'
  | 'Piper'
  | 'Mimic3'
  | 'WindowsSAPI'
  | 'Mock';

/**
 * A cloned voice created from audio samples.
 */
export interface ClonedVoice {
  id: string;
  name: string;
  providerId: string;
  provider: VoiceProvider;
  createdAt: string;
  quality: VoiceCloneQuality;
  samplePaths: string[];
}

/**
 * Dialogue type detected in script.
 */
export type DialogueType = 'Narration' | 'Dialogue' | 'Quote' | 'InternalThought';

/**
 * Emotion hints for TTS synthesis.
 */
export type EmotionHint =
  | 'Neutral'
  | 'Excited'
  | 'Sad'
  | 'Angry'
  | 'Curious'
  | 'Calm';

/**
 * A line of dialogue with metadata.
 */
export interface DialogueLine {
  startIndex: number;
  endIndex: number;
  text: string;
  characterName: string | null;
  type: DialogueType;
  emotion: EmotionHint | null;
}

/**
 * A detected character in the script.
 */
export interface DetectedCharacter {
  name: string;
  suggestedVoiceType: string;
  lineCount: number;
}

/**
 * Result of dialogue analysis.
 */
export interface DialogueAnalysis {
  lines: DialogueLine[];
  characters: DetectedCharacter[];
  hasMultipleCharacters: boolean;
}

/**
 * Voice descriptor with full information.
 */
export interface VoiceDescriptor {
  id: string;
  name: string;
  provider: VoiceProvider;
  locale: string;
  gender: 'Male' | 'Female' | 'Neutral';
  isCloned?: boolean;
}

/**
 * Voice assignment for a character.
 */
export interface VoiceAssignment {
  characterVoices: Record<string, VoiceDescriptor>;
  voicedLines: VoicedLine[];
}

/**
 * A dialogue line with assigned voice.
 */
export interface VoicedLine {
  line: DialogueLine;
  assignedVoice: VoiceDescriptor;
  synthesisSpec: {
    voiceName: string;
    rate: number;
    pitch: number;
    pause: string;
  };
}

/**
 * Voice store state interface.
 */
interface VoiceState {
  // Cloned voices
  clonedVoices: ClonedVoice[];
  isLoadingClonedVoices: boolean;
  cloningInProgress: boolean;
  cloningProgress: number;
  cloningError: string | null;

  // Available voices from providers
  availableVoices: VoiceDescriptor[];
  isLoadingVoices: boolean;

  // Dialogue analysis
  dialogueAnalysis: DialogueAnalysis | null;
  isAnalyzing: boolean;
  analysisError: string | null;

  // Voice assignment
  voiceAssignment: VoiceAssignment | null;
  manualAssignments: Record<string, string>;

  // Synthesis
  isSynthesizing: boolean;
  synthesisProgress: number;
  synthesizedAudioUrl: string | null;

  // Actions
  loadClonedVoices: () => Promise<void>;
  createClonedVoice: (name: string, samples: File[]) => Promise<ClonedVoice | null>;
  deleteClonedVoice: (voiceId: string) => Promise<void>;
  previewVoice: (voiceId: string) => Promise<string | null>;

  loadAvailableVoices: (provider?: string) => Promise<void>;

  analyzeDialogue: (script: string) => Promise<DialogueAnalysis | null>;
  clearDialogueAnalysis: () => void;

  setManualAssignment: (characterName: string, voiceId: string) => void;
  clearManualAssignments: () => void;
  assignVoices: () => Promise<VoiceAssignment | null>;

  synthesizeMultiVoice: () => Promise<string | null>;

  reset: () => void;
}

const initialState = {
  clonedVoices: [] as ClonedVoice[],
  isLoadingClonedVoices: false,
  cloningInProgress: false,
  cloningProgress: 0,
  cloningError: null as string | null,
  availableVoices: [] as VoiceDescriptor[],
  isLoadingVoices: false,
  dialogueAnalysis: null as DialogueAnalysis | null,
  isAnalyzing: false,
  analysisError: null as string | null,
  voiceAssignment: null as VoiceAssignment | null,
  manualAssignments: {} as Record<string, string>,
  isSynthesizing: false,
  synthesisProgress: 0,
  synthesizedAudioUrl: null as string | null,
};

interface ClonedVoicesResponse {
  success: boolean;
  voices: ClonedVoice[];
}

interface CloneVoiceResponse {
  success: boolean;
  voice: ClonedVoice;
}

interface VoicesResponse {
  success: boolean;
  voices: string[];
  provider?: string;
}

interface DialogueAnalysisResponse {
  success: boolean;
  analysis: DialogueAnalysis;
}

interface VoiceAssignmentResponse {
  success: boolean;
  assignment: VoiceAssignment;
}

/**
 * Voice store for managing voice cloning, dialogue detection, and multi-voice synthesis.
 */
export const useVoiceStore = create<VoiceState>((set, get) => ({
  ...initialState,

  loadClonedVoices: async () => {
    set({ isLoadingClonedVoices: true });
    try {
      const response = await apiGet<ClonedVoicesResponse>('/api/voices/cloned');
      set({
        clonedVoices: response.voices || [],
        isLoadingClonedVoices: false,
      });
    } catch (error: unknown) {
      console.error('Failed to load cloned voices:', error);
      set({ clonedVoices: [], isLoadingClonedVoices: false });
    }
  },

  createClonedVoice: async (name: string, samples: File[]) => {
    set({ cloningInProgress: true, cloningProgress: 0, cloningError: null });

    try {
      const formData = new FormData();
      formData.append('name', name);
      samples.forEach((sample) => formData.append('samples', sample));

      // Use fetch directly for FormData with progress tracking
      const response = await fetch('/api/voices/clone', {
        method: 'POST',
        body: formData,
      });

      if (!response.ok) {
        throw new Error(`Failed to clone voice: ${response.statusText}`);
      }

      const data: CloneVoiceResponse = await response.json();

      if (data.success && data.voice) {
        set((state) => ({
          clonedVoices: [...state.clonedVoices, data.voice],
          cloningInProgress: false,
          cloningProgress: 100,
        }));
        return data.voice;
      }

      set({ cloningInProgress: false, cloningError: 'Failed to create voice' });
      return null;
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error ? error.message : 'Voice cloning failed';
      set({ cloningInProgress: false, cloningError: errorMessage });
      return null;
    }
  },

  deleteClonedVoice: async (voiceId: string) => {
    try {
      await apiDel(`/api/voices/${voiceId}`);
      set((state) => ({
        clonedVoices: state.clonedVoices.filter((v) => v.id !== voiceId),
      }));
    } catch (error: unknown) {
      console.error('Failed to delete cloned voice:', error);
      throw error;
    }
  },

  previewVoice: async (voiceId: string) => {
    try {
      const response = await fetch(`/api/voices/${voiceId}/preview`);
      if (!response.ok) {
        throw new Error('Failed to fetch preview');
      }
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      return url;
    } catch (error: unknown) {
      console.error('Failed to preview voice:', error);
      return null;
    }
  },

  loadAvailableVoices: async (provider?: string) => {
    set({ isLoadingVoices: true });
    try {
      const params = provider ? `?provider=${provider}` : '';
      const response = await apiGet<VoicesResponse>(`/api/tts/voices${params}`);

      if (response.success && response.voices) {
        const voiceDescriptors: VoiceDescriptor[] = response.voices.map(
          (name) => ({
            id: `${provider || 'default'}_${name}`,
            name,
            provider: (provider as VoiceProvider) || 'Mock',
            locale: 'en-US',
            gender: 'Neutral' as const,
          })
        );

        // Merge with cloned voices
        const { clonedVoices } = get();
        const clonedDescriptors: VoiceDescriptor[] = clonedVoices.map((cv) => ({
          id: cv.id,
          name: cv.name,
          provider: cv.provider,
          locale: 'en-US',
          gender: 'Neutral' as const,
          isCloned: true,
        }));

        set({
          availableVoices: [...voiceDescriptors, ...clonedDescriptors],
          isLoadingVoices: false,
        });
      }
    } catch (error: unknown) {
      console.error('Failed to load available voices:', error);
      set({ isLoadingVoices: false });
    }
  },

  analyzeDialogue: async (script: string) => {
    set({ isAnalyzing: true, analysisError: null });
    try {
      const response = await apiPost<DialogueAnalysisResponse>(
        '/api/voices/analyze-dialogue',
        { script }
      );

      if (response.success && response.analysis) {
        set({
          dialogueAnalysis: response.analysis,
          isAnalyzing: false,
        });
        return response.analysis;
      }

      set({ isAnalyzing: false, analysisError: 'Analysis failed' });
      return null;
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error ? error.message : 'Dialogue analysis failed';
      set({ isAnalyzing: false, analysisError: errorMessage });
      return null;
    }
  },

  clearDialogueAnalysis: () => {
    set({
      dialogueAnalysis: null,
      voiceAssignment: null,
      manualAssignments: {},
    });
  },

  setManualAssignment: (characterName: string, voiceId: string) => {
    set((state) => ({
      manualAssignments: {
        ...state.manualAssignments,
        [characterName]: voiceId,
      },
    }));
  },

  clearManualAssignments: () => {
    set({ manualAssignments: {} });
  },

  assignVoices: async () => {
    const { dialogueAnalysis, manualAssignments, availableVoices } = get();
    if (!dialogueAnalysis) {
      return null;
    }

    try {
      // Build explicit assignments from manual selections
      const explicitAssignments: Record<string, VoiceDescriptor> = {};
      for (const [character, voiceId] of Object.entries(manualAssignments)) {
        const voice = availableVoices.find((v) => v.id === voiceId);
        if (voice) {
          explicitAssignments[character] = voice;
        }
      }

      const response = await apiPost<VoiceAssignmentResponse>(
        '/api/voices/assign-voices',
        {
          dialogueAnalysis,
          settings: {
            explicitAssignments:
              Object.keys(explicitAssignments).length > 0
                ? explicitAssignments
                : null,
            autoAssignFromPool: true,
          },
        }
      );

      if (response.success && response.assignment) {
        set({ voiceAssignment: response.assignment });
        return response.assignment;
      }

      return null;
    } catch (error: unknown) {
      console.error('Failed to assign voices:', error);
      return null;
    }
  },

  synthesizeMultiVoice: async () => {
    const { voiceAssignment } = get();
    if (!voiceAssignment) {
      return null;
    }

    set({ isSynthesizing: true, synthesisProgress: 0 });

    try {
      const response = await fetch('/api/voices/synthesize-multi', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ assignment: voiceAssignment }),
      });

      if (!response.ok) {
        throw new Error('Synthesis failed');
      }

      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      set({
        synthesizedAudioUrl: url,
        isSynthesizing: false,
        synthesisProgress: 100,
      });
      return url;
    } catch (error: unknown) {
      console.error('Failed to synthesize multi-voice audio:', error);
      set({ isSynthesizing: false });
      return null;
    }
  },

  reset: () => {
    set(initialState);
  },
}));
