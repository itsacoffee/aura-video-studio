/**
 * Service for Ollama detection, installation guidance, and setup
 *
 * This service helps users set up Ollama as a local LLM provider during
 * the first-run wizard.
 *
 * PR 1: Enhanced with proper error classification instead of generic failures
 */

import { classifyError, type ClassifiedError } from '../utils/errorClassification';
import { ollamaClient } from './api/ollamaClient';

export interface OllamaSetupStatus {
  installed: boolean;
  running: boolean;
  version?: string;
  modelsInstalled: string[];
  recommendedModels: OllamaModelRecommendation[];
  installationPath?: string;
  error?: ClassifiedError;
}

export interface OllamaModelRecommendation {
  name: string;
  displayName: string;
  size: string;
  description: string;
  recommended: boolean;
  sizeBytes: number;
}

export interface OllamaInstallGuide {
  platform: string;
  steps: string[];
  downloadUrl: string;
  estimatedTime: string;
}

/**
 * Check if Ollama is installed and running
 * PR 1: Now returns detailed error information instead of silently failing
 */
export async function checkOllamaStatus(): Promise<OllamaSetupStatus> {
  try {
    const status = await ollamaClient.getStatus();

    // If we get a response, Ollama is installed
    const modelsResponse = await ollamaClient.getModels().catch(() => ({ models: [] }));
    const installedModels = modelsResponse.models.map((m) => m.name);

    return {
      installed: status.installed || false,
      running: status.running || false,
      version: status.version,
      modelsInstalled: installedModels,
      recommendedModels: getRecommendedModels(),
      installationPath: status.installPath,
    };
  } catch (error: unknown) {
    // Classify the error to provide specific feedback
    const classified = classifyError(error);
    
    console.warn('[Ollama Setup] Status check failed:', {
      category: classified.category,
      title: classified.title,
      message: classified.message,
    });

    // Ollama is not installed or not running, but include error details
    return {
      installed: false,
      running: false,
      modelsInstalled: [],
      recommendedModels: getRecommendedModels(),
      error: classified,
    };
  }
}

/**
 * Get recommended Ollama models for video script generation
 */
export function getRecommendedModels(): OllamaModelRecommendation[] {
  return [
    {
      name: 'llama3.2:3b',
      displayName: 'Llama 3.2 (3B)',
      size: '2.0 GB',
      sizeBytes: 2 * 1024 * 1024 * 1024,
      description: 'Fast and efficient. Best for systems with limited resources.',
      recommended: true,
    },
    {
      name: 'llama3.1:8b',
      displayName: 'Llama 3.1 (8B)',
      size: '4.7 GB',
      sizeBytes: 4.7 * 1024 * 1024 * 1024,
      description: 'Balanced performance and quality. Recommended for most users.',
      recommended: true,
    },
    {
      name: 'llama3.1:8b-q4_k_m',
      displayName: 'Llama 3.1 8B (Quantized)',
      size: '4.9 GB',
      sizeBytes: 4.9 * 1024 * 1024 * 1024,
      description: 'Optimized version with better quality. Best balance of speed and quality.',
      recommended: true,
    },
    {
      name: 'mistral:7b',
      displayName: 'Mistral (7B)',
      size: '4.1 GB',
      sizeBytes: 4.1 * 1024 * 1024 * 1024,
      description: 'Excellent for creative writing and script generation.',
      recommended: true,
    },
    {
      name: 'llama3.1:70b',
      displayName: 'Llama 3.1 (70B)',
      size: '40 GB',
      sizeBytes: 40 * 1024 * 1024 * 1024,
      description: 'Highest quality, requires powerful hardware (16GB+ RAM).',
      recommended: false,
    },
  ];
}

/**
 * Get installation guide for the current platform
 */
export function getInstallGuide(): OllamaInstallGuide {
  const platform = navigator.platform.toLowerCase();

  if (platform.includes('win')) {
    return {
      platform: 'Windows',
      downloadUrl: 'https://ollama.com/download/windows',
      estimatedTime: '5-10 minutes',
      steps: [
        'Download the Ollama installer from ollama.com',
        'Run the installer (OllamaSetup.exe)',
        'Follow the installation wizard',
        'Ollama will start automatically after installation',
        'Return to this setup wizard to continue',
      ],
    };
  } else if (platform.includes('mac')) {
    return {
      platform: 'macOS',
      downloadUrl: 'https://ollama.com/download/mac',
      estimatedTime: '5-10 minutes',
      steps: [
        'Download Ollama for macOS from ollama.com',
        'Open the downloaded .dmg file',
        'Drag Ollama to your Applications folder',
        'Launch Ollama from Applications',
        'Return to this setup wizard to continue',
      ],
    };
  } else {
    return {
      platform: 'Linux',
      downloadUrl: 'https://ollama.com/download/linux',
      estimatedTime: '2-5 minutes',
      steps: [
        'Open a terminal',
        'Run: curl -fsSL https://ollama.com/install.sh | sh',
        'Wait for installation to complete',
        'Ollama will start automatically',
        'Return to this setup wizard to continue',
      ],
    };
  }
}

/**
 * Start Ollama server
 * PR 1: Enhanced with proper error classification
 */
export async function startOllama(): Promise<{
  success: boolean;
  message: string;
  error?: ClassifiedError;
}> {
  try {
    const result = await ollamaClient.start();
    return {
      success: result.success || false,
      message: result.message || 'Ollama started successfully',
    };
  } catch (error: unknown) {
    const classified = classifyError(error);
    return {
      success: false,
      message: classified.message,
      error: classified,
    };
  }
}

/**
 * Pull a model from Ollama registry
 * PR 1: Enhanced with proper error classification
 */
export async function pullModel(modelName: string): Promise<{
  success: boolean;
  message: string;
  error?: ClassifiedError;
}> {
  try {
    const response = await fetch('/api/ollama/pull', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ model: modelName }),
    });

    if (!response.ok) {
      const errorText = await response.text().catch(() => response.statusText);
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }

    await response.json();
    return {
      success: true,
      message: `Model ${modelName} pulled successfully`,
    };
  } catch (error: unknown) {
    const classified = classifyError(error);
    return {
      success: false,
      message: classified.message,
      error: classified,
    };
  }
}

/**
 * Get estimated download time for a model
 */
export function getEstimatedDownloadTime(sizeBytes: number, speedMbps = 50): string {
  // Convert to megabits
  const sizeMb = (sizeBytes * 8) / (1024 * 1024);
  const timeSeconds = sizeMb / speedMbps;

  if (timeSeconds < 60) {
    return `${Math.ceil(timeSeconds)} seconds`;
  } else if (timeSeconds < 3600) {
    return `${Math.ceil(timeSeconds / 60)} minutes`;
  } else {
    return `${Math.ceil(timeSeconds / 3600)} hours`;
  }
}

/**
 * Check if system has enough resources for a model
 */
export function canRunModel(model: OllamaModelRecommendation, availableMemoryGB: number): boolean {
  // Rule of thumb: Need 1.2x model size in RAM
  const requiredGB = (model.sizeBytes / (1024 * 1024 * 1024)) * 1.2;
  return availableMemoryGB >= requiredGB;
}

/**
 * Get model recommendations based on system specs
 */
export function getModelRecommendationsForSystem(
  availableMemoryGB: number,
  availableDiskGB: number
): OllamaModelRecommendation[] {
  const allModels = getRecommendedModels();

  return allModels
    .filter((model) => {
      const modelSizeGB = model.sizeBytes / (1024 * 1024 * 1024);
      // Check if we have enough disk space and memory
      return modelSizeGB <= availableDiskGB && canRunModel(model, availableMemoryGB);
    })
    .sort((a, b) => {
      // Prioritize recommended models and smaller sizes
      if (a.recommended && !b.recommended) return -1;
      if (!a.recommended && b.recommended) return 1;
      return a.sizeBytes - b.sizeBytes;
    });
}
