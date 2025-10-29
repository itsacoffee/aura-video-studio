/**
 * Hardware detection service for checking video encoding acceleration capabilities
 */

import { get } from './api/apiClient';
import { loggingService as logger } from './loggingService';

export interface HardwareInfo {
  cpuCores: number;
  ramGB: number;
  gpu: GpuInfo | null;
  hardwareAccelerationAvailable: boolean;
  hardwareType: 'NVIDIA' | 'AMD' | 'Intel' | 'None';
  encoderType: string;
}

export interface GpuInfo {
  vendor: string;
  model: string;
  vramGB: number;
}

/**
 * Get hardware capabilities from the API
 */
export async function getHardwareInfo(): Promise<HardwareInfo> {
  try {
    const response = await get<Record<string, unknown>>('/api/diagnostics/hardware');

    // Map the API response to our HardwareInfo interface
    const gpu = response.gpu as { vendor?: string; model?: string; vramGB?: number } | undefined;
    const hasNvidia = gpu?.vendor?.toUpperCase().includes('NVIDIA') || false;
    const hasAmd = gpu?.vendor?.toUpperCase().includes('AMD') || false;
    const hasIntel = gpu?.vendor?.toUpperCase().includes('INTEL') || false;

    let hardwareType: 'NVIDIA' | 'AMD' | 'Intel' | 'None' = 'None';
    let encoderType = 'Software (CPU)';

    if (hasNvidia) {
      hardwareType = 'NVIDIA';
      encoderType = 'NVIDIA NVENC';
    } else if (hasAmd) {
      hardwareType = 'AMD';
      encoderType = 'AMD AMF';
    } else if (hasIntel) {
      hardwareType = 'Intel';
      encoderType = 'Intel Quick Sync';
    }

    return {
      cpuCores: (response.logicalCores as number | undefined) ?? 0,
      ramGB: (response.ramGB as number | undefined) ?? 0,
      gpu: gpu
        ? {
            vendor: gpu.vendor || 'Unknown',
            model: gpu.model || 'Unknown',
            vramGB: gpu.vramGB || 0,
          }
        : null,
      hardwareAccelerationAvailable: hasNvidia || hasAmd || hasIntel,
      hardwareType,
      encoderType,
    };
  } catch (error) {
    logger.warn(
      'Failed to detect hardware, using software fallback',
      'hardwareService',
      'detectHardware',
      { error: String(error) }
    );
    // Return safe defaults if detection fails
    return {
      cpuCores: 4,
      ramGB: 8,
      gpu: null,
      hardwareAccelerationAvailable: false,
      hardwareType: 'None',
      encoderType: 'Software (CPU)',
    };
  }
}
