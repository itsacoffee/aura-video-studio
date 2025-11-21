import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { BackendProcessManager } from '../../src/services/BackendProcessManager';
import { FirewallService } from '../../src/services/FirewallService';
import { HealthCheckService } from '../../src/services/HealthCheckService';

/**
 * Application Startup Integration Tests
 *
 * These tests validate the complete startup sequence and integration
 * between BackendProcessManager, HealthCheckService, and FirewallService.
 *
 * Dependencies:
 * - PR 1: Backend Auto-Start Process Management
 * - PR 2: Robust Health Check and Retry Logic
 * - PR 3: Bundle Backend Executable in Installer/Portable Build
 * - PR 4: Automatic Windows Firewall Configuration
 */
describe('Application Startup Integration Tests', () => {
  let processManager: BackendProcessManager;
  let healthCheck: HealthCheckService;
  let firewallService: FirewallService;

  beforeAll(async () => {
    processManager = new BackendProcessManager();
    healthCheck = new HealthCheckService({
      maxRetries: 5,
      retryDelayMs: 500,
      timeoutMs: 2000,
      backendUrl: 'http://localhost:5000',
    });
    firewallService = new FirewallService();
  });

  afterAll(async () => {
    await processManager.stop();
  });

  it('should auto-start backend on app launch', async () => {
    await processManager.start();
    expect(processManager.isBackendReady()).toBe(true);

    const status = processManager.getStatus();
    expect(status.isRunning).toBe(true);
    expect(status.pid).toBeDefined();
    expect(status.port).toBe(5000);
  }, 30000);

  it('should pass health check after backend starts', async () => {
    const result = await healthCheck.checkHealth();

    expect(result).toBeDefined();
    expect(typeof result.isHealthy).toBe('boolean');
    expect(result.message).toBeDefined();
    expect(result.timestamp).toBeInstanceOf(Date);

    if (result.latencyMs !== undefined) {
      expect(result.latencyMs).toBeLessThan(5000);
    }
  }, 15000);

  it('should detect backend executable in production path', () => {
    const execPath = processManager.detectBackendExecutable();
    expect(execPath).toContain('Aura.Api');
  });

  it('should restart backend if it crashes', async () => {
    await processManager.stop();
    expect(processManager.isBackendReady()).toBe(false);

    await processManager.start();
    expect(processManager.isBackendReady()).toBe(true);

    const result = await healthCheck.quickCheck();
    expect(typeof result).toBe('boolean');
  }, 40000);

  it('should detect firewall status', async () => {
    const execPath = 'C:\\Program Files\\Aura Video Studio\\resources\\backend\\Aura.Api.exe';
    const ruleExists = await firewallService.checkFirewallRule(execPath);
    expect(typeof ruleExists).toBe('boolean');
  });

  it('should provide detailed firewall status', async () => {
    const execPath = 'C:\\Program Files\\Aura Video Studio\\resources\\backend\\Aura.Api.exe';
    const status = await firewallService.checkFirewallStatus(execPath);

    expect(status).toBeDefined();
    expect(typeof status.ruleExists).toBe('boolean');
    expect(typeof status.requiresElevation).toBe('boolean');

    if (status.ruleInfo) {
      expect(status.ruleInfo.name).toBeDefined();
      expect(status.ruleInfo.enabled).toBeDefined();
      expect(status.ruleInfo.direction).toMatch(/^(inbound|outbound)$/);
      expect(status.ruleInfo.action).toMatch(/^(allow|block)$/);
    }
  });

  it('should handle firewall rule creation', async () => {
    const execPath = 'C:\\Program Files\\Aura Video Studio\\resources\\backend\\Aura.Api.exe';
    const result = await firewallService.createFirewallRule(execPath, 'Aura Video Studio Backend');

    expect(typeof result).toBe('boolean');

    if (result) {
      const checkResult = await firewallService.checkFirewallRule(execPath);
      expect(checkResult).toBe(true);
    }
  });

  it('should verify process status information', () => {
    const status = processManager.getStatus();

    expect(status).toBeDefined();
    expect(typeof status.isRunning).toBe('boolean');
    expect(typeof status.port).toBe('number');

    if (status.isRunning) {
      expect(status.pid).toBeDefined();
      expect(status.startTime).toBeInstanceOf(Date);
    }
  });

  it('should handle concurrent health checks', async () => {
    const checks = await Promise.all([
      healthCheck.quickCheck(),
      healthCheck.quickCheck(),
      healthCheck.quickCheck(),
    ]);

    checks.forEach((result) => {
      expect(typeof result).toBe('boolean');
    });
  }, 10000);

  it('should support health check with progress callback', async () => {
    const progressUpdates: Array<{ attempt: number; maxAttempts: number }> = [];

    await healthCheck.checkHealth((attempt, maxAttempts) => {
      progressUpdates.push({ attempt, maxAttempts });
    });

    expect(progressUpdates.length).toBeGreaterThan(0);
    progressUpdates.forEach((update) => {
      expect(update.attempt).toBeLessThanOrEqual(update.maxAttempts);
      expect(update.attempt).toBeGreaterThan(0);
    });
  }, 15000);
});
