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
          message: errorData.message || errorData.error || `Installation failed: ${response.statusText}`,
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
          message: errorData.message || errorData.error || `Installation failed: ${response.statusText}`,
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
}

export const ttsClient = new TtsClient();

