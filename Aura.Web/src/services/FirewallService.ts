/**
 * FirewallService - Manages Windows Firewall configuration for backend
 * This service handles firewall rule creation and detection
 *
 * Note: This is a stub implementation for PR 5 testing.
 * Full implementation will come from PR 4: Automatic Windows Firewall Configuration
 */

export interface FirewallRuleInfo {
  name: string;
  enabled: boolean;
  direction: 'inbound' | 'outbound';
  action: 'allow' | 'block';
  program: string;
}

export interface FirewallCheckResult {
  ruleExists: boolean;
  ruleInfo?: FirewallRuleInfo;
  requiresElevation: boolean;
}

export class FirewallService {
  private mockRuleExists: boolean = false;

  /**
   * Check if firewall rule exists for the backend executable
   */
  async checkFirewallRule(executablePath: string): Promise<boolean> {
    // eslint-disable-next-line no-console
    console.log(`[FirewallService] Checking firewall rule for: ${executablePath}`);

    if (process.platform !== 'win32') {
      // eslint-disable-next-line no-console
      console.log('[FirewallService] Not on Windows, firewall check not applicable');
      return true;
    }

    await this.simulateFirewallCheck();
    return this.mockRuleExists;
  }

  /**
   * Check detailed firewall status
   */
  async checkFirewallStatus(executablePath: string): Promise<FirewallCheckResult> {
    const ruleExists = await this.checkFirewallRule(executablePath);

    if (!ruleExists) {
      return {
        ruleExists: false,
        requiresElevation: true,
      };
    }

    return {
      ruleExists: true,
      requiresElevation: false,
      ruleInfo: {
        name: 'Aura Video Studio Backend',
        enabled: true,
        direction: 'inbound',
        action: 'allow',
        program: executablePath,
      },
    };
  }

  /**
   * Create firewall rule (requires UAC elevation on Windows)
   */
  async createFirewallRule(executablePath: string, _ruleName?: string): Promise<boolean> {
    // eslint-disable-next-line no-console
    console.log(`[FirewallService] Creating firewall rule for: ${executablePath}`);

    if (process.platform !== 'win32') {
      // eslint-disable-next-line no-console
      console.log('[FirewallService] Not on Windows, skipping firewall rule creation');
      return true;
    }

    try {
      await this.simulateRuleCreation();
      this.mockRuleExists = true;
      return true;
    } catch (error) {
      console.error('[FirewallService] Failed to create firewall rule:', error);
      return false;
    }
  }

  /**
   * Remove firewall rule
   */
  async removeFirewallRule(ruleName: string): Promise<boolean> {
    // eslint-disable-next-line no-console
    console.log(`[FirewallService] Removing firewall rule: ${ruleName}`);

    await this.simulateRuleRemoval();
    this.mockRuleExists = false;
    return true;
  }

  /**
   * Set mock rule state (for testing)
   */
  setMockRuleExists(exists: boolean): void {
    this.mockRuleExists = exists;
  }

  /**
   * Simulate firewall check delay
   */
  private async simulateFirewallCheck(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 50));
  }

  /**
   * Simulate rule creation delay
   */
  private async simulateRuleCreation(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 100));
  }

  /**
   * Simulate rule removal delay
   */
  private async simulateRuleRemoval(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
}
