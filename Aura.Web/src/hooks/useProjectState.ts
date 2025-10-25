/**
 * Hook for managing project state with autosave functionality
 */

import { useState, useEffect, useCallback, useRef } from 'react';
import {
  ProjectFile,
  ProjectTrack,
  ProjectClip,
  ProjectMediaItem,
  createEmptyProject,
  timelineClipToProjectClip,
  AutosaveStatus,
} from '../types/project';
import { TimelineClip } from '../pages/VideoEditorPage';
import {
  saveProject,
  saveToLocalStorage,
  loadFromLocalStorage,
  clearLocalStorage,
} from '../services/projectService';

interface UseProjectStateOptions {
  autosaveInterval?: number; // milliseconds, default 120000 (2 minutes)
  enableAutosave?: boolean; // default true
}

interface UseProjectStateReturn {
  projectId: string | null;
  projectName: string | null;
  isDirty: boolean;
  autosaveStatus: AutosaveStatus;
  lastSaved: Date | null;
  saveCurrentProject: (name?: string, showNotification?: boolean) => Promise<void>;
  loadProject: (id: string) => Promise<void>;
  createNewProject: (name: string) => void;
  markDirty: () => void;
  exportProject: () => void;
  importProject: () => Promise<void>;
}

export function useProjectState(
  clips: TimelineClip[],
  tracks: ProjectTrack[],
  mediaLibrary: ProjectMediaItem[],
  currentTime: number,
  onProjectLoaded?: (project: ProjectFile) => void,
  options: UseProjectStateOptions = {}
): UseProjectStateReturn {
  const { autosaveInterval = 120000, enableAutosave = true } = options;

  const [projectId, setProjectId] = useState<string | null>(null);
  const [projectName, setProjectName] = useState<string | null>(null);
  const [isDirty, setIsDirty] = useState(false);
  const [autosaveStatus, setAutosaveStatus] = useState<AutosaveStatus>('idle');
  const [lastSaved, setLastSaved] = useState<Date | null>(null);

  const autosaveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const previousClipsRef = useRef<string>('');

  // Mark project as dirty when clips change
  useEffect(() => {
    const currentClipsStr = JSON.stringify(clips);
    if (previousClipsRef.current !== '' && previousClipsRef.current !== currentClipsStr) {
      setIsDirty(true);
    }
    previousClipsRef.current = currentClipsStr;
  }, [clips]);

  // Mark project as dirty manually
  const markDirty = useCallback(() => {
    setIsDirty(true);
  }, []);

  // Create project file from current state
  const createProjectFile = useCallback(
    (name: string): ProjectFile => {
      const now = new Date().toISOString();
      
      // Calculate total duration
      const duration = clips.reduce((max, clip) => {
        const clipEnd = clip.startTime + clip.duration;
        return Math.max(max, clipEnd);
      }, 0);

      // Convert clips
      const projectClips: ProjectClip[] = clips.map(timelineClipToProjectClip);

      return {
        version: '1.0.0',
        metadata: {
          name,
          createdAt: projectId ? undefined : now,
          lastModifiedAt: now,
          duration,
        },
        settings: {
          resolution: { width: 1920, height: 1080 },
          frameRate: 30,
          sampleRate: 48000,
        },
        tracks,
        clips: projectClips,
        mediaLibrary,
        playerPosition: currentTime,
      } as ProjectFile;
    },
    [clips, tracks, mediaLibrary, currentTime, projectId]
  );

  // Save project
  const saveCurrentProject = useCallback(
    async (name?: string, _showNotification = true) => {
      const projectNameToUse = name || projectName || 'Untitled Project';
      setAutosaveStatus('saving');

      try {
        const projectFile = createProjectFile(projectNameToUse);
        
        // Save to backend
        const response = await saveProject(
          projectNameToUse,
          projectFile,
          projectId || undefined
        );

        setProjectId(response.id);
        setProjectName(projectNameToUse);
        setIsDirty(false);
        setLastSaved(new Date());
        setAutosaveStatus('saved');

        // Also save to local storage for recovery
        saveToLocalStorage(projectFile);

        // Reset status after 3 seconds
        setTimeout(() => {
          setAutosaveStatus('idle');
        }, 3000);
      } catch (error) {
        console.error('Failed to save project:', error);
        setAutosaveStatus('error');
        
        setTimeout(() => {
          setAutosaveStatus('idle');
        }, 5000);
        
        throw error;
      }
    },
    [projectId, projectName, createProjectFile]
  );

  // Load project
  const loadProject = useCallback(
    async (id: string) => {
      try {
        const response = await fetch(`/api/project/${id}`);
        if (!response.ok) {
          throw new Error('Failed to load project');
        }

        const data = await response.json();
        const projectFile = JSON.parse(data.projectData) as ProjectFile;

        setProjectId(data.id);
        setProjectName(projectFile.metadata.name);
        setIsDirty(false);
        setLastSaved(new Date(data.lastModifiedAt));

        if (onProjectLoaded) {
          onProjectLoaded(projectFile);
        }
      } catch (error) {
        console.error('Failed to load project:', error);
        throw error;
      }
    },
    [onProjectLoaded]
  );

  // Create new project
  const createNewProject = useCallback((name: string) => {
    const project = createEmptyProject(name);
    setProjectId(null);
    setProjectName(name);
    setIsDirty(false);
    setLastSaved(null);

    if (onProjectLoaded) {
      onProjectLoaded(project);
    }
  }, [onProjectLoaded]);

  // Export project
  const exportProject = useCallback(() => {
    const name = projectName || 'Untitled Project';
    const projectFile = createProjectFile(name);
    
    const json = JSON.stringify(projectFile, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${name}.aura`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }, [projectName, createProjectFile]);

  // Import project
  const importProject = useCallback(async () => {
    return new Promise<void>((resolve, reject) => {
      const input = document.createElement('input');
      input.type = 'file';
      input.accept = '.aura,application/json';
      
      input.onchange = async (e) => {
        const file = (e.target as HTMLInputElement).files?.[0];
        if (!file) {
          reject(new Error('No file selected'));
          return;
        }

        try {
          const text = await file.text();
          const projectFile = JSON.parse(text) as ProjectFile;
          
          setProjectId(null);
          setProjectName(projectFile.metadata.name);
          setIsDirty(false);
          setLastSaved(null);

          if (onProjectLoaded) {
            onProjectLoaded(projectFile);
          }

          resolve();
        } catch (error) {
          reject(new Error('Failed to parse project file'));
        }
      };

      input.click();
    });
  }, [onProjectLoaded]);

  // Autosave functionality
  useEffect(() => {
    if (!enableAutosave || !isDirty) {
      return;
    }

    // Clear existing timer
    if (autosaveTimerRef.current) {
      clearTimeout(autosaveTimerRef.current);
    }

    // Set new timer
    autosaveTimerRef.current = setTimeout(() => {
      if (isDirty && projectName) {
        saveCurrentProject(projectName, false).catch((error) => {
          console.error('Autosave failed:', error);
        });
      }
    }, autosaveInterval);

    return () => {
      if (autosaveTimerRef.current) {
        clearTimeout(autosaveTimerRef.current);
      }
    };
  }, [isDirty, projectName, autosaveInterval, enableAutosave, saveCurrentProject]);

  // Load autosave on mount if no project is loaded
  useEffect(() => {
    if (!projectId && !projectName) {
      const autosaved = loadFromLocalStorage();
      if (autosaved && onProjectLoaded) {
        // Prompt user to recover
        const shouldRecover = window.confirm(
          'An autosaved project was found. Would you like to recover it?'
        );
        if (shouldRecover) {
          onProjectLoaded(autosaved);
          setProjectName(autosaved.metadata.name);
          setIsDirty(true);
        } else {
          clearLocalStorage();
        }
      }
    }
  }, []); // Run only once on mount

  return {
    projectId,
    projectName,
    isDirty,
    autosaveStatus,
    lastSaved,
    saveCurrentProject,
    loadProject,
    createNewProject,
    markDirty,
    exportProject,
    importProject,
  };
}
