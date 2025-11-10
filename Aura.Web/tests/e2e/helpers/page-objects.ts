/**
 * Page Object Model for E2E tests
 * Provides reusable page objects for common interactions
 */

import { Page, Locator } from '@playwright/test';

/**
 * Base Page Object
 */
export class BasePage {
  constructor(protected page: Page) {}

  async goto(path: string): Promise<void> {
    await this.page.goto(path);
  }

  async waitForLoad(): Promise<void> {
    await this.page.waitForLoadState('networkidle');
  }

  async screenshot(name: string): Promise<void> {
    await this.page.screenshot({ path: `screenshots/${name}.png`, fullPage: true });
  }
}

/**
 * Login Page Object
 */
export class LoginPage extends BasePage {
  private readonly emailInput: Locator;
  private readonly passwordInput: Locator;
  private readonly loginButton: Locator;

  constructor(page: Page) {
    super(page);
    this.emailInput = page.getByLabel(/email/i);
    this.passwordInput = page.getByLabel(/password/i);
    this.loginButton = page.getByRole('button', { name: /login|sign in/i });
  }

  async login(email: string, password: string): Promise<void> {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.loginButton.click();
  }
}

/**
 * Dashboard Page Object
 */
export class DashboardPage extends BasePage {
  private readonly newProjectButton: Locator;
  private readonly projectsList: Locator;
  private readonly searchInput: Locator;

  constructor(page: Page) {
    super(page);
    this.newProjectButton = page.getByRole('button', { name: /new project/i });
    this.projectsList = page.getByRole('list', { name: /projects/i });
    this.searchInput = page.getByRole('searchbox');
  }

  async createNewProject(): Promise<void> {
    await this.newProjectButton.click();
  }

  async searchProjects(query: string): Promise<void> {
    await this.searchInput.fill(query);
    await this.page.waitForTimeout(300); // Debounce
  }

  async getProjectCount(): Promise<number> {
    return await this.projectsList.locator('li').count();
  }
}

/**
 * Video Creation Wizard Page Object
 */
export class VideoWizardPage extends BasePage {
  private readonly titleInput: Locator;
  private readonly descriptionInput: Locator;
  private readonly durationInput: Locator;
  private readonly nextButton: Locator;
  private readonly previousButton: Locator;
  private readonly submitButton: Locator;

  constructor(page: Page) {
    super(page);
    this.titleInput = page.getByLabel(/title/i);
    this.descriptionInput = page.getByLabel(/description/i);
    this.durationInput = page.getByLabel(/duration/i);
    this.nextButton = page.getByRole('button', { name: /next/i });
    this.previousButton = page.getByRole('button', { name: /previous|back/i });
    this.submitButton = page.getByRole('button', { name: /create|generate/i });
  }

  async fillBasicInfo(title: string, description: string, duration: string): Promise<void> {
    await this.titleInput.fill(title);
    await this.descriptionInput.fill(description);
    await this.durationInput.fill(duration);
  }

  async goToNextStep(): Promise<void> {
    await this.nextButton.click();
  }

  async goToPreviousStep(): Promise<void> {
    await this.previousButton.click();
  }

  async submit(): Promise<void> {
    await this.submitButton.click();
  }

  async selectProvider(providerName: string): Promise<void> {
    await this.page.getByRole('radio', { name: new RegExp(providerName, 'i') }).click();
  }

  async selectVoice(voiceName: string): Promise<void> {
    await this.page.getByRole('combobox', { name: /voice/i }).selectOption(voiceName);
  }

  async waitForProcessing(): Promise<void> {
    await this.page.waitForSelector('[data-testid="processing-indicator"]', { timeout: 60000 });
  }

  async waitForCompletion(): Promise<void> {
    await this.page.waitForSelector('[data-testid="success-message"]', { timeout: 120000 });
  }
}

/**
 * Timeline Editor Page Object
 */
export class TimelineEditorPage extends BasePage {
  private readonly timeline: Locator;
  private readonly playButton: Locator;
  private readonly pauseButton: Locator;
  private readonly addTrackButton: Locator;

  constructor(page: Page) {
    super(page);
    this.timeline = page.locator('[data-testid="timeline"]');
    this.playButton = page.getByRole('button', { name: /play/i });
    this.pauseButton = page.getByRole('button', { name: /pause/i });
    this.addTrackButton = page.getByRole('button', { name: /add track/i });
  }

  async play(): Promise<void> {
    await this.playButton.click();
  }

  async pause(): Promise<void> {
    await this.pauseButton.click();
  }

  async addTrack(trackType: 'video' | 'audio' | 'text'): Promise<void> {
    await this.addTrackButton.click();
    await this.page.getByRole('menuitem', { name: new RegExp(trackType, 'i') }).click();
  }

  async addClip(trackIndex: number, assetName: string): Promise<void> {
    const track = this.timeline.locator(`[data-track-index="${trackIndex}"]`);
    await track.click({ position: { x: 100, y: 10 } }); // Click in the middle of track
    await this.page.getByRole('menuitem', { name: new RegExp(assetName, 'i') }).click();
  }

  async getTrackCount(): Promise<number> {
    return await this.timeline.locator('[data-testid="track"]').count();
  }

  async getClipCount(trackIndex: number): Promise<number> {
    const track = this.timeline.locator(`[data-track-index="${trackIndex}"]`);
    return await track.locator('[data-testid="clip"]').count();
  }
}

/**
 * Settings Page Object
 */
export class SettingsPage extends BasePage {
  private readonly apiKeysTab: Locator;
  private readonly preferencesTab: Locator;
  private readonly saveButton: Locator;

  constructor(page: Page) {
    super(page);
    this.apiKeysTab = page.getByRole('tab', { name: /api keys/i });
    this.preferencesTab = page.getByRole('tab', { name: /preferences/i });
    this.saveButton = page.getByRole('button', { name: /save/i });
  }

  async goToApiKeys(): Promise<void> {
    await this.apiKeysTab.click();
  }

  async goToPreferences(): Promise<void> {
    await this.preferencesTab.click();
  }

  async setApiKey(provider: string, key: string): Promise<void> {
    const input = this.page.getByLabel(new RegExp(`${provider}.*key`, 'i'));
    await input.fill(key);
  }

  async save(): Promise<void> {
    await this.saveButton.click();
    await this.page.waitForSelector('[data-testid="save-success"]');
  }

  async toggleFeature(featureName: string): Promise<void> {
    const toggle = this.page.getByRole('switch', { name: new RegExp(featureName, 'i') });
    await toggle.click();
  }
}

/**
 * Modal Dialog Page Object
 */
export class ModalDialog {
  private readonly dialog: Locator;
  private readonly closeButton: Locator;
  private readonly confirmButton: Locator;
  private readonly cancelButton: Locator;

  constructor(private page: Page) {
    this.dialog = page.getByRole('dialog');
    this.closeButton = this.dialog.getByRole('button', { name: /close/i });
    this.confirmButton = this.dialog.getByRole('button', { name: /confirm|ok|yes/i });
    this.cancelButton = this.dialog.getByRole('button', { name: /cancel|no/i });
  }

  async waitForOpen(): Promise<void> {
    await this.dialog.waitFor({ state: 'visible' });
  }

  async close(): Promise<void> {
    await this.closeButton.click();
  }

  async confirm(): Promise<void> {
    await this.confirmButton.click();
  }

  async cancel(): Promise<void> {
    await this.cancelButton.click();
  }

  async getTitle(): Promise<string> {
    return await this.dialog.locator('h2, h3').first().textContent() || '';
  }

  async getMessage(): Promise<string> {
    return await this.dialog.locator('p').first().textContent() || '';
  }
}

/**
 * Notification Toast Page Object
 */
export class Toast {
  constructor(private page: Page) {}

  async waitForToast(type: 'success' | 'error' | 'warning' | 'info' = 'success'): Promise<string> {
    const toast = this.page.locator(`[data-toast-type="${type}"]`).first();
    await toast.waitFor({ state: 'visible', timeout: 5000 });
    const message = await toast.textContent();
    return message || '';
  }

  async dismissToast(): Promise<void> {
    const closeButton = this.page.locator('[data-testid="toast-close"]').first();
    await closeButton.click();
  }
}

/**
 * Factory for creating page objects
 */
export class PageFactory {
  constructor(private page: Page) {}

  loginPage(): LoginPage {
    return new LoginPage(this.page);
  }

  dashboardPage(): DashboardPage {
    return new DashboardPage(this.page);
  }

  videoWizardPage(): VideoWizardPage {
    return new VideoWizardPage(this.page);
  }

  timelineEditorPage(): TimelineEditorPage {
    return new TimelineEditorPage(this.page);
  }

  settingsPage(): SettingsPage {
    return new SettingsPage(this.page);
  }

  modalDialog(): ModalDialog {
    return new ModalDialog(this.page);
  }

  toast(): Toast {
    return new Toast(this.page);
  }
}
