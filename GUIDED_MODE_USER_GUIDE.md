# Guided Mode User Guide

## Overview

Guided Mode provides a beginner-first experience for video creation with AI-powered assistance throughout the process. It offers explanations, improvement suggestions, and intelligent iteration tools to help you create better videos with less effort.

## Features

### 1. Experience Levels

Guided Mode adapts to your experience level:

- **Beginner**: Full tooltips, explanations, and confirmation dialogs
- **Intermediate**: Key explanations with reduced hand-holding
- **Advanced**: Minimal guidance, expert controls

You can change your experience level in Settings at any time.

### 2. Explain This

At any stage of video creation (brief, plan, or script), click **"Explain this"** to get:

- Clear explanation of what the artifact means
- Key points highlighting important aspects
- Context about how it affects your video
- Optional: Ask specific questions about the content

**When to use:**
- You're unsure what a generated script means
- Want to understand why certain creative choices were made
- Need clarification on technical terms or structure

### 3. Improve Actions

Use the **"Improve"** menu to enhance your content:

#### Improve Clarity
Makes the content easier to understand by:
- Simplifying complex sentences
- Adding transitional phrases
- Removing ambiguous language
- Improving flow and coherence

#### Adapt for Audience
Adjusts content to better match your target audience:
- Changes vocabulary level
- Modifies tone and style
- Adjusts pacing and examples
- Tailors cultural references

#### Shorten
Reduces content length while preserving key messages:
- Removes redundancy
- Condenses wordy sections
- Focuses on essentials
- Maintains narrative flow

#### Expand
Adds more detail and context:
- Elaborates on key points
- Adds supporting examples
- Provides additional context
- Enriches the narrative

### 4. Prompt Diff Preview

Before any AI-powered changes are applied, you'll see a **Prompt Diff** that shows:

- **Intended Outcome**: What the change aims to achieve
- **Changes**: Detailed list of modifications to the AI prompt
- **Original Prompt**: What was used before
- **Modified Prompt**: What will be used now

**Why this matters:**
- Transparency in AI operations
- Understand what's being changed and why
- Confirm or cancel before proceeding
- Learn how prompts affect results

### 5. Section Locking

Protect parts of your content from being modified during regeneration:

1. Select the text you want to preserve
2. Click **"Lock Section"**
3. Add a reason (optional but recommended)
4. The section appears in the **"Locked Sections"** panel

**Use cases:**
- Keep a perfect opening paragraph
- Preserve specific facts or statistics
- Lock brand messaging
- Maintain approved legal language

When you regenerate or improve content, locked sections remain unchanged while the rest is updated.

### 6. Guided Tooltips

Hover over icons marked with **ⓘ** to see:
- Field explanations
- Best practices
- "Why this choice?" links for deeper dives
- Keyboard shortcuts and tips

Tooltips can be disabled in Settings if you prefer a cleaner interface.

## Workflow Examples

### Example 1: Creating Your First Video

1. Start the Create Wizard
2. See guided tooltips explaining each field
3. Enter your topic and basic details
4. Click **"Explain this"** on the brief to understand what happens next
5. Generate the script
6. Use **"Improve > Improve Clarity"** if needed
7. Review the Prompt Diff to see what changes
8. Confirm and proceed

### Example 2: Refining a Script

1. Generate initial script
2. Lock the introduction (perfect as-is)
3. Click **"Improve > Adapt for Audience"**
4. Select "Beginners" as target audience
5. Review Prompt Diff
6. Confirm changes
7. Introduction stays the same, rest adapts
8. Click **"Explain this"** to understand the changes

### Example 3: Iterating on Content

1. Generate first draft
2. Not quite right? Click **"Improve > Shorten"**
3. Review Prompt Diff
4. Apply changes
5. Still need work? Click **"Improve > Improve Clarity"**
6. Each iteration preserves locked sections
7. Use **"Explain this"** to understand the cumulative changes

## Telemetry and Feedback

Guided Mode tracks usage to improve the experience:

- **Feature usage**: Which actions you use most
- **Success rates**: How often operations complete successfully
- **User feedback**: Your thumbs up/down ratings
- **Timing**: How long operations take

**Privacy:**
- No personal content is stored
- Only anonymous usage patterns
- Helps improve AI prompts and UX
- Can be disabled in Settings

## Tips and Best Practices

### Getting Better Results

1. **Be specific**: Use "Adapt for Audience > Teenagers" rather than generic improvements
2. **Lock intentionally**: Only lock what truly shouldn't change
3. **Iterate gradually**: Make one improvement at a time to see effects
4. **Review diffs**: Understanding prompt changes helps you work better with AI

### When to Lock Sections

✅ **Do lock:**
- Brand taglines and slogans
- Specific facts, statistics, or quotes
- Legal disclaimers
- Approved messaging

❌ **Don't lock:**
- Entire scripts (prevents improvement)
- Generic transitions
- Placeholder content
- Trial-and-error sections

### Maximizing Explanations

- **Ask specific questions**: "Why is this scene 10 seconds?"
- **Use after changes**: Understand what improved
- **Rate helpfulness**: Improves future explanations
- **Read key points**: Quick way to grasp essentials

## Turning Off Guided Mode

If you prefer working without assistance:

1. Go to Settings > Guided Mode
2. Select "Advanced" experience level
3. Or toggle "Enable Guided Mode" off entirely

You can always turn it back on when needed.

## Keyboard Shortcuts

- `Ctrl+E`: Explain current artifact (when focused)
- `Ctrl+I`: Open Improve menu
- `Ctrl+L`: Lock selected section
- `Esc`: Close explanation panel or prompt diff

## Troubleshooting

### "Explain This" not working
- Check internet connection
- Verify AI provider is configured
- Try again in a moment

### Improvements not applying
- Review locked sections (might be blocking changes)
- Check Prompt Diff for conflicts
- Try a different improvement action

### Prompt Diff not showing
- May be disabled in Advanced mode
- Check Settings > Guided Mode > Require Prompt Diff Confirmation

## API Documentation

For developers integrating guided mode features, see:
- `docs/api/guided-mode-endpoints.md`
- `docs/api/explain-controller.md`
- `docs/api/telemetry.md`

## Feedback

Help us improve Guided Mode:
- Rate explanations (👍/👎)
- Report issues on GitHub
- Suggest features in Discussions
- Share your workflows in Community

---

**Version**: 1.0.0  
**Last Updated**: 2025-11-04
