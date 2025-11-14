/**
 * Global SSE Connection Manager
 *
 * Manages all active SSE connections and provides a centralized way to close them
 * on application shutdown or when needed.
 */

import { loggingService } from '@/services/loggingService';

interface ManagedConnection {
  id: string;
  client: { close: () => void };
  createdAt: Date;
}

class SseConnectionManager {
  private connections: Map<string, ManagedConnection> = new Map();
  private isShuttingDown = false;

  /**
   * Register an SSE connection for management
   */
  register(id: string, client: { close: () => void }): void {
    if (this.isShuttingDown) {
      loggingService.warn(
        'Cannot register connection during shutdown',
        'SseConnectionManager',
        'register'
      );
      client.close();
      return;
    }

    this.connections.set(id, {
      id,
      client,
      createdAt: new Date(),
    });

    loggingService.debug('SSE connection registered', 'SseConnectionManager', 'register', {
      id,
      totalConnections: this.connections.size,
    });
  }

  /**
   * Unregister an SSE connection
   */
  unregister(id: string): void {
    const removed = this.connections.delete(id);

    if (removed) {
      loggingService.debug('SSE connection unregistered', 'SseConnectionManager', 'unregister', {
        id,
        totalConnections: this.connections.size,
      });
    }
  }

  /**
   * Close a specific connection
   */
  close(id: string): void {
    const connection = this.connections.get(id);

    if (connection) {
      try {
        connection.client.close();
        loggingService.info('SSE connection closed', 'SseConnectionManager', 'close', { id });
      } catch (error) {
        loggingService.error(
          'Error closing SSE connection',
          error as Error,
          'SseConnectionManager',
          'close',
          { id }
        );
      } finally {
        this.connections.delete(id);
      }
    }
  }

  /**
   * Close all active SSE connections
   */
  closeAll(): void {
    this.isShuttingDown = true;

    loggingService.info('Closing all SSE connections', 'SseConnectionManager', 'closeAll', {
      count: this.connections.size,
    });

    const closePromises = Array.from(this.connections.values()).map(async (connection) => {
      try {
        connection.client.close();
        loggingService.debug('SSE connection closed', 'SseConnectionManager', 'closeAll', {
          id: connection.id,
        });
      } catch (error) {
        loggingService.error(
          'Error closing SSE connection',
          error as Error,
          'SseConnectionManager',
          'closeAll',
          {
            id: connection.id,
          }
        );
      }
    });

    Promise.allSettled(closePromises).then(() => {
      this.connections.clear();
      loggingService.info('All SSE connections closed', 'SseConnectionManager', 'closeAll');
    });
  }

  /**
   * Get the number of active connections
   */
  getConnectionCount(): number {
    return this.connections.size;
  }

  /**
   * Get list of active connection IDs
   */
  getConnectionIds(): string[] {
    return Array.from(this.connections.keys());
  }

  /**
   * Check if shutting down
   */
  isShutdown(): boolean {
    return this.isShuttingDown;
  }
}

// Export singleton instance
export const sseConnectionManager = new SseConnectionManager();

// Cleanup on window unload
if (typeof window !== 'undefined') {
  window.addEventListener('beforeunload', () => {
    sseConnectionManager.closeAll();
  });
}
