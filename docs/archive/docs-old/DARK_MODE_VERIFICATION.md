> **⚠️ ARCHIVED DOCUMENT**  
> This document is retained for historical context only.  
> It may contain outdated information, references to superseded implementations, or details specific to past pull requests.  
> For current documentation, see the main docs/ directory and canonical guides in the repository root.

---

# Dark Mode Verification Guide

This document provides instructions for manually verifying dark mode styling in Aura.Web.

## Overview

Aura.Web uses Fluent UI's theming system with both light and dark themes. The dark mode implementation uses:
- `webDarkTheme` from `@fluentui/react-components`
- Theme tokens for colors (`tokens.colorNeutralBackground1`, etc.)
- Automatic theme switching via localStorage persistence

## Manual Testing Steps

### 1. Enable Dark Mode

1. Start the development server:
   ```bash
   cd Aura.Web
   npm run dev
   ```

2. Navigate to http://localhost:5173

3. Open browser DevTools (F12) and run in console:
   ```javascript
   localStorage.setItem('darkMode', 'true');
   location.reload();
   ```

   Or toggle dark mode via the UI (if available in the Layout component)

### 2. Verify All Pages

Test each page for proper dark mode styling:

#### Pages to Test
- [ ] Welcome Page (`/`)
- [ ] Dashboard (`/dashboard`)
- [ ] Create Wizard (`/create`)
- [ ] Legacy Create (`/create/legacy`)
- [ ] Render Page (`/render`)
- [ ] Publish Page (`/publish`)
- [ ] Downloads Page (`/downloads`)
- [ ] Settings Page (`/settings`)

#### What to Check

For each page, verify:

1. **Background Colors**
   - Page background should be dark (`colorNeutralBackground1`)
   - Cards should have slightly lighter background (`colorNeutralBackground2`)
   - No white backgrounds bleeding through

2. **Text Colors**
   - Primary text should be light/white (`colorNeutralForeground1`)
   - Secondary text should be dimmed (`colorNeutralForeground2`, `colorNeutralForeground3`)
   - All text should be readable against dark backgrounds

3. **Interactive Elements**
   - Buttons maintain proper contrast
   - Hover states are visible
   - Focus states have clear borders/outlines
   - Disabled states are distinguishable

4. **Form Controls**
   - Input fields have visible borders
   - Dropdowns/selects are readable
   - Checkboxes/switches are visible
   - Sliders maintain contrast

5. **Borders and Dividers**
   - All borders use theme tokens (`colorNeutralStroke1`)
   - Dividers are visible but not too bright

### 3. Contrast Ratio Verification

Use browser DevTools or accessibility tools to verify WCAG AA compliance:

#### Required Contrast Ratios
- **Normal text**: 4.5:1 minimum
- **Large text**: 3:1 minimum
- **UI components**: 3:1 minimum

#### How to Check

1. **Chrome DevTools**:
   - Open DevTools (F12)
   - Go to Elements tab
   - Select an element
   - Check "Accessibility" pane
   - Look for contrast ratio

2. **Using WebAIM Contrast Checker**:
   - Visit https://webaim.org/resources/contrastchecker/
   - Use color picker to sample foreground and background
   - Verify ratios meet WCAG AA

3. **Automated Tools**:
   ```bash
   # Install axe-core DevTools extension
   # Chrome: https://chrome.google.com/webstore/detail/axe-devtools/lhdoppojpmngadmnindnejefpokejbdd
   # Firefox: https://addons.mozilla.org/en-US/firefox/addon/axe-devtools/
   ```

### 4. Focus States

Test keyboard navigation to ensure focus states are visible:

1. Press `Tab` to navigate through interactive elements
2. Verify each focused element has a visible outline/border
3. Focus indicator should have sufficient contrast (3:1 minimum)

#### Elements to Test
- Buttons
- Input fields
- Dropdowns
- Links
- Cards (if clickable)
- Navigation items

### 5. Common Issues to Watch For

❌ **White backgrounds**: Should use `colorNeutralBackground1` or `colorNeutralBackground2`
❌ **Black text**: Should use `colorNeutralForeground1`, `colorNeutralForeground2`, or `colorNeutralForeground3`
❌ **Hardcoded colors**: All colors should use theme tokens
❌ **Invisible borders**: Should use `colorNeutralStroke1` or `colorNeutralStroke2`
❌ **Poor focus states**: Should have clear, visible outlines

### 6. Theme Token Reference

Common tokens used in the application:

#### Backgrounds
```typescript
tokens.colorNeutralBackground1    // Main page background
tokens.colorNeutralBackground2    // Card background
tokens.colorNeutralBackground3    // Elevated surfaces
```

#### Foregrounds
```typescript
tokens.colorNeutralForeground1    // Primary text
tokens.colorNeutralForeground2    // Secondary text
tokens.colorNeutralForeground3    // Tertiary/hint text
```

#### Borders
```typescript
tokens.colorNeutralStroke1        // Standard borders
tokens.colorNeutralStroke2        // Subtle dividers
```

#### Status Colors
```typescript
tokens.colorPaletteGreenForeground1    // Success
tokens.colorPaletteRedForeground1      // Error
tokens.colorPaletteYellowForeground1   // Warning
tokens.colorPaletteBlueForeground1     // Info
```

## Automated Verification (Optional)

The visual regression tests in `tests/e2e/visual.spec.ts` include dark mode snapshots:

```bash
cd Aura.Web
npm run playwright -- visual.spec.ts
```

This will capture screenshots of key pages in dark mode for comparison.

## Reporting Issues

If you find dark mode issues:

1. Take a screenshot
2. Note the page/component
3. Describe the problem (e.g., "Text not visible", "Poor contrast")
4. Include browser and OS version
5. Open an issue with details

## Example Issue Report

```markdown
**Page**: Settings Page
**Component**: API Keys section
**Issue**: Input field borders not visible in dark mode
**Expected**: Borders should use colorNeutralStroke1 for visibility
**Actual**: Borders appear to be missing or too dark
**Browser**: Chrome 120 on Windows 11
**Screenshot**: [attach screenshot]
```

## Summary Checklist

Before marking dark mode as complete:

- [ ] All pages tested in dark mode
- [ ] Text has sufficient contrast (4.5:1 minimum)
- [ ] Interactive elements have 3:1 contrast
- [ ] Focus states are clearly visible
- [ ] No hardcoded colors remain
- [ ] Theme tokens used consistently
- [ ] Borders and dividers are visible
- [ ] Form controls are readable
- [ ] Status colors maintain contrast
- [ ] Visual regression tests pass

## Resources

- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [Fluent UI Theming Docs](https://react.fluentui.dev/?path=/docs/theme-theme--page)
- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)
- [axe DevTools](https://www.deque.com/axe/devtools/)
