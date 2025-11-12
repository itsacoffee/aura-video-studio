/**
 * Initialization Tracker Module
 * Tracks initialization steps with detailed status and error information
 */

const fs = require('fs');
const path = require('path');

/**
 * Enum of initialization steps
 */
const InitializationStep = {
  EARLY_CRASH_LOGGING: 'early-crash-logging',
  STARTUP_LOGGER: 'startup-logger',
  ERROR_HANDLING: 'error-handling',
  DIAGNOSTICS: 'diagnostics',
  APP_CONFIG: 'app-config',
  WINDOW_MANAGER: 'window-manager',
  SPLASH_SCREEN: 'splash-screen',
  PROTOCOL_HANDLER: 'protocol-handler',
  BACKEND_SERVICE: 'backend-service',
  IPC_HANDLERS: 'ipc-handlers',
  MAIN_WINDOW: 'main-window',
  SYSTEM_TRAY: 'system-tray',
  APP_MENU: 'app-menu',
  AUTO_UPDATER: 'auto-updater',
  FIRST_RUN_CHECK: 'first-run-check'
};

/**
 * Step criticality levels
 */
const StepCriticality = {
  CRITICAL: 'critical',      // Must succeed for app to work
  IMPORTANT: 'important',    // Should succeed but app can continue in degraded mode
  OPTIONAL: 'optional'       // Nice to have but not essential
};

/**
 * Criticality map for each step
 */
const STEP_CRITICALITY = {
  [InitializationStep.EARLY_CRASH_LOGGING]: StepCriticality.IMPORTANT,
  [InitializationStep.STARTUP_LOGGER]: StepCriticality.IMPORTANT,
  [InitializationStep.ERROR_HANDLING]: StepCriticality.CRITICAL,
  [InitializationStep.DIAGNOSTICS]: StepCriticality.OPTIONAL,
  [InitializationStep.APP_CONFIG]: StepCriticality.CRITICAL,
  [InitializationStep.WINDOW_MANAGER]: StepCriticality.CRITICAL,
  [InitializationStep.SPLASH_SCREEN]: StepCriticality.OPTIONAL,
  [InitializationStep.PROTOCOL_HANDLER]: StepCriticality.IMPORTANT,
  [InitializationStep.BACKEND_SERVICE]: StepCriticality.CRITICAL,
  [InitializationStep.IPC_HANDLERS]: StepCriticality.CRITICAL,
  [InitializationStep.MAIN_WINDOW]: StepCriticality.CRITICAL,
  [InitializationStep.SYSTEM_TRAY]: StepCriticality.OPTIONAL,
  [InitializationStep.APP_MENU]: StepCriticality.IMPORTANT,
  [InitializationStep.AUTO_UPDATER]: StepCriticality.OPTIONAL,
  [InitializationStep.FIRST_RUN_CHECK]: StepCriticality.IMPORTANT
};

/**
 * Status for each initialization step
 */
class InitializationStatus {
  constructor(step) {
    this.step = step;
    this.criticality = STEP_CRITICALITY[step];
    this.status = 'pending'; // pending, in-progress, success, failed, skipped
    this.startTime = null;
    this.endTime = null;
    this.duration = null;
    this.error = null;
    this.errorMessage = null;
    this.errorStack = null;
    this.recoveryAction = null;
    this.metadata = {};
  }

  /**
   * Mark step as started
   */
  start() {
    this.status = 'in-progress';
    this.startTime = Date.now();
  }

  /**
   * Mark step as succeeded
   */
  succeed(metadata = {}) {
    this.status = 'success';
    this.endTime = Date.now();
    this.duration = this.endTime - this.startTime;
    this.metadata = { ...this.metadata, ...metadata };
  }

  /**
   * Mark step as failed with error details
   */
  fail(error, recoveryAction = null) {
    this.status = 'failed';
    this.endTime = Date.now();
    this.duration = this.endTime - this.startTime;
    this.error = error;
    this.errorMessage = error?.message || String(error);
    this.errorStack = error?.stack || 'No stack trace available';
    this.recoveryAction = recoveryAction;
  }

  /**
   * Mark step as skipped
   */
  skip(reason) {
    this.status = 'skipped';
    this.metadata.skipReason = reason;
  }

  /**
   * Check if step is critical and failed
   */
  isCriticalFailure() {
    return this.criticality === StepCriticality.CRITICAL && this.status === 'failed';
  }

  /**
   * Check if step succeeded
   */
  isSuccessful() {
    return this.status === 'success';
  }

  /**
   * Get human-readable summary
   */
  getSummary() {
    const parts = [
      `Step: ${this.step}`,
      `Criticality: ${this.criticality}`,
      `Status: ${this.status}`
    ];

    if (this.duration !== null) {
      parts.push(`Duration: ${this.duration}ms`);
    }

    if (this.status === 'failed') {
      parts.push(`Error: ${this.errorMessage}`);
      if (this.recoveryAction) {
        parts.push(`Recovery: ${this.recoveryAction}`);
      }
    }

    return parts.join(' | ');
  }
}

/**
 * Initialization Tracker
 * Tracks all initialization steps and their status
 */
class InitializationTracker {
  constructor(app) {
    this.app = app;
    this.steps = new Map();
    this.startTime = Date.now();
    this.endTime = null;
    
    // Initialize all steps
    for (const step of Object.values(InitializationStep)) {
      this.steps.set(step, new InitializationStatus(step));
    }
  }

  /**
   * Start tracking a step
   */
  startStep(step) {
    const status = this.steps.get(step);
    if (!status) {
      throw new Error(`Unknown initialization step: ${step}`);
    }
    status.start();
    return status;
  }

  /**
   * Mark step as succeeded
   */
  succeedStep(step, metadata = {}) {
    const status = this.steps.get(step);
    if (!status) {
      throw new Error(`Unknown initialization step: ${step}`);
    }
    status.succeed(metadata);
    return status;
  }

  /**
   * Mark step as failed
   */
  failStep(step, error, recoveryAction = null) {
    const status = this.steps.get(step);
    if (!status) {
      throw new Error(`Unknown initialization step: ${step}`);
    }
    status.fail(error, recoveryAction);
    return status;
  }

  /**
   * Mark step as skipped
   */
  skipStep(step, reason) {
    const status = this.steps.get(step);
    if (!status) {
      throw new Error(`Unknown initialization step: ${step}`);
    }
    status.skip(reason);
    return status;
  }

  /**
   * Get status of a specific step
   */
  getStepStatus(step) {
    return this.steps.get(step);
  }

  /**
   * Check if all critical steps succeeded
   */
  allCriticalStepsSucceeded() {
    for (const status of this.steps.values()) {
      if (status.criticality === StepCriticality.CRITICAL && status.status !== 'success') {
        return false;
      }
    }
    return true;
  }

  /**
   * Get all failed steps
   */
  getFailedSteps() {
    return Array.from(this.steps.values()).filter(s => s.status === 'failed');
  }

  /**
   * Get all critical failures
   */
  getCriticalFailures() {
    return Array.from(this.steps.values()).filter(s => s.isCriticalFailure());
  }

  /**
   * Get completion percentage (based on steps that have finished)
   */
  getCompletionPercentage() {
    const total = this.steps.size;
    const completed = Array.from(this.steps.values()).filter(
      s => s.status === 'success' || s.status === 'failed' || s.status === 'skipped'
    ).length;
    return Math.round((completed / total) * 100);
  }

  /**
   * Generate summary report
   */
  getSummary() {
    this.endTime = Date.now();
    const totalDuration = this.endTime - this.startTime;

    const stepSummaries = Array.from(this.steps.values()).map(s => ({
      step: s.step,
      criticality: s.criticality,
      status: s.status,
      duration: s.duration,
      error: s.status === 'failed' ? {
        message: s.errorMessage,
        recoveryAction: s.recoveryAction
      } : null,
      metadata: s.metadata
    }));

    const successCount = stepSummaries.filter(s => s.status === 'success').length;
    const failureCount = stepSummaries.filter(s => s.status === 'failed').length;
    const criticalFailureCount = this.getCriticalFailures().length;

    return {
      startTime: new Date(this.startTime).toISOString(),
      endTime: new Date(this.endTime).toISOString(),
      totalDuration: `${totalDuration}ms`,
      totalSteps: this.steps.size,
      successCount,
      failureCount,
      criticalFailureCount,
      completionPercentage: this.getCompletionPercentage(),
      allCriticalStepsSucceeded: this.allCriticalStepsSucceeded(),
      steps: stepSummaries
    };
  }

  /**
   * Write summary to disk
   */
  writeSummary() {
    try {
      const summary = this.getSummary();
      const logsDir = path.join(this.app.getPath('userData'), 'logs');
      
      if (!fs.existsSync(logsDir)) {
        fs.mkdirSync(logsDir, { recursive: true });
      }

      const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
      const summaryFile = path.join(logsDir, `initialization-${timestamp}.json`);
      
      fs.writeFileSync(summaryFile, JSON.stringify(summary, null, 2));
      
      return summaryFile;
    } catch (error) {
      console.error('Failed to write initialization summary:', error);
      return null;
    }
  }
}

module.exports = {
  InitializationStep,
  StepCriticality,
  InitializationStatus,
  InitializationTracker
};
