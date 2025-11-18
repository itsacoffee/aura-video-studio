/**
 * Authentication API Service
 * Handles login, logout, token refresh, and user profile operations
 */

import { loggingService } from '../loggingService';
import { get, post } from './apiClient';

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface LoginResponse {
  token: string;
  refreshToken?: string;
  expiresIn: number;
  user: UserProfile;
}

export interface UserProfile {
  id: string;
  email: string;
  name: string;
  role: string;
  avatarUrl?: string;
  preferences?: Record<string, unknown>;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  token: string;
  refreshToken?: string;
  expiresIn: number;
}

export interface RegisterRequest {
  email: string;
  password: string;
  name: string;
}

export interface RegisterResponse {
  success: boolean;
  message: string;
  user?: UserProfile;
}

/**
 * Login with email and password
 */
export async function login(request: LoginRequest): Promise<LoginResponse> {
  try {
    loggingService.info('Attempting login', 'authApi', 'login', { email: request.email });

    const response = await post<LoginResponse>('/api/auth/login', request);

    loggingService.info('Login successful', 'authApi', 'login', { userId: response.user.id });

    return response;
  } catch (error) {
    loggingService.error(
      'Login failed',
      error instanceof Error ? error : new Error(String(error)),
      'authApi',
      'login'
    );
    throw error;
  }
}

/**
 * Logout current user
 */
export async function logout(): Promise<void> {
  try {
    loggingService.info('Logging out', 'authApi', 'logout');

    await post<void>('/api/auth/logout');

    loggingService.info('Logout successful', 'authApi', 'logout');
  } catch (error) {
    loggingService.error(
      'Logout failed',
      error instanceof Error ? error : new Error(String(error)),
      'authApi',
      'logout'
    );
    throw error;
  }
}

/**
 * Refresh authentication token
 */
export async function refreshToken(request: RefreshTokenRequest): Promise<RefreshTokenResponse> {
  try {
    loggingService.debug('Refreshing token', 'authApi', 'refreshToken');

    const response = await post<RefreshTokenResponse>('/api/auth/refresh', request, {
      _skipRetry: true, // Don't retry token refresh
    });

    loggingService.debug('Token refresh successful', 'authApi', 'refreshToken');

    return response;
  } catch (error) {
    loggingService.error(
      'Token refresh failed',
      error instanceof Error ? error : new Error(String(error)),
      'authApi',
      'refreshToken'
    );
    throw error;
  }
}

/**
 * Register a new user
 */
export async function register(request: RegisterRequest): Promise<RegisterResponse> {
  try {
    loggingService.info('Attempting registration', 'authApi', 'register', { email: request.email });

    const response = await post<RegisterResponse>('/api/auth/register', request);

    loggingService.info('Registration successful', 'authApi', 'register');

    return response;
  } catch (error) {
    loggingService.error(
      'Registration failed',
      error instanceof Error ? error : new Error(String(error)),
      'authApi',
      'register'
    );
    throw error;
  }
}

/**
 * Get current user profile
 */
export async function getCurrentUser(): Promise<UserProfile> {
  try {
    loggingService.debug('Fetching current user', 'authApi', 'getCurrentUser');

    const response = await get<UserProfile>('/api/auth/me');

    loggingService.debug('Current user fetched', 'authApi', 'getCurrentUser', {
      userId: response.id,
    });

    return response;
  } catch (error) {
    loggingService.error(
      'Failed to fetch current user',
      error instanceof Error ? error : new Error(String(error)),
      'authApi',
      'getCurrentUser'
    );
    throw error;
  }
}

/**
 * Update user profile
 */
export async function updateProfile(
  updates: Partial<Omit<UserProfile, 'id' | 'email' | 'role'>>
): Promise<UserProfile> {
  try {
    loggingService.info('Updating user profile', 'authApi', 'updateProfile');

    const response = await post<UserProfile>('/api/auth/profile', updates);

    loggingService.info('Profile updated successfully', 'authApi', 'updateProfile');

    return response;
  } catch (error) {
    loggingService.error(
      'Failed to update profile',
      error instanceof Error ? error : new Error(String(error)),
      'authApi',
      'updateProfile'
    );
    throw error;
  }
}

/**
 * Change password
 */
export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  try {
    loggingService.info('Changing password', 'authApi', 'changePassword');

    await post<void>('/api/auth/change-password', {
      currentPassword,
      newPassword,
    });

    loggingService.info('Password changed successfully', 'authApi', 'changePassword');
  } catch (error) {
    loggingService.error(
      'Failed to change password',
      error instanceof Error ? error : new Error(String(error)),
      'authApi',
      'changePassword'
    );
    throw error;
  }
}

/**
 * Request password reset
 */
export async function requestPasswordReset(email: string): Promise<void> {
  try {
    loggingService.info('Requesting password reset', 'authApi', 'requestPasswordReset', {
      email,
    });

    await post<void>('/api/auth/forgot-password', { email });

    loggingService.info('Password reset requested', 'authApi', 'requestPasswordReset');
  } catch (error) {
    loggingService.error(
      'Failed to request password reset',
      error instanceof Error ? error : new Error(String(error)),
      'authApi',
      'requestPasswordReset'
    );
    throw error;
  }
}

/**
 * Reset password with token
 */
export async function resetPassword(token: string, newPassword: string): Promise<void> {
  try {
    loggingService.info('Resetting password', 'authApi', 'resetPassword');

    await post<void>('/api/auth/reset-password', {
      token,
      newPassword,
    });

    loggingService.info('Password reset successfully', 'authApi', 'resetPassword');
  } catch (error) {
    loggingService.error(
      'Failed to reset password',
      error instanceof Error ? error : new Error(String(error)),
      'authApi',
      'resetPassword'
    );
    throw error;
  }
}

/**
 * Verify email with token
 */
export async function verifyEmail(token: string): Promise<void> {
  try {
    loggingService.info('Verifying email', 'authApi', 'verifyEmail');

    await post<void>('/api/auth/verify-email', { token });

    loggingService.info('Email verified successfully', 'authApi', 'verifyEmail');
  } catch (error) {
    loggingService.error(
      'Failed to verify email',
      error instanceof Error ? error : new Error(String(error)),
      'authApi',
      'verifyEmail'
    );
    throw error;
  }
}

/**
 * Check if email is available for registration
 */
export async function checkEmailAvailability(email: string): Promise<boolean> {
  try {
    const response = await get<{ available: boolean }>(
      `/api/auth/check-email?email=${encodeURIComponent(email)}`
    );

    return response.available;
  } catch (error) {
    loggingService.error(
      'Failed to check email availability',
      error instanceof Error ? error : new Error(String(error)),
      'authApi',
      'checkEmailAvailability'
    );
    return false;
  }
}
