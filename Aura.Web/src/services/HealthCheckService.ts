import axios, { AxiosError } from 'axios';

export interface HealthCheckResult {
  isHealthy: boolean;
  message: string;
  statusCode?: number;
  latencyMs?: number;
  timestamp: Date;
}

export interface HealthCheckOptions {
  maxRetries?: number;
  retryDelayMs?: number;
  timeoutMs?: number;
  exponentialBackoff?: boolean;
  backendUrl?: string;
}

export class HealthCheckService {
  private readonly maxRetries: number;
  private readonly retryDelayMs: number;
  private readonly timeoutMs: number;
  private readonly exponentialBackoff: boolean;
  private readonly backendUrl: string;

  constructor(options: HealthCheckOptions = {}) {
    this.maxRetries = options.maxRetries || 10;
    this.retryDelayMs = options.retryDelayMs || 1000;
    this.timeoutMs = options.timeoutMs || 3000;
    this.exponentialBackoff = options.exponentialBackoff !== false;
    this.backendUrl = options.backendUrl || 'http://localhost:5000';
  }

  async checkHealth(
    onProgress?: (attempt: number, maxAttempts: number) => void
  ): Promise<HealthCheckResult> {
    let lastError: Error | null = null;

    for (let attempt = 1; attempt <= this.maxRetries; attempt++) {
      if (onProgress) {
        onProgress(attempt, this.maxRetries);
      }

      const startTime = Date.now();

      try {
        const response = await axios.get(`${this.backendUrl}/health`, {
          timeout: this.timeoutMs,
          validateStatus: (status) => status < 500,
        });

        const latencyMs = Date.now() - startTime;

        if (response.status === 200) {
          return {
            isHealthy: true,
            message: 'Backend is healthy and responsive',
            statusCode: response.status,
            latencyMs,
            timestamp: new Date(),
          };
        } else if (response.status === 404) {
          return {
            isHealthy: true,
            message: 'Backend is running (no health endpoint)',
            statusCode: response.status,
            latencyMs,
            timestamp: new Date(),
          };
        }
      } catch (error) {
        lastError = error as Error;
        const axiosError = error as AxiosError;

        if (axiosError.code === 'ECONNREFUSED' || axiosError.code === 'ETIMEDOUT') {
          // eslint-disable-next-line no-console
          console.log(`[HealthCheck] Attempt ${attempt}/${this.maxRetries}: Backend not ready yet`);
        } else {
          console.error(`[HealthCheck] Attempt ${attempt}/${this.maxRetries}:`, error);
        }
      }

      if (attempt < this.maxRetries) {
        const delay = this.exponentialBackoff
          ? this.retryDelayMs * Math.pow(1.5, attempt - 1)
          : this.retryDelayMs;

        await new Promise((resolve) => setTimeout(resolve, Math.min(delay, 10000)));
      }
    }

    return {
      isHealthy: false,
      message: lastError?.message || 'Backend failed to respond after multiple attempts',
      timestamp: new Date(),
    };
  }

  async quickCheck(): Promise<boolean> {
    try {
      const response = await axios.get(`${this.backendUrl}/health`, {
        timeout: 2000,
        validateStatus: (status) => status < 500,
      });
      return response.status === 200 || response.status === 404;
    } catch {
      return false;
    }
  }

  async waitForBackend(
    timeoutMs: number = 30000,
    onProgress?: (attempt: number, maxAttempts: number) => void
  ): Promise<HealthCheckResult> {
    const startTime = Date.now();
    const estimatedAttempts = Math.ceil(timeoutMs / this.retryDelayMs);
    let attempt = 0;

    while (Date.now() - startTime < timeoutMs) {
      attempt++;
      if (onProgress) {
        onProgress(attempt, estimatedAttempts);
      }

      const result = await this.checkHealth();

      if (result.isHealthy) {
        return result;
      }

      await new Promise((resolve) => setTimeout(resolve, this.retryDelayMs));
    }

    return {
      isHealthy: false,
      message: `Backend did not become healthy within ${timeoutMs}ms`,
      timestamp: new Date(),
    };
  }
}

export const healthCheckService = new HealthCheckService();
