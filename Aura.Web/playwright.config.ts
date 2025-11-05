import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for Aura.Web E2E tests
 * Enhanced with cross-platform support, flaky test quarantine, and artifact retention
 * See https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  timeout: 60 * 1000,
  expect: {
    timeout: 10 * 1000,
  },

  reporter: [
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['json', { outputFile: 'test-results/results.json' }],
    ['junit', { outputFile: 'test-results/junit.xml' }],
    ['list'],
    process.env.CI ? ['github'] : ['list'],
  ],

  use: {
    baseURL: 'http://127.0.0.1:5173',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 15 * 1000,
    navigationTimeout: 30 * 1000,
  },

  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1920, height: 1080 },
      },
      testMatch: /^(?!.*\.quarantine\.spec\.ts$).*\.spec\.ts$/,
    },

    {
      name: 'chromium-headless',
      use: {
        ...devices['Desktop Chrome'],
        headless: true,
        viewport: { width: 1920, height: 1080 },
      },
      testMatch: /^(?!.*\.quarantine\.spec\.ts$).*\.spec\.ts$/,
    },

    {
      name: 'quarantine',
      use: {
        ...devices['Desktop Chrome'],
        trace: 'on',
        video: 'on',
      },
      testMatch: /.*\.quarantine\.spec\.ts$/,
      retries: 3,
    },
  ],

  webServer: {
    command: 'npm run dev',
    url: 'http://127.0.0.1:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 120 * 1000,
    stdout: 'pipe',
    stderr: 'pipe',
  },
});
