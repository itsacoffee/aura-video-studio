import { useEffect, useRef, useCallback } from 'react';
import { useMutation } from '@tanstack/react-query';
import { projectManagementApi } from '../api/projectManagement';

interface AutoSaveOptions {
  projectId: string;
  interval?: number; // Auto-save interval in milliseconds (default: 30000ms = 30s)
  enabled?: boolean;
}

interface AutoSaveData {
  briefJson?: string;
  planSpecJson?: string;
  voiceSpecJson?: string;
  renderSpecJson?: string;
}

/**
 * Hook for automatic project data saving
 * Debounces save requests and provides manual save functionality
 */
export function useProjectAutoSave({
  projectId,
  interval = 30000,
  enabled = true,
}: AutoSaveOptions) {
  const dataRef = useRef<AutoSaveData>({});
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const lastSaveRef = useRef<Date | null>(null);

  const autoSaveMutation = useMutation({
    mutationFn: (data: AutoSaveData) =>
      projectManagementApi.autoSaveProject(projectId, data),
    onSuccess: () => {
      lastSaveRef.current = new Date();
    },
  });

  // Clear timeout on unmount
  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  // Update data to be saved
  const updateData = useCallback((newData: Partial<AutoSaveData>) => {
    dataRef.current = { ...dataRef.current, ...newData };
  }, []);

  // Schedule auto-save
  const scheduleAutoSave = useCallback(() => {
    if (!enabled) return;

    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }

    timeoutRef.current = setTimeout(() => {
      if (Object.keys(dataRef.current).length > 0) {
        autoSaveMutation.mutate(dataRef.current);
      }
    }, interval);
  }, [enabled, interval, autoSaveMutation]);

  // Manual save
  const saveNow = useCallback(async () => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }

    if (Object.keys(dataRef.current).length > 0) {
      await autoSaveMutation.mutateAsync(dataRef.current);
    }
  }, [autoSaveMutation]);

  return {
    updateData,
    scheduleAutoSave,
    saveNow,
    isSaving: autoSaveMutation.isPending,
    lastSaved: lastSaveRef.current,
    error: autoSaveMutation.error,
  };
}
