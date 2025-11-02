# UI Text Spacing Conventions

This document outlines the spacing conventions for user-facing text in the Aura Video Studio application.

## Overview

All user-facing text labels, error messages, status displays, and other UI text should follow consistent spacing patterns to ensure a professional appearance.

## Label and Value Patterns

### Pattern 1: Inline Labels with Space After Colon

When a label and value are displayed inline (in the same line or adjacent elements without CSS spacing), always include a space after the colon:

✅ **Correct:**
```tsx
<Text>Error: {errorMessage}</Text>
<strong>Status: </strong> {statusValue}
<Text weight="semibold">Name: </Text>
```

❌ **Incorrect:**
```tsx
<Text>Error:{errorMessage}</Text>
<strong>Status:</strong> {statusValue}
<Text weight="semibold">Name:</Text>
```

### Pattern 2: Labels in Separate Elements with CSS Spacing

When labels and values are in separate elements with CSS spacing (flex gap, grid gap, justify-content: space-between), the colon can be without a trailing space:

✅ **Acceptable:**
```tsx
<div style={{ display: 'flex', gap: '12px' }}>
  <Text weight="semibold">Name:</Text>
  <Text>{value}</Text>
</div>

<div style={{ display: 'flex', justifyContent: 'space-between' }}>
  <Text>Label:</Text>
  <Text>Value</Text>
</div>
```

However, for consistency, **adding a space is still preferred** even with CSS spacing.

### Pattern 3: Header Labels

When a label is a section header followed by content below (not inline), no trailing space is needed:

✅ **Correct:**
```tsx
<Text weight="semibold">Logs:</Text>
<div className={styles.logContainer}>
  {logs.map(log => <div>{log}</div>)}
</div>
```

## Common Label Types

### Error Messages

Format: `"Error: {message}"`

Examples:
- `Error: Failed to load data`
- `Error: Invalid input`
- `Error: Network timeout`

### Status Displays

Format: `"Status: {state}"`

Examples:
- `Status: Running`
- `Status: Completed`
- `Status: Failed`

### Information Labels

Format: `"{Label}: {value}"`

Examples:
- `Name: John Doe`
- `Version: 1.0.0`
- `Duration: 5 minutes`
- `Type: Video`
- `Category: Educational`

### Dialog and Modal Fields

Format: `"<strong>{Field}: </strong> {value}"`

Examples:
- `<strong>Error: </strong> {error.name}`
- `<strong>Message: </strong> {error.message}`
- `<strong>Correlation ID: </strong> {correlationId}`

## Special Cases

### React JSX Inline Spacing

When using JSX, you can ensure spacing with the `{' '}` pattern:

```tsx
<Text weight="semibold">Duration:</Text>{' '}
<Text>{duration} minutes</Text>
```

### Template Strings

Always include space after colon in template strings:

```tsx
const message = `Error: ${errorDetails}`;
const status = `Status: ${currentStatus}`;
```

### Multi-line Labels

When a label is on one line and the value is on the next, ensure proper spacing with CSS or inline space:

```tsx
// With inline space
<Text weight="semibold">Error: </Text>
<Text>{errorMessage}</Text>

// Or with CSS margin/padding
<Text weight="semibold">Error:</Text>
<Text style={{ marginTop: '4px' }}>{errorMessage}</Text>
```

## Fixed Files

The following files have been updated to follow these conventions:

1. `Aura.Web/src/pages/Ideation/TrendingTopicsExplorer.tsx` - Error label
2. `Aura.Web/src/pages/Health/SystemHealthDashboard.tsx` - Error label
3. `Aura.Web/src/components/Onboarding/DependencyCheck.tsx` - Error label
4. `Aura.Web/src/components/Engines/EngineCard.tsx` - Last Error label
5. `Aura.Web/src/components/ErrorBoundary/ErrorFallback.tsx` - Error and Message labels
6. `Aura.Web/src/components/ErrorReportDialog.tsx` - Error and Message labels

## Code Review Checklist

When reviewing code, check for:

- [ ] All error messages have proper spacing: `"Error: "`
- [ ] All status labels have proper spacing: `"Status: "`
- [ ] All field labels in dialogs have proper spacing
- [ ] Template strings use proper spacing
- [ ] Inline labels use `{' '}` or CSS spacing appropriately

## Prevention

### Manual Review

During code review, verify that all user-facing text follows these conventions.

### Automated Tools

While ESLint and Prettier handle code formatting, spacing in string literals and JSX content is not automatically fixed. Consider:

1. Adding a custom ESLint rule to catch common patterns
2. Using a linter plugin for string content
3. Implementing i18n for centralized string management

## Examples from Codebase

### Good Examples

```tsx
// From ErrorToast.tsx
<div>Correlation ID: {details.correlationId}</div>
<div>Error Code: {details.errorCode}</div>

// From CreateWizard.tsx
<Text weight="semibold">Topic:</Text> <Text>{settings.brief.topic}</Text>
<Text weight="semibold">Duration:</Text>{' '}<Text>{duration}</Text>

// From RenderPanel.tsx
<Text>Error: {item.error}</Text>
<Text>Status: {status}</Text>
```

### Fixed Examples

Before:
```tsx
<Text weight="semibold">Error:</Text>
<Text>{error}</Text>
```

After:
```tsx
<Text weight="semibold">Error: </Text>
<Text>{error}</Text>
```

## Summary

The key principle is: **User-facing text should always have natural spacing that matches how it would appear in prose.**

If you would write "Error: Something failed" in plain text, ensure your JSX produces the same visual result with proper spacing.
