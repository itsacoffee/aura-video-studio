/**
 * Admin API Service
 * Handles administrative operations
 */

import { loggingService } from '../loggingService';
import { get, post, put, del } from './apiClient';

export interface SystemStats {
  users: {
    total: number;
    active: number;
    new: number;
  };
  projects: {
    total: number;
    generated: number;
  };
  storage: {
    used: number;
    available: number;
    total: number;
  };
  performance: {
    avgResponseTime: number;
    requests: number;
    errors: number;
  };
}

export interface User {
  id: string;
  email: string;
  name: string;
  role: string;
  status: 'active' | 'suspended' | 'deleted';
  createdAt: string;
  lastLoginAt?: string;
}

export interface AuditLog {
  id: string;
  userId: string;
  userEmail: string;
  action: string;
  resource: string;
  timestamp: string;
  ipAddress?: string;
  userAgent?: string;
  metadata?: Record<string, unknown>;
}

/**
 * Get system statistics
 */
export async function getSystemStats(): Promise<SystemStats> {
  try {
    loggingService.debug('Fetching system stats', 'adminApi', 'getSystemStats');
    
    const response = await get<SystemStats>('/api/admin/stats');
    
    loggingService.debug('System stats fetched', 'adminApi', 'getSystemStats');
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to fetch system stats',
      error instanceof Error ? error : new Error(String(error)),
      'adminApi',
      'getSystemStats'
    );
    throw error;
  }
}

/**
 * Get all users
 */
export async function getUsers(
  page: number = 1,
  limit: number = 50,
  filters?: {
    role?: string;
    status?: string;
    search?: string;
  }
): Promise<{
  users: User[];
  total: number;
  page: number;
  limit: number;
}> {
  try {
    loggingService.debug('Fetching users', 'adminApi', 'getUsers');
    
    const params = new URLSearchParams({
      page: page.toString(),
      limit: limit.toString(),
      ...filters,
    });
    
    const response = await get<{
      users: User[];
      total: number;
      page: number;
      limit: number;
    }>(`/api/admin/users?${params.toString()}`);
    
    loggingService.debug('Users fetched', 'adminApi', 'getUsers');
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to fetch users',
      error instanceof Error ? error : new Error(String(error)),
      'adminApi',
      'getUsers'
    );
    throw error;
  }
}

/**
 * Get user by ID
 */
export async function getUser(userId: string): Promise<User> {
  try {
    loggingService.debug('Fetching user', 'adminApi', 'getUser', { userId });
    
    const response = await get<User>(`/api/admin/users/${userId}`);
    
    loggingService.debug('User fetched', 'adminApi', 'getUser');
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to fetch user',
      error instanceof Error ? error : new Error(String(error)),
      'adminApi',
      'getUser'
    );
    throw error;
  }
}

/**
 * Update user
 */
export async function updateUser(
  userId: string,
  updates: Partial<Pick<User, 'name' | 'email' | 'role' | 'status'>>
): Promise<User> {
  try {
    loggingService.info('Updating user', 'adminApi', 'updateUser', { userId });
    
    const response = await put<User>(`/api/admin/users/${userId}`, updates);
    
    loggingService.info('User updated', 'adminApi', 'updateUser');
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to update user',
      error instanceof Error ? error : new Error(String(error)),
      'adminApi',
      'updateUser'
    );
    throw error;
  }
}

/**
 * Delete user
 */
export async function deleteUser(userId: string): Promise<void> {
  try {
    loggingService.warn('Deleting user', 'adminApi', 'deleteUser', { userId });
    
    await del<void>(`/api/admin/users/${userId}`);
    
    loggingService.warn('User deleted', 'adminApi', 'deleteUser');
  } catch (error) {
    loggingService.error(
      'Failed to delete user',
      error instanceof Error ? error : new Error(String(error)),
      'adminApi',
      'deleteUser'
    );
    throw error;
  }
}

/**
 * Suspend user
 */
export async function suspendUser(userId: string, reason?: string): Promise<void> {
  try {
    loggingService.warn('Suspending user', 'adminApi', 'suspendUser', { userId });
    
    await post<void>(`/api/admin/users/${userId}/suspend`, { reason });
    
    loggingService.warn('User suspended', 'adminApi', 'suspendUser');
  } catch (error) {
    loggingService.error(
      'Failed to suspend user',
      error instanceof Error ? error : new Error(String(error)),
      'adminApi',
      'suspendUser'
    );
    throw error;
  }
}

/**
 * Unsuspend user
 */
export async function unsuspendUser(userId: string): Promise<void> {
  try {
    loggingService.info('Unsuspending user', 'adminApi', 'unsuspendUser', { userId });
    
    await post<void>(`/api/admin/users/${userId}/unsuspend`);
    
    loggingService.info('User unsuspended', 'adminApi', 'unsuspendUser');
  } catch (error) {
    loggingService.error(
      'Failed to unsuspend user',
      error instanceof Error ? error : new Error(String(error)),
      'adminApi',
      'unsuspendUser'
    );
    throw error;
  }
}

/**
 * Get audit logs
 */
export async function getAuditLogs(
  page: number = 1,
  limit: number = 100,
  filters?: {
    userId?: string;
    action?: string;
    resource?: string;
    startDate?: string;
    endDate?: string;
  }
): Promise<{
  logs: AuditLog[];
  total: number;
  page: number;
  limit: number;
}> {
  try {
    loggingService.debug('Fetching audit logs', 'adminApi', 'getAuditLogs');
    
    const params = new URLSearchParams({
      page: page.toString(),
      limit: limit.toString(),
      ...filters,
    });
    
    const response = await get<{
      logs: AuditLog[];
      total: number;
      page: number;
      limit: number;
    }>(`/api/admin/audit-logs?${params.toString()}`);
    
    loggingService.debug('Audit logs fetched', 'adminApi', 'getAuditLogs');
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to fetch audit logs',
      error instanceof Error ? error : new Error(String(error)),
      'adminApi',
      'getAuditLogs'
    );
    throw error;
  }
}

/**
 * Clear cache
 */
export async function clearCache(cacheType?: string): Promise<void> {
  try {
    loggingService.info('Clearing cache', 'adminApi', 'clearCache', { cacheType });
    
    await post<void>('/api/admin/cache/clear', { cacheType });
    
    loggingService.info('Cache cleared', 'adminApi', 'clearCache');
  } catch (error) {
    loggingService.error(
      'Failed to clear cache',
      error instanceof Error ? error : new Error(String(error)),
      'adminApi',
      'clearCache'
    );
    throw error;
  }
}

/**
 * Run system maintenance
 */
export async function runMaintenance(tasks: string[]): Promise<{ results: Record<string, unknown> }> {
  try {
    loggingService.info('Running system maintenance', 'adminApi', 'runMaintenance', { tasks });
    
    const response = await post<{ results: Record<string, unknown> }>(
      '/api/admin/maintenance',
      { tasks }
    );
    
    loggingService.info('System maintenance completed', 'adminApi', 'runMaintenance');
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to run maintenance',
      error instanceof Error ? error : new Error(String(error)),
      'adminApi',
      'runMaintenance'
    );
    throw error;
  }
}

/**
 * Get system configuration
 */
export async function getSystemConfig(): Promise<Record<string, unknown>> {
  try {
    loggingService.debug('Fetching system config', 'adminApi', 'getSystemConfig');
    
    const response = await get<Record<string, unknown>>('/api/admin/config');
    
    loggingService.debug('System config fetched', 'adminApi', 'getSystemConfig');
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to fetch system config',
      error instanceof Error ? error : new Error(String(error)),
      'adminApi',
      'getSystemConfig'
    );
    throw error;
  }
}

/**
 * Update system configuration
 */
export async function updateSystemConfig(
  config: Record<string, unknown>
): Promise<Record<string, unknown>> {
  try {
    loggingService.info('Updating system config', 'adminApi', 'updateSystemConfig');
    
    const response = await put<Record<string, unknown>>('/api/admin/config', config);
    
    loggingService.info('System config updated', 'adminApi', 'updateSystemConfig');
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to update system config',
      error instanceof Error ? error : new Error(String(error)),
      'adminApi',
      'updateSystemConfig'
    );
    throw error;
  }
}
