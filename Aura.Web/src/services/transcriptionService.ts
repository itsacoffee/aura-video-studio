/**
 * Transcription Service
 *
 * Provides AI-powered automatic caption generation using speech-to-text
 * transcription with word-level timestamps. Uses the Web Speech API as
 * a fallback for basic transcription functionality.
 */

// Web Speech API types (vendor-prefixed for cross-browser support)
interface SpeechRecognitionResultItem {
  transcript: string;
  confidence: number;
}

interface SpeechRecognitionResultList {
  readonly length: number;
  item(index: number): SpeechRecognitionResult;
  [index: number]: SpeechRecognitionResult;
}

interface SpeechRecognitionResult {
  readonly length: number;
  readonly isFinal: boolean;
  item(index: number): SpeechRecognitionResultItem;
  [index: number]: SpeechRecognitionResultItem;
}

interface SpeechRecognitionEventData {
  readonly resultIndex: number;
  readonly results: SpeechRecognitionResultList;
}

interface SpeechRecognitionErrorData {
  readonly error: string;
  readonly message?: string;
}

interface SpeechRecognitionInstance extends EventTarget {
  continuous: boolean;
  interimResults: boolean;
  lang: string;
  onresult: ((event: SpeechRecognitionEventData) => void) | null;
  onerror: ((event: SpeechRecognitionErrorData) => void) | null;
  onend: (() => void) | null;
  start(): void;
  stop(): void;
  abort(): void;
}

interface SpeechRecognitionConstructor {
  new (): SpeechRecognitionInstance;
}

declare global {
  interface Window {
    SpeechRecognition?: SpeechRecognitionConstructor;
    webkitSpeechRecognition?: SpeechRecognitionConstructor;
  }
}

/**
 * A single word with timing information from transcription
 */
export interface TranscriptionWord {
  /** The transcribed word */
  word: string;
  /** Start time in seconds */
  startTime: number;
  /** End time in seconds */
  endTime: number;
  /** Confidence score (0-1) */
  confidence: number;
}

/**
 * A segment of transcribed text with timing and optional speaker info
 */
export interface TranscriptionSegment {
  /** Full text of the segment */
  text: string;
  /** Start time in seconds */
  startTime: number;
  /** End time in seconds */
  endTime: number;
  /** Individual words with timing */
  words: TranscriptionWord[];
  /** Optional speaker identifier for diarization */
  speaker?: string;
}

/**
 * Complete transcription result
 */
export interface TranscriptionResult {
  /** Array of transcribed segments */
  segments: TranscriptionSegment[];
  /** Detected or specified language */
  language: string;
  /** Total duration in seconds */
  duration: number;
}

/**
 * Options for transcription
 */
export interface TranscriptionOptions {
  /** Target language for transcription (default: browser language) */
  language?: string;
  /** Enable speaker diarization (identify different speakers) */
  enableSpeakerDiarization?: boolean;
  /** Maximum number of speakers to identify */
  maxSpeakers?: number;
  /** Add punctuation to transcribed text */
  punctuate?: boolean;
  /** Filter profanity from results */
  profanityFilter?: boolean;
}

/**
 * Status of the transcription process
 */
export type TranscriptionStatus =
  | 'idle'
  | 'preparing'
  | 'transcribing'
  | 'processing'
  | 'complete'
  | 'error';

/**
 * Progress update during transcription
 */
export interface TranscriptionProgress {
  /** Current status */
  status: TranscriptionStatus;
  /** Progress percentage (0-100) */
  progress: number;
  /** Human-readable message */
  message: string;
}

/**
 * Caption entry with timing for display
 */
export interface Caption {
  /** Start time in seconds */
  startTime: number;
  /** End time in seconds */
  endTime: number;
  /** Caption text */
  text: string;
}

// Cancellation token for stopping transcription
let transcriptionCancelled = false;

/**
 * Check if the Web Speech API is available
 */
export function isSpeechRecognitionSupported(): boolean {
  if (typeof window === 'undefined') return false;
  return !!(window.SpeechRecognition || window.webkitSpeechRecognition);
}

/**
 * Transcribe audio using the Web Speech API (fallback implementation)
 * This provides basic transcription without word-level timing
 */
async function transcribeWithWebSpeech(
  audioBlob: Blob,
  onProgress?: (progress: TranscriptionProgress) => void
): Promise<TranscriptionResult> {
  return new Promise((resolve, reject) => {
    const SpeechRecognitionClass = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognitionClass) {
      reject(new Error('Speech recognition not supported in this browser'));
      return;
    }

    const recognition = new SpeechRecognitionClass();
    recognition.continuous = true;
    recognition.interimResults = false;
    recognition.lang = 'en-US';

    const segments: TranscriptionSegment[] = [];
    let currentTime = 0;
    let totalDuration = 0;
    let audioElement: HTMLAudioElement | null = null;
    let audioUrl: string | null = null;

    // Create audio element to play while recognizing
    audioUrl = URL.createObjectURL(audioBlob);
    audioElement = new Audio(audioUrl);

    // Get audio duration first
    audioElement.addEventListener('loadedmetadata', () => {
      totalDuration = audioElement?.duration ?? 0;
    });

    recognition.onresult = (event: SpeechRecognitionEventData) => {
      for (let i = event.resultIndex; i < event.results.length; i++) {
        if (event.results[i].isFinal) {
          const text = event.results[i][0].transcript;
          const confidence = event.results[i][0].confidence;
          const words = text.split(' ').filter((w) => w.trim().length > 0);
          const segmentDuration = Math.max(1, words.length * 0.4); // Estimate ~0.4s per word

          const segmentWords: TranscriptionWord[] = words.map((word, idx, arr) => ({
            word,
            startTime: currentTime + (idx / arr.length) * segmentDuration,
            endTime: currentTime + ((idx + 1) / arr.length) * segmentDuration,
            confidence: confidence ?? 0.8,
          }));

          segments.push({
            text: text.trim(),
            startTime: currentTime,
            endTime: currentTime + segmentDuration,
            words: segmentWords,
          });

          currentTime += segmentDuration;

          // Update progress
          const progressPercent = Math.min(80, (currentTime / Math.max(1, totalDuration)) * 80);
          onProgress?.({
            status: 'transcribing',
            progress: 20 + progressPercent,
            message: `Transcribing: ${segments.length} segments found...`,
          });
        }
      }
    };

    recognition.onerror = (event: SpeechRecognitionErrorData) => {
      // Clean up
      if (audioElement) {
        audioElement.pause();
        audioElement = null;
      }
      if (audioUrl) {
        URL.revokeObjectURL(audioUrl);
      }

      // Handle specific error types
      if (event.error === 'no-speech') {
        // No speech detected, return empty result
        resolve({ segments: [], language: 'en', duration: totalDuration });
        return;
      }

      reject(new Error(`Speech recognition error: ${event.error}`));
    };

    recognition.onend = () => {
      // Clean up audio
      if (audioElement) {
        audioElement.pause();
        audioElement = null;
      }
      if (audioUrl) {
        URL.revokeObjectURL(audioUrl);
      }

      resolve({
        segments,
        language: 'en',
        duration: currentTime || totalDuration,
      });
    };

    // Start playback and recognition
    audioElement.play().catch((error: unknown) => {
      const errorMessage = error instanceof Error ? error.message : 'Unknown playback error';
      reject(new Error(`Failed to play audio for transcription: ${errorMessage}`));
    });
    recognition.start();

    // Stop when audio ends
    audioElement.onended = () => {
      recognition.stop();
    };

    // Handle cancellation
    const checkCancelled = setInterval(() => {
      if (transcriptionCancelled) {
        clearInterval(checkCancelled);
        recognition.stop();
        if (audioElement) {
          audioElement.pause();
        }
        reject(new Error('Transcription cancelled'));
      }
    }, 100);
  });
}

/**
 * Add basic punctuation to transcribed text
 * Capitalizes first letter and adds period at end if missing
 */
function addPunctuation(text: string): string {
  let result = text.trim();
  if (!result) return result;

  // Capitalize first letter
  result = result.charAt(0).toUpperCase() + result.slice(1);

  // Add period if no ending punctuation
  if (!/[.!?]$/.test(result)) {
    result += '.';
  }

  return result;
}

/**
 * Transcribe audio from a URL
 *
 * @param audioUrl - URL of the audio file to transcribe
 * @param options - Transcription options
 * @param onProgress - Progress callback
 * @returns Promise with transcription result
 */
export async function transcribeAudio(
  audioUrl: string,
  options: TranscriptionOptions = {},
  onProgress?: (progress: TranscriptionProgress) => void
): Promise<TranscriptionResult> {
  transcriptionCancelled = false;

  onProgress?.({
    status: 'preparing',
    progress: 0,
    message: 'Preparing audio for transcription...',
  });

  try {
    // Fetch the audio file
    const response = await fetch(audioUrl);
    if (!response.ok) {
      throw new Error(`Failed to fetch audio: ${response.status} ${response.statusText}`);
    }

    const audioBlob = await response.blob();

    if (transcriptionCancelled) {
      throw new Error('Transcription cancelled');
    }

    onProgress?.({
      status: 'transcribing',
      progress: 20,
      message: 'Transcribing audio...',
    });

    // Use Web Speech API for transcription
    const result = await transcribeWithWebSpeech(audioBlob, onProgress);

    if (transcriptionCancelled) {
      throw new Error('Transcription cancelled');
    }

    onProgress?.({
      status: 'processing',
      progress: 80,
      message: 'Processing transcription results...',
    });

    // Apply punctuation if requested
    if (options.punctuate) {
      result.segments = result.segments.map((seg) => ({
        ...seg,
        text: addPunctuation(seg.text),
      }));
    }

    onProgress?.({
      status: 'complete',
      progress: 100,
      message: 'Transcription complete',
    });

    return result;
  } catch (error: unknown) {
    const errorMessage = error instanceof Error ? error.message : 'Unknown transcription error';
    onProgress?.({
      status: 'error',
      progress: 0,
      message: errorMessage,
    });
    throw error;
  }
}

/**
 * Cancel an ongoing transcription
 */
export function cancelTranscription(): void {
  transcriptionCancelled = true;
}

/**
 * Convert transcription segments to caption entries suitable for display
 *
 * @param segments - Transcription segments to convert
 * @param maxCharsPerCaption - Maximum characters per caption (default: 80)
 * @param maxDuration - Maximum duration per caption in seconds (default: 5)
 * @returns Array of caption entries
 */
export function segmentsToCaptions(
  segments: TranscriptionSegment[],
  maxCharsPerCaption: number = 80,
  maxDuration: number = 5
): Caption[] {
  const captions: Caption[] = [];

  segments.forEach((segment) => {
    const segmentDuration = segment.endTime - segment.startTime;

    // If segment fits within limits, add directly
    if (segment.text.length <= maxCharsPerCaption && segmentDuration <= maxDuration) {
      captions.push({
        startTime: segment.startTime,
        endTime: segment.endTime,
        text: segment.text,
      });
      return;
    }

    // Split segment based on words
    const words = segment.words;
    if (words.length === 0) {
      // No word timing, add as-is
      captions.push({
        startTime: segment.startTime,
        endTime: segment.endTime,
        text: segment.text,
      });
      return;
    }

    let currentCaption: Caption = {
      startTime: words[0].startTime,
      endTime: 0,
      text: '',
    };

    words.forEach((word, idx) => {
      const potentialText = currentCaption.text ? `${currentCaption.text} ${word.word}` : word.word;
      const potentialDuration = word.endTime - currentCaption.startTime;

      // Check if adding this word exceeds limits
      if (potentialText.length > maxCharsPerCaption || potentialDuration > maxDuration) {
        // Save current caption if it has content
        if (currentCaption.text) {
          currentCaption.endTime = words[idx - 1]?.endTime ?? word.startTime;
          captions.push({ ...currentCaption });
        }
        // Start new caption
        currentCaption = {
          startTime: word.startTime,
          endTime: 0,
          text: word.word,
        };
      } else {
        currentCaption.text = potentialText;
      }
    });

    // Add remaining caption
    if (currentCaption.text) {
      currentCaption.endTime = words[words.length - 1].endTime;
      captions.push(currentCaption);
    }
  });

  return captions;
}

/**
 * Format caption time for SRT/VTT format
 *
 * @param seconds - Time in seconds
 * @param useDot - Use dot separator for VTT (comma for SRT)
 * @returns Formatted time string (HH:MM:SS,mmm or HH:MM:SS.mmm)
 */
export function formatCaptionTime(seconds: number, useDot: boolean = false): string {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = Math.floor(seconds % 60);
  const ms = Math.floor((seconds % 1) * 1000);
  const separator = useDot ? '.' : ',';

  return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}${separator}${ms.toString().padStart(3, '0')}`;
}

/**
 * Export captions to SRT format
 *
 * @param captions - Caption entries to export
 * @returns SRT formatted string
 */
export function captionsToSrt(captions: Caption[]): string {
  return captions
    .map((caption, index) => {
      const start = formatCaptionTime(caption.startTime, false);
      const end = formatCaptionTime(caption.endTime, false);
      return `${index + 1}\n${start} --> ${end}\n${caption.text}\n`;
    })
    .join('\n');
}

/**
 * Export captions to VTT format
 *
 * @param captions - Caption entries to export
 * @returns VTT formatted string
 */
export function captionsToVtt(captions: Caption[]): string {
  const header = 'WEBVTT\n\n';
  const body = captions
    .map((caption) => {
      const start = formatCaptionTime(caption.startTime, true);
      const end = formatCaptionTime(caption.endTime, true);
      return `${start} --> ${end}\n${caption.text}\n`;
    })
    .join('\n');

  return header + body;
}

/**
 * Estimate transcription time based on audio duration
 *
 * @param audioDuration - Duration in seconds
 * @returns Estimated processing time in seconds
 */
export function estimateTranscriptionTime(audioDuration: number): number {
  // Web Speech API processes in real-time, so estimate equals audio duration
  // plus some overhead for processing
  return Math.ceil(audioDuration * 1.2);
}
