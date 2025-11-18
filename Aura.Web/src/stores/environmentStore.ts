import { create } from 'zustand';

interface EnvironmentState {
  isElectron: boolean;
  platform: string | null;
  arch: string | null;
  mode: string;
  version: string | null;
  backendUrl: string | null;
  paths: Record<string, unknown> | null;
  hydrate: () => Promise<void>;
}

const defaultPlatform = typeof navigator !== 'undefined' ? navigator.platform : null;

export const useEnvironmentStore = create<EnvironmentState>((set, get) => ({
  isElectron: typeof window !== 'undefined' && !!window.aura,
  platform: defaultPlatform,
  arch: null,
  mode: import.meta.env.MODE,
  version: import.meta.env.VITE_APP_VERSION || null,
  backendUrl: null,
  paths: null,
  hydrate: async () => {
    if (typeof window === 'undefined' || !window.aura?.runtime) {
      return;
    }

    try {
      const diagnostics = await window.aura.runtime.getDiagnostics();
      if (diagnostics) {
        // Safely access nested properties with type guards
        const backend = diagnostics.backend as Record<string, unknown> | undefined;
        const environment = diagnostics.environment as Record<string, unknown> | undefined;
        const os = diagnostics.os as Record<string, unknown> | undefined;
        const paths = diagnostics.paths as Record<string, unknown> | undefined;

        set({
          isElectron: true,
          backendUrl: (backend?.baseUrl as string | undefined) ?? null,
          paths: paths ?? null,
          mode: (environment?.mode as string | undefined) ?? get().mode,
          version: (environment?.version as string | undefined) ?? get().version,
          platform: (os?.platform as string | undefined) ?? defaultPlatform,
          arch: (os?.arch as string | undefined) ?? null,
        });
      }
    } catch (error) {
      console.warn('[environmentStore] Failed to hydrate diagnostics:', error);
    }
  },
}));
