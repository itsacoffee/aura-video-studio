/**
 * Zustand store for Provider Profile Lock state management
 */

import { create } from 'zustand';
import * as profileLockApi from '../api/profileLockClient';
import type { ProviderProfileLock, ProviderStatusInfo } from '../types/profileLock';

interface ProfileLockState {
  // Current active lock
  activeLock: ProviderProfileLock | null;
  hasActiveLock: boolean;

  // Provider status information
  providerStatus: ProviderStatusInfo | null;

  // Loading states
  isLoadingStatus: boolean;
  isSettingLock: boolean;
  isUnlocking: boolean;

  // Error state
  error: string | null;

  // Actions
  fetchStatus: (jobId?: string) => Promise<void>;
  setLock: (
    jobId: string,
    providerName: string,
    providerType: string,
    options?: {
      offlineModeEnabled?: boolean;
      applicableStages?: string[];
      isSessionLevel?: boolean;
    }
  ) => Promise<boolean>;
  unlockLock: (jobId: string, reason?: string) => Promise<boolean>;
  removeLock: (jobId: string, isSessionLevel?: boolean) => Promise<boolean>;
  updateProviderStatus: (status: ProviderStatusInfo) => void;
  clearError: () => void;
  reset: () => void;
}

const initialState = {
  activeLock: null,
  hasActiveLock: false,
  providerStatus: null,
  isLoadingStatus: false,
  isSettingLock: false,
  isUnlocking: false,
  error: null,
};

export const useProfileLockStore = create<ProfileLockState>((set, get) => ({
  ...initialState,

  fetchStatus: async (jobId?: string) => {
    set({ isLoadingStatus: true, error: null });

    try {
      const status = await profileLockApi.getProfileLockStatus(jobId);

      set({
        activeLock: status.activeLock ?? null,
        hasActiveLock: status.hasActiveLock,
        isLoadingStatus: false,
      });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Failed to fetch profile lock status:', errorObj);

      set({
        error: `Failed to fetch profile lock status: ${errorObj.message}`,
        isLoadingStatus: false,
      });
    }
  },

  setLock: async (
    jobId: string,
    providerName: string,
    providerType: string,
    options?: {
      offlineModeEnabled?: boolean;
      applicableStages?: string[];
      isSessionLevel?: boolean;
    }
  ) => {
    set({ isSettingLock: true, error: null });

    try {
      const request = profileLockApi.createProfileLockRequest(jobId, providerName, providerType, {
        offlineModeEnabled: options?.offlineModeEnabled,
        applicableStages: options?.applicableStages,
        isSessionLevel: options?.isSessionLevel,
      });

      const lock = await profileLockApi.setProfileLock(request);

      set({
        activeLock: lock,
        hasActiveLock: lock.isEnabled,
        isSettingLock: false,
      });

      return true;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Failed to set profile lock:', errorObj);

      set({
        error: `Failed to set profile lock: ${errorObj.message}`,
        isSettingLock: false,
      });

      return false;
    }
  },

  unlockLock: async (jobId: string, reason?: string) => {
    set({ isUnlocking: true, error: null });

    try {
      await profileLockApi.unlockProfileLock({
        jobId,
        reason,
      });

      const currentLock = get().activeLock;
      if (currentLock && currentLock.jobId === jobId) {
        set({
          activeLock: { ...currentLock, isEnabled: false },
          hasActiveLock: false,
          isUnlocking: false,
        });
      } else {
        set({ isUnlocking: false });
      }

      return true;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Failed to unlock profile lock:', errorObj);

      set({
        error: `Failed to unlock profile lock: ${errorObj.message}`,
        isUnlocking: false,
      });

      return false;
    }
  },

  removeLock: async (jobId: string, isSessionLevel: boolean = true) => {
    set({ isUnlocking: true, error: null });

    try {
      await profileLockApi.removeProfileLock(jobId, isSessionLevel);

      const currentLock = get().activeLock;
      if (currentLock && currentLock.jobId === jobId) {
        set({
          activeLock: null,
          hasActiveLock: false,
          isUnlocking: false,
        });
      } else {
        set({ isUnlocking: false });
      }

      return true;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Failed to remove profile lock:', errorObj);

      set({
        error: `Failed to remove profile lock: ${errorObj.message}`,
        isUnlocking: false,
      });

      return false;
    }
  },

  updateProviderStatus: (status: ProviderStatusInfo) => {
    set({ providerStatus: status });
  },

  clearError: () => {
    set({ error: null });
  },

  reset: () => {
    set(initialState);
  },
}));
