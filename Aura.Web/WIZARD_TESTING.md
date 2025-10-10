# Wizard UX Testing Guide

## Manual Test Cases

### Test 1: Default Values Applied
**Purpose**: Verify all fields have sensible defaults when wizard loads fresh

**Steps**:
1. Clear browser localStorage: `localStorage.clear()`
2. Navigate to `/create`
3. Verify defaults in Step 1:
   - Topic: empty (required field)
   - Audience: "General"
   - Goal: "Inform"
   - Tone: "Informative"
   - Language: "en-US"
   - Aspect: "Widescreen16x9"
4. Click "Next" without topic → should show error
5. Enter topic and click "Next"
6. Verify defaults in Step 2:
   - Duration: 3.0 minutes
   - Pacing: "Conversational"
   - Density: "Balanced"
   - Style: "Standard"
   - Brand Kit: watermarkOpacity = 0.7
   - Captions: enabled = true, format = "srt", burnIn = false
   - Stock Sources: Pexels, Pixabay, Unsplash = enabled
   - Offline Mode: false

**Expected Result**: ✅ All defaults match specification

---

### Test 2: Settings Persistence
**Purpose**: Verify settings save to localStorage and restore on page reload

**Steps**:
1. Navigate to `/create`
2. Fill in custom values:
   - Topic: "Test Video"
   - Audience: "Professionals"
   - Duration: 5 minutes
   - Brand Color: "#FF6B35"
3. Refresh page (F5)
4. Verify values persist:
   - Topic: "Test Video"
   - Audience: "Professionals"
   - Duration: 5 minutes
   - Brand Color: "#FF6B35"

**Expected Result**: ✅ All custom values persist across refresh

---

### Test 3: Free Profile Workflow
**Purpose**: Complete wizard with Free-Only profile

**Steps**:
1. Clear localStorage
2. Navigate to `/create`
3. Step 1: Enter topic "Free Tier Test", click Next
4. Step 2: Keep defaults, click Next
5. Step 3: 
   - Select profile: "Free-Only"
   - Click "Run Preflight Check"
   - Wait for results
   - If passed: Click "Generate Video"
   - If failed: Enable override, then generate
6. Verify API call made to `/api/script`

**Expected Result**: ✅ Wizard completes successfully with Free profile

---

### Test 4: Pro Profile Workflow
**Purpose**: Complete wizard with Pro-Max profile

**Steps**:
1. Navigate to `/create`
2. Step 1: Enter topic "Pro Tier Test", click Next
3. Step 2: 
   - Enable Offline Mode: false
   - Enable Stable Diffusion
   - Add watermark path
   - Click Next
4. Step 3:
   - Select profile: "Pro-Max"
   - Run preflight check
   - Review settings
   - Generate video

**Expected Result**: ✅ Wizard completes with Pro profile and advanced settings

---

### Test 5: Tooltips Display
**Purpose**: Verify all tooltips show correct content and links

**Steps**:
1. Navigate to `/create`
2. Hover over info icon (ℹ️) next to each field
3. Verify tooltip appears with:
   - Descriptive text
   - "Learn more" link (if applicable)
4. Test tooltips for:
   - Topic, Audience, Tone, Aspect (Step 1)
   - Duration, Pacing, Density (Step 2)
   - Brand Kit, Captions, Stock Sources (Step 2)

**Expected Result**: ✅ All tooltips display with correct content

---

### Test 6: Advanced Settings Progressive Disclosure
**Purpose**: Verify advanced section collapses/expands correctly

**Steps**:
1. Navigate to `/create`, proceed to Step 2
2. Scroll to "Advanced Settings" section
3. Click accordion header
4. Verify section expands with:
   - Visual Style dropdown
   - Stable Diffusion URL (if SD enabled)
   - Reset button
5. Click header again
6. Verify section collapses

**Expected Result**: ✅ Advanced settings expand/collapse smoothly

---

### Test 7: Reset to Defaults
**Purpose**: Verify reset button restores factory settings

**Steps**:
1. Navigate to `/create`
2. Change multiple values:
   - Duration: 10 minutes
   - Pacing: Fast
   - Brand color: "#123456"
   - Disable Pexels
3. Proceed to Step 2, expand Advanced
4. Click "Reset All to Defaults"
5. Confirm dialog
6. Verify all values reset:
   - Duration: 3.0
   - Pacing: Conversational
   - Brand color: empty
   - Pexels: enabled

**Expected Result**: ✅ All settings restore to defaults after confirmation

---

### Test 8: Keyboard Navigation
**Purpose**: Verify keyboard-only operation

**Steps**:
1. Navigate to `/create`
2. Use keyboard only:
   - Tab: move between fields
   - Shift+Tab: move backwards
   - Enter: open dropdowns, select options
   - Space: toggle switches
   - Arrow keys: adjust sliders, navigate dropdowns
3. Complete all 3 steps using keyboard only
4. Verify focus indicators visible
5. Check keyboard hint at top: "Tip: Press Tab to navigate..."

**Expected Result**: ✅ Entire wizard navigable via keyboard

---

### Test 9: Validation Errors
**Purpose**: Verify validation prevents invalid submissions

**Steps**:
1. Navigate to `/create`
2. Step 1: Leave topic empty
3. Click "Next"
4. Verify button disabled or error shown
5. Enter topic, click Next
6. Step 2: Click Next (should work)
7. Step 3: Click "Generate Video" without preflight
8. Verify button disabled with tooltip message

**Expected Result**: ✅ Validation prevents invalid state progression

---

### Test 10: Offline Mode Toggle
**Purpose**: Verify offline mode affects available options

**Steps**:
1. Navigate to `/create`, proceed to Step 2
2. Enable "Offline Mode"
3. Verify UI updates appropriately
4. Proceed to Step 3
5. Verify only Free/Local profiles suggested
6. Go back, disable Offline Mode
7. Verify Pro profiles available again

**Expected Result**: ✅ Offline mode constrains profile selection

---

## Automated Test Ideas

### Unit Tests (when test framework added)
```typescript
describe('CreateWizard Defaults', () => {
  it('should initialize with default brief settings', () => {
    const settings = createDefaultSettings();
    expect(settings.brief.audience).toBe('General');
    expect(settings.brief.tone).toBe('Informative');
    expect(settings.brief.language).toBe('en-US');
  });

  it('should initialize with default plan settings', () => {
    const settings = createDefaultSettings();
    expect(settings.planSpec.targetDurationMinutes).toBe(3.0);
    expect(settings.planSpec.pacing).toBe('Conversational');
    expect(settings.planSpec.density).toBe('Balanced');
  });

  it('should initialize with default brand kit settings', () => {
    const settings = createDefaultSettings();
    expect(settings.brandKit.watermarkOpacity).toBe(0.7);
  });

  it('should initialize with default captions settings', () => {
    const settings = createDefaultSettings();
    expect(settings.captions.enabled).toBe(true);
    expect(settings.captions.format).toBe('srt');
    expect(settings.captions.burnIn).toBe(false);
  });

  it('should initialize with default stock sources', () => {
    const settings = createDefaultSettings();
    expect(settings.stockSources.enablePexels).toBe(true);
    expect(settings.stockSources.enablePixabay).toBe(true);
    expect(settings.stockSources.enableUnsplash).toBe(true);
  });
});
```

### E2E Tests (when Playwright/Cypress added)
```typescript
describe('Wizard E2E', () => {
  it('should complete wizard with Free profile', async () => {
    await page.goto('/create');
    await page.fill('[placeholder="Enter your video topic"]', 'Test Video');
    await page.click('text=Next');
    await page.click('text=Next');
    await page.selectOption('[label="Profile"]', 'Free-Only');
    await page.click('text=Run Preflight Check');
    await page.waitForSelector('text=Generate Video:not([disabled])');
    await page.click('text=Generate Video');
    // Assert API call made
  });

  it('should complete wizard with Pro profile', async () => {
    await page.goto('/create');
    await page.fill('[placeholder="Enter your video topic"]', 'Pro Test');
    await page.click('text=Next');
    await page.click('text=Next');
    await page.selectOption('[label="Profile"]', 'Pro-Max');
    await page.click('text=Run Preflight Check');
    // Continue workflow...
  });
});
```

## Test Checklist

Before merging:
- [ ] All manual tests pass
- [ ] Tooltips display correctly
- [ ] Settings persist across refresh
- [ ] Keyboard navigation works
- [ ] Reset button functions
- [ ] Free profile workflow completes
- [ ] Pro profile workflow completes
- [ ] Validation prevents invalid states
- [ ] Build succeeds without errors
- [ ] TypeScript types are correct
- [ ] Documentation is accurate

## Known Limitations

1. **No automated tests**: Web project lacks test infrastructure (Vitest/Jest)
2. **Manual testing required**: All verification done by hand
3. **API mocking**: Backend API calls not mocked
4. **Visual regression**: No screenshot comparison

## Future Testing Enhancements

1. Add Vitest for unit tests
2. Add Playwright for E2E tests
3. Add Storybook for component testing
4. Add visual regression testing
5. Add accessibility auditing (axe-core)
