import type {
  Brief,
  PlanSpec,
  PromptModifiers,
  PromptPreview,
  FewShotExample,
  PromptVersion,
} from '../../types';
import apiClient from './apiClient';

export interface PromptPreviewRequest {
  topic: string;
  audience?: string;
  goal?: string;
  tone: string;
  language: string;
  aspect: string;
  targetDurationMinutes: number;
  pacing: string;
  density: string;
  style: string;
  promptModifiers?: PromptModifiers;
}

export interface PromptPreviewResponse {
  systemPrompt: string;
  userPrompt: string;
  finalPrompt: string;
  substitutedVariables: Record<string, string>;
  promptVersion: string;
  estimatedTokens: number;
}

export interface ListExamplesResponse {
  examples: FewShotExample[];
  videoTypes: string[];
}

export interface ListPromptVersionsResponse {
  versions: PromptVersion[];
  defaultVersion: string;
}

export interface ValidationResult {
  isValid: boolean;
  message: string;
  errors?: string[];
}

/**
 * Get prompt preview with variable substitutions
 */
export async function getPromptPreview(
  brief: Brief,
  planSpec: PlanSpec,
  promptModifiers?: PromptModifiers
): Promise<PromptPreview> {
  const request: PromptPreviewRequest = {
    topic: brief.topic,
    audience: brief.audience,
    goal: brief.goal,
    tone: brief.tone,
    language: brief.language,
    aspect: brief.aspect,
    targetDurationMinutes: planSpec.targetDurationMinutes,
    pacing: planSpec.pacing,
    density: planSpec.density,
    style: planSpec.style,
    promptModifiers,
  };

  const response = await apiClient.post<PromptPreviewResponse>('/api/prompts/preview', request);

  return {
    systemPrompt: response.data.systemPrompt,
    userPrompt: response.data.userPrompt,
    finalPrompt: response.data.finalPrompt,
    substitutedVariables: response.data.substitutedVariables,
    promptVersion: response.data.promptVersion,
    estimatedTokens: response.data.estimatedTokens,
  };
}

/**
 * Get list of available few-shot examples
 */
export async function listExamples(
  videoType?: string
): Promise<{ examples: FewShotExample[]; videoTypes: string[] }> {
  const params = videoType ? { videoType } : undefined;
  const response = await apiClient.get<ListExamplesResponse>('/api/prompts/list-examples', {
    params,
  });

  return {
    examples: response.data.examples,
    videoTypes: response.data.videoTypes,
  };
}

/**
 * Get available prompt versions
 */
export async function listPromptVersions(): Promise<{
  versions: PromptVersion[];
  defaultVersion: string;
}> {
  const response = await apiClient.get<ListPromptVersionsResponse>('/api/prompts/versions');

  return {
    versions: response.data.versions,
    defaultVersion: response.data.defaultVersion,
  };
}

/**
 * Validate custom instructions for security
 */
export async function validateInstructions(instructions: string): Promise<ValidationResult> {
  const response = await apiClient.post<ValidationResult>('/api/prompts/validate-instructions', {
    instructions,
  });

  return response.data;
}
