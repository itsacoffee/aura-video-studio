/**
 * Retry strategies for handling transient failures in E2E tests
 * Provides exponential backoff and provider-specific retry logic
 */

export interface RetryConfig {
  maxAttempts: number;
  initialDelayMs: number;
  maxDelayMs: number;
  backoffMultiplier: number;
  retryableErrors: string[];
}

export interface RetryResult<T> {
  success: boolean;
  data?: T;
  error?: Error;
  attempts: number;
  totalDelayMs: number;
}

/**
 * Default retry configuration
 */
export const DEFAULT_RETRY_CONFIG: RetryConfig = {
  maxAttempts: 3,
  initialDelayMs: 1000,
  maxDelayMs: 10000,
  backoffMultiplier: 2,
  retryableErrors: [
    'ECONNREFUSED',
    'ETIMEDOUT',
    'ENOTFOUND',
    'Network Error',
    'timeout',
    '503',
    '429',
  ],
};

/**
 * Provider-specific retry configurations
 */
export const PROVIDER_RETRY_CONFIGS: Record<string, Partial<RetryConfig>> = {
  openai: {
    maxAttempts: 3,
    initialDelayMs: 2000,
    retryableErrors: ['429', '503', 'rate_limit', 'timeout'],
  },
  elevenlabs: {
    maxAttempts: 3,
    initialDelayMs: 1500,
    retryableErrors: ['429', '503', 'quota_exceeded', 'timeout'],
  },
  stablediffusion: {
    maxAttempts: 4,
    initialDelayMs: 3000,
    maxDelayMs: 15000,
    retryableErrors: ['503', '502', 'GPU busy', 'timeout'],
  },
  ollama: {
    maxAttempts: 2,
    initialDelayMs: 500,
    retryableErrors: ['ECONNREFUSED', 'model loading', 'timeout'],
  },
  piper: {
    maxAttempts: 2,
    initialDelayMs: 500,
    retryableErrors: ['ENOENT', 'model not found', 'initialization'],
  },
};

/**
 * Execute function with exponential backoff retry
 */
export async function retryWithBackoff<T>(
  fn: () => Promise<T>,
  config: Partial<RetryConfig> = {}
): Promise<RetryResult<T>> {
  const fullConfig = { ...DEFAULT_RETRY_CONFIG, ...config };
  let attempts = 0;
  let totalDelayMs = 0;
  let lastError: Error | undefined;

  while (attempts < fullConfig.maxAttempts) {
    attempts++;

    try {
      const data = await fn();
      return {
        success: true,
        data,
        attempts,
        totalDelayMs,
      };
    } catch (error) {
      lastError = error instanceof Error ? error : new Error(String(error));

      if (attempts >= fullConfig.maxAttempts) {
        break;
      }

      if (!isRetryableError(lastError, fullConfig.retryableErrors)) {
        break;
      }

      const delayMs = Math.min(
        fullConfig.initialDelayMs * Math.pow(fullConfig.backoffMultiplier, attempts - 1),
        fullConfig.maxDelayMs
      );

      totalDelayMs += delayMs;
      await sleep(delayMs);
    }
  }

  return {
    success: false,
    error: lastError,
    attempts,
    totalDelayMs,
  };
}

/**
 * Check if error is retryable
 */
function isRetryableError(error: Error, retryableErrors: string[]): boolean {
  const errorStr = error.message.toLowerCase();

  return retryableErrors.some((retryable) => errorStr.includes(retryable.toLowerCase()));
}

/**
 * Sleep for specified milliseconds
 */
function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Retry with jitter to prevent thundering herd
 */
export async function retryWithJitter<T>(
  fn: () => Promise<T>,
  config: Partial<RetryConfig> = {}
): Promise<RetryResult<T>> {
  const fullConfig = { ...DEFAULT_RETRY_CONFIG, ...config };
  let attempts = 0;
  let totalDelayMs = 0;
  let lastError: Error | undefined;

  while (attempts < fullConfig.maxAttempts) {
    attempts++;

    try {
      const data = await fn();
      return {
        success: true,
        data,
        attempts,
        totalDelayMs,
      };
    } catch (error) {
      lastError = error instanceof Error ? error : new Error(String(error));

      if (attempts >= fullConfig.maxAttempts) {
        break;
      }

      if (!isRetryableError(lastError, fullConfig.retryableErrors)) {
        break;
      }

      const baseDelayMs = Math.min(
        fullConfig.initialDelayMs * Math.pow(fullConfig.backoffMultiplier, attempts - 1),
        fullConfig.maxDelayMs
      );

      const jitter = Math.random() * 0.3 * baseDelayMs;
      const delayMs = Math.floor(baseDelayMs + jitter);

      totalDelayMs += delayMs;
      await sleep(delayMs);
    }
  }

  return {
    success: false,
    error: lastError,
    attempts,
    totalDelayMs,
  };
}

/**
 * Retry with provider-specific configuration
 */
export async function retryProviderCall<T>(
  providerName: string,
  fn: () => Promise<T>
): Promise<RetryResult<T>> {
  const providerConfig = PROVIDER_RETRY_CONFIGS[providerName.toLowerCase()] || {};
  return retryWithJitter(fn, providerConfig);
}

/**
 * Batch retry for multiple operations
 */
export async function retryBatch<T>(
  operations: Array<() => Promise<T>>,
  config: Partial<RetryConfig> = {}
): Promise<Array<RetryResult<T>>> {
  return await Promise.all(operations.map((op) => retryWithBackoff(op, config)));
}

/**
 * Circuit breaker pattern for preventing cascading failures
 */
export class CircuitBreaker<T> {
  private failureCount = 0;
  private successCount = 0;
  private state: 'closed' | 'open' | 'half-open' = 'closed';
  private nextAttemptTime = 0;

  constructor(
    private readonly threshold: number = 5,
    private readonly resetTimeMs: number = 60000,
    private readonly halfOpenSuccessThreshold: number = 2
  ) {}

  async execute(fn: () => Promise<T>): Promise<T> {
    if (this.state === 'open') {
      if (Date.now() < this.nextAttemptTime) {
        throw new Error('Circuit breaker is OPEN');
      }

      this.state = 'half-open';
      this.successCount = 0;
    }

    try {
      const result = await fn();

      if (this.state === 'half-open') {
        this.successCount++;

        if (this.successCount >= this.halfOpenSuccessThreshold) {
          this.state = 'closed';
          this.failureCount = 0;
        }
      } else {
        this.failureCount = 0;
      }

      return result;
    } catch (error) {
      this.failureCount++;

      if (this.state === 'half-open') {
        this.state = 'open';
        this.nextAttemptTime = Date.now() + this.resetTimeMs;
      } else if (this.failureCount >= this.threshold) {
        this.state = 'open';
        this.nextAttemptTime = Date.now() + this.resetTimeMs;
      }

      throw error;
    }
  }

  getState(): { state: string; failureCount: number; successCount: number } {
    return {
      state: this.state,
      failureCount: this.failureCount,
      successCount: this.successCount,
    };
  }

  reset(): void {
    this.state = 'closed';
    this.failureCount = 0;
    this.successCount = 0;
    this.nextAttemptTime = 0;
  }
}

/**
 * Rate limiter for preventing API abuse
 */
export class RateLimiter {
  private tokens: number;
  private lastRefillTime: number;

  constructor(
    private readonly maxTokens: number,
    private readonly refillRatePerSecond: number
  ) {
    this.tokens = maxTokens;
    this.lastRefillTime = Date.now();
  }

  async acquire(tokensNeeded: number = 1): Promise<void> {
    this.refill();

    while (this.tokens < tokensNeeded) {
      const waitTimeMs = ((tokensNeeded - this.tokens) / this.refillRatePerSecond) * 1000;
      await sleep(Math.ceil(waitTimeMs));
      this.refill();
    }

    this.tokens -= tokensNeeded;
  }

  private refill(): void {
    const now = Date.now();
    const elapsedSeconds = (now - this.lastRefillTime) / 1000;
    const tokensToAdd = elapsedSeconds * this.refillRatePerSecond;

    this.tokens = Math.min(this.maxTokens, this.tokens + tokensToAdd);
    this.lastRefillTime = now;
  }

  getAvailableTokens(): number {
    this.refill();
    return this.tokens;
  }
}
