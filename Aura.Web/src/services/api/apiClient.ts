/**
 * Axios-based HTTP client for API requests
 * Provides centralized configuration, interceptors, and error handling
 */

import axios, {
  AxiosInstance,
  AxiosError,
  AxiosRequestConfig,
  InternalAxiosRequestConfig,
} from 'axios';
import { env } from '../../config/env';
import { loggingService } from '../loggingService';

/**
 * Create axios instance with default configuration
 */
const apiClient: AxiosInstance = axios.create({
  baseURL: env.apiBaseUrl,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Request interceptor for logging and auth token injection
 */
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Log API requests using logging service
    const startTime = Date.now();
    
    // Store start time in config for performance logging
    (config as any)._requestStartTime = startTime;

    loggingService.debug(
      `API Request: ${config.method?.toUpperCase()} ${config.url}`,
      'apiClient',
      'request',
      {
        method: config.method,
        url: config.url,
        data: config.data,
      }
    );

    // Add auth token if available (future implementation)
    const token = localStorage.getItem('auth_token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => {
    loggingService.error('API request setup error', error, 'apiClient', 'request');
    return Promise.reject(error);
  }
);

/**
 * Response interceptor for logging and error handling
 */
apiClient.interceptors.response.use(
  (response) => {
    // Calculate request duration for performance logging
    const startTime = (response.config as any)._requestStartTime;
    const duration = startTime ? Date.now() - startTime : 0;

    // Log successful responses with performance metrics
    loggingService.debug(
      `API Response: ${response.config.method?.toUpperCase()} ${response.config.url}`,
      'apiClient',
      'response',
      {
        method: response.config.method,
        url: response.config.url,
        status: response.status,
        data: response.data,
      }
    );

    // Log performance if request took more than 1 second
    if (duration > 1000) {
      loggingService.performance(
        `${response.config.method?.toUpperCase()} ${response.config.url}`,
        duration,
        'apiClient',
        {
          url: response.config.url,
          status: response.status,
        }
      );
    }

    return response;
  },
  async (error: AxiosError) => {
    // Log API errors
    const startTime = (error.config as any)?._requestStartTime;
    const duration = startTime ? Date.now() - startTime : 0;

    // Extract user-friendly error message
    let userMessage = 'An error occurred while communicating with the server';
    let technicalDetails = {};

    if (error.response) {
      // Server responded with error status
      const status = error.response.status;
      const responseData = error.response.data as any;

      // Extract user-friendly message from response if available
      if (responseData?.message) {
        userMessage = responseData.message;
      } else if (responseData?.error) {
        userMessage = responseData.error;
      }

      technicalDetails = {
        status,
        statusText: error.response.statusText,
        data: responseData,
        url: error.config?.url,
        method: error.config?.method,
      };

      // Handle specific error cases
      if (status === 401) {
        userMessage = 'Authentication required. Please log in.';
        // Unauthorized - clear token and redirect to login (future)
        localStorage.removeItem('auth_token');
        // Could dispatch a logout action here
      } else if (status === 403) {
        userMessage = 'Access forbidden. You do not have permission to perform this action.';
      } else if (status === 404) {
        userMessage = 'The requested resource was not found.';
      } else if (status >= 500) {
        userMessage = 'A server error occurred. Please try again later.';
      }
    } else if (error.request) {
      // Request made but no response received
      userMessage = 'Unable to reach the server. Please check your connection.';
      technicalDetails = {
        request: error.request,
        url: error.config?.url,
        method: error.config?.method,
      };
    } else {
      // Error setting up request
      userMessage = 'An error occurred while preparing the request.';
      technicalDetails = {
        message: error.message,
        url: error.config?.url,
        method: error.config?.method,
      };
    }

    // Log the error with full technical details
    loggingService.error(
      `API Error: ${userMessage}`,
      error,
      'apiClient',
      'error',
      technicalDetails
    );

    // Log performance even for errors
    if (duration > 0) {
      loggingService.performance(
        `${error.config?.method?.toUpperCase()} ${error.config?.url} (failed)`,
        duration,
        'apiClient',
        {
          url: error.config?.url,
          error: true,
        }
      );
    }

    // Attach user-friendly message to error for UI display
    (error as any).userMessage = userMessage;

    return Promise.reject(error);
  }
);

/**
 * Retry logic with exponential backoff
 */
export async function requestWithRetry<T>(
  requestFn: () => Promise<T>,
  maxRetries: number = 3,
  baseDelay: number = 1000
): Promise<T> {
  let lastError: Error | undefined;

  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      return await requestFn();
    } catch (error) {
      lastError = error as Error;

      if (attempt < maxRetries - 1) {
        const delay = baseDelay * Math.pow(2, attempt);
        console.warn(
          `Request failed, retrying in ${delay}ms... (Attempt ${attempt + 1}/${maxRetries})`
        );
        await new Promise((resolve) => setTimeout(resolve, delay));
      }
    }
  }

  throw lastError;
}

/**
 * Generic GET request
 */
export async function get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.get<T>(url, config);
  return response.data;
}

/**
 * Generic POST request
 */
export async function post<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig
): Promise<T> {
  const response = await apiClient.post<T>(url, data, config);
  return response.data;
}

/**
 * Generic PUT request
 */
export async function put<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.put<T>(url, data, config);
  return response.data;
}

/**
 * Generic PATCH request
 */
export async function patch<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig
): Promise<T> {
  const response = await apiClient.patch<T>(url, data, config);
  return response.data;
}

/**
 * Generic DELETE request
 */
export async function del<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.delete<T>(url, config);
  return response.data;
}

export default apiClient;
