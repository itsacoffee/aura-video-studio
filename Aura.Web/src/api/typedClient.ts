/**
 * Typed API Client
 *
 * Provides a strongly-typed API client with:
 * - OpenAPI-generated types
 * - Circuit breaker pattern
 * - Retry logic with exponential backoff
 * - Correlation IDs
 * - Comprehensive error handling
 */

import axios, {
  AxiosInstance,
  AxiosError,
  AxiosRequestConfig,
  InternalAxiosRequestConfig,
} from 'axios';
import { env } from '@/config/env';
import { getHttpErrorMessage, isTransientError } from '@/services/api/apiErrorMessages';
import { PersistentCircuitBreaker } from '@/services/api/circuitBreakerPersistence';
import { loggingService } from '@/services/loggingService';

/**
 * Circuit breaker states
 */
enum CircuitState {
  CLOSED = 'CLOSED',
  OPEN = 'OPEN',
  HALF_OPEN = 'HALF_OPEN',
}

/**
 * Circuit breaker configuration
 */
interface CircuitBreakerConfig {
  failureThreshold: number;
  successThreshold: number;
  timeout: number;
}

/**
 * Circuit breaker for preventing cascading failures
 */
class CircuitBreaker {
  private state: CircuitState = CircuitState.CLOSED;
  private failureCount = 0;
  private successCount = 0;
  private nextAttempt = Date.now();
  private config: CircuitBreakerConfig;
  private endpoint: string;

  constructor(endpoint: string, config: Partial<CircuitBreakerConfig> = {}) {
    this.endpoint = endpoint;
    this.config = {
      failureThreshold: config.failureThreshold || 5,
      successThreshold: config.successThreshold || 2,
      timeout: config.timeout || 60000,
    };

    const persistedState = PersistentCircuitBreaker.loadState(endpoint);
    if (persistedState) {
      this.state = persistedState.state as CircuitState;
      this.failureCount = persistedState.failureCount;
      this.successCount = persistedState.successCount;
      this.nextAttempt = persistedState.nextAttempt;
    }
  }

  public canAttempt(): boolean {
    if (this.state === CircuitState.CLOSED) {
      return true;
    }

    if (this.state === CircuitState.OPEN) {
      if (Date.now() >= this.nextAttempt) {
        this.state = CircuitState.HALF_OPEN;
        this.successCount = 0;
        loggingService.info(
          'Circuit breaker entering half-open state',
          'typedClient',
          'circuitBreaker'
        );
        return true;
      }
      return false;
    }

    return this.state === CircuitState.HALF_OPEN;
  }

  public recordSuccess(): void {
    this.failureCount = 0;

    if (this.state === CircuitState.HALF_OPEN) {
      this.successCount++;
      if (this.successCount >= this.config.successThreshold) {
        this.state = CircuitState.CLOSED;
        loggingService.info(
          'Circuit breaker closed after successful recovery',
          'typedClient',
          'circuitBreaker'
        );
      }
    }

    this.persistState();
  }

  public recordFailure(): void {
    this.failureCount++;

    if (this.state === CircuitState.HALF_OPEN) {
      this.state = CircuitState.OPEN;
      this.nextAttempt = Date.now() + this.config.timeout;
      loggingService.warn(
        'Circuit breaker opened after half-open failure',
        'typedClient',
        'circuitBreaker'
      );
    } else if (this.failureCount >= this.config.failureThreshold) {
      this.state = CircuitState.OPEN;
      this.nextAttempt = Date.now() + this.config.timeout;
      loggingService.warn(
        'Circuit breaker opened after threshold reached',
        'typedClient',
        'circuitBreaker',
        {
          failureCount: this.failureCount,
          threshold: this.config.failureThreshold,
        }
      );
    }

    this.persistState();
  }

  private persistState(): void {
    PersistentCircuitBreaker.saveState(this.endpoint, {
      state: this.state,
      failureCount: this.failureCount,
      successCount: this.successCount,
      nextAttempt: this.nextAttempt,
      timestamp: Date.now(),
    });
  }

  public getState(): CircuitState {
    return this.state;
  }
}

/**
 * API Error class for typed error handling
 */
export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status?: number,
    public readonly code?: string,
    public readonly correlationId?: string,
    public readonly details?: unknown
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

/**
 * Typed API Client Configuration
 */
interface TypedClientConfig {
  baseURL: string;
  timeout?: number;
  retryAttempts?: number;
  retryDelay?: number;
}

/**
 * Typed API Client
 *
 * Wraps axios with circuit breaker, retry logic, and correlation IDs
 */
export class TypedApiClient {
  private axiosInstance: AxiosInstance;
  private circuitBreaker: CircuitBreaker;
  private config: Required<TypedClientConfig>;

  constructor(config: TypedClientConfig) {
    this.config = {
      baseURL: config.baseURL,
      timeout: config.timeout || 30000,
      retryAttempts: config.retryAttempts || 3,
      retryDelay: config.retryDelay || 1000,
    };

    this.circuitBreaker = new CircuitBreaker(this.config.baseURL);

    this.axiosInstance = axios.create({
      baseURL: this.config.baseURL,
      timeout: this.config.timeout,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors(): void {
    // Request interceptor: Add correlation ID
    this.axiosInstance.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        const correlationId = crypto.randomUUID();
        config.headers['X-Correlation-ID'] = correlationId;

        loggingService.debug('API request', 'typedClient', 'request', {
          method: config.method,
          url: config.url,
          correlationId,
        });

        return config;
      },
      (error: AxiosError) => {
        loggingService.error(
          'Request interceptor error',
          error instanceof Error ? error : new Error(String(error)),
          'typedClient',
          'request'
        );
        return Promise.reject(error);
      }
    );

    // Response interceptor: Handle errors and circuit breaker
    this.axiosInstance.interceptors.response.use(
      (response) => {
        this.circuitBreaker.recordSuccess();
        return response;
      },
      async (error: AxiosError) => {
        this.circuitBreaker.recordFailure();

        const config = error.config as AxiosRequestConfig & { _retryCount?: number };
        const retryCount = config._retryCount || 0;

        // Check if we should retry
        const shouldRetry =
          retryCount < this.config.retryAttempts &&
          isTransientError(error.response?.status) &&
          this.circuitBreaker.canAttempt();

        if (shouldRetry && config) {
          config._retryCount = retryCount + 1;
          const delay = this.config.retryDelay * Math.pow(2, retryCount);

          loggingService.info('Retrying request', 'typedClient', 'retry', {
            attempt: retryCount + 1,
            delay,
            url: config.url,
          });

          await new Promise((resolve) => setTimeout(resolve, delay));
          return this.axiosInstance(config);
        }

        // Convert axios error to ApiError
        const apiError = this.convertToApiError(error);
        loggingService.error('API request failed', apiError, 'typedClient', 'response', {
          status: apiError.status,
          correlationId: apiError.correlationId,
        });

        return Promise.reject(apiError);
      }
    );
  }

  private convertToApiError(error: AxiosError): ApiError {
    const status = error.response?.status;
    const correlationId = error.response?.headers['x-correlation-id'] as string | undefined;
    const errorMessage = status
      ? getHttpErrorMessage(status)
      : {
          title: 'Network Error',
          message: 'Unable to connect to the server',
          severity: 'error' as const,
        };
    const message = errorMessage.message;

    let code: string | undefined;
    let details: unknown;

    if (error.response?.data && typeof error.response.data === 'object') {
      const data = error.response.data as Record<string, unknown>;
      code = data.code as string | undefined;
      details = data.detail || data.details;
    }

    return new ApiError(message, status, code, correlationId, details);
  }

  /**
   * GET request
   */
  public async get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    if (!this.circuitBreaker.canAttempt()) {
      throw new ApiError('Circuit breaker is open', undefined, 'CIRCUIT_OPEN');
    }

    const response = await this.axiosInstance.get<T>(url, config);
    return response.data;
  }

  /**
   * POST request
   */
  public async post<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    if (!this.circuitBreaker.canAttempt()) {
      throw new ApiError('Circuit breaker is open', undefined, 'CIRCUIT_OPEN');
    }

    const response = await this.axiosInstance.post<T>(url, data, config);
    return response.data;
  }

  /**
   * PUT request
   */
  public async put<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    if (!this.circuitBreaker.canAttempt()) {
      throw new ApiError('Circuit breaker is open', undefined, 'CIRCUIT_OPEN');
    }

    const response = await this.axiosInstance.put<T>(url, data, config);
    return response.data;
  }

  /**
   * DELETE request
   */
  public async delete<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    if (!this.circuitBreaker.canAttempt()) {
      throw new ApiError('Circuit breaker is open', undefined, 'CIRCUIT_OPEN');
    }

    const response = await this.axiosInstance.delete<T>(url, config);
    return response.data;
  }

  /**
   * Get circuit breaker state for debugging
   */
  public getCircuitBreakerState(): CircuitState {
    return this.circuitBreaker.getState();
  }
}

/**
 * Default typed API client instance
 */
export const typedApiClient = new TypedApiClient({
  baseURL: env.apiBaseUrl,
  timeout: 30000,
  retryAttempts: 3,
  retryDelay: 1000,
});
