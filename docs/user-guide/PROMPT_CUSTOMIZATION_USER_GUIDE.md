# Prompt Customization User Guide

## Overview

Aura Video Studio's advanced prompt engineering framework allows you to customize how AI generates video scripts. You can fine-tune prompts with your own instructions, select example styles, choose different prompt versions, and even use chain-of-thought generation for more deliberate content creation.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Custom Instructions](#custom-instructions)
3. [Example Styles](#example-styles)
4. [Prompt Versions](#prompt-versions)
5. [Chain-of-Thought Mode](#chain-of-thought-mode)
6. [Preset Management](#preset-management)
7. [API Reference](#api-reference)
8. [Best Practices](#best-practices)

## Getting Started

### Accessing Prompt Customization

1. Open the video creation wizard
2. Click the **"Customize Prompts"** button (with sparkle icon) in Advanced Settings
3. The Prompt Customization Panel will open, showing all available options

### Basic Workflow

1. Enter your video topic and basic settings
2. Open prompt customization (optional)
3. Add custom instructions or select an example style
4. Preview the generated prompt
5. Save as a preset for future use (optional)
6. Generate your video

## Custom Instructions

### What Are Custom Instructions?

Custom instructions are additional guidelines you provide to influence how the AI generates your script. These instructions are appended to the base prompt with special markers to ensure they're respected.

### How to Use

1. In the Prompt Customization Panel, expand **"Custom Instructions"**
2. Enter your instructions in the textarea
3. Click **"Validate"** to ensure your instructions are safe and valid
4. Your instructions will be applied when generating the video

### Examples

**Good Instructions:**
- "Focus on practical, real-world examples that beginners can relate to"
- "Use analogies to explain complex concepts"
- "Keep sentences short and punchy for better engagement"
- "Include a call-to-action at the end"

**Instructions to Avoid:**
- Avoid trying to override system behavior
- Don't use malicious patterns like "ignore previous instructions"
- Keep instructions under 5,000 characters

### Security Validation

The system automatically validates your instructions for:
- Prompt injection attempts
- Malicious patterns
- Excessive length (max 5,000 characters)

If validation fails, you'll see an error message with suggestions for fixing the issue.

## Example Styles

### What Are Example Styles?

Example styles are curated few-shot examples that demonstrate ideal script structures and techniques for different video types. Selecting an example style helps the AI understand the format and tone you're aiming for.

### Available Types

1. **Educational** - Science explanations, historical events, tutorials
2. **Entertainment** - Top 10 lists, storytelling, reactions
3. **Tutorial** - How-to guides, step-by-step instructions
4. **Documentary** - Investigations, deep dives, explorations
5. **Promotional** - Product launches, announcements, marketing

### How to Use

1. Expand **"Example Styles"** in the customization panel
2. Select a video type from the dropdown
3. Choose a specific example that matches your content style
4. The example's key techniques will be applied to your prompt

### Example: Educational - Science Explainer

When you select this example, the AI will:
- Start with an engaging hook
- Use relatable analogies
- Break down complex concepts step-by-step
- Include visual moments
- End with practical takeaways

## Prompt Versions

### What Are Prompt Versions?

Prompt versions are different optimization strategies for the base system prompt. Each version is tuned for specific goals and audience types.

### Available Versions

#### 1. default-v1 (Default)
**Best For:** General-purpose videos  
**Focus:** Balanced quality, suitable for most content types  
**When to Use:** When you're unsure or want standard quality output

#### 2. high-engagement-v1
**Best For:** Social media, viral content  
**Focus:** Maximum viewer retention and engagement  
**Features:**
- Strong hooks in first 3-5 seconds
- Pattern interrupts
- Emotional peaks
- Curiosity gaps
- 90%+ retention targeting

**When to Use:** YouTube Shorts, TikTok, Instagram Reels, or any platform where retention matters

#### 3. educational-deep-v1
**Best For:** Educational content, courses, explainers  
**Focus:** Comprehensive explanations and clarity  
**Features:**
- Detailed breakdowns
- Step-by-step explanations
- More thorough coverage
- Prioritizes understanding over entertainment

**When to Use:** Educational videos, tutorials, courses, or content where learning is the primary goal

### How to Choose

Select a version that matches your content goal:
- **Entertaining/Viral** → high-engagement-v1
- **Teaching/Explaining** → educational-deep-v1
- **Mixed/Unsure** → default-v1

## Chain-of-Thought Mode

### What Is Chain-of-Thought?

Chain-of-thought mode breaks script generation into three iterative stages, allowing you to review and refine at each step. This produces more thoughtful, deliberate content.

### The Three Stages

#### Stage 1: Topic Analysis
**What Happens:** The AI analyzes your topic and provides strategic insights  
**Output:**
- Key themes and angles
- Audience hooks
- Potential challenges
- Unique perspectives
- Content structure recommendations

**Review:** Review the analysis (no editing needed)

#### Stage 2: Outline Creation
**What Happens:** Based on the analysis, create a detailed outline  
**Output:**
- Scene-by-scene breakdown
- Key points for each section
- Visual moment suggestions
- Transition ideas

**Review:** You can edit the outline before proceeding

#### Stage 3: Full Script
**What Happens:** Expand the outline into a complete script  
**Output:**
- Full narration text
- Scene descriptions
- Visual cues
- Timing recommendations

**Review:** Final opportunity to edit before rendering

### When to Use Chain-of-Thought

**Use When:**
- Creating important or high-stakes content
- You want more control over the creative direction
- The topic is complex and needs careful structuring
- You have time for a more deliberate process

**Skip When:**
- Creating quick, casual content
- You trust the AI's standard approach
- Time is limited
- The topic is straightforward

### How to Enable

1. In Prompt Customization, expand **"Chain-of-Thought"**
2. Toggle **"Enable Chain-of-Thought Mode"**
3. During generation, you'll be prompted to review each stage

## Preset Management

### What Are Presets?

Presets are saved configurations of your prompt customization settings. They allow you to reuse successful combinations without re-entering settings each time.

### Creating a Preset

1. Configure your desired settings (instructions, example style, version)
2. Expand **"Presets"** in the customization panel
3. Enter a name and description for your preset
4. Click **"Save Preset"**

### Loading a Preset

1. Expand **"Presets"**
2. Select a preset from the dropdown
3. Click **"Load"**
4. All settings will be applied automatically

### Managing Presets

- **Edit:** Load a preset, modify settings, and save with the same name
- **Delete:** Select a preset and click **"Delete"**
- **Export:** (Coming soon) Share presets with team members

### Preset Ideas

**"Quick Social"**
- Version: high-engagement-v1
- Instructions: "Keep it punchy, under 60 seconds worth of content"
- Example: Entertainment

**"Educational Deep Dive"**
- Version: educational-deep-v1
- Instructions: "Include research citations and detailed explanations"
- Example: Educational - Science Explainer
- Chain-of-Thought: Enabled

**"Product Launch"**
- Version: high-engagement-v1
- Instructions: "Focus on benefits, include strong CTA"
- Example: Promotional - Product Launch

## API Reference

### Preview Endpoint

**POST** `/api/prompts/preview`

Generate a prompt preview with variable substitutions before LLM invocation.

**Request:**
```json
{
  "topic": "Machine Learning Basics",
  "audience": "Beginners",
  "goal": "Education",
  "tone": "informative",
  "language": "en",
  "targetDurationMinutes": 3,
  "pacing": "Conversational",
  "density": "Balanced",
  "style": "educational",
  "aspect": "Widescreen16x9",
  "promptModifiers": {
    "additionalInstructions": "Focus on practical examples",
    "exampleStyle": "Science Explainer",
    "enableChainOfThought": false,
    "promptVersion": "default-v1"
  }
}
```

**Response:**
```json
{
  "systemPrompt": "You are an expert video creator...",
  "userPrompt": "CREATE A VIDEO SCRIPT:\nTopic: Machine Learning Basics...",
  "finalPrompt": "Combined system and user prompts",
  "substitutedVariables": {
    "{TOPIC}": "Machine Learning Basics",
    "{AUDIENCE}": "Beginners"
  },
  "promptVersion": "default-v1",
  "estimatedTokens": 1250
}
```

### List Examples Endpoint

**GET** `/api/prompts/list-examples?videoType=Educational`

Get few-shot examples, optionally filtered by video type.

**Response:**
```json
{
  "examples": [
    {
      "videoType": "Educational",
      "exampleName": "Science Explainer",
      "description": "Clear, engaging explanation...",
      "keyTechniques": [
        "Hook with surprising fact",
        "Use relatable analogies"
      ]
    }
  ],
  "totalCount": 2
}
```

### List Versions Endpoint

**GET** `/api/prompts/versions`

Get all available prompt versions.

**Response:**
```json
{
  "versions": [
    {
      "version": "default-v1",
      "name": "Standard Quality",
      "description": "Balanced approach...",
      "isDefault": true
    }
  ],
  "totalCount": 3
}
```

### Validate Instructions Endpoint

**POST** `/api/prompts/validate-instructions`

Validate custom instructions for security issues.

**Request:**
```json
{
  "instructions": "Focus on practical examples"
}
```

**Response:**
```json
{
  "isValid": true,
  "errorMessage": null,
  "suggestions": []
}
```

## Best Practices

### Do's

✅ **Be Specific**
- Good: "Use 3-5 practical examples from everyday life"
- Bad: "Make it good"

✅ **Focus on Style, Not Content**
- Good: "Write in a conversational, friendly tone"
- Bad: "Include these exact words: ..."

✅ **Use Example Styles**
- They provide proven structures and techniques
- Match the example to your content type

✅ **Test and Iterate**
- Save successful configurations as presets
- Experiment with different versions

✅ **Preview Before Generating**
- Check the final prompt to ensure it makes sense
- Verify variable substitutions are correct

### Don'ts

❌ **Don't Try to Override System Behavior**
- The system will detect and reject malicious patterns
- Work with the AI, not against it

❌ **Don't Make Instructions Too Long**
- Keep under 5,000 characters
- Be concise and clear

❌ **Don't Ignore Security Warnings**
- If validation fails, revise your instructions
- Malicious patterns are blocked for everyone's safety

❌ **Don't Use Technical Jargon Unnecessarily**
- The AI understands natural language
- "Be more engaging" works better than "Optimize for AVD metrics"

### Tips for Better Results

1. **Start Simple:** Try the default settings first, then customize
2. **One Change at a Time:** Test modifications incrementally
3. **Use Examples:** They dramatically improve output quality
4. **Save Presets:** Don't lose successful configurations
5. **Read the Preview:** Catch issues before generating

### Common Issues

**Problem:** AI ignores my custom instructions  
**Solution:** Ensure instructions are validated and specific. Try rephrasing.

**Problem:** Generated content is off-brand  
**Solution:** Use custom instructions to define brand voice and style explicitly.

**Problem:** Chain-of-thought takes too long  
**Solution:** Use for important content only. For quick videos, stick with standard mode.

**Problem:** Too many options are overwhelming  
**Solution:** Start with presets or just use custom instructions. Add complexity gradually.

## Troubleshooting

### Validation Errors

If you see "Invalid instructions detected":
1. Check for phrases like "ignore previous" or "forget instructions"
2. Ensure length is under 5,000 characters
3. Remove any HTML or script tags
4. Rephrase ambiguous or manipulative language

### Unexpected Results

If the AI produces unexpected content:
1. Check the prompt preview to see what was actually sent
2. Verify your example style matches your content type
3. Try a different prompt version
4. Simplify custom instructions
5. Test without customization to establish a baseline

### Performance Issues

If generation is slow:
1. Chain-of-thought takes 3x longer (by design)
2. Longer custom instructions increase processing time
3. Complex example styles may require more tokens
4. Check your internet connection for cloud-based LLMs

## Support

For additional help:
- GitHub Issues: https://github.com/itsacoffee/aura-video-studio/issues
- Documentation: See PROMPT_ENGINEERING_IMPLEMENTATION.md for technical details
- Community: Join discussions in GitHub Discussions

---

**Last Updated:** October 2025  
**Version:** 1.0 (from PR #3)
