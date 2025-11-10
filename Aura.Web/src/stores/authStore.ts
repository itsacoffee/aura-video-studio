/**
 * Authentication Store
 * Manages authentication state, tokens, and user profile
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import {
  login as apiLogin,
  logout as apiLogout,
  refreshToken as apiRefreshToken,
  getCurrentUser,
  type LoginRequest,
  type UserProfile,
} from '../services/api/authApi';
import { loggingService } from '../services/loggingService';

export interface AuthState {
  // State
  isAuthenticated: boolean;
  user: UserProfile | null;
  token: string | null;
  refreshToken: string | null;
  tokenExpiry: number | null;
  isLoading: boolean;
  error: string | null;

  // Actions
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshAuthToken: () => Promise<void>;
  loadUser: () => Promise<void>;
  updateUser: (user: UserProfile) => void;
  clearError: () => void;
  setToken: (token: string, refreshToken?: string, expiresIn?: number) => void;
  checkTokenExpiry: () => boolean;
}

/**
 * Calculate token expiry timestamp
 */
function calculateExpiry(expiresIn: number): number {
  return Date.now() + expiresIn * 1000;
}

/**
 * Check if token is expired or about to expire (within 5 minutes)
 */
function isTokenExpired(expiry: number | null): boolean {
  if (!expiry) return true;
  const fiveMinutes = 5 * 60 * 1000;
  return Date.now() >= expiry - fiveMinutes;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      // Initial state
      isAuthenticated: false,
      user: null,
      token: null,
      refreshToken: null,
      tokenExpiry: null,
      isLoading: false,
      error: null,

      /**
       * Login with credentials
       */
      login: async (credentials: LoginRequest) => {
        set({ isLoading: true, error: null });

        try {
          loggingService.info('Logging in user', 'authStore', 'login');

          const response = await apiLogin(credentials);

          // Store tokens
          const tokenExpiry = calculateExpiry(response.expiresIn);
          
          // Update local storage for API client
          localStorage.setItem('auth_token', response.token);
          if (response.refreshToken) {
            localStorage.setItem('refresh_token', response.refreshToken);
          }

          set({
            isAuthenticated: true,
            user: response.user,
            token: response.token,
            refreshToken: response.refreshToken || null,
            tokenExpiry,
            isLoading: false,
            error: null,
          });

          loggingService.info('Login successful', 'authStore', 'login', {
            userId: response.user.id,
          });
        } catch (error) {
          const errorMessage =
            error instanceof Error ? error.message : 'Login failed. Please try again.';

          set({
            isAuthenticated: false,
            user: null,
            token: null,
            refreshToken: null,
            tokenExpiry: null,
            isLoading: false,
            error: errorMessage,
          });

          loggingService.error(
            'Login failed',
            error instanceof Error ? error : new Error(String(error)),
            'authStore',
            'login'
          );

          throw error;
        }
      },

      /**
       * Logout current user
       */
      logout: async () => {
        try {
          loggingService.info('Logging out user', 'authStore', 'logout');

          // Call API to invalidate token on server
          await apiLogout();
        } catch (error) {
          loggingService.error(
            'Logout API call failed',
            error instanceof Error ? error : new Error(String(error)),
            'authStore',
            'logout'
          );
          // Continue with local logout even if API call fails
        } finally {
          // Clear local storage
          localStorage.removeItem('auth_token');
          localStorage.removeItem('refresh_token');

          // Clear state
          set({
            isAuthenticated: false,
            user: null,
            token: null,
            refreshToken: null,
            tokenExpiry: null,
            error: null,
          });

          loggingService.info('Logout complete', 'authStore', 'logout');
        }
      },

      /**
       * Refresh authentication token
       */
      refreshAuthToken: async () => {
        const { refreshToken: currentRefreshToken } = get();

        if (!currentRefreshToken) {
          loggingService.warn('No refresh token available', 'authStore', 'refreshAuthToken');
          await get().logout();
          return;
        }

        try {
          loggingService.debug('Refreshing authentication token', 'authStore', 'refreshAuthToken');

          const response = await apiRefreshToken({ refreshToken: currentRefreshToken });

          // Update tokens
          const tokenExpiry = calculateExpiry(response.expiresIn);
          
          // Update local storage
          localStorage.setItem('auth_token', response.token);
          if (response.refreshToken) {
            localStorage.setItem('refresh_token', response.refreshToken);
          }

          set({
            token: response.token,
            refreshToken: response.refreshToken || currentRefreshToken,
            tokenExpiry,
          });

          loggingService.debug('Token refresh successful', 'authStore', 'refreshAuthToken');
        } catch (error) {
          loggingService.error(
            'Token refresh failed',
            error instanceof Error ? error : new Error(String(error)),
            'authStore',
            'refreshAuthToken'
          );

          // If refresh fails, logout
          await get().logout();
        }
      },

      /**
       * Load current user profile
       */
      loadUser: async () => {
        const { token, tokenExpiry } = get();

        // Check if we have a valid token
        if (!token) {
          loggingService.debug('No token available, cannot load user', 'authStore', 'loadUser');
          return;
        }

        // Check if token needs refresh
        if (isTokenExpired(tokenExpiry)) {
          loggingService.debug('Token expired, refreshing before loading user', 'authStore', 'loadUser');
          await get().refreshAuthToken();
        }

        set({ isLoading: true, error: null });

        try {
          loggingService.debug('Loading user profile', 'authStore', 'loadUser');

          const user = await getCurrentUser();

          set({
            user,
            isAuthenticated: true,
            isLoading: false,
            error: null,
          });

          loggingService.debug('User profile loaded', 'authStore', 'loadUser', {
            userId: user.id,
          });
        } catch (error) {
          loggingService.error(
            'Failed to load user profile',
            error instanceof Error ? error : new Error(String(error)),
            'authStore',
            'loadUser'
          );

          // If loading user fails, logout
          await get().logout();

          set({
            isLoading: false,
            error: 'Failed to load user profile',
          });
        }
      },

      /**
       * Update user profile in state
       */
      updateUser: (user: UserProfile) => {
        set({ user });
        loggingService.debug('User profile updated in state', 'authStore', 'updateUser');
      },

      /**
       * Clear error message
       */
      clearError: () => {
        set({ error: null });
      },

      /**
       * Set token manually (for testing or external auth)
       */
      setToken: (token: string, refreshToken?: string, expiresIn?: number) => {
        const tokenExpiry = expiresIn ? calculateExpiry(expiresIn) : null;

        // Update local storage
        localStorage.setItem('auth_token', token);
        if (refreshToken) {
          localStorage.setItem('refresh_token', refreshToken);
        }

        set({
          token,
          refreshToken: refreshToken || null,
          tokenExpiry,
          isAuthenticated: true,
        });

        loggingService.debug('Token set manually', 'authStore', 'setToken');
      },

      /**
       * Check if current token is expired
       */
      checkTokenExpiry: (): boolean => {
        const { tokenExpiry, refreshToken } = get();
        
        if (isTokenExpired(tokenExpiry)) {
          if (refreshToken) {
            // Trigger refresh in background
            get().refreshAuthToken();
          }
          return true;
        }
        
        return false;
      },
    }),
    {
      name: 'auth-store',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        token: state.token,
        refreshToken: state.refreshToken,
        tokenExpiry: state.tokenExpiry,
        user: state.user,
        // Don't persist loading/error states
      }),
      onRehydrateStorage: () => (state) => {
        // After rehydration, check if token is still valid
        if (state) {
          const { token, tokenExpiry } = state;
          
          if (token) {
            // Update API client with token
            localStorage.setItem('auth_token', token);
            
            // Set authenticated state based on token validity
            if (!isTokenExpired(tokenExpiry)) {
              state.isAuthenticated = true;
              // Load user profile in background
              state.loadUser();
            } else {
              // Token expired, try to refresh
              state.refreshAuthToken();
            }
          }
        }
      },
    }
  )
);

/**
 * Auto-refresh token before expiry
 * Checks every minute
 */
if (typeof window !== 'undefined') {
  setInterval(() => {
    const { isAuthenticated, checkTokenExpiry } = useAuthStore.getState();
    
    if (isAuthenticated) {
      checkTokenExpiry();
    }
  }, 60 * 1000); // Check every minute
}
