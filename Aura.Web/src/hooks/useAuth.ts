/**
 * Authentication Hook
 * Provides easy access to auth state and actions
 */

import { useCallback } from 'react';
import { useAuthStore } from '../stores/authStore';
import type { LoginRequest } from '../services/api/authApi';

export function useAuth() {
  const {
    isAuthenticated,
    user,
    isLoading,
    error,
    login,
    logout,
    loadUser,
    clearError,
  } = useAuthStore();

  const handleLogin = useCallback(
    async (credentials: LoginRequest) => {
      try {
        await login(credentials);
      } catch (error) {
        // Error is already handled in the store
        throw error;
      }
    },
    [login]
  );

  const handleLogout = useCallback(async () => {
    try {
      await logout();
    } catch (error) {
      // Error is already handled in the store
      throw error;
    }
  }, [logout]);

  const refreshUser = useCallback(async () => {
    try {
      await loadUser();
    } catch (error) {
      // Error is already handled in the store
      throw error;
    }
  }, [loadUser]);

  return {
    // State
    isAuthenticated,
    user,
    isLoading,
    error,

    // Actions
    login: handleLogin,
    logout: handleLogout,
    refreshUser,
    clearError,

    // Computed
    isAdmin: user?.role === 'admin',
    hasRole: (role: string) => user?.role === role,
  };
}
