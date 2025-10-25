/**
 * Environment configuration
 * Provides type-safe access to environment variables
 */

export const env = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5005',
  appVersion: import.meta.env.VITE_APP_VERSION || '1.0.0',
  appName: import.meta.env.VITE_APP_NAME || 'Aura Video Studio',
  environment: import.meta.env.VITE_ENV || 'development',
  enableAnalytics: import.meta.env.VITE_ENABLE_ANALYTICS === 'true',
  enableDebug: import.meta.env.VITE_ENABLE_DEBUG === 'true',
  enableDevTools: import.meta.env.VITE_ENABLE_DEV_TOOLS === 'true',
  isDevelopment: import.meta.env.DEV,
  isProduction: import.meta.env.PROD,
  mode: import.meta.env.MODE,
} as const;

// Export for debugging purposes (only in development)
if (env.isDevelopment && env.enableDebug) {
  // eslint-disable-next-line no-console
  console.log('Environment Configuration:', env);
}
