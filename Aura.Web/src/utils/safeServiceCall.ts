/**
 * Safe error handler wrapper for service methods
 * PR 1: Provides consistent error handling across all services
 */

import { classifyError, type ClassifiedError } from './errorClassification';

export interface ServiceErrorResult<T = never> {
  success: false;
  error: ClassifiedError;
  data?: T;
}

export interface ServiceSuccessResult<T> {
  success: true;
  data: T;
  error?: never;
}

export type ServiceResult<T> = ServiceSuccessResult<T> | ServiceErrorResult<T>;

/**
 * Wrap an async service call with proper error classification
 * Returns a standardized result object with either data or classified error
 */
export async function safeServiceCall<T>(
  serviceCall: () => Promise<T>,
  context?: string
): Promise<ServiceResult<T>> {
  try {
    const data = await serviceCall();
    return {
      success: true,
      data,
    };
  } catch (error: unknown) {
    const classified = classifyError(error);

    // Log the error with context
    if (context) {
      console.error(`[${context}] Error:`, {
        category: classified.category,
        title: classified.title,
        message: classified.message,
        technical: classified.technicalDetails,
      });
    }

    return {
      success: false,
      error: classified,
    };
  }
}

/**
 * Extract user-friendly message from service result
 */
export function getResultMessage(result: ServiceResult<unknown>): string {
  if (result.success) {
    return 'Operation successful';
  }
  return result.error.message;
}

/**
 * Extract title from service result
 */
export function getResultTitle(result: ServiceResult<unknown>): string {
  if (result.success) {
    return 'Success';
  }
  return result.error.title;
}

/**
 * Check if result is retryable
 */
export function isResultRetryable(result: ServiceResult<unknown>): boolean {
  if (result.success) {
    return false;
  }
  return result.error.isRetryable;
}

/**
 * Get suggested actions from result
 */
export function getResultActions(result: ServiceResult<unknown>): string[] {
  if (result.success) {
    return [];
  }
  return result.error.suggestedActions;
}
