import type { HardwareCapabilities, RenderJob, Profile, DownloadItem } from '../../types';

/**
 * Test data factory for creating HardwareCapabilities objects
 */
export function createMockHardwareCapabilities(
  overrides?: Partial<HardwareCapabilities>
): HardwareCapabilities {
  return {
    tier: 'medium',
    cpu: {
      cores: 8,
      threads: 16,
    },
    ram: {
      gb: 16,
    },
    gpu: {
      model: 'NVIDIA GeForce RTX 3060',
      vramGB: 8,
      vendor: 'NVIDIA',
    },
    enableNVENC: true,
    enableSD: true,
    offlineOnly: false,
    ...overrides,
  };
}

/**
 * Test data factory for creating RenderJob objects
 */
export function createMockRenderJob(overrides?: Partial<RenderJob>): RenderJob {
  return {
    id: 'job-1',
    status: 'pending',
    progress: 0,
    outputPath: null,
    createdAt: new Date().toISOString(),
    ...overrides,
  };
}

/**
 * Test data factory for creating Profile objects
 */
export function createMockProfile(overrides?: Partial<Profile>): Profile {
  return {
    name: 'Balanced',
    description: 'Balanced profile for most use cases',
    ...overrides,
  };
}

/**
 * Test data factory for creating DownloadItem objects
 */
export function createMockDownloadItem(overrides?: Partial<DownloadItem>): DownloadItem {
  return {
    name: 'ffmpeg',
    version: '6.0',
    url: 'https://example.com/ffmpeg.zip',
    sha256: 'abc123',
    sizeBytes: 100000000,
    installPath: '/opt/ffmpeg',
    required: true,
    ...overrides,
  };
}

/**
 * Test data factory for creating multiple render jobs
 */
export function createMockRenderJobs(
  count: number,
  baseOverrides?: Partial<RenderJob>
): RenderJob[] {
  return Array.from({ length: count }, (_, index) =>
    createMockRenderJob({
      ...baseOverrides,
      id: `job-${index + 1}`,
    })
  );
}
