/**
 * Network contract resolver
 * Establishes a single source of truth for how the Electron shell and backend communicate.
 */

const os = require("os");
const path = require("path");

const DEFAULT_DEV_BACKEND_URL =
  process.env.AURA_DEV_BACKEND_URL || "http://127.0.0.1:5272";
const DEFAULT_PROD_BACKEND_URL =
  process.env.AURA_PROD_BACKEND_URL || "http://127.0.0.1:5890";

/**
 * @typedef {Object} NetworkContract
 * @property {string} protocol - Protocol (http or https)
 * @property {string} host - Hostname (e.g., 127.0.0.1)
 * @property {number} port - Port number
 * @property {string} baseUrl - Fully qualified base URL (e.g., "http://127.0.0.1:5272")
 * @property {string} raw - Raw URL string from environment
 * @property {string} healthEndpoint - Health check path (default "/health/live")
 * @property {string} readinessEndpoint - Readiness check path (default "/health/ready")
 * @property {string} sseJobEventsTemplate - SSE job events path template (default "/api/jobs/{id}/events")
 * @property {boolean} shouldSelfHost - Whether Electron should spawn the backend process
 * @property {number} maxStartupMs - Startup timeout in milliseconds
 * @property {number} pollIntervalMs - Health check poll interval in milliseconds
 */

/**
 * Resolve the backend connection contract that every layer must follow.
 * @param {{ isDev: boolean }} options
 * @returns {NetworkContract}
 * @throws {Error} If the contract cannot be resolved or is invalid
 */
function resolveBackendContract({ isDev }) {
  const urlCandidates = [
    process.env.AURA_BACKEND_URL,
    process.env.AURA_API_URL,
    process.env.ASPNETCORE_URLS,
    process.env.ASPNETCORE_URL,
    process.env.AURA_BACKEND_ORIGIN,
  ].filter(Boolean);

  const rawBaseUrl =
    urlCandidates[0] ||
    (isDev ? DEFAULT_DEV_BACKEND_URL : DEFAULT_PROD_BACKEND_URL);

  let parsed;
  try {
    parsed = new URL(rawBaseUrl);
  } catch (error) {
    throw new Error(
      `Invalid backend base URL "${rawBaseUrl}". Set AURA_BACKEND_URL or ASPNETCORE_URLS. ${error.message}`
    );
  }

  const protocol = parsed.protocol.replace(":", "");
  const port = parsed.port
    ? Number(parsed.port)
    : protocol === "https"
    ? 443
    : 80;

  const healthEndpoint =
    process.env.AURA_BACKEND_HEALTH_ENDPOINT || "/health/live";
  const readinessEndpoint =
    process.env.AURA_BACKEND_READY_ENDPOINT || "/health/ready";
  const sseJobEventsTemplate =
    process.env.AURA_BACKEND_SSE_JOB_EVENTS_TEMPLATE || "/api/jobs/{id}/events";
  const shouldSelfHost =
    (process.env.AURA_LAUNCH_BACKEND ?? "true").toLowerCase() !== "false";

  const baseUrl = `${parsed.protocol}//${parsed.hostname}${
    parsed.port ? `:${parsed.port}` : ""
  }`;

  // Runtime validation: ensure contract is complete
  if (!baseUrl || typeof baseUrl !== "string" || baseUrl.trim() === "") {
    throw new Error(
      "NetworkContract validation failed: baseUrl is required and must be a non-empty string"
    );
  }

  if (!port || typeof port !== "number" || port <= 0 || port > 65535) {
    throw new Error(
      `NetworkContract validation failed: port must be a valid integer between 1 and 65535 (got: ${port})`
    );
  }

  if (!healthEndpoint || typeof healthEndpoint !== "string") {
    throw new Error(
      "NetworkContract validation failed: healthEndpoint must be a non-empty string"
    );
  }

  if (!readinessEndpoint || typeof readinessEndpoint !== "string") {
    throw new Error(
      "NetworkContract validation failed: readinessEndpoint must be a non-empty string"
    );
  }

  return {
    protocol,
    host: parsed.hostname,
    port,
    baseUrl,
    raw: rawBaseUrl,
    healthEndpoint,
    readinessEndpoint,
    sseJobEventsTemplate,
    shouldSelfHost,
    maxStartupMs: Number(process.env.AURA_BACKEND_STARTUP_TIMEOUT_MS || 60000),
    pollIntervalMs: Number(
      process.env.AURA_BACKEND_HEALTH_POLL_INTERVAL_MS || 1000
    ),
  };
}

/**
 * Build runtime diagnostic info that can be shared with the renderer.
 */
function buildRuntimeDiagnostics(
  app,
  backendService,
  contract,
  overrides = {}
) {
  const backendUrl = backendService?.getUrl?.() || contract.baseUrl;
  const backendPort = backendService?.getPort?.() || contract.port;

  return {
    backend: {
      baseUrl: backendUrl,
      port: backendPort,
      protocol: contract.protocol,
      managedByElectron: contract.shouldSelfHost,
      healthEndpoint: contract.healthEndpoint,
      readinessEndpoint: contract.readinessEndpoint,
      sseJobEventsTemplate: contract.sseJobEventsTemplate,
      pid: backendService?.pid ?? null,
    },
    environment: {
      mode: app.isPackaged ? "production" : "development",
      isPackaged: app.isPackaged,
      version: app.getVersion(),
    },
    os: {
      platform: os.platform(),
      release: os.release(),
      arch: os.arch(),
      hostname: os.hostname(),
    },
    paths: {
      userData: app.getPath("userData"),
      temp: app.getPath("temp"),
      logs: path.join(app.getPath("userData"), "logs"),
    },
    ...overrides,
  };
}

module.exports = {
  resolveBackendContract,
  buildRuntimeDiagnostics,
};
