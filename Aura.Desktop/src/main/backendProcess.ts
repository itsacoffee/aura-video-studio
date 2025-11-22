import { spawn, ChildProcess } from 'child_process';
import path from 'path';
import { app } from 'electron';
import fs from 'fs';

export class BackendProcessManager {
  private backendProcess: ChildProcess | null = null;
  private readonly backendExecutableName = 'Aura.Api.exe';
  private readonly backendPort = 5000;
  private readonly maxStartupTime = 30000; // 30 seconds
  private readonly healthCheckInterval = 1000; // 1 second

  /**
   * Get the path to the backend executable
   * In production: process.resourcesPath/backend/win-x64/Aura.Api.exe
   * In development: ../Aura.Api/bin/Debug/net8.0/win-x64/Aura.Api.exe
   */
  private getBackendPath(): string {
    const isDev = !app.isPackaged;

    if (isDev) {
      // Development: relative to Aura.Desktop project root
      const devPath = path.join(
        app.getAppPath(),
        '..',
        'Aura.Api',
        'bin',
        'Debug',
        'net8.0',
        'win-x64',
        this.backendExecutableName
      );
      if (fs.existsSync(devPath)) {
        return devPath;
      }
      throw new Error(`Backend executable not found in development path: ${devPath}`);
    }

    // Production: bundled in resources/backend/win-x64/
    const productionPath = path.join(
      process.resourcesPath || app.getAppPath(),
      'backend',
      'win-x64',
      this.backendExecutableName
    );

    if (!fs.existsSync(productionPath)) {
      throw new Error(`Backend executable not found in production path: ${productionPath}`);
    }

    return productionPath;
  }

  /**
   * Start the backend process
   */
  public async start(): Promise<void> {
    if (this.backendProcess) {
      console.log('[BackendProcess] Backend is already running');
      return;
    }

    const backendPath = this.getBackendPath();
    const workingDir = path.dirname(backendPath);

    console.log('[BackendProcess] Starting backend...');
    console.log(`[BackendProcess] Executable: ${backendPath}`);
    console.log(`[BackendProcess] Working Directory: ${workingDir}`);

    // Environment variables for the backend
    const env = {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: app.isPackaged ? 'Production' : 'Development',
      ASPNETCORE_URLS: `http://localhost:${this.backendPort}`,
      DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: '1', // Avoid culture issues
    };

    // Spawn the backend process
    this.backendProcess = spawn(backendPath, [], {
      cwd: workingDir,
      env,
      stdio: ['ignore', 'pipe', 'pipe'], // Capture stdout/stderr
      windowsHide: true, // Don't show console window on Windows
    });

    // Log backend output
    this.backendProcess.stdout?.on('data', (data) => {
      console.log(`[Backend] ${data.toString().trim()}`);
    });

    this.backendProcess.stderr?.on('data', (data) => {
      console.error(`[Backend ERROR] ${data.toString().trim()}`);
    });

    // Handle backend exit
    this.backendProcess.on('exit', (code, signal) => {
      console.log(`[BackendProcess] Backend exited with code ${code}, signal ${signal}`);
      this.backendProcess = null;
    });

    this.backendProcess.on('error', (error) => {
      console.error('[BackendProcess] Failed to start backend:', error);
      this.backendProcess = null;
      throw error;
    });

    // Wait for backend to be ready
    await this.waitForBackendReady();
  }

  /**
   * Wait for backend to respond to health checks
   */
  private async waitForBackendReady(): Promise<void> {
    const startTime = Date.now();

    while (Date.now() - startTime < this.maxStartupTime) {
      try {
        const response = await fetch(`http://localhost:${this.backendPort}/health/live`);
        if (response.ok) {
          console.log('[BackendProcess] Backend is ready');
          return;
        }
      } catch (error) {
        // Backend not ready yet, continue waiting
      }

      await new Promise((resolve) => setTimeout(resolve, this.healthCheckInterval));
    }

    throw new Error(`Backend failed to start within ${this.maxStartupTime}ms`);
  }

  /**
   * Stop the backend process
   */
  public async stop(): Promise<void> {
    if (!this.backendProcess) {
      console.log('[BackendProcess] No backend process to stop');
      return;
    }

    console.log('[BackendProcess] Stopping backend...');

    return new Promise((resolve) => {
      if (!this.backendProcess) {
        resolve();
        return;
      }

      this.backendProcess.once('exit', () => {
        console.log('[BackendProcess] Backend stopped');
        this.backendProcess = null;
        resolve();
      });

      // Send SIGTERM to gracefully shutdown
      this.backendProcess.kill('SIGTERM');

      // Force kill after 5 seconds if it doesn't exit
      setTimeout(() => {
        if (this.backendProcess && !this.backendProcess.killed) {
          console.warn('[BackendProcess] Force killing backend process');
          this.backendProcess.kill('SIGKILL');
        }
      }, 5000);
    });
  }

  /**
   * Check if backend is running
   */
  public isRunning(): boolean {
    return this.backendProcess !== null && !this.backendProcess.killed;
  }

  /**
   * Get backend URL
   */
  public getBackendUrl(): string {
    return `http://localhost:${this.backendPort}`;
  }
}

// Singleton instance
export const backendProcessManager = new BackendProcessManager();
