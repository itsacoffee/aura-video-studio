# Error Modal UI Design

## Layout

```
┌─────────────────────────────────────────────────────────────┐
│ Generation Failed                                     [X]   │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Error                                                   │ │
│ │ ─────                                                   │ │
│ │ Render failed due to FFmpeg error                      │ │
│ │ Error Code: E304-FFMPEG_RUNTIME                        │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                               │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Correlation ID                                          │ │
│ │ ──────────────                                          │ │
│ │ ┌──────────────────────────────────┐                   │ │
│ │ │ a1b2c3d4e5f6g7h8i9j0             │ [Copy]            │ │
│ │ └──────────────────────────────────┘                   │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                               │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Error Output (last 16KB)                                │ │
│ │ ────────────────────────                                │ │
│ │ ┌─────────────────────────────────────────────────────┐ │ │
│ │ │ ... (showing last 16KB)                             │ │ │
│ │ │ [h264 @ 0x7f8b...] No encoder found for codec      │ │ │
│ │ │ [h264 @ 0x7f8b...] Encoder 'h264_nvenc' not found  │ │ │
│ │ │ Error initializing output stream 0:0 -- Error while│ │ │
│ │ │ opening encoder for output stream #0:0             │ │ │
│ │ │ Conversion failed!                                  │ │ │
│ │ └─────────────────────────────────────────────────────┘ │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                               │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Suggested Actions                                       │ │
│ │ ─────────────────                                       │ │
│ │ • Try using software encoder (x264) instead of         │ │
│ │   hardware acceleration                                 │ │
│ │ • Verify FFmpeg is properly installed using            │ │
│ │   Dependencies page                                     │ │
│ │ • Check the full log for detailed error information    │ │
│ │ • Retry the render with different settings             │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                               │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  [View Full Log]  [Repair FFmpeg]  [Attach FFmpeg]          │
│                                          [Close & Retry]     │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

## Visual Details

### Header
- **Title**: "Generation Failed" in Title size font
- **Close Button**: X icon button in top-right corner
- **Background**: Fluent UI DialogSurface background color

### Error Section
- **Title**: "Error" (Title3 size)
- **Message**: User-friendly error message in body text
- **Error Code**: Smaller text, slightly muted color
- **Spacing**: Medium vertical gap

### Correlation ID Section
- **Title**: "Correlation ID" (Title3 size)
- **ID Display**: Monospace font in a subtle background box
- **Copy Button**: Small button with Copy icon
- **Feedback**: Button text changes to "Copied!" for 2 seconds
- **Layout**: Horizontal flex with gap

### Error Output Section (Conditional)
- **Title**: "Error Output (last 16KB)" (Title3 size)
- **Content Box**:
  - Background: Subtle gray (colorNeutralBackground3)
  - Font: Monospace, 12px
  - Max height: 200px
  - Overflow: Scrollable
  - Padding: Medium
  - Border radius: Medium
  - White-space: pre-wrap (preserves formatting)
  - Word-break: break-word (prevents horizontal overflow)

### Suggested Actions Section
- **Title**: "Suggested Actions" (Title3 size)
- **List**: Bulleted list with disc markers
- **Items**: Body text, clear and actionable
- **Padding**: Left-indented for bullets

### Action Buttons (Footer)
- **Layout**: Horizontal flex with gap
- **Alignment**: Spread across footer
- **Button Types**:
  - **View Full Log**: Secondary appearance, Folder icon
  - **Repair FFmpeg**: Secondary appearance, Wrench icon (shown only for FFmpeg errors)
  - **Attach FFmpeg**: Secondary appearance, Settings icon (shown only for FFmpeg errors)
  - **Close & Retry**: Primary appearance, ArrowClockwise icon
- **States**: 
  - Repair button shows "Repairing..." when in progress
  - Disabled state while repairing

## Color Scheme

- **Background**: Fluent UI neutral background (colorNeutralBackground1)
- **Text**: Default foreground (colorNeutralForeground1)
- **Muted Text**: Lighter foreground (colorNeutralForeground3)
- **Code/Error Box**: Subtle background (colorNeutralBackground3)
- **Borders**: Neutral stroke (colorNeutralStroke1)
- **Primary Button**: Brand background (colorBrandBackground)
- **Error Icon**: Red foreground (colorPaletteRedForeground1)

## Responsive Behavior

- **Max Width**: 600px
- **Vertical Scrolling**: Enabled for long content
- **Fixed Sections**: Header and footer remain visible
- **Scrollable Content**: Middle section scrolls if content exceeds viewport

## Interactions

1. **Copy Correlation ID**:
   - Click → Copies to clipboard
   - Button text changes to "Copied!"
   - Resets after 2 seconds

2. **View Full Log**:
   - Opens log file directly (if path available)
   - Falls back to opening logs folder

3. **Repair FFmpeg**:
   - Calls POST /api/downloads/ffmpeg/repair
   - Shows "Repairing..." during operation
   - Displays alert on completion/error

4. **Attach FFmpeg**:
   - Navigates to /dependencies page
   - Allows manual FFmpeg path configuration

5. **Close & Retry**:
   - Closes modal
   - Returns user to generation UI for retry

## Conditional Display

- **Repair/Attach Buttons**: Only shown when `isFFmpegError` is true
  - Determined by error code containing "FFMPEG"
  - Or error message containing "ffmpeg" (case-insensitive)

- **Stderr Snippet**: Only shown when `failure.stderrSnippet` exists
- **Install Log Snippet**: Only shown when `failure.installLogSnippet` exists
- **Suggested Actions**: Only shown when array has items

## Accessibility

- **Focus Management**: First focusable element receives focus on open
- **Keyboard Navigation**: Tab, Shift+Tab for navigation
- **Escape Key**: Closes modal
- **ARIA Labels**: Proper labeling for screen readers
- **Semantic HTML**: Uses appropriate HTML elements
