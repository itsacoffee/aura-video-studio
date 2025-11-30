/**
 * OpenCut Communication Bridge
 *
 * Provides bi-directional communication between Aura Video Studio and the
 * embedded OpenCut editor via postMessage API.
 *
 * This bridge enables:
 * - Passing video projects from Aura to OpenCut for editing
 * - Receiving edited videos back from OpenCut
 * - Sharing state (current project, user preferences)
 * - Coordinating actions between the two applications
 */

import { loggingService as logger } from './loggingService';

/**
 * Message types for Aura-to-OpenCut communication
 */
export enum OpenCutMessageType {
  // Aura -> OpenCut
  LOAD_PROJECT = 'aura:load-project',
  LOAD_MEDIA = 'aura:load-media',
  SET_PREFERENCES = 'aura:set-preferences',
  REQUEST_EXPORT = 'aura:request-export',
  PING = 'aura:ping',

  // OpenCut -> Aura
  READY = 'opencut:ready',
  PROJECT_LOADED = 'opencut:project-loaded',
  EXPORT_COMPLETE = 'opencut:export-complete',
  EXPORT_PROGRESS = 'opencut:export-progress',
  ERROR = 'opencut:error',
  PONG = 'opencut:pong',
}

/**
 * Media item to pass to OpenCut
 */
export interface OpenCutMediaItem {
  id: string;
  type: 'video' | 'audio' | 'image';
  url: string;
  name: string;
  duration?: number;
  width?: number;
  height?: number;
  mimeType?: string;
}

/**
 * Project data to pass to OpenCut
 */
export interface OpenCutProject {
  id: string;
  name: string;
  media: OpenCutMediaItem[];
  timeline?: {
    duration: number;
    tracks: unknown[];
  };
}

/**
 * Export result from OpenCut
 */
export interface OpenCutExportResult {
  success: boolean;
  url?: string;
  blob?: Blob;
  error?: string;
  format?: string;
  width?: number;
  height?: number;
  duration?: number;
}

/**
 * Message structure for postMessage communication
 */
interface OpenCutMessage {
  type: OpenCutMessageType;
  payload?: unknown;
  requestId?: string;
  timestamp: number;
}

/**
 * Listener callback type
 */
type MessageListener = (payload: unknown, requestId?: string) => void;

/**
 * OpenCut Bridge class for managing communication with embedded OpenCut
 */
class OpenCutBridge {
  private iframe: HTMLIFrameElement | null = null;
  private targetOrigin: string = '*';
  private listeners: Map<OpenCutMessageType, Set<MessageListener>> = new Map();
  private pendingRequests: Map<
    string,
    { resolve: (value: unknown) => void; reject: (error: Error) => void; timeout: NodeJS.Timeout }
  > = new Map();
  private isConnected = false;
  private connectionTimeout: NodeJS.Timeout | null = null;
  private readonly REQUEST_TIMEOUT_MS = 30000;

  constructor() {
    this._handleMessage = this._handleMessage.bind(this);
  }

  /**
   * Initialize the bridge with an iframe element
   */
  initialize(iframe: HTMLIFrameElement, targetOrigin?: string): void {
    if (this.iframe === iframe) {
      return;
    }

    this.cleanup();

    this.iframe = iframe;
    if (targetOrigin) {
      this.targetOrigin = targetOrigin;
    }

    window.addEventListener('message', this._handleMessage);

    logger.info('OpenCut bridge initialized', 'services', 'opencutBridge', {
      targetOrigin: this.targetOrigin,
    });

    // Start connection handshake
    this._startConnectionHandshake();
  }

  /**
   * Clean up the bridge
   */
  cleanup(): void {
    window.removeEventListener('message', this._handleMessage);

    if (this.connectionTimeout) {
      clearTimeout(this.connectionTimeout);
      this.connectionTimeout = null;
    }

    // Reject all pending requests
    for (const [requestId, pending] of this.pendingRequests.entries()) {
      clearTimeout(pending.timeout);
      pending.reject(new Error('Bridge cleanup - request cancelled'));
      this.pendingRequests.delete(requestId);
    }

    this.iframe = null;
    this.isConnected = false;
    this.listeners.clear();

    logger.debug('OpenCut bridge cleaned up', 'services', 'opencutBridge');
  }

  /**
   * Check if the bridge is connected to OpenCut
   */
  get connected(): boolean {
    return this.isConnected;
  }

  /**
   * Subscribe to messages from OpenCut
   */
  on(type: OpenCutMessageType, listener: MessageListener): () => void {
    if (!this.listeners.has(type)) {
      this.listeners.set(type, new Set());
    }
    this.listeners.get(type)!.add(listener);

    // Return unsubscribe function
    return () => {
      const typeListeners = this.listeners.get(type);
      if (typeListeners) {
        typeListeners.delete(listener);
      }
    };
  }

  /**
   * Send a message to OpenCut
   */
  send(type: OpenCutMessageType, payload?: unknown): void {
    if (!this.iframe?.contentWindow) {
      logger.warn('Cannot send message - iframe not available', 'services', 'opencutBridge', {
        type,
      });
      return;
    }

    const message: OpenCutMessage = {
      type,
      payload,
      timestamp: Date.now(),
    };

    try {
      this.iframe.contentWindow.postMessage(message, this.targetOrigin);
      logger.debug('Message sent to OpenCut', 'services', 'opencutBridge', { type });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      logger.error('Failed to send message to OpenCut', errorObj, 'services', 'opencutBridge', {
        type,
      });
    }
  }

  /**
   * Send a message and wait for a response
   */
  async sendAndWait<T = unknown>(
    type: OpenCutMessageType,
    payload?: unknown,
    timeoutMs: number = this.REQUEST_TIMEOUT_MS
  ): Promise<T> {
    const requestId = `req-${Date.now()}-${Math.random().toString(36).slice(2)}`;

    return new Promise<T>((resolve, reject) => {
      const timeout = setTimeout(() => {
        this.pendingRequests.delete(requestId);
        reject(new Error(`Request timed out after ${timeoutMs}ms`));
      }, timeoutMs);

      this.pendingRequests.set(requestId, {
        resolve: resolve as (value: unknown) => void,
        reject,
        timeout,
      });

      if (!this.iframe?.contentWindow) {
        this.pendingRequests.delete(requestId);
        clearTimeout(timeout);
        reject(new Error('iframe not available'));
        return;
      }

      const message: OpenCutMessage = {
        type,
        payload,
        requestId,
        timestamp: Date.now(),
      };

      try {
        this.iframe.contentWindow.postMessage(message, this.targetOrigin);
      } catch (error: unknown) {
        this.pendingRequests.delete(requestId);
        clearTimeout(timeout);
        const errorObj = error instanceof Error ? error : new Error(String(error));
        reject(errorObj);
      }
    });
  }

  /**
   * Load a project into OpenCut
   */
  async loadProject(project: OpenCutProject): Promise<boolean> {
    if (!this.isConnected) {
      throw new Error('OpenCut bridge not connected');
    }

    const result = await this.sendAndWait<{ success: boolean }>(
      OpenCutMessageType.LOAD_PROJECT,
      project
    );
    return result.success;
  }

  /**
   * Load media items into OpenCut
   */
  async loadMedia(media: OpenCutMediaItem[]): Promise<boolean> {
    if (!this.isConnected) {
      throw new Error('OpenCut bridge not connected');
    }

    const result = await this.sendAndWait<{ success: boolean }>(
      OpenCutMessageType.LOAD_MEDIA,
      media
    );
    return result.success;
  }

  /**
   * Request an export from OpenCut
   */
  async requestExport(options?: {
    format?: string;
    quality?: string;
    resolution?: { width: number; height: number };
  }): Promise<OpenCutExportResult> {
    if (!this.isConnected) {
      throw new Error('OpenCut bridge not connected');
    }

    return this.sendAndWait<OpenCutExportResult>(
      OpenCutMessageType.REQUEST_EXPORT,
      options,
      60000 // Longer timeout for exports
    );
  }

  /**
   * Ping OpenCut to check connection
   */
  async ping(): Promise<boolean> {
    try {
      const result = await this.sendAndWait<{ pong: boolean }>(OpenCutMessageType.PING, null, 5000);
      return result.pong === true;
    } catch {
      return false;
    }
  }

  /**
   * Start the connection handshake with OpenCut
   */
  private _startConnectionHandshake(): void {
    // Send periodic pings until we get a response
    const attemptConnection = (): void => {
      if (this.isConnected) return;

      this.send(OpenCutMessageType.PING);

      // Retry after delay
      this.connectionTimeout = setTimeout(() => {
        if (!this.isConnected && this.iframe) {
          attemptConnection();
        }
      }, 2000);
    };

    // Start after a short delay to let iframe load
    setTimeout(attemptConnection, 500);
  }

  /**
   * Handle incoming messages from OpenCut
   */
  private _handleMessage(event: MessageEvent): void {
    // Validate origin and message structure
    if (!this._isValidMessage(event)) {
      return;
    }

    const data = event.data as OpenCutMessage;
    logger.debug('Message received from OpenCut', 'services', 'opencutBridge', { type: data.type });

    // Handle connection establishment
    this._handleConnectionMessage(data);

    // Handle pending request responses
    if (this._handlePendingRequest(data)) {
      return;
    }

    // Notify listeners
    this._notifyListeners(data);
  }

  /**
   * Validate if the incoming message is valid
   */
  private _isValidMessage(event: MessageEvent): boolean {
    // Validate origin if we have a specific target origin
    if (this.targetOrigin !== '*' && event.origin !== this.targetOrigin) {
      return false;
    }

    const data = event.data as OpenCutMessage;

    // Validate message structure
    if (!data || typeof data.type !== 'string' || !data.type.startsWith('opencut:')) {
      return false;
    }

    return true;
  }

  /**
   * Handle connection establishment messages
   */
  private _handleConnectionMessage(data: OpenCutMessage): void {
    if (data.type === OpenCutMessageType.READY || data.type === OpenCutMessageType.PONG) {
      if (!this.isConnected) {
        this.isConnected = true;
        if (this.connectionTimeout) {
          clearTimeout(this.connectionTimeout);
          this.connectionTimeout = null;
        }
        logger.info('OpenCut bridge connected', 'services', 'opencutBridge');
      }
    }
  }

  /**
   * Handle pending request responses
   * @returns true if the message was handled as a request response
   */
  private _handlePendingRequest(data: OpenCutMessage): boolean {
    if (!data.requestId || !this.pendingRequests.has(data.requestId)) {
      return false;
    }

    const pending = this.pendingRequests.get(data.requestId)!;
    this.pendingRequests.delete(data.requestId);
    clearTimeout(pending.timeout);

    if (data.type === OpenCutMessageType.ERROR) {
      pending.reject(new Error((data.payload as { message?: string })?.message ?? 'Unknown error'));
    } else {
      pending.resolve(data.payload);
    }
    return true;
  }

  /**
   * Notify listeners of the message
   */
  private _notifyListeners(data: OpenCutMessage): void {
    const typeListeners = this.listeners.get(data.type as OpenCutMessageType);
    if (!typeListeners) {
      return;
    }

    for (const listener of typeListeners) {
      try {
        listener(data.payload, data.requestId);
      } catch (error: unknown) {
        const errorObj = error instanceof Error ? error : new Error(String(error));
        logger.error('Listener error', errorObj, 'services', 'opencutBridge', { type: data.type });
      }
    }
  }
}

// Export singleton instance
export const opencutBridge = new OpenCutBridge();

// Export class for testing
export { OpenCutBridge };
