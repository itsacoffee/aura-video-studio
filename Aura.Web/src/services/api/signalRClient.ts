/**
 * SignalR Client for Real-time Communication
 * Provides connection management, automatic reconnection, and event handling
 */

import * as signalR from '@microsoft/signalr';
import { loggingService } from '../loggingService';
import { env } from '@/config/env';

export type ConnectionState = 'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting' | 'Disconnected';

export interface SignalRConfig {
  hubUrl: string;
  automaticReconnect?: boolean;
  reconnectDelays?: number[];
  logging?: signalR.LogLevel;
}

/**
 * SignalR Client wrapper with automatic reconnection
 */
export class SignalRClient {
  private connection: signalR.HubConnection | null = null;
  private config: Required<SignalRConfig>;
  private eventHandlers: Map<string, Set<(...args: unknown[]) => void>> = new Map();
  private connectionStateHandlers: Set<(state: ConnectionState) => void> = new Set();
  private isManualDisconnect = false;

  constructor(config: SignalRConfig) {
    this.config = {
      hubUrl: config.hubUrl,
      automaticReconnect: config.automaticReconnect ?? true,
      reconnectDelays: config.reconnectDelays ?? [0, 2000, 5000, 10000, 30000],
      logging: config.logging ?? signalR.LogLevel.Information,
    };
  }

  /**
   * Initialize and start the connection
   */
  async connect(): Promise<void> {
    if (this.connection) {
      loggingService.warn('SignalR connection already exists', 'signalRClient', 'connect');
      return;
    }

    try {
      this.isManualDisconnect = false;

      // Build connection
      const connectionBuilder = new signalR.HubConnectionBuilder()
        .withUrl(`${env.apiBaseUrl}${this.config.hubUrl}`, {
          accessTokenFactory: () => {
            // Get auth token from localStorage
            return localStorage.getItem('auth_token') || '';
          },
          // Add additional options for better connection handling
          skipNegotiation: false,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents,
        })
        .configureLogging(this.config.logging);

      // Add automatic reconnect if enabled
      if (this.config.automaticReconnect) {
        connectionBuilder.withAutomaticReconnect(this.config.reconnectDelays);
      }

      this.connection = connectionBuilder.build();

      // Set up event handlers
      this.setupConnectionHandlers();

      // Register all event handlers
      this.reregisterEventHandlers();

      // Start connection
      loggingService.info('Starting SignalR connection', 'signalRClient', 'connect', {
        hubUrl: this.config.hubUrl,
      });

      await this.connection.start();

      loggingService.info('SignalR connection established', 'signalRClient', 'connect');
      this.notifyStateChange('Connected');
    } catch (error) {
      loggingService.error(
        'Failed to start SignalR connection',
        error instanceof Error ? error : new Error(String(error)),
        'signalRClient',
        'connect'
      );
      this.notifyStateChange('Disconnected');
      throw error;
    }
  }

  /**
   * Set up connection event handlers
   */
  private setupConnectionHandlers(): void {
    if (!this.connection) return;

    this.connection.onclose((error) => {
      loggingService.warn('SignalR connection closed', 'signalRClient', 'onclose', {
        error: error?.message,
      });
      this.notifyStateChange('Disconnected');

      // Attempt reconnect if not manual disconnect
      if (!this.isManualDisconnect && this.config.automaticReconnect) {
        this.attemptReconnect();
      }
    });

    this.connection.onreconnecting((error) => {
      loggingService.info('SignalR reconnecting', 'signalRClient', 'onreconnecting', {
        error: error?.message,
      });
      this.notifyStateChange('Reconnecting');
    });

    this.connection.onreconnected((connectionId) => {
      loggingService.info('SignalR reconnected', 'signalRClient', 'onreconnected', {
        connectionId,
      });
      this.notifyStateChange('Connected');
    });
  }

  /**
   * Attempt to reconnect manually
   */
  private async attemptReconnect(): Promise<void> {
    try {
      loggingService.info('Attempting manual reconnect', 'signalRClient', 'attemptReconnect');
      await this.connect();
    } catch (error) {
      loggingService.error(
        'Manual reconnect failed',
        error instanceof Error ? error : new Error(String(error)),
        'signalRClient',
        'attemptReconnect'
      );
    }
  }

  /**
   * Re-register all event handlers after reconnection
   */
  private reregisterEventHandlers(): void {
    if (!this.connection) return;

    this.eventHandlers.forEach((handlers, eventName) => {
      handlers.forEach((handler) => {
        this.connection!.on(eventName, handler);
      });
    });
  }

  /**
   * Disconnect from the hub
   */
  async disconnect(): Promise<void> {
    if (!this.connection) {
      loggingService.warn('No active connection to disconnect', 'signalRClient', 'disconnect');
      return;
    }

    try {
      this.isManualDisconnect = true;
      loggingService.info('Disconnecting from SignalR', 'signalRClient', 'disconnect');
      
      await this.connection.stop();
      this.connection = null;
      
      loggingService.info('Disconnected from SignalR', 'signalRClient', 'disconnect');
      this.notifyStateChange('Disconnected');
    } catch (error) {
      loggingService.error(
        'Error disconnecting from SignalR',
        error instanceof Error ? error : new Error(String(error)),
        'signalRClient',
        'disconnect'
      );
      throw error;
    }
  }

  /**
   * Subscribe to hub events
   */
  on(eventName: string, handler: (...args: unknown[]) => void): void {
    // Store handler for re-registration
    if (!this.eventHandlers.has(eventName)) {
      this.eventHandlers.set(eventName, new Set());
    }
    this.eventHandlers.get(eventName)!.add(handler);

    // Register with connection if it exists
    if (this.connection) {
      this.connection.on(eventName, handler);
    }

    loggingService.debug('Subscribed to SignalR event', 'signalRClient', 'on', { eventName });
  }

  /**
   * Unsubscribe from hub events
   */
  off(eventName: string, handler: (...args: unknown[]) => void): void {
    // Remove from stored handlers
    const handlers = this.eventHandlers.get(eventName);
    if (handlers) {
      handlers.delete(handler);
      if (handlers.size === 0) {
        this.eventHandlers.delete(eventName);
      }
    }

    // Unregister from connection if it exists
    if (this.connection) {
      this.connection.off(eventName, handler);
    }

    loggingService.debug('Unsubscribed from SignalR event', 'signalRClient', 'off', { eventName });
  }

  /**
   * Invoke a hub method
   */
  async invoke<T = unknown>(methodName: string, ...args: unknown[]): Promise<T> {
    if (!this.connection) {
      throw new Error('No active SignalR connection');
    }

    if (this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('SignalR connection is not in Connected state');
    }

    try {
      loggingService.debug('Invoking SignalR method', 'signalRClient', 'invoke', {
        methodName,
        args,
      });

      const result = await this.connection.invoke<T>(methodName, ...args);

      loggingService.debug('SignalR method invoked successfully', 'signalRClient', 'invoke', {
        methodName,
      });

      return result;
    } catch (error) {
      loggingService.error(
        'Failed to invoke SignalR method',
        error instanceof Error ? error : new Error(String(error)),
        'signalRClient',
        'invoke',
        { methodName }
      );
      throw error;
    }
  }

  /**
   * Send a message without waiting for a response
   */
  async send(methodName: string, ...args: unknown[]): Promise<void> {
    if (!this.connection) {
      throw new Error('No active SignalR connection');
    }

    if (this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('SignalR connection is not in Connected state');
    }

    try {
      loggingService.debug('Sending SignalR message', 'signalRClient', 'send', {
        methodName,
        args,
      });

      await this.connection.send(methodName, ...args);

      loggingService.debug('SignalR message sent successfully', 'signalRClient', 'send', {
        methodName,
      });
    } catch (error) {
      loggingService.error(
        'Failed to send SignalR message',
        error instanceof Error ? error : new Error(String(error)),
        'signalRClient',
        'send',
        { methodName }
      );
      throw error;
    }
  }

  /**
   * Get current connection state
   */
  getState(): ConnectionState {
    if (!this.connection) {
      return 'Disconnected';
    }

    switch (this.connection.state) {
      case signalR.HubConnectionState.Connected:
        return 'Connected';
      case signalR.HubConnectionState.Connecting:
        return 'Connecting';
      case signalR.HubConnectionState.Reconnecting:
        return 'Reconnecting';
      case signalR.HubConnectionState.Disconnected:
        return 'Disconnected';
      default:
        return 'Disconnected';
    }
  }

  /**
   * Check if connected
   */
  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  /**
   * Subscribe to connection state changes
   */
  onStateChange(handler: (state: ConnectionState) => void): void {
    this.connectionStateHandlers.add(handler);
  }

  /**
   * Unsubscribe from connection state changes
   */
  offStateChange(handler: (state: ConnectionState) => void): void {
    this.connectionStateHandlers.delete(handler);
  }

  /**
   * Notify all state change handlers
   */
  private notifyStateChange(state: ConnectionState): void {
    this.connectionStateHandlers.forEach((handler) => {
      try {
        handler(state);
      } catch (error) {
        loggingService.error(
          'Error in state change handler',
          error instanceof Error ? error : new Error(String(error)),
          'signalRClient',
          'notifyStateChange'
        );
      }
    });
  }
}

/**
 * Create a SignalR client for video generation progress
 */
export function createProgressHubClient(): SignalRClient {
  return new SignalRClient({
    hubUrl: '/hubs/generation-progress',
    automaticReconnect: true,
    logging: signalR.LogLevel.Information,
  });
}

/**
 * Create a SignalR client for notifications
 */
export function createNotificationHubClient(): SignalRClient {
  return new SignalRClient({
    hubUrl: '/hubs/notifications',
    automaticReconnect: true,
    logging: signalR.LogLevel.Information,
  });
}

/**
 * Singleton instances (lazy-loaded)
 */
let progressHubInstance: SignalRClient | null = null;
let notificationHubInstance: SignalRClient | null = null;

/**
 * Get or create progress hub client singleton
 */
export function getProgressHubClient(): SignalRClient {
  if (!progressHubInstance) {
    progressHubInstance = createProgressHubClient();
  }
  return progressHubInstance;
}

/**
 * Get or create notification hub client singleton
 */
export function getNotificationHubClient(): SignalRClient {
  if (!notificationHubInstance) {
    notificationHubInstance = createNotificationHubClient();
  }
  return notificationHubInstance;
}
