/**
 * Actions API Client
 * Provides methods for server-side action logging and undo operations
 */

import type {
  RecordActionRequest,
  RecordActionResponse,
  UndoActionResponse,
  ActionHistoryQuery,
  ActionHistoryResponse,
  ActionDetailResponse,
} from '../../types/api-v1';
import apiClient from './apiClient';

/**
 * Records a new action in the action log
 */
export async function recordAction(request: RecordActionRequest): Promise<RecordActionResponse> {
  const response = await apiClient.post<RecordActionResponse>('/api/actions', request);
  return response.data;
}

/**
 * Undoes a previously recorded action
 */
export async function undoAction(actionId: string): Promise<UndoActionResponse> {
  const response = await apiClient.post<UndoActionResponse>(`/api/actions/${actionId}/undo`);
  return response.data;
}

/**
 * Gets action history with optional filters
 */
export async function getActionHistory(query?: ActionHistoryQuery): Promise<ActionHistoryResponse> {
  const response = await apiClient.get<ActionHistoryResponse>('/api/actions', {
    params: query,
  });
  return response.data;
}

/**
 * Gets detailed information about a specific action
 */
export async function getActionDetail(actionId: string): Promise<ActionDetailResponse> {
  const response = await apiClient.get<ActionDetailResponse>(`/api/actions/${actionId}`);
  return response.data;
}

/**
 * Exports action history in specified format
 * @param query Query parameters for filtering
 * @param format Export format ('csv' or 'json')
 * @returns Blob containing the exported data
 */
export async function exportActionHistory(
  query?: ActionHistoryQuery,
  format: 'csv' | 'json' = 'csv'
): Promise<Blob> {
  const response = await apiClient.get('/api/actions/export', {
    params: { ...query, format },
    responseType: 'blob',
  });
  return response.data;
}
