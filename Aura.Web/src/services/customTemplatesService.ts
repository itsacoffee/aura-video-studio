import axios from 'axios';
import { apiUrl } from '../config/api';
import type {
  CustomVideoTemplate,
  CreateCustomTemplateRequest,
  UpdateCustomTemplateRequest,
  TemplateExportData,
} from '../types/templates';

const API_BASE = apiUrl;

/**
 * Get all custom video templates
 */
export async function getCustomTemplates(category?: string): Promise<CustomVideoTemplate[]> {
  const params = category ? { category } : {};
  const response = await axios.get<CustomVideoTemplate[]>(`${API_BASE}/api/templates/custom`, {
    params,
  });
  return response.data;
}

/**
 * Get a specific custom template by ID
 */
export async function getCustomTemplate(id: string): Promise<CustomVideoTemplate> {
  const response = await axios.get<CustomVideoTemplate>(`${API_BASE}/api/templates/custom/${id}`);
  return response.data;
}

/**
 * Create a new custom template
 */
export async function createCustomTemplate(
  request: CreateCustomTemplateRequest
): Promise<CustomVideoTemplate> {
  const response = await axios.post<CustomVideoTemplate>(
    `${API_BASE}/api/templates/custom`,
    request
  );
  return response.data;
}

/**
 * Update an existing custom template
 */
export async function updateCustomTemplate(
  id: string,
  request: UpdateCustomTemplateRequest
): Promise<CustomVideoTemplate> {
  const response = await axios.put<CustomVideoTemplate>(
    `${API_BASE}/api/templates/custom/${id}`,
    request
  );
  return response.data;
}

/**
 * Delete a custom template
 */
export async function deleteCustomTemplate(id: string): Promise<void> {
  await axios.delete(`${API_BASE}/api/templates/custom/${id}`);
}

/**
 * Duplicate a custom template
 */
export async function duplicateCustomTemplate(id: string): Promise<CustomVideoTemplate> {
  const response = await axios.post<CustomVideoTemplate>(
    `${API_BASE}/api/templates/custom/${id}/duplicate`
  );
  return response.data;
}

/**
 * Set default custom template
 */
export async function setDefaultCustomTemplate(id: string): Promise<void> {
  await axios.post(`${API_BASE}/api/templates/custom/${id}/set-default`);
}

/**
 * Export a custom template to JSON
 */
export async function exportCustomTemplate(id: string): Promise<TemplateExportData> {
  const response = await axios.get<TemplateExportData>(
    `${API_BASE}/api/templates/custom/${id}/export`
  );
  return response.data;
}

/**
 * Import a custom template from JSON
 */
export async function importCustomTemplate(
  exportData: TemplateExportData
): Promise<CustomVideoTemplate> {
  const response = await axios.post<CustomVideoTemplate>(
    `${API_BASE}/api/templates/custom/import`,
    exportData
  );
  return response.data;
}
