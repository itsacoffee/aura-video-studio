import { apiUrl } from '../config/api';

interface ProviderHealthSummary {
  totalProviders: number;
  healthyProviders: number;
  degradedProviders: number;
  offlineProviders: number;
  lastUpdateTime: string;
  providersByType: Record<string, ProviderTypeHealth>;
}

interface ProviderTypeHealth {
  total: number;
  healthy: number;
  degraded: number;
  offline: number;
}

interface ProviderHealth {
  providerName: string;
  isHealthy: boolean;
  lastCheckTime: string;
  responseTimeMs: number;
  consecutiveFailures: number;
  lastError?: string;
  successRate: number;
  averageResponseTimeMs: number;
}

interface NotificationState {
  lastSummary?: ProviderHealthSummary;
  lastProviders?: Record<string, ProviderHealth>;
  lastNotificationTime: Record<string, number>;
  mutedProviders: Set<string>;
}

const STORAGE_KEY = 'aura_health_notification_state';
const NOTIFICATION_COOLDOWN_MS = 5 * 60 * 1000; // 5 minutes
const POLL_INTERVAL_MS = 30 * 1000; // 30 seconds

class HealthNotificationService {
  private state: NotificationState;
  private pollInterval?: number;
  private isActive = false;
  private onNotificationCallback?: (message: string, type: 'success' | 'error') => void;

  constructor() {
    this.state = this.loadState();
  }

  private loadState(): NotificationState {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        const parsed = JSON.parse(stored);
        return {
          ...parsed,
          mutedProviders: new Set(parsed.mutedProviders || []),
        };
      }
    } catch (error) {
      console.error('Failed to load notification state:', error);
    }

    return {
      lastNotificationTime: {},
      mutedProviders: new Set(),
    };
  }

  private saveState(): void {
    try {
      const toSave = {
        ...this.state,
        mutedProviders: Array.from(this.state.mutedProviders),
      };
      localStorage.setItem(STORAGE_KEY, JSON.stringify(toSave));
    } catch (error) {
      console.error('Failed to save notification state:', error);
    }
  }

  /**
   * Start monitoring provider health and sending notifications
   */
  start(onNotification?: (message: string, type: 'success' | 'error') => void): void {
    if (this.isActive) return;

    this.isActive = true;
    this.onNotificationCallback = onNotification;

    // Initial check
    this.checkHealth();

    // Start polling
    this.pollInterval = window.setInterval(() => {
      this.checkHealth();
    }, POLL_INTERVAL_MS);
  }

  /**
   * Stop monitoring
   */
  stop(): void {
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
      this.pollInterval = undefined;
    }
    this.isActive = false;
  }

  /**
   * Mute notifications for a specific provider
   */
  muteProvider(providerName: string): void {
    this.state.mutedProviders.add(providerName);
    this.saveState();
  }

  /**
   * Unmute notifications for a specific provider
   */
  unmuteProvider(providerName: string): void {
    this.state.mutedProviders.delete(providerName);
    this.saveState();
  }

  /**
   * Check if a provider is muted
   */
  isProviderMuted(providerName: string): boolean {
    return this.state.mutedProviders.has(providerName);
  }

  private async checkHealth(): Promise<void> {
    if (!this.isActive) return;

    try {
      // Fetch current provider status
      const response = await fetch(`${apiUrl}/health/providers`);
      if (!response.ok) return;

      const providers: ProviderHealth[] = await response.json();
      const providersMap = Object.fromEntries(providers.map((p) => [p.providerName, p]));

      // Compare with previous state
      if (this.state.lastProviders) {
        for (const [name, current] of Object.entries(providersMap)) {
          const previous = this.state.lastProviders[name];

          // Skip if provider is muted
          if (this.state.mutedProviders.has(name)) continue;

          // Check for status changes
          if (previous) {
            this.detectStatusChange(name, previous, current);
          }
        }
      }

      // Update state
      this.state.lastProviders = providersMap;
      this.saveState();
    } catch (error) {
      console.error('Failed to check provider health:', error);
    }
  }

  private detectStatusChange(
    name: string,
    previous: ProviderHealth,
    current: ProviderHealth
  ): void {
    const wasHealthy = previous.consecutiveFailures < 3;
    const isHealthy = current.consecutiveFailures < 3;

    // Provider went offline
    if (wasHealthy && !isHealthy) {
      this.sendNotification(
        `Provider Offline: ${name}${current.lastError ? ` - ${current.lastError}` : ''}`,
        'error',
        name
      );
    }
    // Provider recovered
    else if (!wasHealthy && isHealthy) {
      this.sendNotification(`Provider Recovered: ${name}`, 'success', name);
    }
    // Provider degraded (was healthy, now having failures but not offline)
    else if (
      previous.consecutiveFailures === 0 &&
      current.consecutiveFailures > 0 &&
      current.consecutiveFailures < 3
    ) {
      this.sendNotification(
        `Provider Degraded: ${name} - ${current.consecutiveFailures} consecutive failures`,
        'error',
        name
      );
    }
  }

  private sendNotification(message: string, type: 'success' | 'error', providerName: string): void {
    // Check cooldown
    const lastNotification = this.state.lastNotificationTime[providerName] || 0;
    const now = Date.now();

    if (now - lastNotification < NOTIFICATION_COOLDOWN_MS) {
      return;
    }

    // Update last notification time
    this.state.lastNotificationTime[providerName] = now;
    this.saveState();

    // Send notification
    if (this.onNotificationCallback) {
      this.onNotificationCallback(message, type);
    }
  }
}

// Export singleton instance
export const healthNotificationService = new HealthNotificationService();
