/**
 * Error Boundary Components
 */

export { GlobalErrorBoundary } from './GlobalErrorBoundary';
export { ComponentErrorBoundary } from './ComponentErrorBoundary';
export { RouteErrorBoundary } from './RouteErrorBoundary';
export { CrashRecoveryScreen } from './CrashRecoveryScreen';
export { ApiErrorDisplay, parseApiError } from './ApiErrorDisplay';
export type { ApiError } from './ApiErrorDisplay';
export { EnhancedErrorFallback } from './EnhancedErrorFallback';
export { ErrorRecoveryModal } from './ErrorRecoveryModal';
export type { ErrorRecoveryOptions } from './ErrorRecoveryModal';
export {
  ErrorDisplay,
  createNetworkErrorDisplay,
  createAuthErrorDisplay,
  createValidationErrorDisplay,
  createGenericErrorDisplay,
} from './ErrorDisplay';
export type { ErrorDisplayProps } from './ErrorDisplay';

// Default export for convenience - uses GlobalErrorBoundary
export { GlobalErrorBoundary as ErrorBoundary } from './GlobalErrorBoundary';
