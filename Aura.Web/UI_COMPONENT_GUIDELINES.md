# Aura Video Studio - UI Component Guidelines

This document provides guidelines for using UI components in Aura Video Studio to ensure a consistent, premium, Apple/Adobe-level user experience.

## Table of Contents

1. [Button System](#button-system)
2. [Form Layout](#form-layout)
3. [Loading States](#loading-states)
4. [Interaction States](#interaction-states)
5. [Accessibility](#accessibility)

---

## Button System

### Overview

Aura Video Studio uses a canonical button component (`AuraButton`) that provides consistent styling across the application. All buttons should use this component or follow its patterns.

### Button Variants

| Variant         | Usage                            | Example                           |
| --------------- | -------------------------------- | --------------------------------- |
| **Primary**     | Main actions, CTA buttons        | "Generate Video", "Save", "Next"  |
| **Secondary**   | Secondary actions, alternatives  | "Back", "Cancel", "Settings"      |
| **Tertiary**    | Text-like buttons, minor actions | "Learn more", "Advanced settings" |
| **Destructive** | Dangerous actions                | "Delete", "Reset", "Remove"       |

### Button Sizes

| Size       | Min Height | Use Case                  |
| ---------- | ---------- | ------------------------- |
| **Small**  | 28px       | Inline actions, dense UIs |
| **Medium** | 36px       | Default for most buttons  |
| **Large**  | 44px       | Hero CTAs, wizard actions |

### Usage Examples

```tsx
import { AuraButton } from '@/components/ui';
import { Play24Regular } from '@fluentui/react-icons';

// Primary action with icon
<AuraButton variant="primary" size="large" iconStart={<Play24Regular />}>
  Generate Video
</AuraButton>

// Secondary action
<AuraButton variant="secondary" onClick={handleBack}>
  Back
</AuraButton>

// Tertiary/Ghost button
<AuraButton variant="tertiary">
  Advanced Settings
</AuraButton>

// Destructive action (use sparingly)
<AuraButton variant="destructive" onClick={handleDelete}>
  Delete Project
</AuraButton>

// Loading state
<AuraButton variant="primary" loading loadingText="Generating...">
  Generate
</AuraButton>

// Full width
<AuraButton variant="primary" fullWidth>
  Continue
</AuraButton>
```

### Styling Guidelines

1. **Corner radius**: 6px (default), 8px for large buttons
2. **Padding**:
   - Horizontal: 12-24px depending on size
   - Vertical: 4-12px depending on size
3. **Font weight**: Semibold (600)
4. **Label casing**: Sentence case (e.g., "Generate video" not "GENERATE VIDEO")

### When to Use Each Variant

- **Primary**: Use for the single most important action on a page/modal
- **Secondary**: Use for alternative actions or to pair with primary
- **Tertiary**: Use for low-emphasis actions that don't compete with primary
- **Destructive**: Use only for irreversible or dangerous actions

---

## Form Layout

### Overview

Forms should use `AuraFormField` for consistent layout with proper label, input, and message arrangement.

### Form Field Structure

```
┌──────────────────────────────────┐
│ Label *                          │  ← Label (sentence case, semibold)
├──────────────────────────────────┤
│ ┌──────────────────────────────┐ │
│ │ Input field                  │ │  ← Form control
│ └──────────────────────────────┘ │
│ ℹ️ Helper text or ❌ Error msg   │  ← Message area (fixed height)
└──────────────────────────────────┘
```

### Usage Examples

```tsx
import { AuraFormField } from '@/components/ui';
import { Input, Textarea } from '@fluentui/react-components';

// Basic field with label
<AuraFormField label="Email address" required>
  <Input value={email} onChange={handleChange} />
</AuraFormField>

// Field with error
<AuraFormField
  label="Password"
  required
  error={errors.password?.message}
>
  <Input type="password" value={password} onChange={handleChange} />
</AuraFormField>

// Field with helper text
<AuraFormField
  label="API Key"
  helperText="Get your API key from settings.provider.com"
>
  <Input value={apiKey} onChange={handleChange} />
</AuraFormField>

// Horizontal layout for simple forms
<AuraFormField
  label="Duration"
  orientation="horizontal"
  labelWidth="100px"
>
  <Input type="number" value={duration} onChange={handleChange} />
</AuraFormField>
```

### Spacing Guidelines

1. **Field margin**: 16px between fields (consistent vertical rhythm)
2. **Label to input**: 4px gap
3. **Input to message**: 4px gap
4. **Form sections**: 24px gap, with divider between sections

### Form Actions

Place form action buttons at the bottom with proper spacing:

```tsx
<div className="aura-form-actions">
  <AuraButton variant="secondary" onClick={handleCancel}>
    Cancel
  </AuraButton>
  <AuraButton variant="primary" type="submit">
    Save Changes
  </AuraButton>
</div>
```

For wizards with back/next navigation:

```tsx
<div className="aura-form-actions aura-form-actions-spread">
  <AuraButton variant="secondary" iconStart={<ArrowLeftRegular />}>
    Back
  </AuraButton>
  <AuraButton variant="primary" iconEnd={<ArrowRightRegular />}>
    Next
  </AuraButton>
</div>
```

---

## Loading States

### Button Loading

For long-running actions, buttons should display a loading state:

```tsx
const [isLoading, setIsLoading] = useState(false);

const handleGenerate = async () => {
  setIsLoading(true);
  try {
    await generateVideo();
  } finally {
    setIsLoading(false);
  }
};

<AuraButton
  variant="primary"
  loading={isLoading}
  loadingText="Generating..."
  onClick={handleGenerate}
>
  Generate Video
</AuraButton>;
```

### Loading State Rules

1. **Spinner placement**: Inside the button, replacing icon if present
2. **Text**: Keep button text visible (optionally update to loading text)
3. **Disabled**: Button is automatically disabled during loading
4. **No layout shift**: Button maintains same dimensions

### Long-Running Actions That Need Loading States

- Generate Video
- Preview Audio
- Save/Export operations
- API calls for configuration
- File processing operations

---

## Interaction States

### Hover States

Buttons have subtle hover effects:

- Primary: Slightly darker background, increased shadow
- Secondary: Background color shift, border emphasis
- Tertiary: Light background appears

### Focus States

All interactive elements must have visible focus rings:

- 2px solid outline
- 2px offset from element
- Brand color (cyan/blue)

```css
:focus-visible {
  outline: 2px solid var(--colorBrandStroke1);
  outline-offset: 2px;
}
```

### Pressed States

Buttons scale down slightly (98%) on press for tactile feedback.

### Disabled States

- Opacity: 60%
- Cursor: not-allowed
- Still legible (not fully transparent)
- No hover effects

---

## Accessibility

### Keyboard Navigation

1. **Tab order**: Follows logical reading order
2. **Focus visible**: All focusable elements have clear focus indicators
3. **Enter/Space**: Buttons activate on both keys

### Screen Readers

1. **Labels**: All form fields have associated labels
2. **Errors**: Use `role="alert"` for error messages
3. **Loading**: Use `aria-busy="true"` on loading buttons
4. **Disabled**: Use `aria-disabled="true"` on disabled elements

### Color Contrast

- Primary buttons: Meet WCAG AA contrast (4.5:1 for text)
- Disabled: Still readable (3:1 minimum)
- Error text: Red with sufficient contrast

### Reduced Motion

Support `prefers-reduced-motion`:

```tsx
import { useReducedMotion } from '@/hooks/useReducedMotion';

const prefersReducedMotion = useReducedMotion();

// Disable animations when user prefers reduced motion
const variants = prefersReducedMotion ? undefined : animationVariants;
```

---

## Component Import Paths

```tsx
// Canonical Aura components (preferred)
import { AuraButton, AuraFormField } from '@/components/ui';

// Type imports
import type {
  AuraButtonProps,
  AuraButtonVariant,
  AuraButtonSize,
  AuraFormFieldProps,
} from '@/components/ui';
```

---

## Migration Guide

### From Fluent UI Button

```tsx
// Before
<Button appearance="primary" onClick={handle}>Save</Button>

// After
<AuraButton variant="primary" onClick={handle}>Save</AuraButton>
```

### Mapping Fluent UI appearances to Aura variants

| Fluent UI Appearance | Aura Variant  |
| -------------------- | ------------- |
| `primary`            | `primary`     |
| `secondary`          | `secondary`   |
| `subtle`             | `tertiary`    |
| `outline`            | `secondary`   |
| (custom for danger)  | `destructive` |

---

## Best Practices Summary

1. ✅ Use `AuraButton` for all buttons
2. ✅ Use `AuraFormField` for all form fields
3. ✅ Show loading state for async operations
4. ✅ Maintain focus visibility
5. ✅ Use sentence case for button labels
6. ✅ Keep consistent spacing (16px between fields)
7. ❌ Don't remove focus outlines
8. ❌ Don't use only color to indicate state
9. ❌ Don't use destructive variant for non-dangerous actions
10. ❌ Don't override button padding/sizing outside the component
