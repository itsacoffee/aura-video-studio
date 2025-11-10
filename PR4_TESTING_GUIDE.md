# PR #4: Video Creation Workflow UI - Testing Guide

## Quick Start

### Running the Application
```bash
cd Aura.Web
npm install
npm run dev
```

Navigate to: `http://localhost:5173/create`

## Comprehensive Testing Checklist

### 1. Wizard Navigation & Flow ✅

#### Basic Navigation
- [ ] **Open wizard** - Navigate to `/create` and verify VideoCreationWizard loads
- [ ] **Step indicators** - Verify progress bar at top shows "Step 1 of 5"
- [ ] **Next button** - Click "Next" to proceed to Step 2
- [ ] **Back button** - Click "Previous" to return to Step 1
- [ ] **Step labels** - Verify labels show: Brief, Style, Script, Preview, Export
- [ ] **Visual transitions** - Confirm smooth fade-in animation between steps

#### Keyboard Navigation
- [ ] **Ctrl+Enter** - Press to advance to next step
- [ ] **Ctrl+Shift+Enter** - Press to go back to previous step
- [ ] **Escape key** - Press to trigger save dialog
- [ ] **Tab navigation** - Tab through all form fields
- [ ] **Enter on buttons** - Verify buttons activate on Enter key
- [ ] **Accessibility** - Use screen reader to verify ARIA labels

### 2. Step 1: Brief Input ✅

#### Template Selection
- [ ] **Template cards visible** - Verify 6 template cards display
- [ ] **Category badges** - Each card shows category (Education, Business, Lifestyle, Inspiration)
- [ ] **Hover effect** - Cards lift and highlight on hover
- [ ] **Click template** - Click "Educational: AI Basics" and verify prompt populates
- [ ] **Topic fills** - Verify topic textarea updates with template content
- [ ] **Video type updates** - Confirm video type dropdown changes to "educational"

#### Prompt Input
- [ ] **Type in textarea** - Enter custom prompt with 100+ characters
- [ ] **Character counter** - Verify counter updates (e.g., "125 / 500 characters")
- [ ] **Optimal length badge** - When 50-500 chars, "Optimal length" badge appears
- [ ] **Red warning** - Below 10 or above 500 chars shows red counter
- [ ] **"Inspire Me" button** - Click to get random template
- [ ] **Voice input button** - Click and speak (Chrome/Edge only)

#### Prompt Quality Analyzer
- [ ] **Quality score** - Verify score displays (0-100)
- [ ] **Metrics display** - Shows Length, Specificity, Clarity, Actionability percentages
- [ ] **Color coding** - Excellent (green), Good (blue), Fair (yellow), Poor (red)
- [ ] **Suggestions** - Check suggestions appear with appropriate icons
- [ ] **Real-time update** - Type to see score update dynamically

#### Form Validation
- [ ] **Target audience** - Enter "College students"
- [ ] **Key message** - Enter "Understanding AI is essential"
- [ ] **Duration slider** - Set to 90 seconds (1.5 min)
- [ ] **Required field check** - Leave topic empty, verify "Next" is disabled
- [ ] **Fill valid data** - Complete all required fields
- [ ] **Next enabled** - Verify "Next" button becomes enabled

### 3. Step 2: Style Selection ✅

#### Style Presets
- [ ] **5 preset cards** - Verify all presets display with icons
- [ ] **Preset names** - Modern 🎨, Professional 💼, Cinematic 🎬, Minimal ✨, Playful 🎉
- [ ] **Hover effect** - Cards lift on hover
- [ ] **Select Modern** - Click and verify blue border + "Active" badge
- [ ] **Visual style updates** - Dropdown below updates to "modern"
- [ ] **Music genre updates** - Music dropdown shows "upbeat"

#### Provider Selection
- [ ] **Provider cards** - Grid of image providers displays
- [ ] **Availability check** - Green checkmark for available, red X for unavailable
- [ ] **Select provider** - Click available provider card
- [ ] **Selected badge** - "Selected" badge appears on clicked card
- [ ] **Cost display** - Shows cost per image or "Free"
- [ ] **Capabilities** - Display max resolution and supported styles count

#### Voice Settings
- [ ] **Voice provider dropdown** - Select "ElevenLabs"
- [ ] **Voice name dropdown** - Choose "Professional"
- [ ] **Settings persist** - Go back to Step 1, return, verify selections remain

### 4. Step 3: Script Review ✅

#### Script Generation
- [ ] **Generate button** - Click to generate script (if implemented)
- [ ] **Loading state** - Verify spinner with "Generating script..." message
- [ ] **Scene cards** - Generated scenes display in cards
- [ ] **Scene numbering** - Each card shows "Scene 1", "Scene 2", etc.
- [ ] **Narration text** - Verify narration content is editable
- [ ] **Visual prompt** - Shows visual description for each scene

#### Syntax Highlighting
- [ ] **Emphasis highlighting** - Text in *asterisks* appears bold/colored
- [ ] **Pause indicators** - [pause] or ... appears in distinct color
- [ ] **Scene metadata** - Duration and transition display below narration
- [ ] **Color scheme** - Verify readable contrast in both light/dark themes

#### Scene Editing
- [ ] **Edit narration** - Click textarea and modify text
- [ ] **Auto-save indicator** - "Saving..." appears, then "Saved" with timestamp
- [ ] **Regenerate scene** - Click regenerate icon on one scene
- [ ] **Delete scene** - Click delete icon (with confirmation)
- [ ] **Add scene** - Click "Add Scene" button
- [ ] **Scene actions** - Verify merge, split buttons (if enabled)

#### Version History
- [ ] **History button** - Click to open version history dialog
- [ ] **Version list** - Shows timestamps and descriptions
- [ ] **Revert** - Select older version and click "Revert"
- [ ] **Version restored** - Verify script updates to selected version

### 5. Step 4: Preview Generation ✅

#### Generation Process
- [ ] **Start generation** - Click "Generate Preview"
- [ ] **Rich progress display** - Verify RichProgressDisplay component appears
- [ ] **Stage indicators** - Shows all stages (Script, TTS, Visuals, Assembly, Export)
- [ ] **Active stage** - Current stage has spinner and brand background
- [ ] **Completed stages** - Green checkmark icon on completed
- [ ] **Progress bar** - Smooth animation as percentage increases
- [ ] **Time counters** - "Time Elapsed" and "Time Remaining" update

#### Progress Details
- [ ] **Stage descriptions** - Each stage shows descriptive text
- [ ] **Individual progress** - Active stage shows its own progress bar
- [ ] **Stages complete counter** - Shows "2 / 5" as stages finish
- [ ] **Preview thumbnails** - (If available) Thumbnail grid appears
- [ ] **Pause button** - Click pause, verify status changes
- [ ] **Resume button** - Click resume to continue
- [ ] **Cancel button** - Click cancel, confirm cancellation dialog

### 6. Step 5: Final Export ✅

#### Export Presets
- [ ] **5 preset cards** - YouTube HD, Social Media, Web, Professional, Quick Preview
- [ ] **Icons display** - Each preset has emoji icon
- [ ] **Recommended badge** - "Most Popular" on YouTube HD
- [ ] **Select preset** - Click "Social Media" preset
- [ ] **Blue border** - Selected card has brand border
- [ ] **Settings apply** - Quality, resolution, format update automatically
- [ ] **Specs display** - Shows "1080p • MP4 • high" below each preset

#### Export Settings
- [ ] **Quality dropdown** - Verify options: Draft, Standard, High, Ultra
- [ ] **Resolution dropdown** - 480p, 720p, 1080p, 4K options
- [ ] **Format dropdown** - MP4, WebM, MOV options
- [ ] **Include captions checkbox** - Toggle on/off
- [ ] **Batch export** - (If enabled) Select multiple formats
- [ ] **Cost estimate** - Shows estimated export cost/time

#### Export Execution
- [ ] **Export button** - Click "Generate Video" (final button)
- [ ] **Export progress** - Progress bar with stages
- [ ] **Completion** - Success message appears
- [ ] **Celebration effect** - Confetti animation plays
- [ ] **Success sound** - Pleasant chord plays
- [ ] **Download button** - Appears after completion
- [ ] **Open folder** - Link to open output folder

### 7. Auto-Save & Drafts ✅

#### Auto-Save
- [ ] **Auto-save indicator** - Top right shows "Auto-saved X minutes ago"
- [ ] **Clock icon** - Rotating clock during save
- [ ] **30-second interval** - Wait 30 seconds, verify "Just now" appears
- [ ] **Persist on refresh** - Fill form, refresh page, verify data restored
- [ ] **Multiple tabs** - Open two tabs, verify auto-save works independently

#### Draft Management
- [ ] **Save & Exit button** - Click in header
- [ ] **Save dialog** - "Save Progress?" dialog appears
- [ ] **Save option** - Click "Save & Exit", returns to home
- [ ] **Drafts button** - Click "Drafts" in header
- [ ] **Draft list** - Shows saved drafts with timestamps
- [ ] **Load draft** - Click draft to load
- [ ] **Delete draft** - Delete a draft, confirm removal

### 8. Visual Polish & Animations ✅

#### Hover Effects
- [ ] **Card hover** - All cards lift (-4px) on hover
- [ ] **Shadow increase** - Box shadow intensifies
- [ ] **Border highlight** - Border changes to brand color
- [ ] **Smooth transition** - 0.3s cubic-bezier animation
- [ ] **Active state** - Cards respond to mouse down

#### Loading States
- [ ] **Enhanced spinner** - Pulsing ring around spinner
- [ ] **Loading tips** - Random tips display below spinner
- [ ] **Fade-in animation** - Smooth appearance of content
- [ ] **Skeleton loading** - (If implemented) Placeholder content

#### Tooltips
- [ ] **Info icons** - Info icons (ℹ️) appear next to labels
- [ ] **Hover tooltip** - Hover over icon, tooltip appears
- [ ] **Tooltip content** - Shows helpful description
- [ ] **Positioning** - Tooltip doesn't overflow screen
- [ ] **Keyboard accessible** - Focus with Tab key to show tooltip

### 9. Responsive Design 🔧

#### Desktop (1920x1080)
- [ ] **Full layout** - All components display properly
- [ ] **Grid layouts** - Cards arrange in multiple columns
- [ ] **No horizontal scroll** - Content fits width

#### Laptop (1366x768)
- [ ] **Responsive grid** - Cards reflow to fewer columns
- [ ] **Readable text** - All text is legible
- [ ] **Navigation accessible** - All buttons reachable

#### Tablet (768x1024)
- [ ] **Single column** - Template cards stack vertically
- [ ] **Touch targets** - Buttons are large enough (44px min)
- [ ] **Scrolling** - Smooth vertical scroll

#### Mobile (375x667)
- [ ] **Mobile layout** - Wizard adapts to narrow screen
- [ ] **Touch-friendly** - All interactions work with touch
- [ ] **Zoom disabled** - (If needed) viewport meta tag prevents zoom

### 10. Error Handling 🛡️

#### Validation Errors
- [ ] **Empty required field** - Error message displays
- [ ] **Invalid format** - Shows format hint
- [ ] **Character limit** - Red counter when exceeded
- [ ] **Clear error message** - User understands what to fix

#### Network Errors
- [ ] **API timeout** - Shows "Request timed out" message
- [ ] **500 error** - Displays friendly error with retry button
- [ ] **Offline** - "No internet connection" message
- [ ] **Retry functionality** - Retry button re-attempts request

#### Edge Cases
- [ ] **Cancel during save** - Gracefully handles cancellation
- [ ] **Browser back button** - Doesn't break wizard state
- [ ] **Multiple clicks** - Buttons disable during processing
- [ ] **Session timeout** - Handles expired session gracefully

### 11. Accessibility (WCAG 2.1 AA) ♿

#### Keyboard
- [ ] **Tab order** - Logical tab sequence
- [ ] **Focus indicators** - Visible focus ring on all interactive elements
- [ ] **Skip links** - (If applicable) Skip to content link
- [ ] **Keyboard shortcuts** - All shortcuts work

#### Screen Reader
- [ ] **ARIA labels** - All buttons and inputs have labels
- [ ] **Role attributes** - Proper roles (button, textbox, etc.)
- [ ] **Live regions** - Status updates announced
- [ ] **Error announcements** - Errors read aloud

#### Visual
- [ ] **Color contrast** - 4.5:1 for text, 3:1 for UI components
- [ ] **Text size** - Minimum 14px, scalable with zoom
- [ ] **Focus visible** - Focus indicators work in high contrast mode
- [ ] **No color-only** - Information not conveyed by color alone

### 12. Performance ⚡

#### Load Times
- [ ] **Initial page load** - < 3 seconds
- [ ] **Step transition** - < 100ms
- [ ] **Template click** - Instant feedback
- [ ] **Save operation** - Non-blocking

#### Memory
- [ ] **No memory leaks** - Open DevTools, check memory over time
- [ ] **Smooth scrolling** - No jank during scroll
- [ ] **Animation FPS** - 60 FPS transitions

### 13. Browser Compatibility 🌐

#### Modern Browsers
- [ ] **Chrome 120+** - Full functionality
- [ ] **Edge 120+** - Full functionality
- [ ] **Firefox 121+** - Full functionality
- [ ] **Safari 17+** - (Mac only) Full functionality

#### Mobile Browsers
- [ ] **Chrome Mobile** - Touch interactions work
- [ ] **Safari iOS** - Gestures and animations smooth
- [ ] **Samsung Internet** - No layout issues

## Acceptance Criteria Validation

### ✅ Workflow feels professional and intuitive
**Test**: Complete full wizard flow without confusion
- Clear step labels
- Helpful tooltips
- Logical progression
- Visual feedback at every step

**PASS/FAIL**: ______

### ✅ Each step validates before proceeding
**Test**: Try to proceed with invalid data
- Empty required fields block progression
- "Next" button disabled when invalid
- Clear error messages shown
- Validation happens in real-time

**PASS/FAIL**: ______

### ✅ Visual feedback for all interactions
**Test**: Interact with every element
- Buttons show hover state
- Cards lift on hover
- Loading spinners appear
- Success checkmarks display
- Error icons shown
- Progress bars animate

**PASS/FAIL**: ______

### ✅ No jarring transitions or flashes
**Test**: Navigate through entire wizard
- Smooth fade-in animations
- No content jumping
- Loading states are smooth
- Step transitions are fluid
- No flash of unstyled content

**PASS/FAIL**: ______

### ✅ Accessible via keyboard navigation
**Test**: Complete wizard using only keyboard
- Tab through all fields
- Use Ctrl+Enter to advance
- Use Ctrl+Shift+Enter to go back
- Escape to save
- All features accessible

**PASS/FAIL**: ______

## Bug Report Template

### If you find issues, report using this format:

```markdown
**Issue**: [Brief description]
**Severity**: [Critical / High / Medium / Low]
**Steps to Reproduce**:
1. Navigate to...
2. Click on...
3. Observe...

**Expected**: [What should happen]
**Actual**: [What actually happened]
**Browser**: [Chrome 120.0]
**OS**: [Windows 11]
**Screenshot**: [Attach if possible]
```

## Testing Sign-Off

### Tester Information
- Name: ________________
- Date: ________________
- Environment: ________________

### Results Summary
- Total Test Cases: ______
- Passed: ______
- Failed: ______
- Blocked: ______

### Overall Assessment
- [ ] **PASS** - Ready for production
- [ ] **CONDITIONAL PASS** - Minor issues, can deploy
- [ ] **FAIL** - Major issues, needs rework

### Notes:
```
[Add any additional observations or recommendations]
```

---

## Automated Testing Commands

```bash
# Run unit tests
npm test

# Run with coverage
npm run test:coverage

# Run Playwright E2E tests
npm run playwright

# Type check
npm run type-check

# Lint check
npm run lint

# Full quality check
npm run quality-check
```

## Continuous Testing in Dev

```bash
# Watch mode for tests
npm run test:watch

# Vite dev server with HMR
npm run dev
```

## Performance Profiling

### Chrome DevTools
1. Open DevTools (F12)
2. Go to Performance tab
3. Click Record
4. Complete wizard flow
5. Stop recording
6. Analyze flame graph for bottlenecks

### React DevTools
1. Install React DevTools extension
2. Open Components tab
3. Enable "Highlight updates"
4. Navigate wizard, observe re-renders
5. Use Profiler to find unnecessary renders

---

**Total Estimated Testing Time**: 2-3 hours for comprehensive manual testing
**Recommended**: Run through checklist twice - once in light mode, once in dark mode
