import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E Test Configuration for FelizesTracker
 *
 * This configuration sets up:
 * - Backend server (http://localhost:5000)
 * - Frontend dev server (http://localhost:3000)
 * - Test timeouts and retries
 * - Browser support (Chrome, Firefox, Safari)
 */
export default defineConfig({
  testDir: './tests/e2e',

  /* Fully parallelize tests across all browsers and workers */
  fullyParallel: false,

  /* Fail the build on CI if you accidentally left test.only in the source code */
  forbidOnly: !!process.env.CI,

  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,

  /* Opt out of parallel tests on CI */
  workers: 1,

  /* Reporter to use */
  reporter: [
    ['html', { outputFolder: 'playwright-report' }],
    ['list'],
    ['json', { outputFile: 'test-results/test-results.json' }],
  ],

  /* Shared settings for all tests */
  use: {
    /* Base URL for tests */
    baseURL: 'http://localhost:3000',

    /* Collect trace when retrying the failed test */
    trace: 'on-first-retry',

    /* Screenshot on failure */
    screenshot: 'only-on-failure',

    /* Video on failure */
    video: 'retain-on-failure',
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },

    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },

    /* Test against mobile viewports */
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    },
    {
      name: 'Mobile Safari',
      use: { ...devices['iPhone 12'] },
    },
  ],

  /* Run your local dev server before starting the tests */
  webServer: [
    {
      command: 'cd ../backend && dotnet run --project src/FelizesTracker.Api --urls http://localhost:5000',
      url: 'http://localhost:5000/api/v1/health',
      timeout: 120000,
      reuseExistingServer: !process.env.CI,
      stdout: 'pipe',
      stderr: 'pipe',
    },
    {
      command: 'npm run dev',
      url: 'http://localhost:3000',
      timeout: 60000,
      reuseExistingServer: !process.env.CI,
      stdout: 'pipe',
      stderr: 'pipe',
    },
  ],

  /* Test timeout */
  timeout: 30000,
  expect: {
    timeout: 10000,
  },
});
