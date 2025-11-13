/**
 * Error Toast Utilities
 */

import type { ToastIntent } from '@fluentui/react-components';
import type { ErrorSeverity } from '../../services/errorReportingService';

export function severityToIntent(severity: ErrorSeverity): ToastIntent {
  switch (severity) {
    case 'info':
      return 'info';
    case 'warning':
      return 'warning';
    case 'error':
    case 'critical':
      return 'error';
    default:
      return 'info';
  }
}
