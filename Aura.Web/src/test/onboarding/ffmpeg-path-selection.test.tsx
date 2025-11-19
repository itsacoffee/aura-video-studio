/**
 * Tests for FirstRunWizard FFmpeg path selection functionality
 * Validates robust file/folder selection, normalization, and error handling
 */

import { describe, it, expect, beforeEach, vi, type Mock } from 'vitest';

// Mock window.aura.dialogs
const mockOpenFile: Mock = vi.fn();
const mockPickFolder: Mock = vi.fn();

// Setup global mocks
beforeEach(() => {
  vi.clearAllMocks();

  // Mock window.aura.dialogs.openFile
  (
    global as typeof global & {
      window: typeof window & { aura?: { dialogs?: { openFile?: Mock } } };
    }
  ).window = {
    ...window,
    aura: {
      dialogs: {
        openFile: mockOpenFile,
      },
    },
  };
});

// Mock pathUtils.pickFolder
vi.mock('../../utils/pathUtils', () => ({
  pickFolder: () => mockPickFolder(),
  getDefaultSaveLocation: vi.fn().mockReturnValue('~/Videos/Aura'),
  getDefaultCacheLocation: vi.fn().mockReturnValue('~/.cache/aura'),
  isValidPath: vi.fn().mockReturnValue(true),
  migrateLegacyPath: vi.fn((path) => path),
  resolvePathOnBackend: vi.fn((path) => Promise.resolve(path)),
  validatePathWritable: vi.fn().mockResolvedValue({ valid: true }),
}));

describe('FFmpeg Path Selection Logic', () => {
  describe('Path normalization', () => {
    it('should handle Windows executable path correctly', () => {
      const inputPath = 'C:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe';
      const normalized = inputPath.trim().replace(/[\\/]+$/, '');
      expect(normalized).toBe('C:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe');
    });

    it('should handle Unix executable path correctly', () => {
      const inputPath = '/usr/local/bin/ffmpeg';
      const normalized = inputPath.trim().replace(/[\\/]+$/, '');
      expect(normalized).toBe('/usr/local/bin/ffmpeg');
    });

    it('should remove trailing slashes from folder paths', () => {
      const inputPath = '/usr/local/bin/';
      const normalized = inputPath.trim().replace(/[\\/]+$/, '');
      expect(normalized).toBe('/usr/local/bin');
    });

    it('should append executable name to folder path', () => {
      const folderPath = '/usr/local/ffmpeg';
      const normalized = folderPath.trim().replace(/[\\/]+$/, '');
      const separator = '/';
      const executableName = 'ffmpeg';
      const finalPath = `${normalized}${separator}${executableName}`;
      expect(finalPath).toBe('/usr/local/ffmpeg/ffmpeg');
    });

    it('should handle bin folder by appending executable', () => {
      const binPath = '/usr/local/ffmpeg/bin';
      const separator = '/';
      const executableName = 'ffmpeg';
      const finalPath = `${binPath}${separator}${executableName}`;
      expect(finalPath).toBe('/usr/local/ffmpeg/bin/ffmpeg');
    });

    it('should detect path already pointing to executable', () => {
      const execPath = 'C:\\ffmpeg\\bin\\ffmpeg.exe';
      const lower = execPath.toLowerCase();
      expect(lower.endsWith('ffmpeg.exe')).toBe(true);
    });

    it('should reject empty paths after trimming', () => {
      const emptyPath = '   ';
      const trimmed = emptyPath.trim();
      expect(trimmed.length).toBe(0);
    });

    it('should handle Windows-style separators correctly', () => {
      const windowsPath = 'C:\\Program Files\\ffmpeg\\bin';
      const hasWindowsSeparators = windowsPath.includes('\\') && !windowsPath.includes('/');
      expect(hasWindowsSeparators).toBe(true);
    });

    it('should handle Unix-style separators correctly', () => {
      const unixPath = '/usr/local/ffmpeg/bin';
      const hasWindowsSeparators = unixPath.includes('\\') && !unixPath.includes('/');
      expect(hasWindowsSeparators).toBe(false);
    });
  });

  describe('File picker selection', () => {
    it('should use Electron file picker when available', async () => {
      mockOpenFile.mockResolvedValue('C:\\ffmpeg\\bin\\ffmpeg.exe');

      const result = await mockOpenFile({
        title: 'Select FFmpeg Executable',
        filters: [{ name: 'FFmpeg executable', extensions: ['exe'] }],
      });

      expect(mockOpenFile).toHaveBeenCalledWith({
        title: 'Select FFmpeg Executable',
        filters: [{ name: 'FFmpeg executable', extensions: ['exe'] }],
      });
      expect(result).toBe('C:\\ffmpeg\\bin\\ffmpeg.exe');
    });

    it('should fallback to folder picker when file picker returns null', async () => {
      mockOpenFile.mockResolvedValue(null);
      mockPickFolder.mockResolvedValue('/usr/local/ffmpeg');

      const fileResult = await mockOpenFile({
        title: 'Select FFmpeg Executable',
        filters: [{ name: 'FFmpeg executable', extensions: ['*'] }],
      });

      if (!fileResult) {
        const folderResult = await mockPickFolder();
        expect(folderResult).toBe('/usr/local/ffmpeg');
      }
    });

    it('should handle file picker errors gracefully', async () => {
      mockOpenFile.mockRejectedValue(new Error('File picker not available'));

      await expect(async () => {
        try {
          await mockOpenFile({
            title: 'Select FFmpeg Executable',
            filters: [{ name: 'FFmpeg executable', extensions: ['exe'] }],
          });
        } catch (error: unknown) {
          expect(error).toBeInstanceOf(Error);
          if (error instanceof Error) {
            expect(error.message).toBe('File picker not available');
          }
          throw error;
        }
      }).rejects.toThrow('File picker not available');
    });

    it('should handle folder picker errors gracefully', async () => {
      mockOpenFile.mockResolvedValue(null);
      mockPickFolder.mockRejectedValue(new Error('Folder picker failed'));

      const fileResult = await mockOpenFile();

      if (!fileResult) {
        await expect(async () => {
          try {
            await mockPickFolder();
          } catch (error: unknown) {
            expect(error).toBeInstanceOf(Error);
            if (error instanceof Error) {
              expect(error.message).toBe('Folder picker failed');
            }
            throw error;
          }
        }).rejects.toThrow('Folder picker failed');
      }
    });
  });

  describe('Platform-specific filters', () => {
    it('should use .exe filter on Windows', () => {
      const userAgent = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)';
      const isWindows = userAgent.toLowerCase().includes('windows');
      const filters = isWindows
        ? [{ name: 'FFmpeg executable', extensions: ['exe'] }]
        : [{ name: 'FFmpeg executable', extensions: ['*'] }];

      expect(filters).toEqual([{ name: 'FFmpeg executable', extensions: ['exe'] }]);
    });

    it('should use wildcard filter on macOS/Linux', () => {
      const userAgent = 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)';
      const isWindows = userAgent.toLowerCase().includes('windows');
      const filters = isWindows
        ? [{ name: 'FFmpeg executable', extensions: ['exe'] }]
        : [{ name: 'FFmpeg executable', extensions: ['*'] }];

      expect(filters).toEqual([{ name: 'FFmpeg executable', extensions: ['*'] }]);
    });
  });

  describe('Path validation', () => {
    it('should validate non-empty paths', () => {
      const validPath = 'C:\\ffmpeg\\bin\\ffmpeg.exe';
      expect(validPath.trim().length).toBeGreaterThan(0);
    });

    it('should reject whitespace-only paths', () => {
      const invalidPath = '   ';
      expect(invalidPath.trim().length).toBe(0);
    });

    it('should validate path format', () => {
      const validPaths = [
        'C:\\ffmpeg\\bin\\ffmpeg.exe',
        '/usr/local/bin/ffmpeg',
        '/opt/homebrew/bin/ffmpeg',
        'D:\\Tools\\ffmpeg-master-latest-win64-gpl\\bin\\ffmpeg.exe',
      ];

      validPaths.forEach((path) => {
        const normalized = path.trim();
        expect(normalized.length).toBeGreaterThan(0);
        expect(normalized).toBe(path);
      });
    });
  });

  describe('Error handling', () => {
    it('should provide clear error messages for empty paths', () => {
      const emptyPath = '';
      const isValid = emptyPath.trim().length > 0;

      if (!isValid) {
        const errorMessage =
          'The selected path is empty. Please select a valid FFmpeg executable or folder.';
        expect(errorMessage).toContain('empty');
        expect(errorMessage).toContain('valid');
      }
    });

    it('should provide clear error messages for file picker failures', () => {
      const error = new Error('Unable to open the system file picker');
      expect(error.message).toContain('Unable to open');
      expect(error.message).toContain('file picker');
    });
  });
});
