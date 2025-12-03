using System;
using System.Collections.Generic;
using Aura.Core.Models;
using Aura.Core.Models.Templates;

namespace Aura.Core.Services.Templates;

/// <summary>
/// Provides built-in video structure templates for common content formats.
/// </summary>
public static class BuiltInScriptTemplates
{
    /// <summary>
    /// Gets all built-in templates.
    /// </summary>
    public static IReadOnlyList<VideoTemplate> GetAll() => new List<VideoTemplate>
    {
        CreateExplainerTemplate(),
        CreateListicleTemplate(),
        CreateComparisonTemplate(),
        CreateStoryTimeTemplate(),
        CreateTutorialTemplate(),
        CreateProductShowcaseTemplate()
    };

    /// <summary>
    /// Explainer template: Hook → Problem → Solution → Benefits → CTA
    /// Best for educational content that explains concepts or processes.
    /// </summary>
    private static VideoTemplate CreateExplainerTemplate()
    {
        var sections = new List<TemplateSection>
        {
            new TemplateSection(
                Name: "Hook",
                Purpose: "Capture attention with a surprising fact or question",
                Type: SectionType.Hook,
                SuggestedDuration: TimeSpan.FromSeconds(10),
                PromptTemplate: "Write a compelling hook about {{topic}} that captures attention immediately. Use a surprising statistic, provocative question, or bold statement.",
                ExampleContent: new[] { "Did you know that 90% of people...", "What if I told you that..." }),

            new TemplateSection(
                Name: "Problem",
                Purpose: "Identify the problem or pain point the audience faces",
                Type: SectionType.Problem,
                SuggestedDuration: TimeSpan.FromSeconds(20),
                PromptTemplate: "Describe the problem or challenge that {{audience}} faces regarding {{topic}}. Make it relatable and highlight the consequences of not solving it."),

            new TemplateSection(
                Name: "Solution",
                Purpose: "Present the solution or concept being explained",
                Type: SectionType.Solution,
                SuggestedDuration: TimeSpan.FromSeconds(45),
                PromptTemplate: "Explain {{topic}} as the solution. Break down the concept into clear, digestible parts that {{audience}} can understand. Use simple language and concrete examples."),

            new TemplateSection(
                Name: "Benefits",
                Purpose: "Highlight the key benefits and outcomes",
                Type: SectionType.MainPoint,
                SuggestedDuration: TimeSpan.FromSeconds(30),
                PromptTemplate: "Describe the key benefits of understanding or applying {{topic}}. Focus on practical outcomes that matter to {{audience}}."),

            new TemplateSection(
                Name: "Call to Action",
                Purpose: "Guide the viewer on what to do next",
                Type: SectionType.CallToAction,
                SuggestedDuration: TimeSpan.FromSeconds(15),
                PromptTemplate: "End with a clear call to action. Encourage viewers to apply what they learned about {{topic}}, subscribe, or take the next step.")
        };

        var variables = new List<TemplateVariable>
        {
            new TemplateVariable(
                Name: "topic",
                DisplayName: "Topic",
                Type: VariableType.Text,
                DefaultValue: null,
                Placeholder: "e.g., Machine Learning basics",
                IsRequired: true),
            new TemplateVariable(
                Name: "audience",
                DisplayName: "Target Audience",
                Type: VariableType.Text,
                DefaultValue: "beginners",
                Placeholder: "e.g., beginners, professionals, students",
                IsRequired: false)
        };

        var metadata = new TemplateMetadata(
            RecommendedAudiences: new[] { "Beginners", "Students", "General" },
            RecommendedTones: new[] { "Informative", "Professional", "Friendly" },
            SupportedAspects: new[] { Aspect.Widescreen16x9, Aspect.Vertical9x16 },
            MinDuration: TimeSpan.FromMinutes(1),
            MaxDuration: TimeSpan.FromMinutes(10),
            Tags: new[] { "educational", "explainer", "how-to", "learning" });

        return new VideoTemplate(
            Id: "explainer",
            Name: "Explainer",
            Description: "Hook → Problem → Solution → Benefits → CTA. Perfect for educational content that breaks down complex topics.",
            Category: "Educational",
            Structure: new TemplateStructureSpec(sections, TimeSpan.FromMinutes(2), 5),
            Variables: variables,
            Thumbnail: new TemplateThumbnail("Lightbulb", "#4CAF50"),
            Metadata: metadata);
    }

    /// <summary>
    /// Listicle template: Hook → Numbered items → Recap → CTA
    /// Best for top N lists, tips, and ranked content.
    /// </summary>
    private static VideoTemplate CreateListicleTemplate()
    {
        var sections = new List<TemplateSection>
        {
            new TemplateSection(
                Name: "Hook",
                Purpose: "Introduce the list and create anticipation",
                Type: SectionType.Hook,
                SuggestedDuration: TimeSpan.FromSeconds(10),
                PromptTemplate: "Write an engaging introduction for a list of {{count}} {{topic}}. Create anticipation and explain why this list matters to {{audience}}."),

            new TemplateSection(
                Name: "Item",
                Purpose: "Present each numbered item in the list",
                Type: SectionType.NumberedItem,
                SuggestedDuration: TimeSpan.FromSeconds(20),
                PromptTemplate: "Present item #{{itemNumber}} in the list of {{topic}}. Include a brief explanation of why it's important and a practical tip.",
                IsRepeatable: true,
                RepeatCountVariable: "count"),

            new TemplateSection(
                Name: "Recap",
                Purpose: "Summarize all items quickly",
                Type: SectionType.Recap,
                SuggestedDuration: TimeSpan.FromSeconds(15),
                PromptTemplate: "Provide a quick recap of all {{count}} items about {{topic}}. Reinforce the key takeaways."),

            new TemplateSection(
                Name: "Call to Action",
                Purpose: "Encourage engagement",
                Type: SectionType.CallToAction,
                SuggestedDuration: TimeSpan.FromSeconds(10),
                PromptTemplate: "End with a call to action. Ask viewers which item was their favorite and encourage them to share their own additions.")
        };

        var variables = new List<TemplateVariable>
        {
            new TemplateVariable(
                Name: "topic",
                DisplayName: "Topic",
                Type: VariableType.Text,
                DefaultValue: null,
                Placeholder: "e.g., Productivity Tips, Travel Destinations",
                IsRequired: true),
            new TemplateVariable(
                Name: "count",
                DisplayName: "Number of Items",
                Type: VariableType.Number,
                DefaultValue: "5",
                Placeholder: null,
                IsRequired: true,
                MinValue: 3,
                MaxValue: 15),
            new TemplateVariable(
                Name: "audience",
                DisplayName: "Target Audience",
                Type: VariableType.Text,
                DefaultValue: "viewers",
                Placeholder: "e.g., busy professionals, travelers",
                IsRequired: false)
        };

        var metadata = new TemplateMetadata(
            RecommendedAudiences: new[] { "General", "Enthusiasts", "Hobbyists" },
            RecommendedTones: new[] { "Energetic", "Casual", "Informative" },
            SupportedAspects: new[] { Aspect.Widescreen16x9, Aspect.Vertical9x16, Aspect.Square1x1 },
            MinDuration: TimeSpan.FromMinutes(1),
            MaxDuration: TimeSpan.FromMinutes(15),
            Tags: new[] { "listicle", "top-n", "tips", "entertainment", "viral" });

        return new VideoTemplate(
            Id: "listicle",
            Name: "Listicle (Top N List)",
            Description: "Hook → Numbered items → Recap → CTA. Great for top N lists, tips, and ranked content that's easy to consume.",
            Category: "Entertainment",
            Structure: new TemplateStructureSpec(sections, TimeSpan.FromMinutes(3), 7),
            Variables: variables,
            Thumbnail: new TemplateThumbnail("NumberList", "#FF9800"),
            Metadata: metadata);
    }

    /// <summary>
    /// Comparison template: Intro → Option A → Option B → Verdict
    /// Best for product reviews, versus content, and decision guides.
    /// </summary>
    private static VideoTemplate CreateComparisonTemplate()
    {
        var sections = new List<TemplateSection>
        {
            new TemplateSection(
                Name: "Introduction",
                Purpose: "Set up the comparison and why it matters",
                Type: SectionType.Introduction,
                SuggestedDuration: TimeSpan.FromSeconds(15),
                PromptTemplate: "Introduce the comparison between {{optionA}} and {{optionB}}. Explain why {{audience}} would want to compare these options."),

            new TemplateSection(
                Name: "Option A",
                Purpose: "Present the first option with pros and cons",
                Type: SectionType.OptionA,
                SuggestedDuration: TimeSpan.FromSeconds(45),
                PromptTemplate: "Present {{optionA}} in detail. Cover its key features, strengths, and weaknesses. Be balanced and objective."),

            new TemplateSection(
                Name: "Option B",
                Purpose: "Present the second option with pros and cons",
                Type: SectionType.OptionB,
                SuggestedDuration: TimeSpan.FromSeconds(45),
                PromptTemplate: "Present {{optionB}} in detail. Cover its key features, strengths, and weaknesses. Be balanced and objective."),

            new TemplateSection(
                Name: "Verdict",
                Purpose: "Give your recommendation",
                Type: SectionType.Verdict,
                SuggestedDuration: TimeSpan.FromSeconds(30),
                PromptTemplate: "Provide your verdict on {{optionA}} vs {{optionB}}. Who should choose which option? What are the deciding factors for {{audience}}?"),

            new TemplateSection(
                Name: "Call to Action",
                Purpose: "Wrap up and encourage engagement",
                Type: SectionType.CallToAction,
                SuggestedDuration: TimeSpan.FromSeconds(10),
                PromptTemplate: "End with a call to action. Ask viewers which option they prefer and why.")
        };

        var variables = new List<TemplateVariable>
        {
            new TemplateVariable(
                Name: "optionA",
                DisplayName: "Option A",
                Type: VariableType.Text,
                DefaultValue: null,
                Placeholder: "e.g., iPhone 15",
                IsRequired: true),
            new TemplateVariable(
                Name: "optionB",
                DisplayName: "Option B",
                Type: VariableType.Text,
                DefaultValue: null,
                Placeholder: "e.g., Samsung Galaxy S24",
                IsRequired: true),
            new TemplateVariable(
                Name: "audience",
                DisplayName: "Target Audience",
                Type: VariableType.Text,
                DefaultValue: "viewers",
                Placeholder: "e.g., tech enthusiasts, budget shoppers",
                IsRequired: false)
        };

        var metadata = new TemplateMetadata(
            RecommendedAudiences: new[] { "Shoppers", "Researchers", "Enthusiasts" },
            RecommendedTones: new[] { "Objective", "Professional", "Helpful" },
            SupportedAspects: new[] { Aspect.Widescreen16x9 },
            MinDuration: TimeSpan.FromMinutes(2),
            MaxDuration: TimeSpan.FromMinutes(15),
            Tags: new[] { "comparison", "versus", "review", "decision" });

        return new VideoTemplate(
            Id: "comparison",
            Name: "Comparison",
            Description: "Intro → Option A → Option B → Verdict. Perfect for product reviews, versus content, and helping viewers make decisions.",
            Category: "Reviews",
            Structure: new TemplateStructureSpec(sections, TimeSpan.FromMinutes(2.5), 5),
            Variables: variables,
            Thumbnail: new TemplateThumbnail("ScaleBalance", "#2196F3"),
            Metadata: metadata);
    }

    /// <summary>
    /// Story Time template: Hook → Setup → Rising Action → Climax → Resolution → Lesson
    /// Best for narrative content, personal stories, and case studies.
    /// </summary>
    private static VideoTemplate CreateStoryTimeTemplate()
    {
        var sections = new List<TemplateSection>
        {
            new TemplateSection(
                Name: "Hook",
                Purpose: "Tease the story to create curiosity",
                Type: SectionType.Hook,
                SuggestedDuration: TimeSpan.FromSeconds(10),
                PromptTemplate: "Write a compelling hook that teases the story about {{topic}}. Create curiosity without giving away the ending."),

            new TemplateSection(
                Name: "Setup",
                Purpose: "Introduce the characters, setting, and context",
                Type: SectionType.Setup,
                SuggestedDuration: TimeSpan.FromSeconds(30),
                PromptTemplate: "Set the scene for the story about {{topic}}. Introduce the key characters or elements and establish the initial situation."),

            new TemplateSection(
                Name: "Rising Action",
                Purpose: "Build tension and develop the story",
                Type: SectionType.RisingAction,
                SuggestedDuration: TimeSpan.FromSeconds(45),
                PromptTemplate: "Develop the story about {{topic}}. Build tension, introduce challenges, and keep the audience engaged with escalating stakes."),

            new TemplateSection(
                Name: "Climax",
                Purpose: "The turning point or most intense moment",
                Type: SectionType.Climax,
                SuggestedDuration: TimeSpan.FromSeconds(30),
                PromptTemplate: "Present the climax of the story about {{topic}}. This is the most intense or pivotal moment where everything changes."),

            new TemplateSection(
                Name: "Resolution",
                Purpose: "Show how things turned out",
                Type: SectionType.Resolution,
                SuggestedDuration: TimeSpan.FromSeconds(20),
                PromptTemplate: "Resolve the story about {{topic}}. Show how things turned out and tie up loose ends."),

            new TemplateSection(
                Name: "Lesson",
                Purpose: "Share the key takeaway or moral",
                Type: SectionType.Lesson,
                SuggestedDuration: TimeSpan.FromSeconds(15),
                PromptTemplate: "Share the lesson or moral from the story about {{topic}}. What should viewers take away from this experience?")
        };

        var variables = new List<TemplateVariable>
        {
            new TemplateVariable(
                Name: "topic",
                DisplayName: "Story Topic",
                Type: VariableType.LongText,
                DefaultValue: null,
                Placeholder: "e.g., How I learned to code in 30 days, The time I met my hero",
                IsRequired: true)
        };

        var metadata = new TemplateMetadata(
            RecommendedAudiences: new[] { "General", "Story lovers", "Casual viewers" },
            RecommendedTones: new[] { "Engaging", "Personal", "Dramatic" },
            SupportedAspects: new[] { Aspect.Widescreen16x9, Aspect.Vertical9x16 },
            MinDuration: TimeSpan.FromMinutes(2),
            MaxDuration: TimeSpan.FromMinutes(15),
            Tags: new[] { "storytelling", "narrative", "personal", "entertainment" });

        return new VideoTemplate(
            Id: "story-time",
            Name: "Story Time",
            Description: "Hook → Setup → Rising Action → Climax → Resolution → Lesson. Ideal for personal stories, case studies, and narrative content.",
            Category: "Entertainment",
            Structure: new TemplateStructureSpec(sections, TimeSpan.FromMinutes(2.5), 6),
            Variables: variables,
            Thumbnail: new TemplateThumbnail("BookOpen", "#9C27B0"),
            Metadata: metadata);
    }

    /// <summary>
    /// Tutorial template: Overview → Prerequisites → Steps → Common Mistakes → Summary
    /// Best for how-to content, guides, and instructional videos.
    /// </summary>
    private static VideoTemplate CreateTutorialTemplate()
    {
        var sections = new List<TemplateSection>
        {
            new TemplateSection(
                Name: "Overview",
                Purpose: "Explain what viewers will learn",
                Type: SectionType.Overview,
                SuggestedDuration: TimeSpan.FromSeconds(15),
                PromptTemplate: "Introduce the tutorial on {{topic}}. Explain what {{audience}} will learn and why it's valuable."),

            new TemplateSection(
                Name: "Prerequisites",
                Purpose: "List what viewers need before starting",
                Type: SectionType.Prerequisites,
                SuggestedDuration: TimeSpan.FromSeconds(20),
                PromptTemplate: "List the prerequisites for this tutorial on {{topic}}. What tools, knowledge, or materials does {{audience}} need?",
                IsOptional: true),

            new TemplateSection(
                Name: "Step",
                Purpose: "Walk through each step of the process",
                Type: SectionType.Step,
                SuggestedDuration: TimeSpan.FromSeconds(30),
                PromptTemplate: "Explain step #{{stepNumber}} of {{topic}}. Be clear and specific so {{audience}} can follow along easily.",
                IsRepeatable: true,
                RepeatCountVariable: "stepCount"),

            new TemplateSection(
                Name: "Common Mistakes",
                Purpose: "Warn about common pitfalls",
                Type: SectionType.CommonMistakes,
                SuggestedDuration: TimeSpan.FromSeconds(20),
                PromptTemplate: "Highlight common mistakes to avoid when doing {{topic}}. Help {{audience}} succeed on their first try.",
                IsOptional: true),

            new TemplateSection(
                Name: "Summary",
                Purpose: "Recap and encourage practice",
                Type: SectionType.Summary,
                SuggestedDuration: TimeSpan.FromSeconds(15),
                PromptTemplate: "Summarize the key steps for {{topic}}. Encourage {{audience}} to practice and offer additional resources.")
        };

        var variables = new List<TemplateVariable>
        {
            new TemplateVariable(
                Name: "topic",
                DisplayName: "Tutorial Topic",
                Type: VariableType.Text,
                DefaultValue: null,
                Placeholder: "e.g., Setting up a React project",
                IsRequired: true),
            new TemplateVariable(
                Name: "stepCount",
                DisplayName: "Number of Steps",
                Type: VariableType.Number,
                DefaultValue: "5",
                Placeholder: null,
                IsRequired: true,
                MinValue: 3,
                MaxValue: 12),
            new TemplateVariable(
                Name: "audience",
                DisplayName: "Target Audience",
                Type: VariableType.Text,
                DefaultValue: "beginners",
                Placeholder: "e.g., developers, students",
                IsRequired: false)
        };

        var metadata = new TemplateMetadata(
            RecommendedAudiences: new[] { "Beginners", "Learners", "Students" },
            RecommendedTones: new[] { "Instructional", "Patient", "Clear" },
            SupportedAspects: new[] { Aspect.Widescreen16x9 },
            MinDuration: TimeSpan.FromMinutes(2),
            MaxDuration: TimeSpan.FromMinutes(20),
            Tags: new[] { "tutorial", "how-to", "guide", "educational", "step-by-step" });

        return new VideoTemplate(
            Id: "tutorial",
            Name: "Tutorial",
            Description: "Overview → Prerequisites → Steps → Common Mistakes → Summary. Perfect for how-to guides and instructional content.",
            Category: "Educational",
            Structure: new TemplateStructureSpec(sections, TimeSpan.FromMinutes(4), 8),
            Variables: variables,
            Thumbnail: new TemplateThumbnail("GraduationCap", "#00BCD4"),
            Metadata: metadata);
    }

    /// <summary>
    /// Product Showcase template: Attention → Interest → Desire → Action (AIDA)
    /// Best for marketing, product demos, and promotional content.
    /// </summary>
    private static VideoTemplate CreateProductShowcaseTemplate()
    {
        var sections = new List<TemplateSection>
        {
            new TemplateSection(
                Name: "Attention",
                Purpose: "Grab attention with a bold statement or visual",
                Type: SectionType.Attention,
                SuggestedDuration: TimeSpan.FromSeconds(10),
                PromptTemplate: "Grab attention for {{product}}. Use a bold statement, striking visual description, or surprising fact that stops {{audience}} in their tracks."),

            new TemplateSection(
                Name: "Interest",
                Purpose: "Build interest by addressing pain points",
                Type: SectionType.Interest,
                SuggestedDuration: TimeSpan.FromSeconds(25),
                PromptTemplate: "Build interest in {{product}} by addressing the problems {{audience}} faces. Show you understand their needs and frustrations."),

            new TemplateSection(
                Name: "Desire",
                Purpose: "Create desire by showcasing benefits",
                Type: SectionType.Desire,
                SuggestedDuration: TimeSpan.FromSeconds(40),
                PromptTemplate: "Create desire for {{product}} by showcasing its key features and benefits. Paint a picture of how {{audience}}'s life will improve."),

            new TemplateSection(
                Name: "Action",
                Purpose: "Drive action with a clear CTA",
                Type: SectionType.Action,
                SuggestedDuration: TimeSpan.FromSeconds(15),
                PromptTemplate: "Drive action for {{product}}. Provide a clear, compelling call to action. Include urgency if appropriate.")
        };

        var variables = new List<TemplateVariable>
        {
            new TemplateVariable(
                Name: "product",
                DisplayName: "Product/Service Name",
                Type: VariableType.Text,
                DefaultValue: null,
                Placeholder: "e.g., TaskMaster Pro App",
                IsRequired: true),
            new TemplateVariable(
                Name: "audience",
                DisplayName: "Target Audience",
                Type: VariableType.Text,
                DefaultValue: "potential customers",
                Placeholder: "e.g., busy professionals, small business owners",
                IsRequired: false),
            new TemplateVariable(
                Name: "keyBenefit",
                DisplayName: "Key Benefit",
                Type: VariableType.Text,
                DefaultValue: null,
                Placeholder: "e.g., Save 10 hours per week",
                IsRequired: false)
        };

        var metadata = new TemplateMetadata(
            RecommendedAudiences: new[] { "Customers", "Prospects", "Buyers" },
            RecommendedTones: new[] { "Persuasive", "Energetic", "Professional" },
            SupportedAspects: new[] { Aspect.Widescreen16x9, Aspect.Vertical9x16, Aspect.Square1x1 },
            MinDuration: TimeSpan.FromSeconds(30),
            MaxDuration: TimeSpan.FromMinutes(5),
            Tags: new[] { "marketing", "product", "promotional", "aida", "sales" });

        return new VideoTemplate(
            Id: "product-showcase",
            Name: "Product Showcase",
            Description: "Attention → Interest → Desire → Action (AIDA). Ideal for marketing videos, product demos, and promotional content.",
            Category: "Marketing",
            Structure: new TemplateStructureSpec(sections, TimeSpan.FromSeconds(90), 4),
            Variables: variables,
            Thumbnail: new TemplateThumbnail("ShoppingBag", "#E91E63"),
            Metadata: metadata);
    }
}
