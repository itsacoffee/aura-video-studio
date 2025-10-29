/**
 * Cancellable Request Wrappers
 * Provides a clean API for making cancellable HTTP requests using AbortController
 */

import { AxiosRequestConfig } from 'axios';
import apiClient from './apiClient';

/**
 * Represents a cancellable request
 */
export interface CancellableRequest<T> {
  /** The promise representing the HTTP request */
  promise: Promise<T>;
  /** Function to cancel the request */
  cancel: () => void;
}

/**
 * Make a cancellable GET request
 */
export function getCancellable<T>(url: string, config?: AxiosRequestConfig): CancellableRequest<T> {
  const abortController = new AbortController();

  const promise = apiClient
    .get<T>(url, {
      ...config,
      signal: abortController.signal,
    })
    .then((response) => response.data);

  const cancel = () => {
    abortController.abort();
  };

  return { promise, cancel };
}

/**
 * Make a cancellable POST request
 */
export function postCancellable<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig
): CancellableRequest<T> {
  const abortController = new AbortController();

  const promise = apiClient
    .post<T>(url, data, {
      ...config,
      signal: abortController.signal,
    })
    .then((response) => response.data);

  const cancel = () => {
    abortController.abort();
  };

  return { promise, cancel };
}

/**
 * Make a cancellable PUT request
 */
export function putCancellable<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig
): CancellableRequest<T> {
  const abortController = new AbortController();

  const promise = apiClient
    .put<T>(url, data, {
      ...config,
      signal: abortController.signal,
    })
    .then((response) => response.data);

  const cancel = () => {
    abortController.abort();
  };

  return { promise, cancel };
}

/**
 * Make a cancellable DELETE request
 */
export function deleteCancellable<T>(
  url: string,
  config?: AxiosRequestConfig
): CancellableRequest<T> {
  const abortController = new AbortController();

  const promise = apiClient
    .delete<T>(url, {
      ...config,
      signal: abortController.signal,
    })
    .then((response) => response.data);

  const cancel = () => {
    abortController.abort();
  };

  return { promise, cancel };
}

/**
 * Make a cancellable PATCH request
 */
export function patchCancellable<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig
): CancellableRequest<T> {
  const abortController = new AbortController();

  const promise = apiClient
    .patch<T>(url, data, {
      ...config,
      signal: abortController.signal,
    })
    .then((response) => response.data);

  const cancel = () => {
    abortController.abort();
  };

  return { promise, cancel };
}

/**
 * Check if an error is an abort error (request was cancelled)
 */
export function isAbortError(error: unknown): boolean {
  if (error instanceof Error) {
    return error.name === 'AbortError' || error.name === 'CanceledError';
  }
  return false;
}
