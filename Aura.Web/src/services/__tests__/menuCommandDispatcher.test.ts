/**
 * Unit Tests for Menu Command Dispatcher
 *
 * Tests the renderer-side command dispatcher including:
 * - Handler registration and unregistration
 * - Context-aware command availability
 * - Command dispatch with correlation IDs
 * - User feedback for unavailable commands
 * - Error handling in handlers
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import {
  MenuCommandDispatcher,
  AppContext,
  type MenuCommandPayload,
} from '../menuCommandDispatcher';

describe('MenuCommandDispatcher', () => {
  let dispatcher: MenuCommandDispatcher;
  let mockToast: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    dispatcher = new MenuCommandDispatcher();
    mockToast = vi.fn();
    dispatcher.setToastHandler(mockToast);
  });

  describe('Handler Registration', () => {
    it('should register a command handler', () => {
      const handler = vi.fn();
      const unregister = dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler,
        context: AppContext.GLOBAL,
        feature: 'test',
      });

      expect(unregister).toBeInstanceOf(Function);
      expect(dispatcher.getRegisteredCommands()).toContain('menu:newProject');
      expect(dispatcher.getHandlers('menu:newProject')).toHaveLength(1);
    });

    it('should allow multiple handlers for same command', () => {
      const handler1 = vi.fn();
      const handler2 = vi.fn();

      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler: handler1,
        context: AppContext.GLOBAL,
      });

      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler: handler2,
        context: AppContext.PROJECT_LOADED,
      });

      expect(dispatcher.getHandlers('menu:newProject')).toHaveLength(2);
    });

    it('should unregister handler when calling returned function', () => {
      const handler = vi.fn();
      const unregister = dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler,
      });

      expect(dispatcher.getHandlers('menu:newProject')).toHaveLength(1);

      unregister();

      expect(dispatcher.getHandlers('menu:newProject')).toHaveLength(0);
    });
  });

  describe('Command Dispatch', () => {
    it('should dispatch command to registered handler', async () => {
      const handler = vi.fn();
      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler,
        context: AppContext.GLOBAL,
      });

      const payload: MenuCommandPayload = {
        _correlationId: 'test-123',
        _command: {
          label: 'New Project',
          category: 'File',
          description: 'Test',
        },
      };

      await dispatcher.dispatch('menu:newProject', payload);

      expect(handler).toHaveBeenCalledWith(payload);
    });

    it('should show toast when no handlers registered', async () => {
      await dispatcher.dispatch('menu:unknown', {
        _correlationId: 'test-123',
      });

      expect(mockToast).toHaveBeenCalledWith(expect.stringContaining('not available'), 'warning');
    });

    it('should execute all matching handlers', async () => {
      const handler1 = vi.fn();
      const handler2 = vi.fn();

      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler: handler1,
        context: AppContext.GLOBAL,
      });

      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler: handler2,
        context: AppContext.GLOBAL,
      });

      await dispatcher.dispatch('menu:newProject', {});

      expect(handler1).toHaveBeenCalled();
      expect(handler2).toHaveBeenCalled();
    });
  });

  describe('Context Awareness', () => {
    it('should respect command context', async () => {
      const handler = vi.fn();
      dispatcher.registerHandler({
        commandId: 'menu:saveProject',
        handler,
        context: AppContext.PROJECT_LOADED,
      });

      // Set global context (no project loaded)
      dispatcher.setContext(AppContext.GLOBAL);

      await dispatcher.dispatch('menu:saveProject', {
        _command: { label: 'Save', category: 'File', description: 'Test' },
      });

      // Handler should NOT be called in wrong context
      expect(handler).not.toHaveBeenCalled();

      // Should show feedback
      expect(mockToast).toHaveBeenCalledWith(
        expect.stringContaining('not available in this view'),
        'info'
      );
    });

    it('should execute handler when context matches', async () => {
      const handler = vi.fn();
      dispatcher.registerHandler({
        commandId: 'menu:saveProject',
        handler,
        context: AppContext.PROJECT_LOADED,
      });

      // Set correct context
      dispatcher.setContext(AppContext.PROJECT_LOADED);

      await dispatcher.dispatch('menu:saveProject', {});

      expect(handler).toHaveBeenCalled();
    });

    it('should always execute GLOBAL handlers regardless of context', async () => {
      const handler = vi.fn();
      dispatcher.registerHandler({
        commandId: 'menu:openPreferences',
        handler,
        context: AppContext.GLOBAL,
      });

      // Set any context
      dispatcher.setContext(AppContext.TIMELINE);

      await dispatcher.dispatch('menu:openPreferences', {});

      expect(handler).toHaveBeenCalled();
    });
  });

  describe('Error Handling', () => {
    it('should catch and report handler errors', async () => {
      const handler = vi.fn().mockRejectedValue(new Error('Handler failed'));

      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler,
        context: AppContext.GLOBAL,
      });

      await dispatcher.dispatch('menu:newProject', {
        _command: { label: 'New Project', category: 'File', description: 'Test' },
      });

      // Should show error toast
      expect(mockToast).toHaveBeenCalledWith(expect.stringContaining('Failed to execute'), 'error');
    });

    it('should continue dispatching to other handlers if one fails', async () => {
      const handler1 = vi.fn().mockRejectedValue(new Error('Failed'));
      const handler2 = vi.fn();

      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler: handler1,
        context: AppContext.GLOBAL,
      });

      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler: handler2,
        context: AppContext.GLOBAL,
      });

      await dispatcher.dispatch('menu:newProject', {});

      expect(handler1).toHaveBeenCalled();
      expect(handler2).toHaveBeenCalled();
    });

    it('should show toast for validation errors', async () => {
      const handler = vi.fn();
      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler,
      });

      await dispatcher.dispatch('menu:newProject', {
        _validationError: 'Invalid payload',
      });

      // Handler should not be called for validation errors
      expect(handler).not.toHaveBeenCalled();

      // Should show error toast
      expect(mockToast).toHaveBeenCalledWith(expect.stringContaining('validation failed'), 'error');
    });
  });

  describe('Utility Methods', () => {
    it('should return all registered command IDs', () => {
      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler: vi.fn(),
      });

      dispatcher.registerHandler({
        commandId: 'menu:saveProject',
        handler: vi.fn(),
      });

      const commands = dispatcher.getRegisteredCommands();
      expect(commands).toContain('menu:newProject');
      expect(commands).toContain('menu:saveProject');
    });

    it('should get handlers for specific command', () => {
      const handler1 = vi.fn();
      const handler2 = vi.fn();

      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler: handler1,
      });

      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler: handler2,
      });

      const handlers = dispatcher.getHandlers('menu:newProject');
      expect(handlers).toHaveLength(2);
    });

    it('should clear all handlers', () => {
      dispatcher.registerHandler({
        commandId: 'menu:newProject',
        handler: vi.fn(),
      });

      dispatcher.registerHandler({
        commandId: 'menu:saveProject',
        handler: vi.fn(),
      });

      expect(dispatcher.getRegisteredCommands()).toHaveLength(2);

      dispatcher.clearHandlers();

      expect(dispatcher.getRegisteredCommands()).toHaveLength(0);
    });

    it('should get and set current context', () => {
      expect(dispatcher.getContext()).toBe(AppContext.GLOBAL);

      dispatcher.setContext(AppContext.PROJECT_LOADED);

      expect(dispatcher.getContext()).toBe(AppContext.PROJECT_LOADED);
    });
  });
});
