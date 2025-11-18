# Model Selection Finalization - UI Changes Guide

## Overview

This document describes the UI enhancements made to improve model selection clarity and user control.

## 1. Enhanced ModelPicker Component

### Location
`Aura.Web/src/components/ModelSelection/ModelPicker.tsx`

### Visual Layout

```
┌─────────────────────────────────────────────────────────────┐
│ Script Generation Model                                     │
│ Choose which AI model to use for script generation          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌────────────────────────┐  ┌─────┐  ┌──────┐  ┌────────┐│
│  │ gpt-4 ▼                │  │ 📌  │  │ ⚡    │  │ ℹ️      ││
│  └────────────────────────┘  │ Pin │  │ Test │  │ Explain││
│                              └─────┘  └──────┘  └────────┘│
│                                                             │
│  🔴 Pinned  🔵 Stage Override                              │
│                                                             │
│  Context: 8,192 tokens | Max output: 8,192 tokens          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Components

#### 1. Model Dropdown
- Shows all available models for the provider
- Indicates deprecated models with "(Deprecated)" suffix
- Updates immediately on selection

#### 2. Pin Button
- **Unpinned**: Shows 🔓 icon, text "Pin"
- **Pinned**: Shows 🔒 icon, text "Pinned"
- **Tooltip (Unpinned)**: "Pin model (never auto-change)"
- **Tooltip (Pinned)**: "Unpin model (allow fallback)"
- Clicking toggles the pin state

#### 3. Test Button (Existing, Enhanced)
- Icon: ⚡ Flash/Lightning
- Text: "Test"
- Tooltip: "Test model availability with a lightweight probe"
- Opens Test Model Dialog

#### 4. Explain Button (NEW)
- Icon: ℹ️ Info
- Text: "Explain"
- Tooltip: "Explain this model choice and compare with recommendations"
- Opens Explain Choice Dialog

### Badges

#### Pinned Badge (Red)
```
🔴 Pinned
```
- Color: Red/Important
- Icon: Lock
- Tooltip: "This model is pinned and will never be automatically changed. If unavailable, operations will be blocked until you make a manual choice."

#### Stage Override Badge (Blue)
```
🔵 Stage Override
```
- Color: Brand Blue
- Tooltip: "This is a per-stage override (Stage scope). It takes precedence over project and global defaults."

#### Project Override Badge (Informative)
```
🔵 Project Override
```
- Color: Informative Blue
- Tooltip: "This is a project-level override (Project scope). It takes precedence over global defaults but not stage pins."

#### Deprecated Badge (Warning)
```
⚠️ Deprecated
```
- Color: Warning Yellow/Orange
- Icon: Warning triangle
- Tooltip: "This model is deprecated and may be removed soon. Consider migrating to {replacementModel}."

## 2. Test Model Dialog (Existing)

### Visual Layout

```
┌───────────────────────────────────────────────────────┐
│ Test Model: gpt-4                                 ╳   │
├───────────────────────────────────────────────────────┤
│                                                       │
│  Test if the model gpt-4 from provider OpenAI is     │
│  available and working properly.                      │
│                                                       │
│  API Key (required) *                                 │
│  ┌─────────────────────────────────────────────────┐ │
│  │ ••••••••••••••••••••••••••••••                  │ │
│  └─────────────────────────────────────────────────┘ │
│                                                       │
│  ℹ️ Note: Your API key is not stored and only used   │
│     for this test.                                    │
│                                                       │
│  ✓ Model is available and working!                   │
│    Context: 8,192 tokens                              │
│                                                       │
│  ┌──────────┐  ┌──────────────┐                     │
│  │  Close   │  │ 🧪 Test Model │                     │
│  └──────────┘  └──────────────┘                     │
│                                                       │
└───────────────────────────────────────────────────────┘
```

### Features
- Secure API key input (password field)
- Real-time test results
- Shows model capabilities on success
- Clear error messages on failure

## 3. Explain Choice Dialog (NEW)

### Visual Layout

```
┌───────────────────────────────────────────────────────┐
│ Explain Model Choice: gpt-4                       ╳   │
├───────────────────────────────────────────────────────┤
│                                                       │
│  Your Selection                                       │
│  gpt-4 - Context: 8,192 tokens, Max output: 8,192    │
│                                                       │
│  Recommended Model                                    │
│  gpt-4o - Context: 128,000 tokens, Max output: 16K   │
│                                                       │
│  Reasoning                                            │
│  You selected 'gpt-4' which has a smaller context    │
│  window (8,192 tokens) compared to the recommended   │
│  'gpt-4o' (128,000 tokens). For script operations,   │
│  context window size affects how much information    │
│  can be processed at once.                           │
│                                                       │
│  Tradeoffs                                            │
│  • Smaller context window: 8,192 vs 128,000 tokens   │
│  • May require breaking large scripts into smaller   │
│    chunks                                             │
│  • Lower output limit: 8,192 vs 16,384 tokens        │
│                                                       │
│  Suggestions                                          │
│  • For larger scripts, consider gpt-4o (128,000      │
│    tokens)                                            │
│  • Alternative with similar capabilities: gpt-4-     │
│    turbo                                              │
│                                                       │
│  ┌──────────┐                                        │
│  │  Close   │                                        │
│  └──────────┘                                        │
│                                                       │
└───────────────────────────────────────────────────────┘
```

### Features
- **Comparison**: Side-by-side view of selected vs recommended
- **Reasoning**: Clear explanation of choice implications
- **Tradeoffs**: Bulleted list of pros/cons
- **Suggestions**: Actionable alternatives
- **Success indicator**: If selected matches recommended, shows ✓ badge

## 4. Job Details - Model Selection Audit (NEW INTEGRATION)

### Location
`Aura.Web/src/pages/Jobs/RunDetailsPage.tsx`

### Visual Layout

```
┌───────────────────────────────────────────────────────────┐
│ Model Selection Audit Trail                              │
├───────────────────────────────────────────────────────────┤
│ This shows which AI models were selected for each stage, │
│ and the reasoning behind each choice.                     │
├───────────────────────────────────────────────────────────┤
│                                                           │
│  OpenAI / script                                          │
│  Model: gpt-4                                             │
│  🟡 Stage Pinned  🔒 Pinned  ✓ Used                      │
│                                                           │
│  Reasoning: Using stage-pinned model: gpt-4              │
│  Selected at: 2025-11-06 12:00:00                        │
│                                                           │
│  ────────────────────────────────────────────────────    │
│                                                           │
│  OpenAI / visual                                          │
│  Model: gpt-4o-mini                                       │
│  🟢 Automatic Fallback  ✓ Used                           │
│                                                           │
│  Reasoning: Using automatic fallback: Safe default       │
│  ℹ️ Fallback Applied: Project-override 'gpt-4o' was     │
│     unavailable                                           │
│  Selected at: 2025-11-06 12:00:05                        │
│                                                           │
└───────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────┐
│ ℹ️ Selection Precedence: Run Override (Pinned) > Run     │
│    Override > Stage Pinned > Project Override > Global   │
│    Default > Automatic Fallback                          │
└───────────────────────────────────────────────────────────┘
```

### Badge Colors (Source Indicators)

| Badge | Color | Meaning |
|-------|-------|---------|
| 🔴 Run Override (Pinned) | Danger | Highest priority, blocks if unavailable |
| 🟠 Run Override | Important | High priority, falls back if unavailable |
| 🟡 Stage Pinned | Warning | Stage-level pin, blocks if unavailable |
| 🔵 Project Override | Informative | Project preference |
| ⚪ Global Default | Subtle | Application-wide default |
| 🟢 Automatic Fallback | Success | Safe catalog default (only if enabled) |

### Additional Badges

| Badge | Meaning |
|-------|---------|
| 🔒 Pinned | Model is pinned (won't auto-change) |
| ✓ Used | Model was successfully used |
| ⚠️ Blocked | Model unavailable, blocked |

### Features
- **Per-stage breakdown**: Shows each stage's model selection
- **Source visibility**: Clear indication of why each model was chosen
- **Fallback transparency**: Explicit fallback reasons with info icon
- **Timestamp**: When selection was made
- **Precedence reference**: Footer shows hierarchy for clarity

## User Experience Flows

### Flow 1: Selecting a Model
1. User navigates to Models page
2. Chooses model from dropdown
3. Sees appropriate badges (Stage Override, Project Override, etc.)
4. Optionally clicks "Pin" to lock the selection
5. Optionally clicks "Test" to verify availability
6. Optionally clicks "Explain" to understand choice implications

### Flow 2: Understanding a Choice
1. User selects a model
2. Clicks "Explain" button
3. Dialog shows:
   - Their selection vs recommended
   - Reasoning for differences
   - Tradeoffs of their choice
   - Suggestions for alternatives
4. User makes informed decision

### Flow 3: Reviewing Job Audit
1. User navigates to Job Details for completed run
2. Scrolls to "Model Selection Audit Trail"
3. Sees all models used with:
   - Source (why this model was selected)
   - Fallback reason (if applicable)
   - Timestamps
4. Understands exactly what happened and why

## Accessibility

All UI components include:
- ✅ Keyboard navigation support
- ✅ Screen reader labels (ARIA)
- ✅ Tooltips for additional context
- ✅ High-contrast badge colors
- ✅ Clear visual hierarchy

## Responsive Design

Components adapt to different screen sizes:
- **Desktop**: Full layout as shown
- **Tablet**: Badges wrap to new line if needed
- **Mobile**: Vertical stacking of controls

---

**All UI changes maintain consistency with Fluent UI design system and follow zero-placeholder policy.**
