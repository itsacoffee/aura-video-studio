/**
 * Localization and Translation API Service
 * Provides methods for script translation, cultural adaptation, and glossary management
 */

import type {
  TranslateScriptRequest,
  TranslationResultDto,
  BatchTranslateRequest,
  BatchTranslationResultDto,
  CulturalAnalysisRequest,
  CulturalAnalysisResultDto,
  LanguageInfoDto,
  ProjectGlossaryDto,
  CreateGlossaryRequest,
  AddGlossaryEntryRequest,
  GlossaryEntryDto,
} from '../../types/api-v1';
import apiClient from './apiClient';

/**
 * Translate script with cultural localization
 */
export async function translateScript(
  request: TranslateScriptRequest
): Promise<TranslationResultDto> {
  const response = await apiClient.post<TranslationResultDto>(
    '/api/localization/translate',
    request
  );
  return response.data;
}

/**
 * Batch translate to multiple languages
 */
export async function batchTranslate(
  request: BatchTranslateRequest
): Promise<BatchTranslationResultDto> {
  const response = await apiClient.post<BatchTranslationResultDto>(
    '/api/localization/translate/batch',
    request
  );
  return response.data;
}

/**
 * Analyze content for cultural appropriateness
 */
export async function analyzeCulturalContent(
  request: CulturalAnalysisRequest
): Promise<CulturalAnalysisResultDto> {
  const response = await apiClient.post<CulturalAnalysisResultDto>(
    '/api/localization/analyze-culture',
    request
  );
  return response.data;
}

/**
 * Get list of all supported languages
 */
export async function getSupportedLanguages(): Promise<LanguageInfoDto[]> {
  const response = await apiClient.get<LanguageInfoDto[]>('/api/localization/languages');
  return response.data;
}

/**
 * Get specific language information
 */
export async function getLanguageInfo(languageCode: string): Promise<LanguageInfoDto> {
  const response = await apiClient.get<LanguageInfoDto>(
    `/api/localization/languages/${languageCode}`
  );
  return response.data;
}

/**
 * Create new glossary
 */
export async function createGlossary(request: CreateGlossaryRequest): Promise<ProjectGlossaryDto> {
  const response = await apiClient.post<ProjectGlossaryDto>('/api/localization/glossary', request);
  return response.data;
}

/**
 * Get glossary by ID
 */
export async function getGlossary(glossaryId: string): Promise<ProjectGlossaryDto> {
  const response = await apiClient.get<ProjectGlossaryDto>(
    `/api/localization/glossary/${glossaryId}`
  );
  return response.data;
}

/**
 * List all glossaries
 */
export async function listGlossaries(): Promise<ProjectGlossaryDto[]> {
  const response = await apiClient.get<ProjectGlossaryDto[]>('/api/localization/glossary');
  return response.data;
}

/**
 * Add entry to glossary
 */
export async function addGlossaryEntry(
  glossaryId: string,
  request: AddGlossaryEntryRequest
): Promise<GlossaryEntryDto> {
  const response = await apiClient.post<GlossaryEntryDto>(
    `/api/localization/glossary/${glossaryId}/entries`,
    request
  );
  return response.data;
}

/**
 * Delete glossary
 */
export async function deleteGlossary(glossaryId: string): Promise<void> {
  await apiClient.delete(`/api/localization/glossary/${glossaryId}`);
}
