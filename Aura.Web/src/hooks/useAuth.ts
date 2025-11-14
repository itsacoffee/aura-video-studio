/**
 * Authentication Hook
 * Provides easy access to auth state and actions
 */

import { useCallback } from 'react';
import type { LoginRequest } from '../services/api/authApi';
import { useAuthStore } from '../stores/authStore';

export function useAuth() {
  const { isAuthenticated, user, isLoading, error, login, logout, loadUser, clearError } =
    useAuthStore();

  const handleLogin = useCallback(
    async (credentials: LoginRequest) => {
      // Error is already handled in the store
      await login(credentials);
    },
    [login]
  );

  const handleLogout = useCallback(async () => {
    // Error is already handled in the store
    await logout();
  }, [logout]);

  const refreshUser = useCallback(async () => {
    // Error is already handled in the store
    await loadUser();
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
