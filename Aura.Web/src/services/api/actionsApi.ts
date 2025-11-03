/**
 * Actions API Client
 * Provides methods for server-side action logging and undo operations
 */

import apiClient from './apiClient';
import type {
  RecordActionRequest,
  RecordActionResponse,
  UndoActionResponse,
  ActionHistoryQuery,
  ActionHistoryResponse,
  ActionDetailResponse,
} from '../../types/api-v1';

/**
 * Records a new action in the action log
 */
export async function recordAction(
  request: RecordActionRequest
): Promise<RecordActionResponse> {
  const response = await apiClient.post<RecordActionResponse>(
    '/api/actions',
    request
  );
  return response.data;
}

/**
 * Undoes a previously recorded action
 */
export async function undoAction(actionId: string): Promise<UndoActionResponse> {
  const response = await apiClient.post<UndoActionResponse>(
    `/api/actions/${actionId}/undo`
  );
  return response.data;
}

/**
 * Gets action history with optional filters
 */
export async function getActionHistory(
  query?: ActionHistoryQuery
): Promise<ActionHistoryResponse> {
  const response = await apiClient.get<ActionHistoryResponse>('/api/actions', {
    params: query,
  });
  return response.data;
}

/**
 * Gets detailed information about a specific action
 */
export async function getActionDetail(
  actionId: string
): Promise<ActionDetailResponse> {
  const response = await apiClient.get<ActionDetailResponse>(
    `/api/actions/${actionId}`
  );
  return response.data;
}
