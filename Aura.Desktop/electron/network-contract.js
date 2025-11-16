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
 * Resolve the backend connection contract that every layer must follow.
 * @param {{ isDev: boolean }} options
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
    process.env.AURA_BACKEND_HEALTH_ENDPOINT || "/api/health";
  const readinessEndpoint =
    process.env.AURA_BACKEND_READY_ENDPOINT || "/health/ready";
  const shouldSelfHost =
    (process.env.AURA_LAUNCH_BACKEND ?? "true").toLowerCase() !== "false";

  return {
    protocol,
    host: parsed.hostname,
    port,
    baseUrl: `${parsed.protocol}//${parsed.hostname}${
      parsed.port ? `:${parsed.port}` : ""
    }`,
    raw: rawBaseUrl,
    healthEndpoint,
    readinessEndpoint,
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
