/**
 * E2E tests for timeline multi-select functionality
 */

import { test, expect } from '@playwright/test';

test.describe('Timeline Multi-Select', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/timeline');
  });

  test('should select single clip by clicking', async ({ page }) => {
    const clip = page.locator('[data-testid="timeline-clip"]').first();
    await clip.click();

    await expect(clip).toHaveAttribute('data-selected', 'true');
  });

  test('should select multiple clips with Ctrl+Click', async ({ page }) => {
    const clips = page.locator('[data-testid="timeline-clip"]');

    await clips.nth(0).click();
    await page.keyboard.down('Control');
    await clips.nth(1).click();
    await clips.nth(2).click();
    await page.keyboard.up('Control');

    await expect(clips.nth(0)).toHaveAttribute('data-selected', 'true');
    await expect(clips.nth(1)).toHaveAttribute('data-selected', 'true');
    await expect(clips.nth(2)).toHaveAttribute('data-selected', 'true');
  });

  test('should select range with Shift+Click', async ({ page }) => {
    const clips = page.locator('[data-testid="timeline-clip"]');

    await clips.nth(0).click();
    await page.keyboard.down('Shift');
    await clips.nth(4).click();
    await page.keyboard.up('Shift');

    for (let i = 0; i <= 4; i++) {
      await expect(clips.nth(i)).toHaveAttribute('data-selected', 'true');
    }
  });

  test('should deselect with Ctrl+D', async ({ page }) => {
    const clip = page.locator('[data-testid="timeline-clip"]').first();
    await clip.click();
    await expect(clip).toHaveAttribute('data-selected', 'true');

    await page.keyboard.press('Control+d');

    await expect(clip).not.toHaveAttribute('data-selected', 'true');
  });

  test('should select all clips with Ctrl+A', async ({ page }) => {
    await page.keyboard.press('Control+a');

    const clips = page.locator('[data-testid="timeline-clip"]');
    const count = await clips.count();

    for (let i = 0; i < count; i++) {
      await expect(clips.nth(i)).toHaveAttribute('data-selected', 'true');
    }
  });

  test('should delete multiple selected clips with Delete', async ({ page }) => {
    const clips = page.locator('[data-testid="timeline-clip"]');
    const initialCount = await clips.count();

    await clips.nth(0).click();
    await page.keyboard.down('Control');
    await clips.nth(1).click();
    await page.keyboard.up('Control');

    await page.keyboard.press('Delete');

    const newCount = await clips.count();
    expect(newCount).toBe(initialCount - 2);
  });

  test('should ripple delete with Shift+Delete', async ({ page }) => {
    const clips = page.locator('[data-testid="timeline-clip"]');
    const firstClip = clips.nth(0);

    const firstClipPosition = await firstClip.boundingBox();
    await firstClip.click();
    await page.keyboard.press('Shift+Delete');

    const newFirstClip = clips.nth(0);
    const newFirstClipPosition = await newFirstClip.boundingBox();

    expect(newFirstClipPosition?.x).toBe(firstClipPosition?.x);
  });

  test('should move multiple clips together', async ({ page }) => {
    const clips = page.locator('[data-testid="timeline-clip"]');

    await clips.nth(0).click();
    await page.keyboard.down('Control');
    await clips.nth(1).click();
    await page.keyboard.up('Control');

    const clip0Before = await clips.nth(0).boundingBox();
    const clip1Before = await clips.nth(1).boundingBox();

    await page.keyboard.press('ArrowRight');
    await page.keyboard.press('ArrowRight');
    await page.keyboard.press('ArrowRight');

    const clip0After = await clips.nth(0).boundingBox();
    const clip1After = await clips.nth(1).boundingBox();

    expect(clip0After?.x).toBeGreaterThan(clip0Before?.x || 0);
    expect(clip1After?.x).toBeGreaterThan(clip1Before?.x || 0);

    const deltaClip0 = (clip0After?.x || 0) - (clip0Before?.x || 0);
    const deltaClip1 = (clip1After?.x || 0) - (clip1Before?.x || 0);

    expect(Math.abs(deltaClip0 - deltaClip1)).toBeLessThan(2);
  });

  test('should undo multi-select delete with Ctrl+Z', async ({ page }) => {
    const clips = page.locator('[data-testid="timeline-clip"]');
    const initialCount = await clips.count();

    await clips.nth(0).click();
    await page.keyboard.down('Control');
    await clips.nth(1).click();
    await page.keyboard.up('Control');

    await page.keyboard.press('Delete');

    const countAfterDelete = await clips.count();
    expect(countAfterDelete).toBe(initialCount - 2);

    await page.keyboard.press('Control+z');

    const countAfterUndo = await clips.count();
    expect(countAfterUndo).toBe(initialCount);
  });

  test('should redo with Ctrl+Y', async ({ page }) => {
    const clips = page.locator('[data-testid="timeline-clip"]');
    const initialCount = await clips.count();

    await clips.nth(0).click();
    await page.keyboard.press('Delete');
    await page.keyboard.press('Control+z');

    const countAfterUndo = await clips.count();
    expect(countAfterUndo).toBe(initialCount);

    await page.keyboard.press('Control+y');

    const countAfterRedo = await clips.count();
    expect(countAfterRedo).toBe(initialCount - 1);
  });

  test('should split clip at playhead with S key', async ({ page }) => {
    const clips = page.locator('[data-testid="timeline-clip"]');
    const initialCount = await clips.count();

    await clips.nth(0).click();

    const playhead = page.locator('[data-testid="playhead"]');
    await playhead.dragTo(clips.nth(0), {
      targetPosition: { x: 50, y: 10 },
    });

    await page.keyboard.press('s');

    const newCount = await clips.count();
    expect(newCount).toBe(initialCount + 1);
  });

  test('should add marker with M key', async ({ page }) => {
    const markers = page.locator('[data-testid="timeline-marker"]');
    const initialCount = await markers.count();

    await page.keyboard.press('m');

    const newCount = await markers.count();
    expect(newCount).toBe(initialCount + 1);
  });

  test('should toggle ripple edit mode with Ctrl+R', async ({ page }) => {
    const rippleIndicator = page.locator('[data-testid="ripple-mode-indicator"]');

    await expect(rippleIndicator).not.toBeVisible();

    await page.keyboard.press('Control+r');

    await expect(rippleIndicator).toBeVisible();

    await page.keyboard.press('Control+r');

    await expect(rippleIndicator).not.toBeVisible();
  });

  test('should zoom in with + key', async ({ page }) => {
    const timeline = page.locator('[data-testid="timeline-container"]');
    const initialWidth = await timeline.evaluate((el) => el.scrollWidth);

    await page.keyboard.press('+');
    await page.keyboard.press('+');
    await page.keyboard.press('+');

    const newWidth = await timeline.evaluate((el) => el.scrollWidth);
    expect(newWidth).toBeGreaterThan(initialWidth);
  });

  test('should zoom out with - key', async ({ page }) => {
    await page.keyboard.press('+');
    await page.keyboard.press('+');
    await page.keyboard.press('+');

    const timeline = page.locator('[data-testid="timeline-container"]');
    const beforeWidth = await timeline.evaluate((el) => el.scrollWidth);

    await page.keyboard.press('-');
    await page.keyboard.press('-');

    const afterWidth = await timeline.evaluate((el) => el.scrollWidth);
    expect(afterWidth).toBeLessThan(beforeWidth);
  });

  test('should navigate with arrow keys', async ({ page }) => {
    const playhead = page.locator('[data-testid="playhead"]');
    const initialPosition = await playhead.boundingBox();

    await page.keyboard.press('ArrowRight');
    await page.keyboard.press('ArrowRight');
    await page.keyboard.press('ArrowRight');

    const newPosition = await playhead.boundingBox();
    expect(newPosition?.x).toBeGreaterThan(initialPosition?.x || 0);

    await page.keyboard.press('ArrowLeft');

    const afterLeftPosition = await playhead.boundingBox();
    expect(afterLeftPosition?.x).toBeLessThan(newPosition?.x || 0);
  });

  test('should jump to start with Home key', async ({ page }) => {
    await page.keyboard.press('End');
    await page.keyboard.press('Home');

    const timecodeDisplay = page.locator('[data-testid="timecode-display"]');
    await expect(timecodeDisplay).toHaveText('00:00:00:00');
  });

  test('should jump to end with End key', async ({ page }) => {
    await page.keyboard.press('End');

    const timecodeDisplay = page.locator('[data-testid="timecode-display"]');
    const timecode = await timecodeDisplay.textContent();
    expect(timecode).not.toBe('00:00:00:00');
  });

  test('should set in point with I key', async ({ page }) => {
    await page.keyboard.press('i');

    const inPointMarker = page.locator('[data-testid="in-point-marker"]');
    await expect(inPointMarker).toBeVisible();
  });

  test('should set out point with O key', async ({ page }) => {
    await page.keyboard.press('End');
    await page.keyboard.press('o');

    const outPointMarker = page.locator('[data-testid="out-point-marker"]');
    await expect(outPointMarker).toBeVisible();
  });

  test('should clear in/out points with X key', async ({ page }) => {
    await page.keyboard.press('i');
    await page.keyboard.press('End');
    await page.keyboard.press('o');

    const inPointMarker = page.locator('[data-testid="in-point-marker"]');
    const outPointMarker = page.locator('[data-testid="out-point-marker"]');

    await expect(inPointMarker).toBeVisible();
    await expect(outPointMarker).toBeVisible();

    await page.keyboard.press('x');

    await expect(inPointMarker).not.toBeVisible();
    await expect(outPointMarker).not.toBeVisible();
  });
});
