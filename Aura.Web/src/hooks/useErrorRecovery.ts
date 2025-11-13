/**
 * Hook for managing error recovery state and UI
 */

import { useState, useCallback } from 'react';
import type { ErrorRecoveryOptions } from '../components/ErrorBoundary/ErrorRecoveryModal';
import { errorReportingService } from '../services/errorReportingService';
import { loggingService } from '../services/loggingService';

export interface UseErrorRecoveryReturn {
  showErrorRecovery: (options: Omit<ErrorRecoveryOptions, 'onClose'>) => void;
  hideErrorRecovery: () => void;
  isErrorRecoveryOpen: boolean;
  errorRecoveryOptions: ErrorRecoveryOptions | null;
}

/**
 * Hook for managing error recovery modal
 */
export function useErrorRecovery(): UseErrorRecoveryReturn {
  const [isOpen, setIsOpen] = useState(false);
  const [options, setOptions] = useState<ErrorRecoveryOptions | null>(null);

  const showErrorRecovery = useCallback((opts: Omit<ErrorRecoveryOptions, 'onClose'>) => {
    const recoveryOptions: ErrorRecoveryOptions = {
      ...opts,
      onClose: () => {
        setIsOpen(false);
        setOptions(null);
      },
    };

    setOptions(recoveryOptions);
    setIsOpen(true);

    loggingService.info('Error recovery modal opened', 'useErrorRecovery', 'showErrorRecovery', {
      title: opts.title,
      severity: opts.severity,
      canRetry: opts.canRetry,
    });

    if (opts.severity === 'error' || opts.severity === 'critical') {
      errorReportingService.reportError(opts.severity, opts.title, opts.message, opts.error, {
        userAction: 'Error recovery modal shown',
      });
    }
  }, []);

  const hideErrorRecovery = useCallback(() => {
    setIsOpen(false);
    setOptions(null);

    loggingService.info('Error recovery modal closed', 'useErrorRecovery', 'hideErrorRecovery');
  }, []);

  return {
    showErrorRecovery,
    hideErrorRecovery,
    isErrorRecoveryOpen: isOpen,
    errorRecoveryOptions: options,
  };
}
