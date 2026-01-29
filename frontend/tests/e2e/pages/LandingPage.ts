import { expect, type Page, type Locator } from '@playwright/test';

/**
 * Page Object Model for Landing Page
 * Encapsulates all locators and actions for the landing page
 */
export class LandingPagePO {
  readonly page: Page;

  // Locators
  readonly pageTitle: Locator;
  readonly subtitle: Locator;
  readonly healthIndicator: Locator;
  readonly statusLabel: Locator;
  readonly refreshButton: Locator;
  readonly retryButton: Locator;
  readonly errorMessage: Locator;
  readonly versionLabel: Locator;
  readonly uptimeLabel: Locator;
  readonly serviceDatabase: Locator;
  readonly serviceCache: Locator;

  constructor(page: Page) {
    this.page = page;

    // Header locators
    this.pageTitle = page.getByText('FelizesTracker');
    this.subtitle = page.getByText('Track your feliz journey with confidence');

    // Health indicator locators
    this.healthIndicator = page.locator('.health-indicator');
    this.statusLabel = page.locator('.health-indicator__label');
    this.refreshButton = page.getByRole('button', { name: /refresh health status/i });
    this.retryButton = page.getByRole('button', { name: /retry/i });
    this.errorMessage = page.locator('.health-indicator__error');
    this.versionLabel = page.locator('.health-indicator__version');
    this.uptimeLabel = page.locator('.health-indicator__uptime');
    this.serviceDatabase = page.locator('.health-indicator__service').filter({ hasText: /Database/i });
    this.serviceCache = page.locator('.health-indicator__service').filter({ hasText: /Cache/i });
  }

  /**
   * Navigate to the landing page
   */
  async goto() {
    await this.page.goto('/');
  }

  /**
   * Wait for the page to be fully loaded
   */
  async waitForLoad() {
    await this.page.waitForLoadState('networkidle');
    await this.page.waitForSelector('.landing-page', { state: 'visible' });
  }

  /**
   * Wait for health status to be loaded
   */
  async waitForHealthStatus() {
    await this.page.waitForSelector('.health-indicator:not(.health-indicator--loading)', {
      timeout: 15000,
    });
  }

  /**
   * Click the refresh button
   */
  async refresh() {
    await this.refreshButton.click();
  }

  /**
   * Click the retry button
   */
  async retry() {
    await this.retryButton.click();
  }

  /**
   * Get the current health status text
   */
  async getHealthStatus(): Promise<string> {
    await this.waitForHealthStatus();
    const label = await this.statusLabel.textContent();
    return label || '';
  }

  /**
   * Check if the page shows healthy status
   */
  async isHealthy(): Promise<boolean> {
    const status = await this.getHealthStatus();
    return status.includes('Healthy');
  }

  /**
   * Check if the page shows unhealthy status
   */
  async isUnhealthy(): Promise<boolean> {
    const status = await this.getHealthStatus();
    return status.includes('Unhealthy');
  }

  /**
   * Check if the page shows degraded status
   */
  async isDegraded(): Promise<boolean> {
    const status = await this.getHealthStatus();
    return status.includes('Degraded');
  }

  /**
   * Get the version number
   */
  async getVersion(): Promise<string | null> {
    const versionText = await this.versionLabel.textContent();
    if (versionText) {
      const match = versionText.match(/Version: (.+)/);
      return match ? match[1] : null;
    }
    return null;
  }

  /**
   * Get the uptime text
   */
  async getUptime(): Promise<string | null> {
    return await this.uptimeLabel.textContent();
  }

  /**
   * Take a screenshot
   */
  async screenshot(filename: string) {
    await this.page.screenshot({
      path: `test-results/screenshots/${filename}`,
      fullPage: true,
    });
  }

  /**
   * Assert that the page has all expected elements
   */
  async assertPageStructure() {
    await expect(this.pageTitle).toBeVisible();
    await expect(this.subtitle).toBeVisible();
    await expect(this.healthIndicator).toBeVisible();
  }

  /**
   * Assert that the status is healthy
   */
  async assertHealthy() {
    await this.waitForHealthStatus();
    await expect(this.statusLabel).toContainText('Healthy', { ignoreCase: true });
  }

  /**
   * Assert that the status is unhealthy
   */
  async assertUnhealthy() {
    await this.waitForHealthStatus();
    await expect(this.statusLabel).toContainText('Unhealthy', { ignoreCase: true });
  }

  /**
   * Assert that version is displayed
   */
  async assertVersionDisplayed(version: string) {
    await this.waitForHealthStatus();
    await expect(this.versionLabel).toContainText(version);
  }
}
