using System;
using System.Collections.Generic;
using Aura.Core.Models.PromptManagement;

namespace Aura.Core.Services.PromptManagement;

/// <summary>
/// Factory for creating system-provided prompt templates
/// These are the built-in templates that ship with Aura Video Studio
/// </summary>
public static class SystemPromptTemplateFactory
{
    /// <summary>
    /// Create all system prompt templates
    /// </summary>
    public static List<PromptTemplate> CreateSystemTemplates()
    {
        var templates = new List<PromptTemplate>();

        templates.Add(CreateBriefToOutlineTemplate());
        templates.Add(CreateOutlineToScriptTemplate());
        templates.Add(CreateHookGenerationTemplate());
        templates.Add(CreateCallToActionTemplate());
        templates.Add(CreateScriptOptimizationTemplate());
        templates.Add(CreateVisualDescriptionTemplate());
        templates.Add(CreateImagePromptFormattingTemplate());
        templates.Add(CreateSceneMoodTemplate());
        templates.Add(CreateQualityScoringTemplate());
        templates.Add(CreateTranslationTemplate());
        templates.Add(CreateTitleOptimizationTemplate());
        templates.Add(CreateHashtagGenerationTemplate());

        return templates;
    }

    private static PromptTemplate CreateBriefToOutlineTemplate()
    {
        return new PromptTemplate
        {
            Id = "system-brief-to-outline-v1",
            Name = "Brief to Outline",
            Description = "Convert a creative brief into a structured video outline",
            Category = PromptCategory.ScriptGeneration,
            Stage = PipelineStage.BriefToOutline,
            Source = TemplateSource.System,
            Status = TemplateStatus.Active,
            IsDefault = true,
            PromptText = @"Create a detailed video outline based on the following brief:

Topic: {{topic}}
Target Audience: {{audience}}
Goal: {{goal}}
Tone: {{tone}}
Duration: {{duration}} minutes
Language: {{language}}

Please create an outline that:
1. Starts with a compelling hook to capture attention in the first 5 seconds
2. Breaks down the content into clear sections with descriptive headers
3. Identifies key points and examples for each section
4. Suggests visual moments that would enhance the narrative
5. Includes a strong call-to-action at the end

Format the outline with clear section markers and brief descriptions of what each section should cover.",
            Variables = new List<PromptVariable>
            {
                new() { Name = "topic", Description = "Video topic", Type = VariableType.String, Required = true, ExampleValue = "Climate Change Solutions" },
                new() { Name = "audience", Description = "Target audience", Type = VariableType.String, Required = true, ExampleValue = "High school students" },
                new() { Name = "goal", Description = "Video goal", Type = VariableType.String, Required = true, ExampleValue = "Educate" },
                new() { Name = "tone", Description = "Video tone", Type = VariableType.String, Required = true, ExampleValue = "informative" },
                new() { Name = "duration", Description = "Target duration in minutes", Type = VariableType.Numeric, Required = true, ExampleValue = "3" },
                new() { Name = "language", Description = "Content language", Type = VariableType.String, Required = true, DefaultValue = "en", ExampleValue = "en" }
            },
            Tags = new List<string> { "script", "outline", "planning", "structure" },
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromptTemplate CreateOutlineToScriptTemplate()
    {
        return new PromptTemplate
        {
            Id = "system-outline-to-script-v1",
            Name = "Outline to Full Script",
            Description = "Expand an outline into a complete video script with timing",
            Category = PromptCategory.ScriptGeneration,
            Stage = PipelineStage.OutlineToScript,
            Source = TemplateSource.System,
            Status = TemplateStatus.Active,
            IsDefault = true,
            PromptText = @"Expand the following outline into a complete video script:

{{outline}}

Requirements:
- Target Duration: {{duration}} minutes
- Tone: {{tone}}
- Pacing: {{pacing}}
- Content Density: {{density}}
- Target Audience: {{audience}}

Script Guidelines:
1. Write in a natural, conversational style appropriate for {{tone}} tone
2. Keep sentences short and clear
3. Include specific examples and details
4. Mark natural pauses and emphasis
5. Ensure smooth transitions between sections
6. Write for the ear, not the eye (avoid complex sentence structures)
7. Include stage directions in [brackets] where helpful for visuals

Output a complete script ready for narration, properly paced for {{duration}} minutes.",
            Variables = new List<PromptVariable>
            {
                new() { Name = "outline", Description = "Video outline to expand", Type = VariableType.String, Required = true },
                new() { Name = "duration", Description = "Target duration in minutes", Type = VariableType.Numeric, Required = true, ExampleValue = "3" },
                new() { Name = "tone", Description = "Video tone", Type = VariableType.String, Required = true, ExampleValue = "professional" },
                new() { Name = "pacing", Description = "Content pacing", Type = VariableType.String, Required = true, ExampleValue = "Conversational" },
                new() { Name = "density", Description = "Information density", Type = VariableType.String, Required = true, ExampleValue = "Balanced" },
                new() { Name = "audience", Description = "Target audience", Type = VariableType.String, Required = true, ExampleValue = "General public" }
            },
            Tags = new List<string> { "script", "expansion", "dialogue", "narration" },
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromptTemplate CreateHookGenerationTemplate()
    {
        return new PromptTemplate
        {
            Id = "system-hook-generation-v1",
            Name = "Compelling Hook Generator",
            Description = "Generate attention-grabbing opening hooks for videos",
            Category = PromptCategory.ScriptGeneration,
            Stage = PipelineStage.HookGeneration,
            Source = TemplateSource.System,
            Status = TemplateStatus.Active,
            IsDefault = true,
            PromptText = @"Create 3 compelling hook options for a video about:

Topic: {{topic}}
Target Audience: {{audience}}
Platform: {{platform}}

Each hook should:
1. Capture attention in the first 3-5 seconds
2. Create curiosity or emotional engagement
3. Be appropriate for {{audience}}
4. Work well on {{platform}}

Hook types to consider:
- Question: Pose an intriguing question
- Surprising Fact: Share an unexpected statistic or fact
- Story Opening: Start with a relatable scenario
- Bold Statement: Make a provocative claim
- Problem Statement: Identify a pain point

Provide 3 distinct hook variations, each 1-2 sentences.",
            Variables = new List<PromptVariable>
            {
                new() { Name = "topic", Description = "Video topic", Type = VariableType.String, Required = true, ExampleValue = "Productivity Tips" },
                new() { Name = "audience", Description = "Target audience", Type = VariableType.String, Required = true, ExampleValue = "Young professionals" },
                new() { Name = "platform", Description = "Target platform", Type = VariableType.String, Required = false, DefaultValue = "YouTube", ExampleValue = "YouTube" }
            },
            Tags = new List<string> { "hook", "opening", "engagement", "retention" },
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromptTemplate CreateCallToActionTemplate()
    {
        return new PromptTemplate
        {
            Id = "system-cta-generation-v1",
            Name = "Call-to-Action Generator",
            Description = "Generate effective CTAs for video endings",
            Category = PromptCategory.ScriptGeneration,
            Stage = PipelineStage.CallToActionGeneration,
            Source = TemplateSource.System,
            Status = TemplateStatus.Active,
            IsDefault = true,
            PromptText = @"Create a compelling call-to-action for the end of this video:

Video Topic: {{topic}}
Video Goal: {{goal}}
Target Audience: {{audience}}
Desired Action: {{desired_action}}

The CTA should:
1. Be clear and specific
2. Create urgency or value
3. Be appropriate for {{audience}}
4. Align with {{goal}}
5. Be natural and not overly salesy

Provide 2 CTA variations:
- Standard CTA: Direct and straightforward
- Soft CTA: More subtle and value-focused",
            Variables = new List<PromptVariable>
            {
                new() { Name = "topic", Description = "Video topic", Type = VariableType.String, Required = true },
                new() { Name = "goal", Description = "Video goal", Type = VariableType.String, Required = true },
                new() { Name = "audience", Description = "Target audience", Type = VariableType.String, Required = true },
                new() { Name = "desired_action", Description = "What action should viewers take", Type = VariableType.String, Required = true, ExampleValue = "Subscribe to newsletter" }
            },
            Tags = new List<string> { "cta", "conversion", "ending", "action" },
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromptTemplate CreateScriptOptimizationTemplate()
    {
        return new PromptTemplate
        {
            Id = "system-script-optimization-v1",
            Name = "Script Optimization & Refinement",
            Description = "Review and optimize video scripts for quality and engagement",
            Category = PromptCategory.ScriptGeneration,
            Stage = PipelineStage.ScriptOptimization,
            Source = TemplateSource.System,
            Status = TemplateStatus.Active,
            IsDefault = true,
            PromptText = @"Review and optimize this video script:

{{script}}

Target Audience: {{audience}}
Video Goal: {{goal}}
Platform: {{platform}}

Analyze and improve:
1. Clarity: Are concepts explained clearly?
2. Engagement: Will it hold viewer attention?
3. Pacing: Is the information flow appropriate?
4. Language: Is it conversational and easy to understand?
5. Structure: Does it have a clear beginning, middle, and end?
6. Specificity: Are there enough concrete examples?

Provide:
- Overall quality score (1-10)
- Top 3 strengths
- Top 3 areas for improvement
- Optimized version of the script with improvements highlighted",
            Variables = new List<PromptVariable>
            {
                new() { Name = "script", Description = "Script to optimize", Type = VariableType.String, Required = true },
                new() { Name = "audience", Description = "Target audience", Type = VariableType.String, Required = true },
                new() { Name = "goal", Description = "Video goal", Type = VariableType.String, Required = true },
                new() { Name = "platform", Description = "Target platform", Type = VariableType.String, Required = false, DefaultValue = "YouTube" }
            },
            Tags = new List<string> { "optimization", "review", "quality", "refinement" },
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromptTemplate CreateVisualDescriptionTemplate()
    {
        return new PromptTemplate
        {
            Id = "system-visual-description-v1",
            Name = "Visual Scene Description",
            Description = "Generate detailed visual descriptions for video scenes",
            Category = PromptCategory.SceneDescription,
            Stage = PipelineStage.VisualDescription,
            Source = TemplateSource.System,
            Status = TemplateStatus.Active,
            IsDefault = true,
            PromptText = @"Create a detailed visual description for this video scene:

Scene Text: {{scene_text}}
Visual Style: {{visual_style}}
Mood: {{mood}}
Previous Scene: {{previous_scene}}

Describe:
1. Main subject/focus of the scene
2. Setting and environment
3. Color palette and lighting
4. Composition and framing
5. Any text overlays or graphics needed
6. Transition from previous scene

Ensure visual continuity with the previous scene while clearly representing the current content.

Output a comprehensive visual description suitable for directing visual asset creation.",
            Variables = new List<PromptVariable>
            {
                new() { Name = "scene_text", Description = "Script text for this scene", Type = VariableType.String, Required = true },
                new() { Name = "visual_style", Description = "Overall visual style", Type = VariableType.String, Required = true, ExampleValue = "modern minimalist" },
                new() { Name = "mood", Description = "Scene mood", Type = VariableType.String, Required = true, ExampleValue = "professional" },
                new() { Name = "previous_scene", Description = "Description of previous scene", Type = VariableType.String, Required = false, DefaultValue = "None" }
            },
            Tags = new List<string> { "visual", "scene", "description", "imagery" },
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromptTemplate CreateImagePromptFormattingTemplate()
    {
        return new PromptTemplate
        {
            Id = "system-image-prompt-formatting-v1",
            Name = "Stable Diffusion Prompt Formatter",
            Description = "Format visual descriptions as optimized Stable Diffusion prompts",
            Category = PromptCategory.SceneDescription,
            Stage = PipelineStage.ImagePromptFormatting,
            Source = TemplateSource.System,
            Status = TemplateStatus.Active,
            IsDefault = true,
            PromptText = @"Convert this visual description into an optimized Stable Diffusion prompt:

Visual Description: {{description}}
Style Keywords: {{style_keywords}}
Quality Modifiers: {{quality_modifiers}}

Create a prompt that:
1. Leads with the main subject
2. Includes relevant style keywords
3. Specifies composition and lighting
4. Adds technical quality modifiers
5. Uses comma-separated keywords
6. Avoids unnecessary words

Also provide negative prompt with elements to avoid.

Output Format:
Prompt: [optimized positive prompt]
Negative Prompt: [negative prompt]",
            Variables = new List<PromptVariable>
            {
                new() { Name = "description", Description = "Visual description to format", Type = VariableType.String, Required = true },
                new() { Name = "style_keywords", Description = "Style keywords", Type = VariableType.String, Required = false, DefaultValue = "digital art, professional" },
                new() { Name = "quality_modifiers", Description = "Quality modifiers", Type = VariableType.String, Required = false, DefaultValue = "8k, highly detailed, sharp focus" }
            },
            Tags = new List<string> { "stable-diffusion", "image-generation", "prompt-engineering" },
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromptTemplate CreateSceneMoodTemplate()
    {
        return new PromptTemplate
        {
            Id = "system-scene-mood-v1",
            Name = "Scene Mood & Atmosphere",
            Description = "Analyze and describe the emotional mood of a scene",
            Category = PromptCategory.SceneDescription,
            Stage = PipelineStage.SceneMood,
            Source = TemplateSource.System,
            Status = TemplateStatus.Active,
            IsDefault = true,
            PromptText = @"Analyze the mood and atmosphere for this scene:

Scene Content: {{scene_content}}
Video Tone: {{tone}}
Target Emotion: {{target_emotion}}

Describe:
1. Primary emotional tone
2. Color palette that supports the mood
3. Lighting style (bright, dramatic, soft, etc.)
4. Music/audio mood suggestions
5. Pacing recommendation (fast, slow, contemplative)

Provide a concise mood description that can guide visual and audio decisions.",
            Variables = new List<PromptVariable>
            {
                new() { Name = "scene_content", Description = "Content of the scene", Type = VariableType.String, Required = true },
                new() { Name = "tone", Description = "Overall video tone", Type = VariableType.String, Required = true },
                new() { Name = "target_emotion", Description = "Desired emotional response", Type = VariableType.String, Required = false, DefaultValue = "engaging" }
            },
            Tags = new List<string> { "mood", "atmosphere", "emotion", "tone" },
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromptTemplate CreateQualityScoringTemplate()
    {
        return new PromptTemplate
        {
            Id = "system-quality-scoring-v1",
            Name = "Content Quality Scoring",
            Description = "Evaluate content quality with detailed scoring",
            Category = PromptCategory.ContentAnalysis,
            Stage = PipelineStage.QualityScoring,
            Source = TemplateSource.System,
            Status = TemplateStatus.Active,
            IsDefault = true,
            PromptText = @"Evaluate the quality of this content:

Content: {{content}}
Content Type: {{content_type}}
Target Audience: {{audience}}

Score each category from 1-10:
1. Clarity - Is the message clear and easy to understand?
2. Accuracy - Is the information accurate and well-researched?
3. Engagement - Will it capture and hold attention?
4. Relevance - Is it relevant to the target audience?
5. Grammar - Is it grammatically correct?
6. Structure - Is it well-organized and logical?
7. Originality - Does it offer unique insights or perspectives?

Provide:
- Score for each category
- Overall quality score (average)
- Brief justification for each score
- Top 3 recommendations for improvement",
            Variables = new List<PromptVariable>
            {
                new() { Name = "content", Description = "Content to evaluate", Type = VariableType.String, Required = true },
                new() { Name = "content_type", Description = "Type of content", Type = VariableType.String, Required = true, ExampleValue = "video script" },
                new() { Name = "audience", Description = "Target audience", Type = VariableType.String, Required = true }
            },
            Tags = new List<string> { "quality", "scoring", "evaluation", "review" },
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromptTemplate CreateTranslationTemplate()
    {
        return new PromptTemplate
        {
            Id = "system-translation-v1",
            Name = "Content Translation with Cultural Adaptation",
            Description = "Translate content while adapting for cultural context",
            Category = PromptCategory.Translation,
            Stage = PipelineStage.Translation,
            Source = TemplateSource.System,
            Status = TemplateStatus.Active,
            IsDefault = true,
            PromptText = @"Translate this content from {{source_language}} to {{target_language}}:

Content: {{content}}

Requirements:
1. Maintain the original tone and intent
2. Adapt cultural references appropriately for {{target_language}} speakers
3. Localize idioms and expressions
4. Preserve formatting and structure
5. Ensure natural, fluent language in the target
6. Keep technical terms accurate

Provide:
- Translated content
- Notes on any cultural adaptations made
- Alternative phrasings for ambiguous sections if applicable",
            Variables = new List<PromptVariable>
            {
                new() { Name = "content", Description = "Content to translate", Type = VariableType.String, Required = true },
                new() { Name = "source_language", Description = "Source language code", Type = VariableType.String, Required = true, ExampleValue = "en" },
                new() { Name = "target_language", Description = "Target language code", Type = VariableType.String, Required = true, ExampleValue = "es" }
            },
            Tags = new List<string> { "translation", "localization", "cultural-adaptation" },
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromptTemplate CreateTitleOptimizationTemplate()
    {
        return new PromptTemplate
        {
            Id = "system-title-optimization-v1",
            Name = "Video Title Optimization",
            Description = "Generate optimized, engaging video titles",
            Category = PromptCategory.Optimization,
            Stage = PipelineStage.TitleOptimization,
            Source = TemplateSource.System,
            Status = TemplateStatus.Active,
            IsDefault = true,
            PromptText = @"Create optimized video titles for:

Video Topic: {{topic}}
Platform: {{platform}}
Target Keywords: {{keywords}}
Target Audience: {{audience}}

Generate 5 title variations that:
1. Include primary keywords naturally
2. Are compelling and clickable
3. Stay within platform character limits ({{platform}} recommended length)
4. Accurately represent the content
5. Appeal to {{audience}}

Title styles to include:
- Question format
- How-to format
- List format
- Benefit-focused
- Curiosity-driven

Provide 5 distinct title options with brief explanation of each approach.",
            Variables = new List<PromptVariable>
            {
                new() { Name = "topic", Description = "Video topic", Type = VariableType.String, Required = true },
                new() { Name = "platform", Description = "Target platform", Type = VariableType.String, Required = true, DefaultValue = "YouTube" },
                new() { Name = "keywords", Description = "Target keywords", Type = VariableType.String, Required = false, DefaultValue = "" },
                new() { Name = "audience", Description = "Target audience", Type = VariableType.String, Required = true }
            },
            Tags = new List<string> { "title", "optimization", "seo", "metadata" },
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromptTemplate CreateHashtagGenerationTemplate()
    {
        return new PromptTemplate
        {
            Id = "system-hashtag-generation-v1",
            Name = "Hashtag Strategy Generator",
            Description = "Generate strategic hashtags for video promotion",
            Category = PromptCategory.Optimization,
            Stage = PipelineStage.HashtagGeneration,
            Source = TemplateSource.System,
            Status = TemplateStatus.Active,
            IsDefault = true,
            PromptText = @"Generate a hashtag strategy for this video:

Topic: {{topic}}
Platform: {{platform}}
Target Audience: {{audience}}
Video Goal: {{goal}}

Create a mix of:
1. Broad hashtags (high volume, competitive)
2. Niche hashtags (targeted, specific to content)
3. Trending hashtags (if applicable)
4. Branded hashtags (channel/brand specific)

Provide:
- 10-15 recommended hashtags
- Category for each (broad/niche/trending/branded)
- Estimated reach level (high/medium/low)
- Strategic order for posting

Focus on {{platform}} best practices and current trends.",
            Variables = new List<PromptVariable>
            {
                new() { Name = "topic", Description = "Video topic", Type = VariableType.String, Required = true },
                new() { Name = "platform", Description = "Target platform", Type = VariableType.String, Required = true, DefaultValue = "YouTube" },
                new() { Name = "audience", Description = "Target audience", Type = VariableType.String, Required = true },
                new() { Name = "goal", Description = "Video goal", Type = VariableType.String, Required = true }
            },
            Tags = new List<string> { "hashtags", "social-media", "discovery", "seo" },
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
    }
}
