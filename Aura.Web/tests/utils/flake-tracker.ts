/**
 * Flake Tracker - Detects and tracks flaky tests
 * Provides quarantine mechanism for known flaky tests
 */

import * as fs from 'fs';
import * as path from 'path';

export interface FlakeRecord {
  testName: string;
  testFile: string;
  failureCount: number;
  successCount: number;
  lastFailure: string;
  lastSuccess: string;
  flakeRate: number;
  quarantined: boolean;
  quarantineReason?: string;
  quarantinedAt?: string;
}

export interface FlakeTrackingData {
  version: string;
  lastUpdated: string;
  records: Record<string, FlakeRecord>;
}

export class FlakeTracker {
  private dataPath: string;
  private data: FlakeTrackingData;
  private readonly flakeThreshold = 0.3; // 30% failure rate triggers quarantine

  constructor(dataPath?: string) {
    this.dataPath = dataPath || path.join(process.cwd(), '.flake-tracker.json');
    this.data = this.loadData();
  }

  private loadData(): FlakeTrackingData {
    try {
      if (fs.existsSync(this.dataPath)) {
        const content = fs.readFileSync(this.dataPath, 'utf-8');
        return JSON.parse(content);
      }
    } catch (error) {
      console.warn('Failed to load flake tracking data:', error);
    }

    return {
      version: '1.0',
      lastUpdated: new Date().toISOString(),
      records: {},
    };
  }

  private saveData(): void {
    try {
      this.data.lastUpdated = new Date().toISOString();
      fs.writeFileSync(this.dataPath, JSON.stringify(this.data, null, 2));
    } catch (error) {
      console.error('Failed to save flake tracking data:', error);
    }
  }

  public recordTestResult(testName: string, testFile: string, passed: boolean): void {
    const key = `${testFile}::${testName}`;
    
    if (!this.data.records[key]) {
      this.data.records[key] = {
        testName,
        testFile,
        failureCount: 0,
        successCount: 0,
        lastFailure: '',
        lastSuccess: '',
        flakeRate: 0,
        quarantined: false,
      };
    }

    const record = this.data.records[key];

    if (passed) {
      record.successCount++;
      record.lastSuccess = new Date().toISOString();
    } else {
      record.failureCount++;
      record.lastFailure = new Date().toISOString();
    }

    const totalRuns = record.failureCount + record.successCount;
    record.flakeRate = totalRuns > 0 ? record.failureCount / totalRuns : 0;

    // Auto-quarantine if flake rate exceeds threshold
    if (record.flakeRate >= this.flakeThreshold && totalRuns >= 5 && !record.quarantined) {
      this.quarantineTest(testName, testFile, `Auto-quarantined: flake rate ${(record.flakeRate * 100).toFixed(1)}%`);
    }

    this.saveData();
  }

  public quarantineTest(testName: string, testFile: string, reason: string): void {
    const key = `${testFile}::${testName}`;
    
    if (!this.data.records[key]) {
      this.data.records[key] = {
        testName,
        testFile,
        failureCount: 0,
        successCount: 0,
        lastFailure: '',
        lastSuccess: '',
        flakeRate: 0,
        quarantined: true,
        quarantineReason: reason,
        quarantinedAt: new Date().toISOString(),
      };
    } else {
      this.data.records[key].quarantined = true;
      this.data.records[key].quarantineReason = reason;
      this.data.records[key].quarantinedAt = new Date().toISOString();
    }

    this.saveData();
  }

  public unquarantineTest(testName: string, testFile: string): void {
    const key = `${testFile}::${testName}`;
    
    if (this.data.records[key]) {
      this.data.records[key].quarantined = false;
      delete this.data.records[key].quarantineReason;
      delete this.data.records[key].quarantinedAt;
      this.saveData();
    }
  }

  public isQuarantined(testName: string, testFile: string): boolean {
    const key = `${testFile}::${testName}`;
    return this.data.records[key]?.quarantined ?? false;
  }

  public getFlakeRate(testName: string, testFile: string): number {
    const key = `${testFile}::${testName}`;
    return this.data.records[key]?.flakeRate ?? 0;
  }

  public getAllQuarantined(): FlakeRecord[] {
    return Object.values(this.data.records).filter((r) => r.quarantined);
  }

  public getHighFlakeTests(threshold = 0.2): FlakeRecord[] {
    return Object.values(this.data.records)
      .filter((r) => r.flakeRate >= threshold && !r.quarantined)
      .sort((a, b) => b.flakeRate - a.flakeRate);
  }

  public generateReport(): string {
    const quarantined = this.getAllQuarantined();
    const highFlake = this.getHighFlakeTests();
    
    let report = '# Flake Tracker Report\n\n';
    report += `Generated: ${new Date().toISOString()}\n\n`;
    
    report += `## Summary\n`;
    report += `- Total tests tracked: ${Object.keys(this.data.records).length}\n`;
    report += `- Quarantined tests: ${quarantined.length}\n`;
    report += `- High flake rate tests: ${highFlake.length}\n\n`;
    
    if (quarantined.length > 0) {
      report += `## Quarantined Tests\n\n`;
      quarantined.forEach((record) => {
        report += `### ${record.testName}\n`;
        report += `- File: ${record.testFile}\n`;
        report += `- Flake rate: ${(record.flakeRate * 100).toFixed(1)}%\n`;
        report += `- Failures: ${record.failureCount} / Successes: ${record.successCount}\n`;
        report += `- Reason: ${record.quarantineReason}\n`;
        report += `- Quarantined: ${record.quarantinedAt}\n\n`;
      });
    }
    
    if (highFlake.length > 0) {
      report += `## High Flake Rate Tests (Not Quarantined)\n\n`;
      highFlake.forEach((record) => {
        report += `### ${record.testName}\n`;
        report += `- File: ${record.testFile}\n`;
        report += `- Flake rate: ${(record.flakeRate * 100).toFixed(1)}%\n`;
        report += `- Failures: ${record.failureCount} / Successes: ${record.successCount}\n`;
        report += `- Last failure: ${record.lastFailure || 'Never'}\n`;
        report += `- Last success: ${record.lastSuccess || 'Never'}\n\n`;
      });
    }
    
    return report;
  }

  public reset(): void {
    this.data = {
      version: '1.0',
      lastUpdated: new Date().toISOString(),
      records: {},
    };
    this.saveData();
  }
}

// Singleton instance
export const flakeTracker = new FlakeTracker();
