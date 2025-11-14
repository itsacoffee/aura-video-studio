/**
 * Menu Command Dispatcher
 *
 * Central dispatcher for menu commands in the renderer process.
 * Provides:
 * - Single entry point for all menu commands
 * - Feature-based handler registration
 * - Context-aware command availability
 * - User feedback for unavailable commands
 * - Structured logging with correlation IDs
 */

import { loggingService } from './loggingService';

export interface MenuCommandPayload {
  _correlationId?: string;
  _timestamp?: string;
  _command?: {
    label: string;
    category: string;
    description: string;
  };
  _validationError?: string;
  _validationIssues?: Array<{ path: string[]; message: string }>;
  _error?: string;
  [key: string]: unknown;
}

export interface CommandHandler {
  (payload: MenuCommandPayload): void | Promise<void>;
}

export interface CommandHandlerRegistration {
  commandId: string;
  handler: CommandHandler;
  context?: string;
  feature?: string;
}

/**
 * Application contexts where commands can be executed
 */
export enum AppContext {
  GLOBAL = 'global',
  PROJECT_LOADED = 'project',
  TIMELINE = 'timeline',
  MEDIA_LIBRARY = 'media',
  SETTINGS = 'settings',
  HELP = 'help',
}

class MenuCommandDispatcher {
  private handlers: Map<string, CommandHandlerRegistration[]>;
  private currentContext: AppContext;
  private showToast?: (message: string, type: 'info' | 'warning' | 'error') => void;

  constructor() {
    this.handlers = new Map();
    this.currentContext = AppContext.GLOBAL;
  }

  /**
   * Set the toast notification function
   */
  setToastHandler(toastFn: (message: string, type: 'info' | 'warning' | 'error') => void): void {
    this.showToast = toastFn;
  }

  /**
   * Update current application context
   */
  setContext(context: AppContext): void {
    const previousContext = this.currentContext;
    this.currentContext = context;

    loggingService.info('Context changed', {
      from: previousContext,
      to: context,
    });
  }

  /**
   * Get current application context
   */
  getContext(): AppContext {
    return this.currentContext;
  }

  /**
   * Register a command handler
   */
  registerHandler(registration: CommandHandlerRegistration): () => void {
    const { commandId, handler, context, feature } = registration;

    if (!this.handlers.has(commandId)) {
      this.handlers.set(commandId, []);
    }

    const handlers = this.handlers.get(commandId)!;
    handlers.push(registration);

    loggingService.info('Command handler registered', {
      commandId,
      context: context || 'any',
      feature: feature || 'unknown',
      totalHandlers: handlers.length,
    });

    // Return unregister function
    return () => {
      const handlers = this.handlers.get(commandId);
      if (handlers) {
        const index = handlers.findIndex((h) => h.handler === handler);
        if (index > -1) {
          handlers.splice(index, 1);
          loggingService.info('Command handler unregistered', { commandId, feature });
        }
      }
    };
  }

  /**
   * Dispatch a command to its registered handlers
   */
  async dispatch(commandId: string, payload: MenuCommandPayload = {}): Promise<void> {
    const correlationId = payload._correlationId || `disp_${Date.now()}`;
    const startTime = Date.now();

    loggingService.info('Dispatching menu command', {
      correlationId,
      commandId,
      command: payload._command?.label,
      category: payload._command?.category,
      currentContext: this.currentContext,
      hasValidationError: !!payload._validationError,
    });

    // Check for validation errors from preload
    if (payload._validationError) {
      loggingService.error('Command payload validation failed', {
        correlationId,
        commandId,
        error: payload._validationError,
        issues: payload._validationIssues,
      });

      this.showUserFeedback(
        `Command validation failed: ${payload._validationError}`,
        'error',
        correlationId
      );
      return;
    }

    // Get handlers for this command
    const registrations = this.handlers.get(commandId);

    if (!registrations || registrations.length === 0) {
      loggingService.warn('No handlers registered for command', {
        correlationId,
        commandId,
        command: payload._command?.label,
      });

      this.showUserFeedback(
        `Command "${payload._command?.label || commandId}" is not available`,
        'warning',
        correlationId
      );
      return;
    }

    // Filter handlers by context
    const contextualHandlers = registrations.filter((reg) => {
      // Global context handlers always match
      if (reg.context === AppContext.GLOBAL || !reg.context) {
        return true;
      }
      // Otherwise must match current context
      return reg.context === this.currentContext;
    });

    if (contextualHandlers.length === 0) {
      loggingService.warn('Command not available in current context', {
        correlationId,
        commandId,
        command: payload._command?.label,
        currentContext: this.currentContext,
        requiredContexts: registrations.map((r) => r.context || 'any'),
      });

      this.showUserFeedback(
        `"${payload._command?.label || commandId}" is not available in this view`,
        'info',
        correlationId
      );
      return;
    }

    // Execute all matching handlers
    const handlerPromises = contextualHandlers.map(async (registration) => {
      try {
        loggingService.info('Executing command handler', {
          correlationId,
          commandId,
          feature: registration.feature || 'unknown',
          context: registration.context || 'any',
        });

        await registration.handler(payload);

        const duration = Date.now() - startTime;
        loggingService.info('Command handler completed', {
          correlationId,
          commandId,
          feature: registration.feature || 'unknown',
          duration: `${duration}ms`,
        });
      } catch (error) {
        const duration = Date.now() - startTime;
        const errorObj = error instanceof Error ? error : new Error(String(error));

        loggingService.error('Command handler failed', {
          correlationId,
          commandId,
          feature: registration.feature || 'unknown',
          duration: `${duration}ms`,
          error: {
            message: errorObj.message,
            stack: errorObj.stack,
            name: errorObj.name,
          },
        });

        this.showUserFeedback(
          `Failed to execute "${payload._command?.label || commandId}": ${errorObj.message}`,
          'error',
          correlationId
        );
      }
    });

    await Promise.all(handlerPromises);

    const totalDuration = Date.now() - startTime;
    loggingService.info('Command dispatch completed', {
      correlationId,
      commandId,
      handlersExecuted: contextualHandlers.length,
      duration: `${totalDuration}ms`,
    });
  }

  /**
   * Show user feedback (toast notification)
   */
  private showUserFeedback(
    message: string,
    type: 'info' | 'warning' | 'error',
    correlationId: string
  ): void {
    if (this.showToast) {
      this.showToast(message, type);
    } else {
      console.log(`[MenuCommand:${type.toUpperCase()}] ${message} (${correlationId})`);
    }
  }

  /**
   * Get list of all registered command IDs
   */
  getRegisteredCommands(): string[] {
    return Array.from(this.handlers.keys());
  }

  /**
   * Get handlers for a specific command
   */
  getHandlers(commandId: string): CommandHandlerRegistration[] {
    return this.handlers.get(commandId) || [];
  }

  /**
   * Clear all handlers (useful for testing)
   */
  clearHandlers(): void {
    this.handlers.clear();
    loggingService.info('All command handlers cleared');
  }
}

// Export singleton instance
export const menuCommandDispatcher = new MenuCommandDispatcher();

// Export types and class for testing
export { MenuCommandDispatcher };
