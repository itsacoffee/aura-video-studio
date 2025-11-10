/**
 * Critical User Journeys - E2E Tests
 * Tests complete user workflows from start to finish
 */

import { test, expect } from '@playwright/test';
import { PageFactory } from './helpers/page-objects';

test.describe('Critical User Journeys', () => {
  let pages: PageFactory;

  test.beforeEach(async ({ page }) => {
    pages = new PageFactory(page);
    await page.goto('/');
  });

  test('Complete video creation workflow', async ({ page }) => {
    test.setTimeout(120000); // 2 minutes for complete workflow

    // Step 1: Navigate to dashboard
    const dashboard = pages.dashboardPage();
    await dashboard.goto('/dashboard');
    await dashboard.waitForLoad();

    // Step 2: Start new project
    await dashboard.createNewProject();

    // Step 3: Fill video wizard - Basic Info
    const wizard = pages.videoWizardPage();
    await wizard.fillBasicInfo(
      'Test Video E2E',
      'A comprehensive E2E test video',
      '30'
    );
    await wizard.goToNextStep();

    // Step 4: Select providers
    await wizard.selectProvider('OpenAI');
    await wizard.selectVoice('Alloy');
    await wizard.goToNextStep();

    // Step 5: Submit and wait for processing
    await wizard.submit();
    
    // Verify processing starts
    await expect(page.locator('[data-testid="processing-indicator"]')).toBeVisible({
      timeout: 10000
    });

    // Wait for completion (with reasonable timeout)
    await wizard.waitForCompletion();

    // Step 6: Verify success
    const toast = pages.toast();
    const message = await toast.waitForToast('success');
    expect(message).toContain('Video created successfully');

    // Step 7: Verify video appears in dashboard
    await dashboard.goto('/dashboard');
    const projectCount = await dashboard.getProjectCount();
    expect(projectCount).toBeGreaterThan(0);
  });

  test('Video editing workflow', async ({ page }) => {
    test.setTimeout(60000);

    // Step 1: Open existing project (assuming one exists)
    const dashboard = pages.dashboardPage();
    await dashboard.goto('/dashboard');
    
    // Create a quick test project first
    await dashboard.createNewProject();
    const wizard = pages.videoWizardPage();
    await wizard.fillBasicInfo('Edit Test', 'Test for editing', '10');
    await page.getByRole('button', { name: /skip to editor/i }).click();

    // Step 2: Open timeline editor
    const editor = pages.timelineEditorPage();
    await editor.waitForLoad();

    // Step 3: Add tracks
    await editor.addTrack('video');
    await editor.addTrack('audio');
    await editor.addTrack('text');

    // Verify tracks added
    const trackCount = await editor.getTrackCount();
    expect(trackCount).toBeGreaterThanOrEqual(3);

    // Step 4: Play preview
    await editor.play();
    await page.waitForTimeout(2000); // Play for 2 seconds
    await editor.pause();

    // Step 5: Save changes
    await page.getByRole('button', { name: /save/i }).click();
    
    const toast = pages.toast();
    const message = await toast.waitForToast('success');
    expect(message).toContain('saved');
  });

  test('Settings configuration workflow', async ({ page }) => {
    test.setTimeout(30000);

    // Step 1: Navigate to settings
    const settings = pages.settingsPage();
    await settings.goto('/settings');

    // Step 2: Configure API keys
    await settings.goToApiKeys();
    await settings.setApiKey('OpenAI', 'sk-test-key-123');
    await settings.setApiKey('ElevenLabs', 'el-test-key-456');

    // Step 3: Save settings
    await settings.save();

    // Verify success
    const toast = pages.toast();
    const message = await toast.waitForToast('success');
    expect(message).toContain('Settings saved');

    // Step 4: Reload and verify persistence
    await page.reload();
    await settings.goToApiKeys();

    // Verify API keys are still there (masked)
    const openaiInput = page.getByLabel(/openai.*key/i);
    const value = await openaiInput.inputValue();
    expect(value).toBeTruthy();
  });

  test('Error handling and recovery', async ({ page }) => {
    test.setTimeout(45000);

    // Step 1: Try to create video without API keys
    const dashboard = pages.dashboardPage();
    await dashboard.goto('/dashboard');
    await dashboard.createNewProject();

    const wizard = pages.videoWizardPage();
    await wizard.fillBasicInfo('Error Test', 'Should fail validation', '30');
    
    // Clear API keys to force error
    await page.evaluate(() => {
      localStorage.removeItem('apiKeys');
    });

    await wizard.goToNextStep();
    await wizard.submit();

    // Verify error handling
    const toast = pages.toast();
    const errorMessage = await toast.waitForToast('error');
    expect(errorMessage).toContain('API key');

    // Step 2: Fix the issue
    await page.getByRole('link', { name: /settings/i }).click();
    
    const settings = pages.settingsPage();
    await settings.goToApiKeys();
    await settings.setApiKey('OpenAI', 'sk-test-key-123');
    await settings.save();

    // Step 3: Retry video creation
    await dashboard.goto('/dashboard');
    await dashboard.createNewProject();
    
    await wizard.fillBasicInfo('Retry Test', 'Should succeed now', '10');
    await wizard.goToNextStep();
    await wizard.submit();

    // Verify success this time
    await expect(page.locator('[data-testid="processing-indicator"]')).toBeVisible({
      timeout: 10000
    });
  });

  test('Project search and filtering', async ({ page }) => {
    test.setTimeout(30000);

    const dashboard = pages.dashboardPage();
    await dashboard.goto('/dashboard');

    // Create multiple test projects
    for (let i = 1; i <= 3; i++) {
      await dashboard.createNewProject();
      const wizard = pages.videoWizardPage();
      await wizard.fillBasicInfo(`Search Test ${i}`, `Description ${i}`, '5');
      await page.getByRole('button', { name: /save draft/i }).click();
      await page.waitForTimeout(500);
    }

    // Test search functionality
    await dashboard.searchProjects('Search Test 2');
    await page.waitForTimeout(500); // Wait for debounce

    const results = await page.locator('[data-testid="project-card"]').count();
    expect(results).toBe(1);

    // Verify correct project shown
    const projectTitle = await page.locator('[data-testid="project-card"]').first().textContent();
    expect(projectTitle).toContain('Search Test 2');
  });

  test('Keyboard navigation', async ({ page }) => {
    test.setTimeout(30000);

    const dashboard = pages.dashboardPage();
    await dashboard.goto('/dashboard');

    // Test keyboard shortcuts
    await page.keyboard.press('n'); // New project shortcut
    await expect(page.getByRole('dialog')).toBeVisible();

    await page.keyboard.press('Escape'); // Close dialog
    await expect(page.getByRole('dialog')).not.toBeVisible();

    // Navigate with Tab
    await page.keyboard.press('Tab');
    const activeElement = await page.evaluate(() => document.activeElement?.tagName);
    expect(activeElement).toBeTruthy();
  });

  test('Responsive behavior', async ({ page }) => {
    test.setTimeout(30000);

    const dashboard = pages.dashboardPage();
    await dashboard.goto('/dashboard');

    // Test mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    await page.waitForTimeout(500);

    // Verify mobile menu visible
    const mobileMenu = page.getByRole('button', { name: /menu/i });
    await expect(mobileMenu).toBeVisible();

    // Test tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.waitForTimeout(500);

    // Test desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.waitForTimeout(500);

    // Verify layout adapted correctly
    const mainNav = page.getByRole('navigation');
    await expect(mainNav).toBeVisible();
  });

  test('Accessibility compliance', async ({ page }) => {
    test.setTimeout(30000);

    const dashboard = pages.dashboardPage();
    await dashboard.goto('/dashboard');

    // Check for proper ARIA labels
    const buttons = page.getByRole('button');
    const buttonCount = await buttons.count();
    
    for (let i = 0; i < Math.min(buttonCount, 5); i++) {
      const button = buttons.nth(i);
      const ariaLabel = await button.getAttribute('aria-label');
      const text = await button.textContent();
      
      // Button should have either aria-label or visible text
      expect(ariaLabel || text).toBeTruthy();
    }

    // Check for proper heading hierarchy
    const h1Count = await page.locator('h1').count();
    expect(h1Count).toBeGreaterThan(0);

    // Check for skip links
    const skipLink = page.getByRole('link', { name: /skip to main content/i });
    // Skip link may not be visible but should exist
    const skipLinkExists = await skipLink.count() > 0;
    expect(skipLinkExists).toBe(true);
  });

  test('Concurrent operations handling', async ({ page, context }) => {
    test.setTimeout(60000);

    // Open multiple tabs
    const page1 = page;
    const page2 = await context.newPage();

    const factory1 = new PageFactory(page1);
    const factory2 = new PageFactory(page2);

    // Navigate both to dashboard
    await factory1.dashboardPage().goto('/dashboard');
    await factory2.dashboardPage().goto('/dashboard');

    // Create projects simultaneously
    const promise1 = (async () => {
      await factory1.dashboardPage().createNewProject();
      const wizard = factory1.videoWizardPage();
      await wizard.fillBasicInfo('Concurrent 1', 'Test 1', '5');
      await page1.getByRole('button', { name: /save draft/i }).click();
    })();

    const promise2 = (async () => {
      await factory2.dashboardPage().createNewProject();
      const wizard = factory2.videoWizardPage();
      await wizard.fillBasicInfo('Concurrent 2', 'Test 2', '5');
      await page2.getByRole('button', { name: /save draft/i }).click();
    })();

    // Wait for both to complete
    await Promise.all([promise1, promise2]);

    // Verify both projects created successfully
    await factory1.dashboardPage().goto('/dashboard');
    const projectCount = await factory1.dashboardPage().getProjectCount();
    expect(projectCount).toBeGreaterThanOrEqual(2);

    await page2.close();
  });

  test('Long-running operation cancellation', async ({ page }) => {
    test.setTimeout(45000);

    const dashboard = pages.dashboardPage();
    await dashboard.goto('/dashboard');
    await dashboard.createNewProject();

    const wizard = pages.videoWizardPage();
    await wizard.fillBasicInfo('Cancel Test', 'Test cancellation', '60');
    await wizard.goToNextStep();
    await wizard.submit();

    // Wait for processing to start
    await expect(page.locator('[data-testid="processing-indicator"]')).toBeVisible({
      timeout: 10000
    });

    // Cancel the operation
    const cancelButton = page.getByRole('button', { name: /cancel/i });
    await cancelButton.click();

    // Confirm cancellation in modal
    const modal = pages.modalDialog();
    await modal.waitForOpen();
    await modal.confirm();

    // Verify cancellation
    const toast = pages.toast();
    const message = await toast.waitForToast('info');
    expect(message).toContain('cancelled');

    // Verify can create new project after cancellation
    await dashboard.goto('/dashboard');
    await dashboard.createNewProject();
    await expect(page.getByRole('dialog')).toBeVisible();
  });
});
