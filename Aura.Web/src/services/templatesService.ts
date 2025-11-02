/**
 * Service for managing project templates
 */

import {
  ProjectTemplate,
  CreateFromTemplateRequest,
  SaveAsTemplateRequest,
  EffectPreset,
  TransitionPreset,
  TitleTemplate,
  TemplateCategory,
  PaginatedTemplatesResponse,
} from '../types/templates';
import { get, post, del } from './api/apiClient';

const API_BASE_URL = '/api/templates';

/**
 * Get templates with pagination and optional filtering
 */
export async function getTemplates(
  category?: TemplateCategory,
  subCategory?: string,
  systemOnly?: boolean,
  communityOnly?: boolean,
  page?: number,
  pageSize?: number,
  signal?: AbortSignal
): Promise<PaginatedTemplatesResponse> {
  const params = new URLSearchParams();
  if (category) params.append('category', category);
  if (subCategory) params.append('subCategory', subCategory);
  if (systemOnly) params.append('systemOnly', 'true');
  if (communityOnly) params.append('communityOnly', 'true');
  if (page !== undefined) params.append('page', page.toString());
  if (pageSize !== undefined) params.append('pageSize', pageSize.toString());

  const queryString = params.toString();
  const url = queryString ? `${API_BASE_URL}?${queryString}` : API_BASE_URL;

  return get<PaginatedTemplatesResponse>(url, signal ? { signal } : undefined);
}

/**
 * Get a specific template by ID
 */
export async function getTemplate(id: string): Promise<ProjectTemplate> {
  return get<ProjectTemplate>(`${API_BASE_URL}/${id}`);
}

/**
 * Create a new project from a template
 */
export async function createFromTemplate(
  request: CreateFromTemplateRequest
): Promise<{ projectFile: unknown; templateName: string }> {
  return post<{ projectFile: unknown; templateName: string }>(
    `${API_BASE_URL}/create-from-template`,
    request
  );
}

/**
 * Save current project as a template
 */
export async function saveAsTemplate(
  request: SaveAsTemplateRequest
): Promise<{ id: string; name: string; message: string }> {
  return post<{ id: string; name: string; message: string }>(
    `${API_BASE_URL}/save-as-template`,
    request
  );
}

/**
 * Delete a template
 */
export async function deleteTemplate(id: string): Promise<void> {
  return del<void>(`${API_BASE_URL}/${id}`);
}

/**
 * Get effect presets
 */
export async function getEffectPresets(): Promise<EffectPreset[]> {
  return get<EffectPreset[]>(`${API_BASE_URL}/effect-presets`);
}

/**
 * Get transition presets
 */
export async function getTransitionPresets(): Promise<TransitionPreset[]> {
  return get<TransitionPreset[]>(`${API_BASE_URL}/transition-presets`);
}

/**
 * Get title templates
 */
export async function getTitleTemplates(): Promise<TitleTemplate[]> {
  return get<TitleTemplate[]>(`${API_BASE_URL}/title-templates`);
}

/**
 * Seed sample templates (for development/testing)
 */
export async function seedSampleTemplates(): Promise<{ message: string }> {
  return post<{ message: string }>(`${API_BASE_URL}/seed`, {});
}
