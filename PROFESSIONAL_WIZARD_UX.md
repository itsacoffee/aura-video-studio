# Professional First-Run Wizard - Visual User Experience

## Overview
This document describes the visual experience users will have when going through the new 7-step first-run wizard.

---

## Step 0: Welcome Screen

**Visual Elements:**
- Large animated logo (🎬) at top center
- Bold title: "Welcome to Aura Video Studio!"
- Subtitle explaining the purpose
- Three feature cards in a grid layout:
  - **Create Amazing Videos** 🎬
  - **AI-Powered Tools** ✨  
  - **Save Time** ⏱️
- Info box at bottom: "⏱️ Setup takes 3-5 minutes"
- Blue "Next" button

**User Actions:**
- Click "Next" to begin setup
- No required input on this step

---

## Step 1: Choose Your Experience

**Visual Elements:**
- Two large cards side-by-side:
  - **Left Card: "🆓 Start Free"**
    - List of free features with green checkmarks
    - Secondary button "Select Free"
  - **Right Card: "⭐ Unlock Pro Features"**
    - "RECOMMENDED" badge in top-right
    - List of pro features with green checkmarks
    - Cost estimate box showing "$1-5 per video"
    - Primary/Secondary button "Select Pro"
- Selected card has blue outline
- Comparison table below showing feature differences

**User Actions:**
- Click either card to select tier
- Button changes to "Selected" when clicked
- Cannot proceed without selecting a tier

**Navigation Logic:**
- If "Free" selected → Skip to Step 3 (Hardware)
- If "Pro" selected → Continue to Step 2 (API Keys)

---

## Step 2: API Key Setup (Pro Only)

**Visual Elements:**
- Info tip box: "💡 You don't need all of these..."
- Six expandable accordion sections, one per provider:
  - OpenAI 🤖
  - Anthropic 🧠
  - Google Gemini ✨
  - ElevenLabs 🎙️
  - PlayHT 🔊
  - Replicate 🎨

**Collapsed Accordion View:**
- Provider logo emoji
- Provider name in bold
- One-line description
- Status badge (Not Set / Valid / Invalid / Validating...)

**Expanded Accordion View:**
Each accordion contains:
1. **Three collapsible sections:**
   - "What it's used for" - Detailed explanation
   - "Pricing" - Free tier info + monthly cost calculator
   - "How to get your API key" - Numbered steps

2. **API Key Input Area:**
   - Password-style input field
   - Eye icon button to show/hide key
   - "Validate" button
   - Status indicator (spinner/checkmark/X)
   - Error message if validation fails

3. **Action Buttons:**
   - "Get API Key" - Opens provider website in new tab
   - "Show Video Tutorial" - Placeholder for future

**User Actions:**
- Expand/collapse any accordion
- Enter API keys
- Click eye icon to view/hide keys
- Click "Validate" to test keys
- Click "Skip All" to continue without keys

**Validation Feedback:**
- **Validating**: Blue spinner, disabled buttons
- **Valid**: Green checkmark, "✓ Valid! API key verified"
- **Invalid**: Red X, specific error message with suggestions
- Rate limiting: "Too many attempts. Please wait..."

---

## Step 3: Hardware Detection

**Visual Elements:**
- Title: "Hardware Detection"
- Card with content based on state:

**Before Detection:**
- Text: "Click Next to detect your hardware..."

**During Detection:**
- Spinner animation
- Text: "Detecting your hardware capabilities..."

**After Detection:**
- **System Information Card:**
  - GPU name
  - VRAM amount
  - Recommendation text
- **Warning Card (if needed):**
  - Orange warning badge
  - Note about Stable Diffusion compatibility

**User Actions:**
- Click "Next" to run detection
- Review hardware info
- Continue to next step

---

## Step 4: Install Required Components

**Visual Elements:**
- Title: "Install Required Components"
- List of component cards:
  - **FFmpeg** (Required) - Video encoding
  - **Ollama** (Optional) - Local AI
  - **Stable Diffusion** (Optional) - Image generation

**Each Component Card Shows:**
- Component name and description
- Default install path
- Status badge (Not Installed / Installing... / Installed)
- Three action buttons:
  - "Install" - Auto-download and install
  - "Use Existing" - Browse for existing installation
  - "Skip" (optional items only)

**Info Box Below:**
- 📌 Installation Options explanation
- Describes each button's purpose

**Error Display:**
- Red error card appears if installations fail
- Lists specific errors with details

**User Actions:**
- Click "Install" to auto-install
- Click "Use Existing" to specify path
- Click "Skip" for optional items
- Progress bars show during installation

---

## Step 5: Validation & Preflight Checks

**Visual Elements:**

**Before Validation:**
- Info card: "Click Validate to check your setup..."

**During Validation:**
- Spinner with text: "Running preflight checks..."

**Validation Passed:**
- Large green checkmark (32px)
- Title: "All Checks Passed!"
- Text: "Your system is ready to create videos"
- File locations summary card showing where things are installed

**Validation Failed:**
- Red error card with warning icon
- Title: "Validation Failed"
- Text: "Some providers are not available..."
- Individual issue cards showing:
  - Stage name (Script / TTS / Visuals)
  - Provider name
  - Issue description
  - Hint/suggestions
  - Quick fix action buttons

**User Actions:**
- Click "Validate" to run checks
- Review any issues
- Click fix action buttons
- Can click "Next Anyway" to continue with issues

---

## Step 6: Completion

**Visual Elements:**
- Animated checkmark (scales up smoothly)
- Large title: "All Set!"
- Subtitle: "Your system is configured and ready..."

**Configuration Summary Card:**
- **Tier:** Free (Stock) or Pro (AI-Powered)
- **API Keys:** List of configured providers
- **Hardware:** Detected and optimized
- **Components:** List of installed items

**Quick Start Tips Section:**
Three cards with tips:
1. 🎬 **Start Simple** - Try a 30-second video first
2. 📖 **Explore Templates** - Use pre-built templates
3. ⚙️ **Customize Settings** - Fine-tune preferences

**Action Buttons:**
- **Primary**: "Create Your First Video" (blue, large)
- **Secondary**: "Explore the App" (gray, large)

**User Actions:**
- Click "Create Your First Video" → Navigate to /create
- Click "Explore the App" → Navigate to /home
- Both clear wizard state and mark onboarding complete

---

## Progress Indicator (Always Visible)

**Visual Elements:**
- Top of screen, centered
- Step counter: "Step X of 7"
- Seven horizontal bars representing steps:
  - **Completed steps**: Green with checkmark icon
  - **Current step**: Blue and slightly taller
  - **Future steps**: Gray
- Step labels below each bar:
  - Welcome
  - Choose Tier  
  - API Keys
  - Hardware
  - Dependencies
  - Validation
  - Complete
- "Save and Exit" button in top-right corner

**Interactions:**
- Completed steps are clickable (hover shows pointer cursor)
- Clicking completed step navigates back
- Current and future steps not clickable
- Progress bars have smooth 300ms transitions

---

## Navigation Buttons (Footer)

**Left Side:**
- "Save and Exit" button (subtle appearance)

**Right Side:**
- "Back" button (secondary, with left arrow)
  - Disabled on Step 0
  - Smart navigation respects tier selection
- "Next" button (primary, with right arrow)
  - Changes to "Validate" on Step 5
  - Disabled when required input missing
  - Shows spinner during async operations

---

## Responsive Design

**Desktop (900px+):**
- Cards side-by-side in grid
- Wide comfortable layout
- Maximum content width: 900px

**Tablet (600-900px):**
- Cards stack vertically
- Full-width accordions
- Reduced padding

**Mobile (320-600px):**
- Single column layout
- Smaller fonts
- Touch-friendly button sizes
- Simplified comparison tables

---

## Animations & Transitions

All transitions use **300ms ease-in-out** timing:
- Step changes: Fade in/out
- Card hovers: Lift effect (-4px transform)
- Progress bar updates: Smooth color/height changes
- Accordion expand/collapse: Height animation
- Success checkmark: Scale animation (0 → 1.2 → 1)
- Loading spinners: Continuous rotation

---

## Color Scheme (Fluent UI 2 Tokens)

- **Primary (Brand)**: Blue - buttons, selected states
- **Success**: Green - valid states, checkmarks, completed steps
- **Warning**: Orange - hardware warnings, optional notices
- **Error**: Red - invalid states, errors, failed validations
- **Neutral**: Gray - unselected states, secondary buttons
- **Background**: White/light gray cards on white background

---

## Accessibility Features

**Keyboard Navigation:**
- Tab through all interactive elements
- Enter/Space to activate buttons
- Escape to close dialogs
- Arrow keys work where appropriate

**Screen Reader Support:**
- Proper ARIA labels on all components
- Role attributes (button, progressbar, dialog)
- aria-current on active step
- aria-disabled on disabled buttons
- Meaningful alt text

**Visual Accessibility:**
- High contrast mode compatible
- Clear focus indicators (blue outline)
- Large touch targets (44px minimum)
- Readable font sizes (14px minimum)
- Color not sole indicator (icons + text)

---

## Error Messages

**User-Friendly Language:**
- ❌ "Please enter your API key" (not "Field is required")
- ❌ "API key format incorrect. OpenAI keys start with 'sk-'" (not "Invalid format")
- ❌ "Could not connect. Check your internet connection." (not "Network error 500")
- ❌ "Too many attempts. Please wait a moment." (not "Rate limit exceeded")

**Helpful Suggestions:**
- Specific format requirements shown
- Links to provider documentation
- Quick fix action buttons where possible
- Step-by-step remediation guides

---

## Persistence Behavior

**Auto-Save:**
- State saves to localStorage after each step
- Saves: step, tier, apiKeys, hardware, installItems
- Does NOT save: validation results, errors

**Resume Dialog:**
- On return: "You have incomplete setup. Resume?"
- Yes → Restore saved state
- No → Start fresh, clear storage

**Completion:**
- Clears all wizard state from localStorage
- Sets hasSeenOnboarding=true
- Won't show wizard again unless flag cleared

---

## Loading States

**Types of Loading:**
1. **Hardware Detection**: Spinner + "Detecting..."
2. **Installing**: Progress indicator + "Installing..."
3. **Validating API**: Spinner + "Validating..."
4. **Running Checks**: Spinner + "Running preflight checks..."

**During Loading:**
- Relevant buttons disabled
- Spinner visible
- Text updated to show activity
- User cannot proceed until complete

---

## Edge Cases Handled

1. **No Tier Selected**: Button disabled, can't proceed
2. **Free Tier**: Skips API key step entirely
3. **Skip All APIs**: Warning confirmation, then proceeds
4. **Validation Fails**: Shows issues but allows "Next Anyway"
5. **Install Fails**: Shows error, can retry or skip
6. **Hardware Detection Fails**: Uses fallback, shows recommendation
7. **Network Error**: Friendly message, suggests retry
8. **Browser Refresh**: Resume dialog on return
9. **Going Back**: Smart navigation preserves tier selection
10. **Rate Limiting**: 20-second cooldown with message

---

## Success Criteria

✅ Professional visual design matching Fluent UI 2
✅ Smooth animations and transitions (300ms)
✅ Clear progress indication at all times
✅ Helpful inline documentation
✅ User-friendly error messages
✅ Accessible to keyboard and screen reader users
✅ Responsive on all screen sizes
✅ Persistent state across sessions
✅ Smart navigation based on user choices
✅ No dead ends or confusing states

---

## User Testing Checklist

- [ ] Welcome screen is inviting and clear
- [ ] Tier selection is obvious and comparison helpful
- [ ] API key instructions are easy to follow
- [ ] Validation feedback is immediate and clear
- [ ] Error messages are helpful not frustrating
- [ ] Hardware detection provides useful info
- [ ] Installation progress is visible
- [ ] Can successfully skip optional steps
- [ ] Save and resume works as expected
- [ ] Completion screen feels rewarding
- [ ] Navigation is intuitive throughout
- [ ] No confusion about what to do next
- [ ] Works well on mobile devices
- [ ] Accessible with keyboard only
- [ ] Screen reader provides good experience
