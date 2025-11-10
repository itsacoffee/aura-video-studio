# Accessibility Testing Guide

## Overview

This guide provides comprehensive instructions for testing the accessibility features implemented in PR #10. It covers keyboard navigation, screen reader compatibility, visual accessibility, and WCAG 2.1 AA compliance.

## Quick Test Checklist

### Essential Tests (5 minutes)
- [ ] Tab through entire page - can reach all interactive elements
- [ ] Press Ctrl+/ to open keyboard shortcuts cheat sheet
- [ ] Press Tab from top of page - skip links appear
- [ ] Toggle high contrast mode in accessibility settings
- [ ] Test Ctrl+N, Ctrl+S, Ctrl+G, Ctrl+, shortcuts

### Comprehensive Tests (30 minutes)
Follow sections 2-7 below for detailed testing procedures.

---

## 1. Keyboard Navigation Testing

### 1.1 Basic Navigation
**Purpose**: Verify all interactive elements are keyboard accessible

**Steps**:
1. Load the application
2. Press `Tab` key repeatedly
3. Verify focus indicator is visible on each element
4. Press `Shift+Tab` to navigate backwards
5. Test on multiple pages (Dashboard, Create, Editor, Settings)

**Expected Results**:
- ✅ Focus moves to next interactive element with Tab
- ✅ Focus moves to previous element with Shift+Tab
- ✅ Focus indicator is clearly visible (blue outline)
- ✅ No elements are skipped
- ✅ No keyboard traps (can always move focus)
- ✅ Focus order is logical (follows visual layout)

### 1.2 Skip Links
**Purpose**: Verify skip navigation links work

**Steps**:
1. Load any page
2. Press `Tab` once from the top
3. Observe "Skip to main content" link appears
4. Press `Enter` to activate
5. Verify focus moves to main content area

**Expected Results**:
- ✅ Skip links appear on first Tab
- ✅ Skip links are visually styled (not just hidden)
- ✅ Activating skip link moves focus correctly
- ✅ Multiple skip links available (main, nav, footer)

### 1.3 Global Keyboard Shortcuts
**Purpose**: Test all global shortcuts work as expected

| Shortcut | Expected Action | Test Status |
|----------|----------------|-------------|
| `Ctrl+N` | Navigate to /create | [ ] |
| `Ctrl+S` | Save current project | [ ] |
| `Ctrl+G` | Navigate to generate video | [ ] |
| `Ctrl+,` | Open settings | [ ] |
| `Ctrl+/` | Open shortcuts cheat sheet | [ ] |
| `Ctrl+K` | Open command palette | [ ] |
| `Ctrl+O` | Open projects | [ ] |
| `Ctrl+I` | Open ideation | [ ] |
| `Ctrl+E` | Open video editor | [ ] |
| `?` | Open shortcuts panel | [ ] |

**Windows/Linux**: Use `Ctrl`  
**macOS**: Automatically uses `Cmd` instead of `Ctrl`

### 1.4 Modal/Dialog Navigation
**Purpose**: Verify focus trap in modals works correctly

**Steps**:
1. Open any modal (e.g., keyboard shortcuts with `Ctrl+/`)
2. Press `Tab` repeatedly
3. Verify focus stays within modal
4. When reaching last element, Tab cycles back to first
5. Press `Escape` to close modal
6. Verify focus returns to trigger element

**Expected Results**:
- ✅ Focus trapped within modal
- ✅ Tab cycles through modal elements
- ✅ Shift+Tab works in reverse
- ✅ Escape closes modal
- ✅ Focus returns to original element after closing

---

## 2. Screen Reader Testing

### 2.1 Recommended Screen Readers
- **Windows**: NVDA (free), JAWS (paid)
- **macOS**: VoiceOver (built-in)
- **Linux**: Orca (free)
- **Mobile**: TalkBack (Android), VoiceOver (iOS)

### 2.2 NVDA Testing (Windows)

**Setup**:
1. Download NVDA from https://www.nvaccess.org/
2. Install and launch NVDA
3. Open application in browser

**Basic Tests**:
```
Test 1: Page Structure
- Press H to navigate by headings
- Verify all major sections have headings
- Press L to navigate by links
- Press B to navigate by buttons

Test 2: Forms
- Navigate to a form (Settings page)
- Tab to each form field
- NVDA should announce: label, field type, value, required status
- Enter invalid data and Tab away
- NVDA should announce error message

Test 3: ARIA Live Regions
- Trigger an action (e.g., save project)
- NVDA should announce success/error message
- No need to move focus
```

**Expected Announcements**:
- "Main content, main landmark"
- "Main navigation, navigation landmark"
- "Email, edit, required, enter your email address"
- "Invalid email format, alert"

### 2.3 VoiceOver Testing (macOS)

**Setup**:
1. Press `Cmd+F5` to enable VoiceOver
2. Open application in Safari or Chrome

**Basic Tests**:
```
Test 1: Rotor Navigation
- Press VO+U to open rotor
- Navigate by headings, links, form controls
- Verify all elements are listed

Test 2: Forms
- Navigate to Settings page
- Use VO+Right Arrow to move through form
- Verify all labels are announced
- Test error message announcements

Test 3: Images
- Navigate to pages with images
- Verify all images have alt text
- Decorative images should be ignored
```

### 2.4 Screen Reader Checklist

| Element | Test | Status |
|---------|------|--------|
| Headings | Hierarchical (h1 → h2 → h3) | [ ] |
| Links | Descriptive text (not "click here") | [ ] |
| Buttons | Clear purpose announced | [ ] |
| Forms | All fields have labels | [ ] |
| Errors | Associated with fields | [ ] |
| Images | Alt text or aria-label | [ ] |
| Landmarks | Main, nav, footer defined | [ ] |
| Live regions | Announcements work | [ ] |

---

## 3. Visual Accessibility Testing

### 3.1 High Contrast Mode

**Steps**:
1. Navigate to Settings → Accessibility
2. Enable "High Contrast Mode"
3. Navigate through application
4. Check all pages for readability

**Expected Results**:
- ✅ Text has higher contrast with background
- ✅ Borders are more prominent
- ✅ Interactive elements clearly visible
- ✅ No loss of information
- ✅ Icons and graphics remain visible

**Browser High Contrast Test**:
- Windows: Enable High Contrast in Windows Settings
- Verify app respects system preference

### 3.2 Focus Indicators

**Steps**:
1. Tab through all interactive elements
2. Verify focus indicator on each element
3. Enable "Enhanced Focus Indicators" in settings
4. Re-test with enhanced mode

**Expected Results**:
- ✅ Focus indicator always visible
- ✅ Minimum 2px outline
- ✅ Enhanced mode has 3px outline + shadow
- ✅ Sufficient contrast (3:1 minimum)
- ✅ Doesn't obscure content

### 3.3 Color Contrast

**Tool**: Use WebAIM Contrast Checker or browser DevTools

**Test Cases**:
```
Text on Background:
- Normal text: Minimum 4.5:1 contrast
- Large text (18pt+): Minimum 3:1 contrast
- UI components: Minimum 3:1 contrast

Check:
- Body text vs background
- Link text vs background  
- Button text vs button background
- Error messages vs background
- Disabled element text (minimum 3:1)
```

**Quick Test**:
1. Open browser DevTools
2. Inspect element
3. Check "Contrast" in Styles panel
4. Verify ✓ mark for WCAG AA

### 3.4 Font Size Adjustment

**Steps**:
1. Navigate to Settings → Accessibility
2. Test each font size option:
   - Small (12px)
   - Medium (14px) - default
   - Large (16px)
   - Extra Large (18px)
3. Verify readability on all pages

**Expected Results**:
- ✅ Text scales proportionally
- ✅ No text overlap
- ✅ No horizontal scrolling (up to 200% zoom)
- ✅ Layouts remain usable

---

## 4. Reduced Motion Testing

### 4.1 System Preference Detection

**Windows**:
```
Settings → Ease of Access → Display → Show animations
Toggle OFF
```

**macOS**:
```
System Preferences → Accessibility → Display
Enable "Reduce motion"
```

**Expected Results**:
- ✅ App automatically detects preference
- ✅ Animations disabled on page load
- ✅ Transitions minimized

### 4.2 In-App Toggle

**Steps**:
1. Navigate to Settings → Accessibility
2. Enable "Reduced Motion"
3. Navigate through application
4. Trigger animations (modals, transitions)

**Expected Results**:
- ✅ Smooth animations replaced with instant transitions
- ✅ No jarring movements
- ✅ Page transitions still work (just faster)
- ✅ No loss of functionality

---

## 5. Form Accessibility Testing

### 5.1 Form Labels and Instructions

**Test Pages**:
- Settings page
- Create/wizard forms
- Login/setup forms

**Checklist**:
```
[ ] All form fields have visible labels
[ ] Labels are properly associated (htmlFor/id)
[ ] Required fields marked with *
[ ] Required status announced to screen readers
[ ] Helper text provided where needed
[ ] Placeholder text is NOT the only label
```

### 5.2 Error Validation

**Steps**:
1. Find form with validation (e.g., email field)
2. Enter invalid data
3. Tab out of field or submit form
4. Observe error message

**Expected Results**:
- ✅ Error message appears visually
- ✅ Error announced to screen readers
- ✅ Field marked with aria-invalid="true"
- ✅ Error associated with field (aria-describedby)
- ✅ Error message is clear and specific
- ✅ Focus moves to first error field

### 5.3 Success States

**Steps**:
1. Enter valid data in form field
2. Observe success indicator

**Expected Results**:
- ✅ Success checkmark appears
- ✅ Success announced to screen readers
- ✅ Field styling changes (green border)

---

## 6. Automated Testing

### 6.1 Lighthouse Accessibility Audit

**Steps**:
1. Open Chrome DevTools (F12)
2. Go to "Lighthouse" tab
3. Select "Accessibility" only
4. Click "Generate report"

**Expected Results**:
- ✅ Score: 90+ (ideally 95-100)
- ✅ No critical issues
- ✅ All ARIA attributes valid

### 6.2 axe DevTools

**Setup**:
1. Install axe DevTools browser extension
2. Open application in browser
3. Open DevTools → axe tab

**Steps**:
1. Click "Scan all of my page"
2. Review any issues found
3. Fix critical and serious issues
4. Re-scan to verify

**Expected Results**:
- ✅ 0 critical issues
- ✅ 0 serious issues
- ✅ Few (if any) moderate issues
- ✅ Best practices followed

### 6.3 WAVE Tool

**URL**: https://wave.webaim.org/

**Steps**:
1. Enter application URL
2. Review visual indicators on page:
   - Green: Accessibility features (good!)
   - Red: Errors (must fix)
   - Yellow: Alerts (review needed)
   - Blue: Structural elements
3. Click each indicator for details

---

## 7. Mobile Accessibility Testing

### 7.1 iOS VoiceOver

**Enable VoiceOver**:
```
Settings → Accessibility → VoiceOver → ON
```

**Gestures**:
- Swipe right: Next element
- Swipe left: Previous element
- Double tap: Activate element
- Three-finger swipe: Scroll

**Tests**:
1. Navigate through app with VoiceOver
2. Test all interactive elements
3. Verify skip links work
4. Test form inputs

### 7.2 Android TalkBack

**Enable TalkBack**:
```
Settings → Accessibility → TalkBack → ON
```

**Gestures**:
- Swipe right: Next element
- Swipe left: Previous element
- Double tap: Activate
- Swipe down then up: First item

**Tests**:
1. Navigate app with TalkBack
2. Test all buttons and links
3. Verify form accessibility
4. Test navigation

---

## 8. Browser Compatibility

### 8.1 Test Matrix

| Browser | Version | Keyboard Nav | Screen Reader | High Contrast |
|---------|---------|--------------|---------------|---------------|
| Chrome | Latest | [ ] | [ ] | [ ] |
| Firefox | Latest | [ ] | [ ] | [ ] |
| Safari | Latest | [ ] | [ ] | [ ] |
| Edge | Latest | [ ] | [ ] | [ ] |

### 8.2 Browser-Specific Tests

**Firefox**:
- Test with NVDA screen reader
- Verify focus indicators work
- Check high contrast mode

**Safari**:
- Test with VoiceOver
- Verify skip links work
- Check reduced motion support

**Chrome**:
- Run Lighthouse audit
- Test with ChromeVox extension
- Verify keyboard navigation

---

## 9. Issues to Look For

### Common Accessibility Violations

**Keyboard Navigation**:
- ⚠️ Cannot reach element with keyboard
- ⚠️ Keyboard trap (can't escape)
- ⚠️ Focus indicator not visible
- ⚠️ Illogical tab order

**Screen Readers**:
- ⚠️ Missing alt text on images
- ⚠️ Form field without label
- ⚠️ Generic link text ("click here")
- ⚠️ Error not announced
- ⚠️ Missing ARIA landmarks

**Visual**:
- ⚠️ Low color contrast
- ⚠️ Information conveyed by color only
- ⚠️ Text too small
- ⚠️ Focus indicator missing

**Forms**:
- ⚠️ Error message not associated with field
- ⚠️ Required field not indicated
- ⚠️ No helper text for complex inputs
- ⚠️ Placeholder used as label

---

## 10. Reporting Issues

### Issue Template

```markdown
**Issue Type**: [Keyboard Nav | Screen Reader | Visual | Forms]
**Severity**: [Critical | High | Medium | Low]
**WCAG Criterion**: [e.g., 2.1.1 Keyboard]

**Description**:
Clear description of the issue

**Steps to Reproduce**:
1. Navigate to [page]
2. [Action]
3. [Expected result]

**Actual Result**:
What happened

**Expected Result**:
What should happen

**Browser/AT**: [e.g., Chrome + NVDA]
**Screenshots**: [if applicable]
```

### Priority Levels

**Critical** (P0):
- Complete keyboard trap
- Essential functionality not accessible
- WCAG A violation

**High** (P1):
- Important feature not keyboard accessible
- Screen reader cannot access key feature
- WCAG AA violation

**Medium** (P2):
- Minor keyboard navigation issue
- Unclear screen reader announcement
- Enhancement opportunity

**Low** (P3):
- Cosmetic issue
- Nice-to-have improvement
- Best practice not followed

---

## 11. Certification Checklist

Before considering accessibility complete, verify:

### WCAG 2.1 Level AA
- [ ] 1.1.1 Non-text Content (A)
- [ ] 1.3.1 Info and Relationships (A)
- [ ] 1.4.3 Contrast (Minimum) (AA)
- [ ] 2.1.1 Keyboard (A)
- [ ] 2.1.2 No Keyboard Trap (A)
- [ ] 2.4.3 Focus Order (A)
- [ ] 2.4.7 Focus Visible (AA)
- [ ] 3.2.1 On Focus (A)
- [ ] 3.2.2 On Input (A)
- [ ] 3.3.1 Error Identification (A)
- [ ] 3.3.2 Labels or Instructions (A)
- [ ] 4.1.2 Name, Role, Value (A)

### Additional Checks
- [ ] Works with NVDA/JAWS (Windows)
- [ ] Works with VoiceOver (macOS)
- [ ] Passes Lighthouse (90+)
- [ ] Passes axe DevTools (0 critical/serious)
- [ ] Works on mobile screen readers
- [ ] High contrast mode functional
- [ ] Reduced motion works
- [ ] All keyboard shortcuts work
- [ ] Focus management in modals works

---

## Resources

### Tools
- **axe DevTools**: https://www.deque.com/axe/devtools/
- **WAVE**: https://wave.webaim.org/
- **Lighthouse**: Built into Chrome DevTools
- **NVDA**: https://www.nvaccess.org/
- **Contrast Checker**: https://webaim.org/resources/contrastchecker/

### Documentation
- **WCAG 2.1**: https://www.w3.org/WAI/WCAG21/quickref/
- **ARIA Practices**: https://www.w3.org/WAI/ARIA/apg/
- **MDN Accessibility**: https://developer.mozilla.org/en-US/docs/Web/Accessibility

### Testing
- **Keyboard Testing**: https://webaim.org/articles/keyboard/
- **Screen Reader Testing**: https://webaim.org/articles/screenreader_testing/
- **Mobile Accessibility**: https://www.w3.org/WAI/mobile/

---

## Test Sign-Off

**Date Tested**: _______________  
**Tested By**: _______________  
**Tools Used**: _______________  
**Result**: [ ] Pass [ ] Pass with Minor Issues [ ] Fail  
**Notes**:

```
```

---

**Document Version**: 1.0  
**Last Updated**: 2025-11-10  
**Related PR**: #10
