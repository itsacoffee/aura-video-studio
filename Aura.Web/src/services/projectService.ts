/**
 * Service for managing video editor projects
 */

import {
  ProjectFile,
  ProjectListItem,
  LoadProjectResponse,
} from '../types/project';

const API_BASE_URL = '/api/project';

/**
 * Get all projects
 */
export async function getProjects(): Promise<ProjectListItem[]> {
  const response = await fetch(API_BASE_URL);
  if (!response.ok) {
    throw new Error(`Failed to fetch projects: ${response.statusText}`);
  }
  return response.json();
}

/**
 * Get a specific project by ID
 */
export async function getProject(id: string): Promise<LoadProjectResponse> {
  const response = await fetch(`${API_BASE_URL}/${id}`);
  if (!response.ok) {
    throw new Error(`Failed to fetch project: ${response.statusText}`);
  }
  return response.json();
}

/**
 * Save a project (create or update)
 */
export async function saveProject(
  name: string,
  projectFile: ProjectFile,
  id?: string,
  description?: string,
  thumbnail?: string
): Promise<{ id: string; name: string; lastModifiedAt: string }> {
  const body = {
    id,
    name,
    description,
    thumbnail,
    projectData: JSON.stringify(projectFile),
  };

  const response = await fetch(API_BASE_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ detail: response.statusText }));
    throw new Error(error.detail || 'Failed to save project');
  }

  return response.json();
}

/**
 * Delete a project
 */
export async function deleteProject(id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/${id}`, {
    method: 'DELETE',
  });

  if (!response.ok && response.status !== 404) {
    throw new Error(`Failed to delete project: ${response.statusText}`);
  }
}

/**
 * Duplicate a project
 */
export async function duplicateProject(
  id: string
): Promise<{ id: string; name: string; lastModifiedAt: string }> {
  const response = await fetch(`${API_BASE_URL}/${id}/duplicate`, {
    method: 'POST',
  });

  if (!response.ok) {
    throw new Error(`Failed to duplicate project: ${response.statusText}`);
  }

  return response.json();
}

/**
 * Export project as .aura file
 */
export function exportProjectFile(projectFile: ProjectFile, filename: string): void {
  const json = JSON.stringify(projectFile, null, 2);
  const blob = new Blob([json], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `${filename}.aura`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

/**
 * Import project from .aura file
 */
export function importProjectFile(): Promise<ProjectFile> {
  return new Promise((resolve, reject) => {
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
        
        // Validate project file structure
        if (!projectFile.version || !projectFile.metadata || !projectFile.clips) {
          reject(new Error('Invalid project file format'));
          return;
        }

        resolve(projectFile);
      } catch (error) {
        reject(new Error('Failed to parse project file'));
      }
    };

    input.click();
  });
}

/**
 * Local storage key for autosave
 */
const AUTOSAVE_KEY = 'aura-project-autosave';

/**
 * Save project to local storage (autosave)
 */
export function saveToLocalStorage(projectFile: ProjectFile): void {
  try {
    localStorage.setItem(AUTOSAVE_KEY, JSON.stringify(projectFile));
  } catch (error) {
    console.error('Failed to save to local storage:', error);
  }
}

/**
 * Load project from local storage (autosave recovery)
 */
export function loadFromLocalStorage(): ProjectFile | null {
  try {
    const json = localStorage.getItem(AUTOSAVE_KEY);
    if (!json) return null;
    return JSON.parse(json) as ProjectFile;
  } catch (error) {
    console.error('Failed to load from local storage:', error);
    return null;
  }
}

/**
 * Clear autosave from local storage
 */
export function clearLocalStorage(): void {
  try {
    localStorage.removeItem(AUTOSAVE_KEY);
  } catch (error) {
    console.error('Failed to clear local storage:', error);
  }
}
