/**
 * useAIScriptContextMenu - Custom hook for AI script context menu interactions
 *
 * Provides a React-friendly interface for triggering context menus on AI-generated
 * script scenes and listening for context menu action callbacks.
 */

import { useCallback } from 'react';
import type { AIScriptMenuData } from '../types/electron-context-menu';
import { useContextMenu, useContextMenuAction } from './useContextMenu';

export interface AIScriptContextMenuCallbacks {
  onRegenerate: (sceneIndex: number) => void;
  onExpand: (sceneIndex: number) => void;
  onShorten: (sceneIndex: number) => void;
  onGenerateBRoll: (sceneIndex: number) => void;
  onCopyText: (text: string) => void;
}

/**
 * Hook to integrate context menu functionality for AI-generated script scenes
 *
 * @param callbacks - Object containing callback functions for each context menu action
 * @returns A function to call with the mouse event and scene data to show the context menu
 */
export function useAIScriptContextMenu(callbacks: AIScriptContextMenuCallbacks) {
  const { onRegenerate, onExpand, onShorten, onGenerateBRoll, onCopyText } = callbacks;

  const showContextMenu = useContextMenu<AIScriptMenuData>('ai-script');

  const handleContextMenu = useCallback(
    (e: React.MouseEvent, sceneIndex: number, sceneText: string, jobId: string) => {
      const menuData: AIScriptMenuData = {
        sceneIndex,
        sceneText,
        jobId,
      };
      showContextMenu(e, menuData);
    },
    [showContextMenu]
  );

  // Register action handlers for context menu callbacks
  useContextMenuAction<AIScriptMenuData>(
    'ai-script',
    'onRegenerate',
    useCallback(
      (data: AIScriptMenuData) => {
        console.info('Regenerating scene:', data.sceneIndex);
        onRegenerate(data.sceneIndex);
      },
      [onRegenerate]
    )
  );

  useContextMenuAction<AIScriptMenuData>(
    'ai-script',
    'onExpand',
    useCallback(
      (data: AIScriptMenuData) => {
        console.info('Expanding scene:', data.sceneIndex);
        onExpand(data.sceneIndex);
      },
      [onExpand]
    )
  );

  useContextMenuAction<AIScriptMenuData>(
    'ai-script',
    'onShorten',
    useCallback(
      (data: AIScriptMenuData) => {
        console.info('Shortening scene:', data.sceneIndex);
        onShorten(data.sceneIndex);
      },
      [onShorten]
    )
  );

  useContextMenuAction<AIScriptMenuData>(
    'ai-script',
    'onGenerateBRoll',
    useCallback(
      (data: AIScriptMenuData) => {
        console.info('Generating B-Roll suggestions for scene:', data.sceneIndex);
        onGenerateBRoll(data.sceneIndex);
      },
      [onGenerateBRoll]
    )
  );

  useContextMenuAction<AIScriptMenuData>(
    'ai-script',
    'onCopyText',
    useCallback(
      (data: AIScriptMenuData) => {
        console.info('Copying scene text to clipboard');
        onCopyText(data.sceneText);
        navigator.clipboard.writeText(data.sceneText).catch((err: unknown) => {
          const errorMessage = err instanceof Error ? err.message : String(err);
          console.error('Failed to copy text to clipboard:', errorMessage);
        });
      },
      [onCopyText]
    )
  );

  return handleContextMenu;
}
