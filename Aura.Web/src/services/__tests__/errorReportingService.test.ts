/**
 * Tests for Error Reporting Service
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { errorReportingService } from '../errorReportingService';

describe('ErrorReportingService', () => {
  beforeEach(() => {
    // Clear error reports before each test
    errorReportingService.clearErrorReports();
  });

  describe('Error Reporting', () => {
    it('should report an error with severity', () => {
      const errorId = errorReportingService.reportError(
        'error',
        'Test Error',
        'This is a test error',
        new Error('Test error')
      );

      expect(errorId).toBeTruthy();
      expect(errorId).toMatch(/^err-/);

      const reports = errorReportingService.getErrorReports();
      expect(reports).toHaveLength(1);
      expect(reports[0].severity).toBe('error');
      expect(reports[0].title).toBe('Test Error');
    });

    it('should retrieve specific error report by ID', () => {
      const errorId = errorReportingService.reportError(
        'warning',
        'Test Warning',
        'This is a test warning'
      );

      const report = errorReportingService.getErrorReport(errorId);
      expect(report).not.toBeNull();
      expect(report?.id).toBe(errorId);
      expect(report?.severity).toBe('warning');
    });

    it('should include browser info in error reports', () => {
      const errorId = errorReportingService.reportError(
        'error',
        'Test Error',
        'This is a test error'
      );

      const report = errorReportingService.getErrorReport(errorId);
      expect(report?.browserInfo).toBeDefined();
      expect(report?.browserInfo.userAgent).toBeDefined();
      expect(report?.browserInfo.platform).toBeDefined();
    });

    it('should include user action and app state in context', () => {
      const errorId = errorReportingService.reportError(
        'error',
        'Test Error',
        'This is a test error',
        undefined,
        {
          userAction: 'Clicked delete button',
          appState: { isEditing: true },
        }
      );

      const report = errorReportingService.getErrorReport(errorId);
      expect(report?.userAction).toBe('Clicked delete button');
      expect(report?.appState).toEqual({ isEditing: true });
    });
  });

  describe('Notifications', () => {
    it('should show info notification', () => {
      const listener = vi.fn();
      errorReportingService.addNotificationListener(listener);

      errorReportingService.info('Info Title', 'Info message');

      expect(listener).toHaveBeenCalledOnce();
      const notification = listener.mock.calls[0][0];
      expect(notification.severity).toBe('info');
      expect(notification.autoHide).toBe(true);
      expect(notification.dismissible).toBe(true);

      errorReportingService.removeNotificationListener(listener);
    });

    it('should show warning notification', () => {
      const listener = vi.fn();
      errorReportingService.addNotificationListener(listener);

      errorReportingService.warning('Warning Title', 'Warning message');

      expect(listener).toHaveBeenCalledOnce();
      const notification = listener.mock.calls[0][0];
      expect(notification.severity).toBe('warning');
      expect(notification.autoHide).toBe(true);

      errorReportingService.removeNotificationListener(listener);
    });

    it('should show error notification', () => {
      const listener = vi.fn();
      errorReportingService.addNotificationListener(listener);

      errorReportingService.error('Error Title', 'Error message');

      expect(listener).toHaveBeenCalledOnce();
      const notification = listener.mock.calls[0][0];
      expect(notification.severity).toBe('error');
      expect(notification.autoHide).toBe(false);
      expect(notification.dismissible).toBe(true);

      errorReportingService.removeNotificationListener(listener);
    });

    it('should show critical notification', () => {
      const listener = vi.fn();
      errorReportingService.addNotificationListener(listener);

      errorReportingService.critical('Critical Error', 'Critical message');

      expect(listener).toHaveBeenCalledOnce();
      const notification = listener.mock.calls[0][0];
      expect(notification.severity).toBe('critical');
      expect(notification.autoHide).toBe(false);
      expect(notification.dismissible).toBe(false);

      errorReportingService.removeNotificationListener(listener);
    });

    it('should support custom actions in notifications', () => {
      const listener = vi.fn();
      errorReportingService.addNotificationListener(listener);

      const action = {
        label: 'Retry',
        handler: vi.fn(),
        primary: true,
      };

      errorReportingService.showNotification('error', 'Error', 'Message', {
        actions: [action],
      });

      const notification = listener.mock.calls[0][0];
      expect(notification.actions).toHaveLength(1);
      expect(notification.actions![0].label).toBe('Retry');

      errorReportingService.removeNotificationListener(listener);
    });
  });

  describe('Listener Management', () => {
    it('should add and remove notification listeners', () => {
      const listener = vi.fn();

      errorReportingService.addNotificationListener(listener);
      errorReportingService.info('Test', 'Message');
      expect(listener).toHaveBeenCalledOnce();

      errorReportingService.removeNotificationListener(listener);
      errorReportingService.info('Test', 'Message');
      expect(listener).toHaveBeenCalledOnce(); // Should not be called again
    });
  });

  describe('Severity Helpers', () => {
    it('should get correct color for severity', () => {
      expect(errorReportingService.getSeverityColor('info')).toBe('blue');
      expect(errorReportingService.getSeverityColor('warning')).toBe('yellow');
      expect(errorReportingService.getSeverityColor('error')).toBe('red');
      expect(errorReportingService.getSeverityColor('critical')).toBe('darkred');
    });

    it('should get correct icon for severity', () => {
      expect(errorReportingService.getSeverityIcon('info')).toBe('Info');
      expect(errorReportingService.getSeverityIcon('warning')).toBe('Warning');
      expect(errorReportingService.getSeverityIcon('error')).toBe('ErrorCircle');
      expect(errorReportingService.getSeverityIcon('critical')).toBe('StatusErrorFull');
    });
  });

  describe('Queue Management', () => {
    it('should maintain error queue', () => {
      for (let i = 0; i < 10; i++) {
        errorReportingService.reportError('error', `Error ${i}`, `Message ${i}`);
      }

      const reports = errorReportingService.getErrorReports();
      expect(reports).toHaveLength(10);
    });

    it('should clear all error reports', () => {
      errorReportingService.reportError('error', 'Error 1', 'Message 1');
      errorReportingService.reportError('error', 'Error 2', 'Message 2');

      expect(errorReportingService.getErrorReports()).toHaveLength(2);

      errorReportingService.clearErrorReports();

      expect(errorReportingService.getErrorReports()).toHaveLength(0);
    });
  });
});
