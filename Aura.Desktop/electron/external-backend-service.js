/**
 * Lightweight stand-in for scenarios where the backend is hosted outside of Electron.
 * Provides the minimal surface area expected by existing orchestration code.
 */
class ExternalBackendService {
  constructor(networkContract) {
    this.networkContract = networkContract;
    this.port = networkContract?.port ?? null;
    this.baseUrl = networkContract?.baseUrl ?? null;
    this.pid = null;
  }

  async start() {
    return this.port;
  }

  async stop() {
    // No-op – external backend lifecycle is managed elsewhere
    return true;
  }

  async restart() {
    throw new Error(
      "Backend restart is not supported when Electron is not managing the backend process."
    );
  }

  isRunning() {
    return true;
  }

  getPort() {
    return this.port;
  }

  getUrl() {
    return this.baseUrl;
  }

  async checkFirewallCompatibility() {
    return {
      compatible: true,
      message:
        "Firewall checks are not available for externally hosted backends.",
    };
  }

  async getFirewallRuleStatus() {
    return { exists: null, error: "Not supported for external backend" };
  }

  getFirewallRuleCommand() {
    return null;
  }
}

module.exports = ExternalBackendService;
