/**
 * Wizard API Integration Service
 * Connects Video Creation Wizard steps to backend API endpoints
 */

import { post, get, postWithTimeout } from './api/apiClient';
import type { ExtendedAxiosRequestConfig } from './api/apiClient';
import { streamGeneration } from './api/ollamaService';
import type { StreamingScriptEvent, StreamGenerationRequest } from './api/ollamaService';
import { loggingService as logger } from './loggingService';

/**
 * Brief data from Step 1
 */
export interface WizardBriefData {
  topic: string;
  audience: string;
  goal: string;
  tone: string;
  language: string;
  duration: number;
  videoType: string;
}

/**
 * Style data from Step 2
 */
export interface WizardStyleData {
  voiceProvider: string;
  voiceName: string;
  visualStyle: string;
  musicGenre?: string;
  musicEnabled: boolean;
}

/**
 * Script data from Step 3
 */
export interface WizardScriptData {
  generatedScript: string;
  scenes: Array<{
    id: string;
    text: string;
    duration: number;
    visualDescription: string;
  }>;
  totalDuration: number;
}

/**
 * Preview configuration from Step 4
 */
export interface WizardPreviewConfig {
  resolution: string;
  quality: string;
  previewDuration: number;
}

/**
 * Final export configuration from Step 5
 */
export interface WizardExportConfig {
  resolution: string;
  fps: number;
  codec: string;
  quality: number;
  includeSubs: boolean;
  outputFormat: string;
}

/**
 * Response from brief storage
 */
export interface StoreBriefResponse {
  briefId: string;
  correlationId: string;
  savedAt: string;
}

/**
 * Available voices response
 */
export interface AvailableVoicesResponse {
  voices: Array<{
    id: string;
    name: string;
    provider: string;
    language: string;
    gender: string;
    sampleUrl?: string;
  }>;
}

/**
 * Available visual styles response
 */
export interface AvailableStylesResponse {
  styles: Array<{
    id: string;
    name: string;
    description: string;
    thumbnail?: string;
  }>;
}

/**
 * Script generation response
 */
export interface ScriptGenerationResponse {
  jobId: string;
  script: string;
  scenes: Array<{
    id: string;
    text: string;
    duration: number;
    visualDescription: string;
  }>;
  totalDuration: number;
  correlationId: string;
}

/**
 * Preview generation response
 */
export interface PreviewGenerationResponse {
  previewId: string;
  previewUrl: string;
  status: string;
  correlationId: string;
}

/**
 * Step 1: Store brief data in backend
 */
export async function storeBrief(
  briefData: WizardBriefData,
  config?: ExtendedAxiosRequestConfig
): Promise<StoreBriefResponse> {
  try {
    logger.info('Storing brief', 'wizardService', 'storeBrief', { topic: briefData.topic });

    const response = await post<StoreBriefResponse>('/api/wizard/brief', briefData, config);

    logger.info('Brief stored successfully', 'wizardService', 'storeBrief', {
      briefId: response.briefId,
    });

    return response;
  } catch (error: unknown) {
    const errorObj = error instanceof Error ? error : new Error(String(error));
    logger.error('Failed to store brief', errorObj, 'wizardService', 'storeBrief');
    throw errorObj;
  }
}

/**
 * Step 2: Fetch available voices from API
 */
export async function fetchAvailableVoices(
  provider?: string,
  config?: ExtendedAxiosRequestConfig
): Promise<AvailableVoicesResponse> {
  try {
    logger.debug('Fetching available voices', 'wizardService', 'fetchAvailableVoices', {
      provider,
    });

    const url = provider ? `/api/voices?provider=${provider}` : '/api/voices';
    const response = await get<AvailableVoicesResponse>(url, config);

    logger.info('Voices fetched successfully', 'wizardService', 'fetchAvailableVoices', {
      count: response.voices.length,
    });

    return response;
  } catch (error: unknown) {
    const errorObj = error instanceof Error ? error : new Error(String(error));
    logger.error('Failed to fetch voices', errorObj, 'wizardService', 'fetchAvailableVoices');

    // Return empty response on error to allow graceful fallback
    return { voices: [] };
  }
}

/**
 * Step 2: Fetch available visual styles from API
 */
export async function fetchAvailableStyles(
  config?: ExtendedAxiosRequestConfig
): Promise<AvailableStylesResponse> {
  try {
    logger.debug('Fetching available styles', 'wizardService', 'fetchAvailableStyles');

    const response = await get<AvailableStylesResponse>('/api/styles', config);

    logger.info('Styles fetched successfully', 'wizardService', 'fetchAvailableStyles', {
      count: response.styles.length,
    });

    return response;
  } catch (error: unknown) {
    const errorObj = error instanceof Error ? error : new Error(String(error));
    logger.error('Failed to fetch styles', errorObj, 'wizardService', 'fetchAvailableStyles');

    // Return empty response on error to allow graceful fallback
    return { styles: [] };
  }
}

/**
 * Step 3: Call script generation endpoint
 */
export async function generateScript(
  briefData: WizardBriefData,
  styleData: WizardStyleData,
  config?: ExtendedAxiosRequestConfig
): Promise<ScriptGenerationResponse> {
  try {
    logger.info('Generating script', 'wizardService', 'generateScript', {
      topic: briefData.topic,
      voiceProvider: styleData.voiceProvider,
    });

    // Use extended timeout for script generation (21 minutes - very lenient for slow systems)
    // Must exceed backend timeout (20 min after PR #523) to allow for network overhead
    // Ollama can take 10-15 minutes on slow systems with large models
    const response = await postWithTimeout<ScriptGenerationResponse>(
      '/api/wizard/generate-script',
      {
        brief: briefData,
        style: styleData,
      },
      1260000, // 21 minutes - exceeds backend 20-minute timeout (after PR #523) to allow for network overhead
      config
    );

    logger.info('Script generated successfully', 'wizardService', 'generateScript', {
      jobId: response.jobId,
      sceneCount: response.scenes.length,
    });

    return response;
  } catch (error: unknown) {
    const errorObj = error instanceof Error ? error : new Error(String(error));
    logger.error('Failed to generate script', errorObj, 'wizardService', 'generateScript');
    throw errorObj;
  }
}

/**
 * Step 4: Trigger preview generation
 */
export async function generatePreview(
  briefData: WizardBriefData,
  styleData: WizardStyleData,
  scriptData: WizardScriptData,
  previewConfig: WizardPreviewConfig,
  config?: ExtendedAxiosRequestConfig
): Promise<PreviewGenerationResponse> {
  try {
    logger.info('Generating preview', 'wizardService', 'generatePreview', {
      topic: briefData.topic,
      resolution: previewConfig.resolution,
    });

    // Use extended timeout for preview generation (3 minutes)
    const response = await postWithTimeout<PreviewGenerationResponse>(
      '/api/wizard/generate-preview',
      {
        brief: briefData,
        style: styleData,
        script: scriptData,
        previewConfig,
      },
      180000, // 3 minute timeout
      config
    );

    logger.info('Preview generated successfully', 'wizardService', 'generatePreview', {
      previewId: response.previewId,
    });

    return response;
  } catch (error: unknown) {
    const errorObj = error instanceof Error ? error : new Error(String(error));
    logger.error('Failed to generate preview', errorObj, 'wizardService', 'generatePreview');
    throw errorObj;
  }
}

/**
 * Step 5: Start final video rendering
 */
export async function startFinalRendering(
  briefData: WizardBriefData,
  styleData: WizardStyleData,
  _scriptData: WizardScriptData,
  exportConfig: WizardExportConfig,
  config?: ExtendedAxiosRequestConfig
): Promise<{ jobId: string; correlationId: string }> {
  try {
    logger.info('Starting final rendering', 'wizardService', 'startFinalRendering', {
      topic: briefData.topic,
      resolution: exportConfig.resolution,
      codec: exportConfig.codec,
    });

    // Map wizard data to job creation format
    const jobRequest = {
      brief: {
        topic: briefData.topic,
        audience: briefData.audience,
        goal: briefData.goal,
        tone: briefData.tone,
        language: briefData.language,
        aspect: '16:9', // Default aspect ratio
      },
      planSpec: {
        targetDuration: `PT${briefData.duration}S`, // ISO 8601 duration format
        pacing: 'Medium',
        density: 'Balanced',
        style: styleData.visualStyle,
      },
      voiceSpec: {
        voiceName: styleData.voiceName,
        rate: 1.0,
        pitch: 0.0,
        pause: 'Natural',
      },
      renderSpec: {
        res: exportConfig.resolution,
        container: exportConfig.outputFormat || 'mp4',
        videoBitrateK: 5000,
        audioBitrateK: 192,
        fps: exportConfig.fps,
        codec: exportConfig.codec,
        qualityLevel: exportConfig.quality.toString(),
        enableSceneCut: true,
      },
    };

    // Use extended timeout for job creation (5 minutes)
    const response = await postWithTimeout<{
      jobId: string;
      correlationId: string;
      status: string;
    }>(
      '/api/jobs',
      jobRequest,
      300000, // 5 minute timeout
      config
    );

    logger.info('Final rendering started', 'wizardService', 'startFinalRendering', {
      jobId: response.jobId,
    });

    return { jobId: response.jobId, correlationId: response.correlationId };
  } catch (error: unknown) {
    const errorObj = error instanceof Error ? error : new Error(String(error));
    logger.error(
      'Failed to start final rendering',
      errorObj,
      'wizardService',
      'startFinalRendering'
    );
    throw errorObj;
  }
}

/**
 * Save wizard state for later resumption
 */
export async function saveWizardState(
  wizardData: {
    brief: WizardBriefData;
    style: WizardStyleData;
    script?: WizardScriptData;
    currentStep: number;
  },
  config?: ExtendedAxiosRequestConfig
): Promise<{ wizardId: string }> {
  try {
    logger.info('Saving wizard state', 'wizardService', 'saveWizardState', {
      currentStep: wizardData.currentStep,
    });

    const response = await post<{ wizardId: string }>('/api/wizard/save-state', wizardData, config);

    logger.info('Wizard state saved', 'wizardService', 'saveWizardState', {
      wizardId: response.wizardId,
    });

    return response;
  } catch (error: unknown) {
    const errorObj = error instanceof Error ? error : new Error(String(error));
    logger.error('Failed to save wizard state', errorObj, 'wizardService', 'saveWizardState');
    throw errorObj;
  }
}

/**
 * Load saved wizard state
 */
export async function loadWizardState(
  wizardId: string,
  config?: ExtendedAxiosRequestConfig
): Promise<{
  brief: WizardBriefData;
  style: WizardStyleData;
  script?: WizardScriptData;
  currentStep: number;
}> {
  try {
    logger.debug('Loading wizard state', 'wizardService', 'loadWizardState', { wizardId });

    const response = await get<{
      brief: WizardBriefData;
      style: WizardStyleData;
      script?: WizardScriptData;
      currentStep: number;
    }>(`/api/wizard/load-state/${wizardId}`, config);

    logger.info('Wizard state loaded', 'wizardService', 'loadWizardState', {
      wizardId,
      currentStep: response.currentStep,
    });

    return response;
  } catch (error: unknown) {
    const errorObj = error instanceof Error ? error : new Error(String(error));
    logger.error('Failed to load wizard state', errorObj, 'wizardService', 'loadWizardState');
    throw errorObj;
  }
}

/**
 * Helper: Parse script text into scenes
 */
function parseScriptIntoScenes(
  scriptText: string
): Array<{ id: string; text: string; duration: number; visualDescription: string }> {
  // Simple scene extraction - split by scene markers or paragraphs
  const sceneTexts = scriptText.split(/(?:\n\n|Scene \d+:)/g).filter((s) => s.trim());

  return sceneTexts.map((text, index) => ({
    id: `scene-${index + 1}`,
    text: text.trim(),
    duration: estimateSceneDuration(text),
    visualDescription: extractVisualDescription(text),
  }));
}

/**
 * Helper: Estimate scene duration from text (150 words per minute = 2.5 words per second)
 */
function estimateSceneDuration(text: string): number {
  const wordCount = text.split(/\s+/).length;
  return Math.ceil(wordCount / 2.5);
}

/**
 * Helper: Extract visual description from text (e.g., [Visual: ...])
 */
function extractVisualDescription(text: string): string {
  const match = text.match(/\[Visual:\s*([^\]]+)\]/);
  return match ? match[1] : 'General scene visuals';
}

/**
 * Helper: Calculate total duration from script text
 */
function calculateDuration(scriptText: string): number {
  const scenes = parseScriptIntoScenes(scriptText);
  return scenes.reduce((total, scene) => total + scene.duration, 0);
}

/**
 * Step 3: Generate script with SSE streaming for real-time progress
 */
export async function generateScriptWithProgress(
  briefData: WizardBriefData,
  styleData: WizardStyleData,
  onProgress: (event: StreamingScriptEvent) => void,
  _config?: ExtendedAxiosRequestConfig
): Promise<ScriptGenerationResponse> {
  try {
    logger.info(
      'Generating script with SSE streaming',
      'wizardService',
      'generateScriptWithProgress',
      {
        topic: briefData.topic,
        voiceProvider: styleData.voiceProvider,
      }
    );

    let accumulatedScript = '';

    // Convert wizard data to generation request
    const request: StreamGenerationRequest = {
      topic: briefData.topic,
      audience: briefData.audience,
      goal: briefData.goal,
      tone: briefData.tone,
      language: briefData.language,
      targetDurationSeconds: briefData.duration || 60,
    };

    await streamGeneration(request, (event: StreamingScriptEvent) => {
      // Forward progress events to caller
      onProgress(event);

      // Accumulate script content - prefer accumulatedContent from event when available
      if (event.eventType === 'chunk') {
        if (event.accumulatedContent) {
          accumulatedScript = event.accumulatedContent;
        } else if (event.content) {
          accumulatedScript += event.content;
        }
      } else if (event.eventType === 'complete') {
        if (event.accumulatedContent) {
          accumulatedScript = event.accumulatedContent;
        }
      }
    });

    // Parse final accumulated script
    if (!accumulatedScript) {
      throw new Error('No script content received from streaming generation');
    }

    // Create response format matching non-streaming endpoint
    const finalResponse: ScriptGenerationResponse = {
      jobId: crypto.randomUUID(),
      script: accumulatedScript,
      scenes: parseScriptIntoScenes(accumulatedScript),
      totalDuration: calculateDuration(accumulatedScript),
      correlationId: crypto.randomUUID(),
    };

    logger.info(
      'Script generated successfully via SSE',
      'wizardService',
      'generateScriptWithProgress',
      {
        jobId: finalResponse.jobId,
        sceneCount: finalResponse.scenes.length,
      }
    );

    return finalResponse;
  } catch (error: unknown) {
    const errorObj = error instanceof Error ? error : new Error(String(error));
    logger.error(
      'Failed to generate script via SSE',
      errorObj,
      'wizardService',
      'generateScriptWithProgress'
    );
    throw errorObj;
  }
}
