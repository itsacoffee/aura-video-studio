import { resetCircuitBreaker } from './apiClient';

export interface PiperStatus {
  installed: boolean;
  path?: string;
  voiceModelPath?: string;
  executableExists?: boolean;
  voiceModelExists?: boolean;
  error?: string | null;
}

export interface Mimic3Status {
  installed: boolean;
  baseUrl?: string;
  reachable?: boolean;
  error?: string | null;
}

export interface TtsInstallResult {
  success: boolean;
  message?: string;
  path?: string;
  baseUrl?: string;
  voiceModelPath?: string;
  voiceModelDownloaded?: boolean;
  requiresDocker?: boolean;
  requiresManualInstall?: boolean;
  dockerUrl?: string;
  alternativeInstructions?: string;
}

export interface TestVoiceResult {
  success: boolean;
  message?: string;
  audioBase64?: string | null;
  audioFormat?: string;
}

export interface PiperVoice {
  id: string;
  name: string;
  language: string;
  quality: string;
  sizeBytes: number;
  url: string;
}

export interface PiperVoicesResponse {
  voices: PiperVoice[];
  total: number;
}

export interface InstalledVoice {
  id: string;
  name: string;
  path: string;
  sizeBytes: number;
  installedAt: string;
}

export interface InstalledVoicesResponse {
  voices: InstalledVoice[];
  currentVoice?: string | null;
  voicesDirectory: string;
  total: number;
}

export interface DownloadVoiceResult {
  success: boolean;
  message?: string;
  path?: string;
  voiceId?: string;
}

class TtsClient {
  private baseUrl = '';

  constructor() {
    // Use relative URL for API calls
    this.baseUrl = '';
  }

  /**
   * Check Piper TTS installation status
   */
  async checkPiper(): Promise<PiperStatus> {
    try {
      resetCircuitBreaker();
      const response = await fetch('/api/setup/check-piper');

      if (!response.ok) {
        throw new Error(`Failed to check Piper status: ${response.statusText}`);
      }

      return (await response.json()) as PiperStatus;
    } catch (error) {
      console.error('Failed to check Piper status:', error);
      return {
        installed: false,
        error: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Check Mimic3 TTS installation status
   */
  async checkMimic3(): Promise<Mimic3Status> {
    try {
      resetCircuitBreaker();
      const response = await fetch('/api/setup/check-mimic3');

      if (!response.ok) {
        throw new Error(`Failed to check Mimic3 status: ${response.statusText}`);
      }

      return (await response.json()) as Mimic3Status;
    } catch (error) {
      console.error('Failed to check Mimic3 status:', error);
      return {
        installed: false,
        reachable: false,
        error: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Install Piper TTS
   */
  async installPiper(): Promise<TtsInstallResult> {
    try {
      resetCircuitBreaker();
      const response = await fetch('/api/setup/install-piper', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        return {
          success: false,
          message:
            errorData.message || errorData.error || `Installation failed: ${response.statusText}`,
        };
      }

      return (await response.json()) as TtsInstallResult;
    } catch (error) {
      console.error('Failed to install Piper:', error);
      return {
        success: false,
        message: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Install Mimic3 TTS
   */
  async installMimic3(): Promise<TtsInstallResult> {
    try {
      resetCircuitBreaker();
      const response = await fetch('/api/setup/install-mimic3', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        return {
          success: false,
          message:
            errorData.message || errorData.error || `Installation failed: ${response.statusText}`,
        };
      }

      return (await response.json()) as TtsInstallResult;
    } catch (error) {
      console.error('Failed to install Mimic3:', error);
      return {
        success: false,
        message: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Test Piper TTS voice synthesis
   */
  async testPiperVoice(): Promise<TestVoiceResult> {
    try {
      resetCircuitBreaker();
      const response = await fetch('/api/setup/piper/test-voice', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        return {
          success: false,
          message: errorData.message || errorData.error || `Test failed: ${response.statusText}`,
        };
      }

      return (await response.json()) as TestVoiceResult;
    } catch (error) {
      console.error('Failed to test Piper voice:', error);
      return {
        success: false,
        message: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Test Mimic3 TTS voice synthesis
   */
  async testMimic3Voice(): Promise<TestVoiceResult> {
    try {
      resetCircuitBreaker();
      const response = await fetch('/api/setup/mimic3/test-voice', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        return {
          success: false,
          message: errorData.message || errorData.error || `Test failed: ${response.statusText}`,
        };
      }

      return (await response.json()) as TestVoiceResult;
    } catch (error) {
      console.error('Failed to test Mimic3 voice:', error);
      return {
        success: false,
        message: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Get available Piper voice models
   */
  async getPiperVoices(): Promise<PiperVoicesResponse> {
    try {
      resetCircuitBreaker();
      const response = await fetch('/api/setup/piper/voices');

      if (!response.ok) {
        throw new Error(`Failed to get voices: ${response.statusText}`);
      }

      return (await response.json()) as PiperVoicesResponse;
    } catch (error) {
      console.error('Failed to get Piper voices:', error);
      return { voices: [], total: 0 };
    }
  }

  /**
   * Get installed Piper voice models
   */
  async getInstalledPiperVoices(): Promise<InstalledVoicesResponse> {
    try {
      resetCircuitBreaker();
      const response = await fetch('/api/setup/piper/voices/installed');

      if (!response.ok) {
        throw new Error(`Failed to get installed voices: ${response.statusText}`);
      }

      return (await response.json()) as InstalledVoicesResponse;
    } catch (error) {
      console.error('Failed to get installed Piper voices:', error);
      return { voices: [], voicesDirectory: '', total: 0 };
    }
  }

  /**
   * Download a Piper voice model
   */
  async downloadPiperVoice(voiceId: string): Promise<DownloadVoiceResult> {
    try {
      resetCircuitBreaker();
      const response = await fetch(
        `/api/setup/piper/voices/${encodeURIComponent(voiceId)}/download`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
        }
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        return {
          success: false,
          message:
            errorData.message || errorData.error || `Download failed: ${response.statusText}`,
        };
      }

      return (await response.json()) as DownloadVoiceResult;
    } catch (error) {
      console.error('Failed to download Piper voice:', error);
      return {
        success: false,
        message: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Activate a Piper voice model
   */
  async activatePiperVoice(voiceId: string): Promise<DownloadVoiceResult> {
    try {
      resetCircuitBreaker();
      const response = await fetch(
        `/api/setup/piper/voices/${encodeURIComponent(voiceId)}/activate`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
        }
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        return {
          success: false,
          message:
            errorData.message || errorData.error || `Activation failed: ${response.statusText}`,
        };
      }

      return (await response.json()) as DownloadVoiceResult;
    } catch (error) {
      console.error('Failed to activate Piper voice:', error);
      return {
        success: false,
        message: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }
}

export const ttsClient = new TtsClient();
