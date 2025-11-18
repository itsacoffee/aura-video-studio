/**
 * User API Service
 * Handles user profile management and preferences
 */

import { loggingService } from '../loggingService';
import { get, post, put, del } from './apiClient';
import type { UserProfile } from './authApi';

export interface UserPreferences {
  theme?: 'light' | 'dark' | 'auto';
  language?: string;
  notifications?: {
    email?: boolean;
    push?: boolean;
    desktop?: boolean;
  };
  privacy?: {
    profileVisible?: boolean;
    showActivity?: boolean;
  };
  [key: string]: unknown;
}

export interface UserSettings {
  autoSave?: boolean;
  autoSaveInterval?: number;
  defaultQuality?: string;
  defaultFormat?: string;
  [key: string]: unknown;
}

/**
 * Get user preferences
 */
export async function getUserPreferences(): Promise<UserPreferences> {
  try {
    loggingService.debug('Fetching user preferences', 'userApi', 'getUserPreferences');

    const response = await get<UserPreferences>('/api/user/preferences');

    loggingService.debug('User preferences fetched', 'userApi', 'getUserPreferences');

    return response;
  } catch (error) {
    loggingService.error(
      'Failed to fetch user preferences',
      error instanceof Error ? error : new Error(String(error)),
      'userApi',
      'getUserPreferences'
    );
    throw error;
  }
}

/**
 * Update user preferences
 */
export async function updateUserPreferences(
  preferences: Partial<UserPreferences>
): Promise<UserPreferences> {
  try {
    loggingService.info('Updating user preferences', 'userApi', 'updateUserPreferences');

    const response = await put<UserPreferences>('/api/user/preferences', preferences);

    loggingService.info('User preferences updated', 'userApi', 'updateUserPreferences');

    return response;
  } catch (error) {
    loggingService.error(
      'Failed to update user preferences',
      error instanceof Error ? error : new Error(String(error)),
      'userApi',
      'updateUserPreferences'
    );
    throw error;
  }
}

/**
 * Get user settings
 */
export async function getUserSettings(): Promise<UserSettings> {
  try {
    loggingService.debug('Fetching user settings', 'userApi', 'getUserSettings');

    const response = await get<UserSettings>('/api/user/settings');

    loggingService.debug('User settings fetched', 'userApi', 'getUserSettings');

    return response;
  } catch (error) {
    loggingService.error(
      'Failed to fetch user settings',
      error instanceof Error ? error : new Error(String(error)),
      'userApi',
      'getUserSettings'
    );
    throw error;
  }
}

/**
 * Update user settings
 */
export async function updateUserSettings(settings: Partial<UserSettings>): Promise<UserSettings> {
  try {
    loggingService.info('Updating user settings', 'userApi', 'updateUserSettings');

    const response = await put<UserSettings>('/api/user/settings', settings);

    loggingService.info('User settings updated', 'userApi', 'updateUserSettings');

    return response;
  } catch (error) {
    loggingService.error(
      'Failed to update user settings',
      error instanceof Error ? error : new Error(String(error)),
      'userApi',
      'updateUserSettings'
    );
    throw error;
  }
}

/**
 * Upload user avatar
 */
export async function uploadAvatar(file: File): Promise<{ avatarUrl: string }> {
  try {
    loggingService.info('Uploading user avatar', 'userApi', 'uploadAvatar');

    const formData = new FormData();
    formData.append('avatar', file);

    const response = await post<{ avatarUrl: string }>('/api/user/avatar', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });

    loggingService.info('Avatar uploaded successfully', 'userApi', 'uploadAvatar');

    return response;
  } catch (error) {
    loggingService.error(
      'Failed to upload avatar',
      error instanceof Error ? error : new Error(String(error)),
      'userApi',
      'uploadAvatar'
    );
    throw error;
  }
}

/**
 * Delete user avatar
 */
export async function deleteAvatar(): Promise<void> {
  try {
    loggingService.info('Deleting user avatar', 'userApi', 'deleteAvatar');

    await del<void>('/api/user/avatar');

    loggingService.info('Avatar deleted successfully', 'userApi', 'deleteAvatar');
  } catch (error) {
    loggingService.error(
      'Failed to delete avatar',
      error instanceof Error ? error : new Error(String(error)),
      'userApi',
      'deleteAvatar'
    );
    throw error;
  }
}

/**
 * Get user activity log
 */
export async function getUserActivity(
  limit: number = 50,
  offset: number = 0
): Promise<{
  activities: Array<{
    id: string;
    type: string;
    description: string;
    timestamp: string;
    metadata?: Record<string, unknown>;
  }>;
  total: number;
}> {
  try {
    loggingService.debug('Fetching user activity', 'userApi', 'getUserActivity');

    const response = await get<{
      activities: Array<{
        id: string;
        type: string;
        description: string;
        timestamp: string;
        metadata?: Record<string, unknown>;
      }>;
      total: number;
    }>(`/api/user/activity?limit=${limit}&offset=${offset}`);

    loggingService.debug('User activity fetched', 'userApi', 'getUserActivity');

    return response;
  } catch (error) {
    loggingService.error(
      'Failed to fetch user activity',
      error instanceof Error ? error : new Error(String(error)),
      'userApi',
      'getUserActivity'
    );
    throw error;
  }
}

/**
 * Delete user account
 */
export async function deleteAccount(password: string): Promise<void> {
  try {
    loggingService.warn('Deleting user account', 'userApi', 'deleteAccount');

    await post<void>('/api/user/delete-account', { password });

    loggingService.warn('Account deleted successfully', 'userApi', 'deleteAccount');
  } catch (error) {
    loggingService.error(
      'Failed to delete account',
      error instanceof Error ? error : new Error(String(error)),
      'userApi',
      'deleteAccount'
    );
    throw error;
  }
}

/**
 * Export user data (GDPR compliance)
 */
export async function exportUserData(): Promise<Blob> {
  try {
    loggingService.info('Exporting user data', 'userApi', 'exportUserData');

    const response = await get<Blob>('/api/user/export-data', {
      responseType: 'blob',
    });

    loggingService.info('User data exported successfully', 'userApi', 'exportUserData');

    return response;
  } catch (error) {
    loggingService.error(
      'Failed to export user data',
      error instanceof Error ? error : new Error(String(error)),
      'userApi',
      'exportUserData'
    );
    throw error;
  }
}
