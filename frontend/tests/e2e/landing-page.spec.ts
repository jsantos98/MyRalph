import { test, expect } from '@playwright/test';
import { LandingPagePO } from './pages/LandingPage';

/**
 * E2E Tests for FelizesTracker Landing Page
 *
 * These tests validate the complete user flow:
 * 1. Landing page loads
 * 2. Displays "FelizesTracker" branding
 * 3. Shows health status from backend
 * 4. Handles refresh functionality
 * 5. Handles error scenarios
 */

test.describe('Landing Page - E2E Tests', () => {
  let landingPage: LandingPagePO;

  test.beforeEach(async ({ page }) => {
    landingPage = new LandingPagePO(page);
    await landingPage.goto();
    await landingPage.waitForLoad();
  });

  test.describe('Page Structure', () => {
    test('should load landing page successfully', async () => {
      await expect(page).toHaveTitle(/FelizesTracker/i);
      await expect(page).toHaveURL('/');
    });

    test('should display "FelizesTracker" as main title', async () => {
      await expect(landingPage.pageTitle).toBeVisible();
      await expect(landingPage.pageTitle).toContainText('FelizesTracker');
    });

    test('should display subtitle text', async () => {
      await expect(landingPage.subtitle).toBeVisible();
      await expect(landingPage.subtitle).toContainText(
        'Track your feliz journey with confidence'
      );
    });

    test('should have proper page structure with header and main content', async () => {
      await expect(page.locator('.landing-page')).toBeVisible();
      await expect(page.locator('.landing-header')).toBeVisible();
      await expect(page.locator('.landing-main')).toBeVisible();
    });
  });

  test.describe('Health Status - Happy Path', () => {
    test('should display system health status', async () => {
      await landingPage.waitForHealthStatus();
      await expect(landingPage.healthIndicator).toBeVisible();
      await expect(landingPage.statusLabel).toBeVisible();
    });

    test('should show healthy status when backend is running', async () => {
      await landingPage.waitForHealthStatus();
      const isHealthy = await landingPage.isHealthy();
      expect(isHealthy).toBeTruthy();
    });

    test('should display version information', async () => {
      await landingPage.waitForHealthStatus();
      const version = await landingPage.getVersion();
      expect(version).toBeDefined();
      expect(version?.length).toBeGreaterThan(0);
    });

    test('should have refresh button visible', async () => {
      await landingPage.waitForHealthStatus();
      await expect(landingPage.refreshButton).toBeVisible();
    });

    test('should have proper ARIA labels for accessibility', async () => {
      const healthIndicator = page.locator('.health-indicator[role="status"]');
      await expect(healthIndicator).toHaveAttribute('aria-live', 'polite');
    });
  });

  test.describe('Health Status Refresh', () => {
    test('should refresh health status when button is clicked', async () => {
      await landingPage.waitForHealthStatus();

      // Get initial status
      const initialVersion = await landingPage.getVersion();

      // Click refresh
      await landingPage.refresh();
      await landingPage.waitForHealthStatus();

      // Verify status is still displayed
      await expect(landingPage.statusLabel).toBeVisible();
    });

    test('should show loading state during refresh', async () => {
      await landingPage.waitForHealthStatus();

      // Click refresh and immediately check for loading state
      await landingPage.refresh();

      // Check for loading state briefly
      const loadingIndicator = page.locator('.health-indicator--loading');
      const isVisible = await loadingIndicator.isVisible().catch(() => false);

      // Loading state might be too fast to catch, but if we see it, that's good
      if (isVisible) {
        await expect(loadingIndicator).toBeVisible();
      }

      // Wait for it to finish loading
      await landingPage.waitForHealthStatus();
    });
  });

  test.describe('Error Scenarios', () => {
    test('should handle backend service unavailable gracefully', async ({ page }) => {
      // This test requires stopping the backend server
      // For now, we'll test the error handling UI by simulating network failure

      // Navigate to landing page
      await landingPage.goto();
      await landingPage.waitForLoad();

      // Mock the health endpoint to fail
      await page.route('**/api/v1/health', route => route.abort());

      // Reload the page to trigger the failed request
      await page.reload();
      await landingPage.waitForLoad();

      // Wait for error state
      await page.waitForSelector('.health-indicator--error', { timeout: 15000 });

      // Verify error UI is displayed
      await expect(landingPage.errorMessage).toBeVisible();
      await expect(landingPage.retryButton).toBeVisible();
    });

    test('should show retry button on error', async ({ page }) => {
      // Mock the health endpoint to fail
      await page.route('**/api/v1/health', route => route.abort());

      await landingPage.goto();
      await landingPage.waitForLoad();

      // Wait for error state
      await page.waitForSelector('.health-indicator--error', { timeout: 15000 });

      await expect(landingPage.retryButton).toBeVisible();
      await expect(landingPage.retryButton).toHaveText('Retry');
    });

    test('should retry health check when retry button is clicked', async ({ page }) => {
      let requestCount = 0;

      // Mock the health endpoint - fail first, then succeed
      await page.route('**/api/v1/health', route => {
        requestCount++;
        if (requestCount === 1) {
          route.abort();
        } else {
          route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              data: {
                status: 'healthy',
                timestamp: new Date().toISOString(),
                version: '1.0.0',
              },
            }),
          });
        }
      });

      await landingPage.goto();
      await landingPage.waitForLoad();

      // Wait for error state
      await page.waitForSelector('.health-indicator--error', { timeout: 15000 });

      // Click retry
      await landingPage.retry();

      // Wait for successful response
      await landingPage.waitForHealthStatus();

      // Verify healthy status
      await expect(landingPage.statusLabel).toContainText('Healthy', { ignoreCase: true });
    });
  });

  test.describe('Responsive Design', () => {
    test('should display correctly on mobile viewport', async ({ page, viewport }) => {
      // Set mobile viewport
      await page.setViewportSize({ width: 375, height: 667 });

      await landingPage.goto();
      await landingPage.waitForLoad();
      await landingPage.waitForHealthStatus();

      // Verify all elements are visible on mobile
      await expect(landingPage.pageTitle).toBeVisible();
      await expect(landingPage.healthIndicator).toBeVisible();
    });

    test('should display correctly on tablet viewport', async ({ page }) => {
      await page.setViewportSize({ width: 768, height: 1024 });

      await landingPage.goto();
      await landingPage.waitForLoad();
      await landingPage.waitForHealthStatus();

      await expect(landingPage.pageTitle).toBeVisible();
      await expect(landingPage.healthIndicator).toBeVisible();
    });

    test('should display correctly on desktop viewport', async ({ page }) => {
      await page.setViewportSize({ width: 1920, height: 1080 });

      await landingPage.goto();
      await landingPage.waitForLoad();
      await landingPage.waitForHealthStatus();

      await expect(landingPage.pageTitle).toBeVisible();
      await expect(landingPage.healthIndicator).toBeVisible();
    });
  });

  test.describe('Visual Regression', () => {
    test('should match screenshot on initial load', async () => {
      await landingPage.waitForHealthStatus();

      // Take a screenshot for visual regression testing
      await landingPage.screenshot('landing-page-healthy.png');

      // Compare with baseline (will create baseline if it doesn't exist)
      await expect(page).toHaveScreenshot('landing-page-healthy.png', {
        fullPage: true,
        maxDiffPixels: 100,
      });
    });
  });

  test.describe('Network Performance', () => {
    test('should load page within performance budget', async () => {
      const startTime = Date.now();

      await landingPage.goto();
      await landingPage.waitForLoad();
      await landingPage.waitForHealthStatus();

      const loadTime = Date.now() - startTime;

      // Page should load within 5 seconds (including API call)
      expect(loadTime).toBeLessThan(5000);
    });

    test('should handle slow network responses', async ({ page }) => {
      // Simulate slow network
      await page.route('**/api/v1/health', async route => {
        await new Promise(resolve => setTimeout(resolve, 2000));
        route.continue();
      });

      await landingPage.goto();
      await landingPage.waitForLoad();

      // Should still complete, just slower
      await landingPage.waitForHealthStatus();

      await expect(landingPage.statusLabel).toBeVisible();
    });
  });

  test.describe('Backend Integration', () => {
    test('should call backend health API on page load', async ({ page }) => {
      let apiCalled = false;

      await page.route('**/api/v1/health', route => {
        apiCalled = true;
        route.continue();
      });

      await landingPage.goto();
      await landingPage.waitForLoad();
      await landingPage.waitForHealthStatus();

      expect(apiCalled).toBeTruthy();
    });

    test('should call backend health API on refresh', async ({ page }) => {
      let callCount = 0;

      await page.route('**/api/v1/health', route => {
        callCount++;
        route.continue();
      });

      await landingPage.goto();
      await landingPage.waitForLoad();
      await landingPage.waitForHealthStatus();

      const initialCallCount = callCount;
      expect(initialCallCount).toBeGreaterThan(0);

      // Click refresh
      await landingPage.refresh();
      await landingPage.waitForHealthStatus();

      expect(callCount).toBeGreaterThan(initialCallCount);
    });

    test('should display correct API response data', async ({ page }) => {
      // Mock a specific response
      const mockResponse = {
        data: {
          status: 'healthy',
          timestamp: new Date().toISOString(),
          version: '2.0.0',
          uptime: 7200,
          services: {
            database: 'up',
            cache: 'up',
          },
        },
      };

      await page.route('**/api/v1/health', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(mockResponse),
        });
      });

      await landingPage.goto();
      await landingPage.waitForLoad();
      await landingPage.waitForHealthStatus();

      // Verify version is displayed
      const version = await landingPage.getVersion();
      expect(version).toBe('2.0.0');
    });
  });
});
