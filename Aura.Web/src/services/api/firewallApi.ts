import { apiUrl } from '../../config/api';
import { post } from './apiClient';

export interface FirewallCheckResult {
  ruleExists: boolean;
}

export interface FirewallConfigResult {
  success: boolean;
  message: string;
}

/**
 * Check if firewall rule exists for the given executable
 */
export async function checkFirewallRule(executablePath: string): Promise<boolean> {
  try {
    const response = await post<FirewallCheckResult>(
      `${apiUrl}/system/firewall/check`,
      null,
      {
        params: { executablePath },
        timeout: 5000
      }
    );
    return response.ruleExists;
  } catch (error: unknown) {
    console.error('[FirewallAPI] Failed to check firewall rule:', error);
    return false;
  }
}

/**
 * Add firewall rule for the backend executable
 */
export async function addFirewallRule(
  executablePath: string, 
  includePublic: boolean = false
): Promise<FirewallConfigResult> {
  try {
    const response = await post<{ message: string }>(
      `${apiUrl}/system/firewall/add`,
      null,
      {
        params: { executablePath, includePublic },
        timeout: 10000
      }
    );
    return {
      success: true,
      message: response.message
    };
  } catch (error: unknown) {
    const errorMessage = error instanceof Error 
      ? error.message 
      : 'Failed to add firewall rule';
    return {
      success: false,
      message: errorMessage
    };
  }
}
