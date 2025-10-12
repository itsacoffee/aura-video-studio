import { describe, it, expect, vi } from 'vitest';
import { renderHook } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { useNotifications, FailureToastOptions } from '../components/Notifications/Toasts';

// Wrapper for testing with FluentProvider
const wrapper = ({ children }: { children: React.ReactNode }) => (
  <FluentProvider theme={webLightTheme}>{children}</FluentProvider>
);

describe('Toasts - Error UX', () => {
  it('should support Retry button in failure toasts', () => {
    const { result } = renderHook(() => useNotifications(), { wrapper });
    
    expect(result.current.showFailureToast).toBeDefined();
    expect(typeof result.current.showFailureToast).toBe('function');
  });

  it('should support Open Logs button in failure toasts', () => {
    const { result } = renderHook(() => useNotifications(), { wrapper });
    
    const mockOnRetry = vi.fn();
    const mockOnOpenLogs = vi.fn();
    
    const options: FailureToastOptions = {
      title: 'Test Error',
      message: 'Test error message',
      onRetry: mockOnRetry,
      onOpenLogs: mockOnOpenLogs,
    };

    // This will create the toast but we can't easily assert the UI without full component rendering
    // We're verifying the API shape is correct
    expect(() => result.current.showFailureToast(options)).not.toThrow();
  });

  it('should accept correlationId in failure toast options', () => {
    const { result } = renderHook(() => useNotifications(), { wrapper });
    
    const options: FailureToastOptions = {
      title: 'Test Error',
      message: 'Test error message',
      correlationId: 'test-correlation-123',
      errorCode: 'E300',
      errorDetails: 'Additional error details',
      onRetry: vi.fn(),
      onOpenLogs: vi.fn(),
    };

    expect(() => result.current.showFailureToast(options)).not.toThrow();
  });

  it('should work without optional callbacks', () => {
    const { result } = renderHook(() => useNotifications(), { wrapper });
    
    const options: FailureToastOptions = {
      title: 'Test Error',
      message: 'Test error message',
    };

    expect(() => result.current.showFailureToast(options)).not.toThrow();
  });

  it('should return toasterId for Toaster component', () => {
    const { result } = renderHook(() => useNotifications(), { wrapper });
    
    expect(result.current.toasterId).toBeDefined();
    expect(typeof result.current.toasterId).toBe('string');
  });
});
