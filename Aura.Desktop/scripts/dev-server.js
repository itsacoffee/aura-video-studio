#!/usr/bin/env node

/**
 * Developer convenience script that runs the Aura backend build,
 * launches the Vite dev server for the renderer, and finally starts Electron.
 *
 * Usage:
 *   npm run dev:desktop
 */

const { spawn } = require("child_process");
const path = require("path");

const ROOT_DESKTOP_DIR = path.resolve(__dirname, "..");
const WEB_DIR = path.resolve(__dirname, "../../Aura.Web");
const API_PROJECT = path.resolve(__dirname, "../../Aura.Api/Aura.Api.csproj");
const DEV_SERVER_URL =
  process.env.AURA_VITE_DEV_SERVER_URL || "http://127.0.0.1:5173";
const VITE_SCRIPT = process.env.AURA_VITE_DEV_SCRIPT || "dev:desktop";
const DOTNET_CONFIGURATION = process.env.AURA_BACKEND_CONFIGURATION || "Debug";

async function main() {
  console.log("🔧 Preparing Aura development environment...");
  await runDotnetBuild();

  console.log("🚀 Starting Vite renderer dev server...");
  const viteProcess = startProcess(getNpmCommand(), ["run", VITE_SCRIPT], {
    cwd: WEB_DIR,
    stdio: "inherit",
  });

  await waitForDevServer(DEV_SERVER_URL);

  console.log("🪟 Launching Electron shell...");
  const electronEnv = {
    ...process.env,
    AURA_VITE_DEV_SERVER_URL: DEV_SERVER_URL,
  };

  const electronProcess = startProcess(
    getNpmCommand(),
    ["run", "dev"],
    {
      cwd: ROOT_DESKTOP_DIR,
      stdio: "inherit",
      env: electronEnv,
    }
  );

  setupCleanup([viteProcess, electronProcess]);

  electronProcess.on("exit", (code) => {
    console.log(`Electron exited with code ${code ?? 0}`);
    cleanupChildren([viteProcess]);
    process.exit(code ?? 0);
  });

  viteProcess.on("exit", (code) => {
    if (!electronProcess.killed) {
      console.warn(
        `Vite dev server exited unexpectedly (code ${code}). Stopping Electron...`
      );
      electronProcess.kill();
    }
  });
}

function runDotnetBuild() {
  return new Promise((resolve, reject) => {
    const args = [
      "build",
      API_PROJECT,
      "-c",
      DOTNET_CONFIGURATION,
      "--nologo",
    ];
    const dotnet = startProcess("dotnet", args, {
      cwd: ROOT_DESKTOP_DIR,
      stdio: "inherit",
    });

    dotnet.on("exit", (code) => {
      if (code === 0) {
        resolve();
      } else {
        reject(new Error(`dotnet build failed with exit code ${code}`));
      }
    });
  });
}

function waitForDevServer(url) {
  const maxAttempts = 50;
  const delayMs = 200;

  console.log(`⌛ Waiting for Vite dev server at ${url}...`);

  return new Promise((resolve, reject) => {
    let attempts = 0;

    const tryRequest = async () => {
      attempts += 1;
      try {
        const response = await fetch(url, { method: "GET" });
        if (response.ok) {
          console.log("✅ Vite dev server is ready\n");
          resolve(true);
          return;
        }
      } catch {
        // ignore until max attempts reached
      }

      if (attempts >= maxAttempts) {
        reject(new Error("Timed out waiting for Vite dev server to start"));
        return;
      }

      setTimeout(tryRequest, delayMs);
    };

    tryRequest();
  });
}

function startProcess(command, args, options) {
  const child = spawn(command, args, {
    shell: false,
    ...options,
  });

  child.on("error", (error) => {
    console.error(`Failed to start process ${command}:`, error);
  });

  return child;
}

function getNpmCommand() {
  return process.platform === "win32" ? "npm.cmd" : "npm";
}

function setupCleanup(children) {
  const handleExit = () => {
    cleanupChildren(children);
    process.exit();
  };

  process.on("SIGINT", handleExit);
  process.on("SIGTERM", handleExit);
}

function cleanupChildren(children) {
  children.forEach((child) => {
    if (child && !child.killed) {
      child.kill();
    }
  });
}

main().catch((error) => {
  console.error("❌ Aura desktop dev server failed:", error.message);
  cleanupChildren([]);
  process.exit(1);
});

