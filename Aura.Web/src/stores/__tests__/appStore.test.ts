/**
 * App Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useAppStore } from '../appStore';

describe('AppStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useAppStore.setState({
      notifications: [],
      activeModal: null,
      globalLoading: false,
      loadingMessage: null,
    });
  });

  it('should toggle sidebar', () => {
    const { toggleSidebar } = useAppStore.getState();
    const initialState = useAppStore.getState().isSidebarOpen;

    toggleSidebar();

    const state = useAppStore.getState();
    expect(state.isSidebarOpen).toBe(!initialState);
  });

  it('should open and close modal', () => {
    const { openModal, closeModal } = useAppStore.getState();

    openModal('test-modal');
    let state = useAppStore.getState();
    expect(state.activeModal).toBe('test-modal');

    closeModal();
    state = useAppStore.getState();
    expect(state.activeModal).toBeNull();
  });

  it('should add notification', () => {
    const { addNotification } = useAppStore.getState();

    const notificationId = addNotification({
      type: 'success',
      title: 'Test',
      message: 'Test message',
    });

    const state = useAppStore.getState();
    expect(state.notifications.length).toBe(1);
    expect(state.notifications[0].id).toBe(notificationId);
    expect(state.notifications[0].type).toBe('success');
  });

  it('should remove notification', () => {
    const { addNotification, removeNotification } = useAppStore.getState();

    const id = addNotification({
      type: 'info',
      title: 'Test',
      message: 'Test message',
    });

    removeNotification(id);

    const state = useAppStore.getState();
    expect(state.notifications.length).toBe(0);
  });

  it('should maintain max notifications limit', () => {
    const { addNotification, maxNotifications } = useAppStore.getState();

    // Add more notifications than limit
    for (let i = 0; i < maxNotifications + 3; i++) {
      addNotification({
        type: 'info',
        title: `Test ${i}`,
        message: `Message ${i}`,
      });
    }

    const state = useAppStore.getState();
    expect(state.notifications.length).toBe(maxNotifications);
  });

  it('should clear all notifications', () => {
    const { addNotification, clearNotifications } = useAppStore.getState();

    addNotification({ type: 'info', title: 'Test 1', message: 'Message 1' });
    addNotification({ type: 'info', title: 'Test 2', message: 'Message 2' });
    addNotification({ type: 'info', title: 'Test 3', message: 'Message 3' });

    clearNotifications();

    const state = useAppStore.getState();
    expect(state.notifications.length).toBe(0);
  });

  it('should update settings', () => {
    const { updateSettings } = useAppStore.getState();

    updateSettings({ theme: 'dark', autoSave: false });

    const state = useAppStore.getState();
    expect(state.settings.theme).toBe('dark');
    expect(state.settings.autoSave).toBe(false);
  });

  it('should reset settings to default', () => {
    const { updateSettings, resetSettings } = useAppStore.getState();

    updateSettings({ theme: 'dark', compactMode: true });
    resetSettings();

    const state = useAppStore.getState();
    expect(state.settings.theme).toBe('auto');
    expect(state.settings.compactMode).toBe(false);
  });

  it('should set global loading state', () => {
    const { setGlobalLoading } = useAppStore.getState();

    setGlobalLoading(true, 'Loading data...');

    let state = useAppStore.getState();
    expect(state.globalLoading).toBe(true);
    expect(state.loadingMessage).toBe('Loading data...');

    setGlobalLoading(false);

    state = useAppStore.getState();
    expect(state.globalLoading).toBe(false);
    expect(state.loadingMessage).toBeNull();
  });

  it('should set online status', () => {
    const { setOnlineStatus } = useAppStore.getState();

    setOnlineStatus(false);

    let state = useAppStore.getState();
    expect(state.isOnline).toBe(false);
    // Should also add a notification
    expect(state.notifications.length).toBeGreaterThan(0);

    setOnlineStatus(true);

    state = useAppStore.getState();
    expect(state.isOnline).toBe(true);
  });
});
