import { create } from 'zustand';
import type {
  VersionResponse,
  VersionDetailResponse,
  VersionListResponse,
  VersionComparisonResponse,
  StorageUsageResponse,
} from '@/types/api-v1';

interface ProjectVersionsState {
  versions: VersionResponse[];
  totalCount: number;
  totalStorageBytes: number;
  selectedVersion: VersionDetailResponse | null;
  comparison: VersionComparisonResponse | null;
  storageUsage: StorageUsageResponse | null;
  loading: boolean;
  error: string | null;
  autosaveEnabled: boolean;
  lastAutosave: Date | null;

  setVersions: (data: VersionListResponse) => void;
  setSelectedVersion: (version: VersionDetailResponse | null) => void;
  setComparison: (comparison: VersionComparisonResponse | null) => void;
  setStorageUsage: (usage: StorageUsageResponse | null) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  setAutosaveEnabled: (enabled: boolean) => void;
  setLastAutosave: (date: Date | null) => void;
  addVersion: (version: VersionResponse) => void;
  updateVersion: (versionId: string, updates: Partial<VersionResponse>) => void;
  removeVersion: (versionId: string) => void;
  reset: () => void;
}

const initialState = {
  versions: [],
  totalCount: 0,
  totalStorageBytes: 0,
  selectedVersion: null,
  comparison: null,
  storageUsage: null,
  loading: false,
  error: null,
  autosaveEnabled: true,
  lastAutosave: null,
};

export const useProjectVersionsStore = create<ProjectVersionsState>((set) => ({
  ...initialState,

  setVersions: (data) =>
    set({
      versions: data.versions,
      totalCount: data.totalCount,
      totalStorageBytes: data.totalStorageBytes,
      error: null,
    }),

  setSelectedVersion: (version) => set({ selectedVersion: version }),

  setComparison: (comparison) => set({ comparison }),

  setStorageUsage: (usage) => set({ storageUsage: usage }),

  setLoading: (loading) => set({ loading }),

  setError: (error) => set({ error }),

  setAutosaveEnabled: (enabled) => set({ autosaveEnabled: enabled }),

  setLastAutosave: (date) => set({ lastAutosave: date }),

  addVersion: (version) =>
    set((state) => ({
      versions: [version, ...state.versions],
      totalCount: state.totalCount + 1,
      totalStorageBytes: state.totalStorageBytes + version.storageSizeBytes,
    })),

  updateVersion: (versionId, updates) =>
    set((state) => ({
      versions: state.versions.map((v) => (v.id === versionId ? { ...v, ...updates } : v)),
    })),

  removeVersion: (versionId) =>
    set((state) => {
      const removed = state.versions.find((v) => v.id === versionId);
      return {
        versions: state.versions.filter((v) => v.id !== versionId),
        totalCount: state.totalCount - 1,
        totalStorageBytes: state.totalStorageBytes - (removed?.storageSizeBytes || 0),
      };
    }),

  reset: () => set(initialState),
}));
