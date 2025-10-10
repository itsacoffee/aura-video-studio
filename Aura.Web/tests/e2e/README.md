# Playwright Visual Regression Testing

This directory contains visual regression tests for Aura.Web. Snapshots are stored per-branch to avoid conflicts during development.

## Directory Structure

```
.playwright-snapshots/
├── chromium/
│   ├── wizard-step1-brief.png
│   ├── wizard-step1-filled.png
│   ├── settings-dark-mode.png
│   ├── settings-light-mode.png
│   └── dashboard.png
```

## Running Tests

```bash
# Run all E2E tests
npm run playwright

# Run only visual regression tests
npm run playwright -- visual.spec.ts

# Run in UI mode (interactive)
npm run playwright:ui

# Update snapshots (when intentional UI changes are made)
npm run playwright -- --update-snapshots
```

## Updating Baselines on Main

When merging to `main` branch and you've made intentional UI changes:

1. **On your feature branch**, update snapshots:
   ```bash
   npm run playwright -- --update-snapshots
   ```

2. **Commit the updated snapshots** to your branch:
   ```bash
   git add .playwright-snapshots/
   git commit -m "Update visual regression baselines"
   ```

3. **Push and merge** your PR. The CI will verify the snapshots match.

4. **On main branch**, other developers will automatically use the updated baselines.

## How It Works

- Snapshots are taken using Playwright's `toHaveScreenshot()` matcher
- Each snapshot is compared pixel-by-pixel with the baseline
- Small differences trigger test failures
- Threshold can be adjusted in `playwright.config.ts`

## Best Practices

1. **Always review snapshot diffs** before updating baselines
2. **Update snapshots only for intentional changes**, not random test flakiness
3. **Run tests locally** before pushing to catch visual regressions early
4. **Disable animations** in tests to reduce flakiness (already configured)
5. **Use full-page screenshots** to catch layout issues

## Troubleshooting

### Tests failing due to font rendering differences
- Ensure you have the same fonts installed as CI
- Consider increasing pixel difference threshold

### Tests failing due to timing issues
- Add explicit waits before taking snapshots
- Use `await page.waitForLoadState('networkidle')` if needed

### Snapshots look correct but tests still fail
- Delete `.playwright-snapshots/` and regenerate:
  ```bash
  rm -rf .playwright-snapshots/
  npm run playwright -- --update-snapshots
  ```

## CI Integration

Visual regression tests run on every PR:
- Baseline snapshots are stored in the repository
- CI compares test screenshots against baselines
- Failed tests provide diff images in the test report
- Review the HTML test report artifact in GitHub Actions
