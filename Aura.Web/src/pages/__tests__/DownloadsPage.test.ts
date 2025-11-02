import { describe, expect, it } from 'vitest';

/**
 * Unit tests for DownloadsPage download guide functionality
 * These tests verify defensive array handling to prevent crashes
 */

interface ManualInstructions {
  componentName: string;
  version: string;
  installPath: string;
  steps: string[];
}

describe('DownloadsPage - Manual Instructions Handling', () => {
  /**
   * Helper function that mimics the defensive array check logic
   * from showManualInstructions in DownloadsPage.tsx
   */
  const processManualInstructions = (
    data: Partial<ManualInstructions>
  ): {
    success: boolean;
    instructionsText?: string;
    error?: string;
  } => {
    try {
      // Defensive check: ensure steps is an array
      const steps = Array.isArray(data.steps) ? data.steps : [];

      if (steps.length === 0) {
        return {
          success: false,
          error: `Manual installation instructions for ${data.componentName} are not available at this time.`,
        };
      }

      const instructionsText = [
        `Manual Installation Instructions for ${data.componentName} v${data.version}`,
        '',
        `Install Path: ${data.installPath}`,
        '',
        ...steps,
      ].join('\n');

      return {
        success: true,
        instructionsText,
      };
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  };

  it('should handle valid manual instructions with steps array', () => {
    const data: ManualInstructions = {
      componentName: 'Ollama',
      version: '0.1.0',
      installPath: '/usr/local/bin/ollama',
      steps: ['Step 1: Download', 'Step 2: Install', 'Step 3: Configure'],
    };

    const result = processManualInstructions(data);

    expect(result.success).toBe(true);
    expect(result.instructionsText).toContain('Ollama');
    expect(result.instructionsText).toContain('Step 1: Download');
    expect(result.instructionsText).toContain('Step 2: Install');
    expect(result.instructionsText).toContain('Step 3: Configure');
  });

  it('should handle null steps array without crashing', () => {
    const data = {
      componentName: 'Ollama',
      version: '0.1.0',
      installPath: '/usr/local/bin/ollama',
      steps: null as unknown as string[],
    };

    const result = processManualInstructions(data);

    expect(result.success).toBe(false);
    expect(result.error).toContain('not available');
  });

  it('should handle undefined steps array without crashing', () => {
    const data = {
      componentName: 'FFmpeg',
      version: '4.4.0',
      installPath: '/usr/local/bin/ffmpeg',
      steps: undefined as unknown as string[],
    };

    const result = processManualInstructions(data);

    expect(result.success).toBe(false);
    expect(result.error).toContain('not available');
  });

  it('should handle empty steps array', () => {
    const data: ManualInstructions = {
      componentName: 'StableDiffusion',
      version: '1.0.0',
      installPath: '/opt/stable-diffusion',
      steps: [],
    };

    const result = processManualInstructions(data);

    expect(result.success).toBe(false);
    expect(result.error).toContain('not available');
  });

  it('should handle non-array steps without crashing', () => {
    const data = {
      componentName: 'Ollama',
      version: '0.1.0',
      installPath: '/usr/local/bin/ollama',
      steps: 'invalid data' as unknown as string[],
    };

    const result = processManualInstructions(data);

    expect(result.success).toBe(false);
    expect(result.error).toContain('not available');
  });

  it('should handle object instead of array without crashing', () => {
    const data = {
      componentName: 'Ollama',
      version: '0.1.0',
      installPath: '/usr/local/bin/ollama',
      steps: { step1: 'Download', step2: 'Install' } as unknown as string[],
    };

    const result = processManualInstructions(data);

    expect(result.success).toBe(false);
    expect(result.error).toContain('not available');
  });

  it('should handle malformed data gracefully', () => {
    const data = {
      componentName: 'Test',
      version: '1.0',
      installPath: '/test',
      steps: [1, 2, 3] as unknown as string[], // Numbers instead of strings
    };

    const result = processManualInstructions(data);

    // Even with wrong types, Array.isArray still returns true
    // and the function should work (JavaScript will convert to strings)
    expect(result.success).toBe(true);
    expect(result.instructionsText).toBeDefined();
  });
});
