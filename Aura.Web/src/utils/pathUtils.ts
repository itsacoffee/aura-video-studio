/**
 * Utility functions for handling file paths and folder selection
 * Provides cross-platform default paths and folder picker functionality
 */

/**
 * Get the OS-specific default save location for videos
 * Returns an absolute path based on the operating system
 */
export function getDefaultSaveLocation(): string {
  // Detect platform
  const platform = navigator.platform.toLowerCase();
  const userAgent = navigator.userAgent.toLowerCase();

  // Windows
  if (platform.includes('win') || userAgent.includes('windows')) {
    // Use USERPROFILE\Videos\Aura
    // In browser context, we return the path template that backend will resolve
    return '%USERPROFILE%\\Videos\\Aura';
  }

  // macOS
  if (platform.includes('mac') || userAgent.includes('mac')) {
    return '~/Movies/Aura';
  }

  // Linux and others
  return '~/Videos/Aura';
}

/**
 * Get the OS-specific default cache location
 * Returns an absolute path based on the operating system
 */
export function getDefaultCacheLocation(): string {
  // Detect platform
  const platform = navigator.platform.toLowerCase();
  const userAgent = navigator.userAgent.toLowerCase();

  // Windows
  if (platform.includes('win') || userAgent.includes('windows')) {
    return '%LOCALAPPDATA%\\Aura\\Cache';
  }

  // macOS
  if (platform.includes('mac') || userAgent.includes('mac')) {
    return '~/Library/Caches/Aura';
  }

  // Linux and others
  return '~/.cache/aura';
}

/**
 * Open a folder picker dialog and return the selected path
 * Uses different methods based on the environment (browser, Electron, etc.)
 */
export async function pickFolder(): Promise<string | null> {
  // Try modern File System Access API (Chrome/Edge 86+)
  if ('showDirectoryPicker' in window) {
    try {
      const dirHandle = await (
        window as Window & { showDirectoryPicker?: () => Promise<{ name: string }> }
      ).showDirectoryPicker?.();

      if (!dirHandle) {
        return null;
      }

      // Get the path from the directory handle
      // Note: File System Access API doesn't expose the full path for security reasons
      // We'll return the directory name and let backend handle it
      return dirHandle.name;
    } catch (error: unknown) {
      // User cancelled or permission denied
      if (error instanceof Error && error.name === 'AbortError') {
        return null;
      }
      // Fallthrough to next method
      console.warn('showDirectoryPicker failed:', error);
    }
  }

  // Try Electron IPC if available
  if (window.electron?.selectFolder) {
    try {
      return await window.electron.selectFolder();
    } catch (error: unknown) {
      console.warn('Electron folder picker failed:', error);
    }
  }

  // Fallback: Use file input with webkitdirectory
  return new Promise<string | null>((resolve) => {
    const input = document.createElement('input');
    input.type = 'file';
    input.webkitdirectory = true;
    input.style.display = 'none';

    input.addEventListener('change', () => {
      if (input.files && input.files.length > 0) {
        const firstFile = input.files[0];
        // Extract directory path from file path
        const fullPath =
          (firstFile as File & { path?: string }).path || firstFile.webkitRelativePath;
        if (fullPath) {
          // Get directory path by removing the file name
          const dirPath = fullPath.substring(0, fullPath.lastIndexOf('/'));
          resolve(dirPath || null);
        } else {
          resolve(null);
        }
      } else {
        resolve(null);
      }
      document.body.removeChild(input);
    });

    input.addEventListener('cancel', () => {
      resolve(null);
      document.body.removeChild(input);
    });

    document.body.appendChild(input);
    input.click();
  });
}

/**
 * Validate if a path string is likely valid (basic check)
 */
export function isValidPath(path: string): boolean {
  if (!path || path.trim().length === 0) {
    return false;
  }

  // Check for invalid characters (basic validation)
  const invalidChars = /[<>"|?*]/;
  if (invalidChars.test(path)) {
    return false;
  }

  // Check if path contains placeholder text
  const placeholderPatterns = ['yourname', 'username', 'user name', '<user>', '{user}'];

  const lowerPath = path.toLowerCase();
  for (const pattern of placeholderPatterns) {
    if (lowerPath.includes(pattern)) {
      return false;
    }
  }

  return true;
}

/**
 * Migrate legacy placeholder paths to correct defaults
 */
export function migrateLegacyPath(path: string): string {
  if (!isValidPath(path)) {
    return getDefaultSaveLocation();
  }

  // Check if path contains placeholder username patterns
  const placeholderPatterns = [
    /\\YourName\\/gi,
    /\/YourName\//gi,
    /\\username\\/gi,
    /\/username\//gi,
  ];

  const migratedPath = path;
  let needsMigration = false;

  for (const pattern of placeholderPatterns) {
    if (pattern.test(migratedPath)) {
      needsMigration = true;
      break;
    }
  }

  if (needsMigration) {
    return getDefaultSaveLocation();
  }

  return path;
}

/**
 * Request backend to resolve environment variables in path
 * e.g., %USERPROFILE% or ~ expansion
 */
export async function resolvePathOnBackend(path: string): Promise<string> {
  try {
    const response = await fetch('/api/paths/resolve', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ path }),
    });

    if (!response.ok) {
      throw new Error(`Failed to resolve path: ${response.statusText}`);
    }

    const data = await response.json();
    return data.resolvedPath || path;
  } catch (error: unknown) {
    console.error('Failed to resolve path on backend:', error);
    return path;
  }
}

/**
 * Validate that a path is writable by checking with the backend
 */
export async function validatePathWritable(
  path: string
): Promise<{ valid: boolean; error?: string }> {
  try {
    const response = await fetch('/api/paths/validate', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ path }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return {
        valid: false,
        error: errorData.error || 'Path validation failed',
      };
    }

    const data = await response.json();
    return {
      valid: data.valid === true,
      error: data.error,
    };
  } catch (error: unknown) {
    return {
      valid: false,
      error: error instanceof Error ? error.message : 'Failed to validate path',
    };
  }
}
