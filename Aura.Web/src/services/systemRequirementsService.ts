/**
 * Service for checking system requirements during first-run setup
 *
 * This service validates that the user's system meets the minimum requirements
 * for running Aura Video Studio, including:
 * - Disk space availability
 * - GPU detection and capabilities
 * - Memory availability
 * - OS compatibility
 */

import { apiUrl } from '../config/api';

export interface SystemRequirements {
  diskSpace: DiskSpaceInfo;
  gpu: GPUInfo;
  memory: MemoryInfo;
  os: OSInfo;
  overall: RequirementStatus;
}

export interface DiskSpaceInfo {
  available: number; // in GB
  total: number; // in GB
  percentage: number;
  status: RequirementStatus;
  warnings: string[];
}

export interface GPUInfo {
  detected: boolean;
  vendor?: string;
  model?: string;
  memory?: number; // in MB
  capabilities: GPUCapabilities;
  status: RequirementStatus;
  recommendations: string[];
}

export interface GPUCapabilities {
  hardwareAcceleration: boolean;
  videoEncoding: boolean;
  videoDecoding: boolean;
}

export interface MemoryInfo {
  total: number; // in GB
  available: number; // in GB
  percentage: number;
  status: RequirementStatus;
  warnings: string[];
}

export interface OSInfo {
  platform: string;
  version: string;
  architecture: string;
  compatible: boolean;
}

export type RequirementStatus = 'pass' | 'warning' | 'fail';

/**
 * Check all system requirements
 */
export async function checkSystemRequirements(): Promise<SystemRequirements> {
  const [diskSpace, gpu, memory, os] = await Promise.all([
    checkDiskSpace(),
    checkGPU(),
    checkMemory(),
    checkOS(),
  ]);

  // Determine overall status
  let overall: RequirementStatus = 'pass';
  if (
    diskSpace.status === 'fail' ||
    gpu.status === 'fail' ||
    memory.status === 'fail' ||
    !os.compatible
  ) {
    overall = 'fail';
  } else if (
    diskSpace.status === 'warning' ||
    gpu.status === 'warning' ||
    memory.status === 'warning'
  ) {
    overall = 'warning';
  }

  return {
    diskSpace,
    gpu,
    memory,
    os,
    overall,
  };
}

/**
 * Check available disk space
 */
async function checkDiskSpace(): Promise<DiskSpaceInfo> {
  try {
    // Try to get disk space from backend API
    const response = await fetch(apiUrl('/api/system/disk-space'));
    if (response.ok) {
      const data = await response.json();
      return analyzeDiskSpace(data.availableGB, data.totalGB);
    }
  } catch (error) {
    console.warn('Failed to check disk space from backend:', error);
  }

  // Fallback: estimate based on browser storage API
  if ('storage' in navigator && 'estimate' in navigator.storage) {
    try {
      const estimate = await navigator.storage.estimate();
      const usedGB = (estimate.usage || 0) / (1024 * 1024 * 1024);
      const quotaGB = (estimate.quota || 0) / (1024 * 1024 * 1024);
      const availableGB = quotaGB - usedGB;
      return analyzeDiskSpace(availableGB, quotaGB);
    } catch (error) {
      console.warn('Failed to estimate storage:', error);
    }
  }

  // Cannot determine, return unknown
  return {
    available: 0,
    total: 0,
    percentage: 0,
    status: 'warning',
    warnings: ['Could not determine available disk space'],
  };
}

function analyzeDiskSpace(availableGB: number, totalGB: number): DiskSpaceInfo {
  const percentage = totalGB > 0 ? (availableGB / totalGB) * 100 : 0;
  const warnings: string[] = [];
  let status: RequirementStatus = 'pass';

  // Minimum 10GB required for basic operation
  // Recommended 50GB for comfortable usage
  if (availableGB < 10) {
    status = 'fail';
    warnings.push('Less than 10GB available. Video generation requires significant disk space.');
  } else if (availableGB < 50) {
    status = 'warning';
    warnings.push('Less than 50GB available. Consider freeing up disk space for larger projects.');
  }

  return {
    available: availableGB,
    total: totalGB,
    percentage,
    status,
    warnings,
  };
}

/**
 * Check GPU availability and capabilities
 */
async function checkGPU(): Promise<GPUInfo> {
  const recommendations: string[] = [];
  let status: RequirementStatus = 'pass';

  try {
    // Try to get GPU info from backend API
    const response = await fetch(apiUrl('/api/system/gpu'));
    if (response.ok) {
      const data = await response.json();

      if (!data.detected) {
        status = 'warning';
        recommendations.push('No dedicated GPU detected. Video encoding will use CPU (slower).');
        recommendations.push('Consider using a system with a dedicated GPU for faster rendering.');
      } else if (
        data.vendor.toLowerCase().includes('intel') &&
        data.model.toLowerCase().includes('uhd')
      ) {
        status = 'warning';
        recommendations.push(
          'Integrated GPU detected. Performance may be limited for complex projects.'
        );
      }

      // Check for NVIDIA GPU (best for video encoding)
      if (data.vendor.toLowerCase().includes('nvidia')) {
        recommendations.push(
          'NVIDIA GPU detected. Enable NVENC hardware acceleration in Settings for optimal performance.'
        );
      }

      return {
        detected: data.detected,
        vendor: data.vendor,
        model: data.model,
        memory: data.memoryMB,
        capabilities: {
          hardwareAcceleration: data.hardwareAcceleration || false,
          videoEncoding: data.videoEncoding || false,
          videoDecoding: data.videoDecoding || false,
        },
        status,
        recommendations,
      };
    }
  } catch (error) {
    console.warn('Failed to check GPU from backend:', error);
  }

  // Fallback: Try WebGL to detect GPU
  const canvas = document.createElement('canvas');
  const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');

  if (gl && gl instanceof WebGLRenderingContext) {
    const debugInfo = gl.getExtension('WEBGL_debug_renderer_info');
    if (debugInfo) {
      const vendor = gl.getParameter(debugInfo.UNMASKED_VENDOR_WEBGL);
      const renderer = gl.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL);

      return {
        detected: true,
        vendor: vendor,
        model: renderer,
        capabilities: {
          hardwareAcceleration: true,
          videoEncoding: false, // Unknown from WebGL
          videoDecoding: false,
        },
        status: 'warning',
        recommendations: ['GPU detected via WebGL. Check backend for detailed capabilities.'],
      };
    }
  }

  // No GPU detected
  return {
    detected: false,
    capabilities: {
      hardwareAcceleration: false,
      videoEncoding: false,
      videoDecoding: false,
    },
    status: 'warning',
    recommendations: [
      'No GPU detected. Video rendering will be slower without hardware acceleration.',
      'Consider running on a system with a dedicated GPU for best performance.',
    ],
  };
}

/**
 * Check system memory
 */
async function checkMemory(): Promise<MemoryInfo> {
  try {
    // Try to get memory info from backend API
    const response = await fetch(apiUrl('/api/system/memory'));
    if (response.ok) {
      const data = await response.json();
      return analyzeMemory(data.totalGB, data.availableGB);
    }
  } catch (error) {
    console.warn('Failed to check memory from backend:', error);
  }

  // Fallback: Use browser performance API
  if ('deviceMemory' in navigator) {
    const deviceMemory = (navigator as any).deviceMemory as number;
    // Estimate available as 70% of total
    return analyzeMemory(deviceMemory, deviceMemory * 0.7);
  }

  // Cannot determine
  return {
    total: 0,
    available: 0,
    percentage: 0,
    status: 'warning',
    warnings: ['Could not determine system memory'],
  };
}

function analyzeMemory(totalGB: number, availableGB: number): MemoryInfo {
  const percentage = totalGB > 0 ? (availableGB / totalGB) * 100 : 0;
  const warnings: string[] = [];
  let status: RequirementStatus = 'pass';

  // Minimum 4GB required
  // Recommended 8GB+
  if (totalGB < 4) {
    status = 'fail';
    warnings.push('Less than 4GB RAM detected. Minimum 4GB required for video generation.');
  } else if (totalGB < 8) {
    status = 'warning';
    warnings.push('Less than 8GB RAM detected. 8GB+ recommended for smooth performance.');
  }

  return {
    total: totalGB,
    available: availableGB,
    percentage,
    status,
    warnings,
  };
}

/**
 * Check operating system compatibility
 */
async function checkOS(): Promise<OSInfo> {
  const platform = navigator.platform;
  const userAgent = navigator.userAgent;

  let osName = 'Unknown';
  let version = 'Unknown';
  let compatible = true;

  // Detect OS
  if (platform.includes('Win')) {
    osName = 'Windows';
    // Try to extract version from user agent
    const match = userAgent.match(/Windows NT ([\d.]+)/);
    if (match) {
      const ntVersion = parseFloat(match[1]);
      if (ntVersion >= 10.0) {
        version = '10/11';
      } else if (ntVersion >= 6.3) {
        version = '8.1';
      } else if (ntVersion >= 6.2) {
        version = '8';
      } else if (ntVersion >= 6.1) {
        version = '7';
        compatible = false; // Windows 7 is not supported
      }
    }
  } else if (platform.includes('Mac')) {
    osName = 'macOS';
    const match = userAgent.match(/Mac OS X ([\d_]+)/);
    if (match) {
      version = match[1].replace(/_/g, '.');
    }
  } else if (platform.includes('Linux')) {
    osName = 'Linux';
    version = 'Unknown';
  }

  const architecture =
    navigator.userAgent.includes('x64') || navigator.userAgent.includes('WOW64') ? 'x64' : 'x86';

  return {
    platform: osName,
    version,
    architecture,
    compatible,
  };
}

/**
 * Get recommendations based on system requirements
 */
export function getSystemRecommendations(requirements: SystemRequirements): string[] {
  const recommendations: string[] = [];

  // Disk space recommendations
  if (requirements.diskSpace.status === 'warning') {
    recommendations.push('Free up disk space before starting large video projects');
  } else if (requirements.diskSpace.status === 'fail') {
    recommendations.push('⚠️ Critical: Free up at least 10GB of disk space before proceeding');
  }

  // GPU recommendations
  recommendations.push(...requirements.gpu.recommendations);

  // Memory recommendations
  if (requirements.memory.status === 'warning') {
    recommendations.push('Close other applications to free up memory during video rendering');
  } else if (requirements.memory.status === 'fail') {
    recommendations.push('⚠️ Critical: System does not meet minimum memory requirements');
  }

  // OS recommendations
  if (!requirements.os.compatible) {
    recommendations.push(
      '⚠️ Your operating system is not officially supported. Some features may not work correctly.'
    );
  }

  return recommendations;
}
