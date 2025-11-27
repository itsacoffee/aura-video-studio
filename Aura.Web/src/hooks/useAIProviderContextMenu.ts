/**
 * useAIProviderContextMenu - Custom hook for AI provider context menu interactions
 *
 * Provides a React-friendly interface for triggering context menus on AI provider
 * cards in the Settings page and listening for context menu action callbacks.
 */

import { useCallback } from 'react';
import type { AIProviderMenuData } from '../types/electron-context-menu';
import { useContextMenu, useContextMenuAction } from './useContextMenu';

export interface AIProviderContextMenuCallbacks {
  onTestConnection: (providerId: string) => void;
  onViewStats: (providerId: string) => void;
  onSetDefault: (providerId: string) => void;
  onConfigure: (providerId: string) => void;
}

export interface AIProviderForContextMenu {
  id: string;
  type: 'llm' | 'tts' | 'image';
  isDefault?: boolean;
  hasFallback?: boolean;
}

/**
 * Hook to integrate context menu functionality for AI providers
 *
 * @param onTestConnection - Callback to test provider connection
 * @param onViewStats - Callback to view provider statistics
 * @param onSetDefault - Callback to set provider as default
 * @param onConfigure - Callback to open provider configuration
 * @returns A function to call with the mouse event and provider data to show the context menu
 */
export function useAIProviderContextMenu(
  onTestConnection: (providerId: string) => void,
  onViewStats: (providerId: string) => void,
  onSetDefault: (providerId: string) => void,
  onConfigure: (providerId: string) => void
) {
  const showContextMenu = useContextMenu<AIProviderMenuData>('ai-provider');

  const handleContextMenu = useCallback(
    (e: React.MouseEvent, provider: AIProviderForContextMenu) => {
      const menuData: AIProviderMenuData = {
        providerId: provider.id,
        providerType: provider.type,
        isDefault: provider.isDefault || false,
        hasFallback: provider.hasFallback || false,
      };
      showContextMenu(e, menuData);
    },
    [showContextMenu]
  );

  // Register action handlers for context menu callbacks
  useContextMenuAction<AIProviderMenuData>(
    'ai-provider',
    'onTestConnection',
    useCallback(
      (data: AIProviderMenuData) => {
        console.info('Testing connection for provider:', data.providerId);
        onTestConnection(data.providerId);
      },
      [onTestConnection]
    )
  );

  useContextMenuAction<AIProviderMenuData>(
    'ai-provider',
    'onViewStats',
    useCallback(
      (data: AIProviderMenuData) => {
        console.info('Viewing stats for provider:', data.providerId);
        onViewStats(data.providerId);
      },
      [onViewStats]
    )
  );

  useContextMenuAction<AIProviderMenuData>(
    'ai-provider',
    'onSetDefault',
    useCallback(
      (data: AIProviderMenuData) => {
        console.info('Setting default provider:', data.providerId);
        onSetDefault(data.providerId);
      },
      [onSetDefault]
    )
  );

  useContextMenuAction<AIProviderMenuData>(
    'ai-provider',
    'onConfigure',
    useCallback(
      (data: AIProviderMenuData) => {
        console.info('Configuring provider:', data.providerId);
        onConfigure(data.providerId);
      },
      [onConfigure]
    )
  );

  return handleContextMenu;
}
