/**
 * Lightweight stand-in for scenarios where the backend is hosted outside of Electron.
 * Provides the minimal surface area expected by existing orchestration code.
 */
class ExternalBackendService {
  constructor(networkContract) {
    // Enforce contract validation
    if (!networkContract) {
      throw new Error(
        "ExternalBackendService requires a valid networkContract. " +
        "Contract must be resolved via resolveBackendContract() before initializing ExternalBackendService."
      );
    }

    if (!networkContract.baseUrl || typeof networkContract.baseUrl !== "string") {
      throw new Error(
        "ExternalBackendService networkContract missing baseUrl. " +
        "Set AURA_BACKEND_URL or ASPNETCORE_URLS environment variable."
      );
    }

    if (!networkContract.port || typeof networkContract.port !== "number" || networkContract.port <= 0) {
      throw new Error(
        "ExternalBackendService networkContract missing valid port. " +
        "Set AURA_BACKEND_URL or ASPNETCORE_URLS environment variable."
      );
    }

    this.networkContract = networkContract;
    this.port = networkContract.port;
    this.baseUrl = networkContract.baseUrl;
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
