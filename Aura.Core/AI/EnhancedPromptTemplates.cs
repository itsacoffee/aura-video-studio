using System.Text;
using System.Text.Json;
using Aura.Core.Models;
using Aura.Core.Models.Audience;
using Aura.Core.Models.Visual;
using PacingEnum = Aura.Core.Models.Pacing;
using DensityEnum = Aura.Core.Models.Density;

namespace Aura.Core.AI;

/// <summary>
/// Enhanced prompt templates for AI-guided video creation that produces high-quality,
/// natural-sounding content that doesn't feel AI-generated or like "slop".
/// Supports both static (default) and dynamic (ML-optimized) modes.
/// </summary>
public static class EnhancedPromptTemplates
{
    /// <summary>
    /// Prompt mode for template usage
    /// </summary>
    public enum PromptMode
    {
        /// <summary>
        /// Use static, predefined templates
        /// </summary>
        Static,
        
        /// <summary>
        /// Allow dynamic ML-driven enhancements
        /// </summary>
        Dynamic
    }

    /// <summary>
    /// Current prompt mode (default: Static)
    /// </summary>
    public static PromptMode CurrentMode { get; set; } = PromptMode.Static;

    /// <summary>
    /// System prompt for video script generation with emphasis on quality and naturalness
    /// </summary>
    public static string GetSystemPromptForScriptGeneration()
    {
        return @"You are an expert video creator specializing in social media content that performs exceptionally well. Your expertise includes:

CORE PRINCIPLES:
- Create content that feels authentic, human, and engaging - never robotic or obviously AI-generated
- Focus on storytelling and emotional connection with the audience
- Use natural language patterns, varied sentence structures, and conversational flow
- Include subtle personality and relatable examples
- Build genuine value and insight rather than surface-level content

CONTENT QUALITY STANDARDS:
- Hook viewers in the first 3-5 seconds with intrigue, surprise, or immediate value
- Use the AIDA framework (Attention, Interest, Desire, Action) naturally
- Balance information density with entertainment value
- Include specific, verifiable facts and examples (when appropriate)
- Use pattern interrupts to maintain engagement
- Build to clear payoffs and actionable takeaways
- End with memorable conclusions and clear next steps

PACING & RHYTHM:
- Vary sentence length for natural flow (mix short punchy statements with longer explanatory ones)
- Use transitions that feel organic, not formulaic
- Build momentum through the video with strategic peaks and valleys
- Time reveals and key points for maximum impact

VOICE & TONE:
- Write for the ear, not the eye (how it sounds when spoken aloud)
- Avoid generic phrases like 'in today's video' or 'don't forget to subscribe'
- Use active voice and vivid language
- Match tone to content type (educational should still be engaging, entertainment should have substance)

VISUAL SYNERGY:
- Write with visual storytelling in mind - describe scenes that can be illustrated
- Include natural moments for B-roll, graphics, or demonstrations
- Mark key visual moments in the script structure

AVOID AI DETECTION FLAGS:
- No overly formal or academic language unless the content demands it
- No repetitive sentence structures or formulaic patterns
- No generic lists without context or unique insights
- No excessive adjectives or marketing speak
- No 'AI voice' patterns (e.g., constant rhetorical questions, overuse of 'delve into')";
    }

    /// <summary>
    /// Build a detailed user prompt for script generation with content awareness and optional audience profile
    /// </summary>
    public static string BuildScriptGenerationPrompt(Brief brief, PlanSpec spec, string? additionalContext = null, AudienceProfile? audienceProfile = null)
    {
        var sb = new StringBuilder();

        // Topic and core details
        sb.AppendLine($"CREATE A VIDEO SCRIPT:");
        sb.AppendLine($"Topic: {brief.Topic}");
        sb.AppendLine();

        // Target specifications
        sb.AppendLine($"TARGET SPECIFICATIONS:");
        sb.AppendLine($"- Duration: {spec.TargetDuration.TotalMinutes:F1} minutes ({EstimateWordCount(spec):N0} words)");
        sb.AppendLine($"- Tone: {brief.Tone}");
        sb.AppendLine($"- Pacing: {GetPacingDescription(spec.Pacing)}");
        sb.AppendLine($"- Content Density: {GetDensityDescription(spec.Density)}");
        sb.AppendLine($"- Language: {brief.Language}");
        
        if (!string.IsNullOrEmpty(brief.Audience))
        {
            sb.AppendLine($"- Target Audience: {brief.Audience}");
        }

        if (!string.IsNullOrEmpty(brief.Goal))
        {
            sb.AppendLine($"- Content Goal: {brief.Goal}");
        }

        sb.AppendLine();

        // Add detailed audience profile context if provided
        if (audienceProfile != null)
        {
            sb.AppendLine($"DETAILED AUDIENCE PROFILE:");
            sb.AppendLine($"- Education Level: {audienceProfile.EducationLevel}");
            sb.AppendLine($"- Expertise Level: {audienceProfile.ExpertiseLevel}");
            sb.AppendLine($"- Age Range: {audienceProfile.AgeRange}");
            sb.AppendLine($"- Formality Preference: {audienceProfile.FormalityLevel}");
            sb.AppendLine($"- Energy Preference: {audienceProfile.EnergyLevel}");
            sb.AppendLine($"- Cognitive Load Capacity: {audienceProfile.CognitiveLoadCapacity}/100");
            sb.AppendLine($"- Prefers Technical Language: {audienceProfile.PrefersTechnicalLanguage}");
            
            if (!string.IsNullOrEmpty(audienceProfile.ProfessionalDomain))
            {
                sb.AppendLine($"- Professional Domain: {audienceProfile.ProfessionalDomain}");
            }
            
            if (!string.IsNullOrEmpty(audienceProfile.GeographicRegion))
            {
                sb.AppendLine($"- Geographic Region: {audienceProfile.GeographicRegion}");
            }
            
            if (audienceProfile.Interests.Count > 0)
            {
                sb.AppendLine($"- Key Interests: {string.Join(", ", audienceProfile.Interests)}");
            }

            sb.AppendLine();
            sb.AppendLine("AUDIENCE-AWARE CONTENT GUIDELINES:");
            sb.AppendLine($"- Match vocabulary to {audienceProfile.EducationLevel} education level");
            sb.AppendLine($"- Adjust complexity for {audienceProfile.ExpertiseLevel} expertise");
            sb.AppendLine($"- Use {audienceProfile.FormalityLevel} formality level");
            sb.AppendLine($"- Maintain {audienceProfile.EnergyLevel} energy throughout");
            
            if (!string.IsNullOrEmpty(audienceProfile.ProfessionalDomain))
            {
                sb.AppendLine($"- Include {audienceProfile.ProfessionalDomain}-specific examples and analogies");
            }
            
            if (!string.IsNullOrEmpty(audienceProfile.GeographicRegion))
            {
                sb.AppendLine($"- Use culturally appropriate references for {audienceProfile.GeographicRegion}");
            }
            
            sb.AppendLine();
        }

        // Content creation guidelines specific to this video
        sb.AppendLine($"SPECIFIC GUIDELINES FOR THIS VIDEO:");
        sb.AppendLine(GetToneSpecificGuidelines(brief.Tone));
        sb.AppendLine();

        // Add context if provided
        if (!string.IsNullOrEmpty(additionalContext))
        {
            sb.AppendLine($"ADDITIONAL CONTEXT:");
            sb.AppendLine(additionalContext);
            sb.AppendLine();
        }

        // Structure requirements
        sb.AppendLine($"REQUIRED STRUCTURE:");
        sb.AppendLine($"# [Compelling, Specific Title - Not Generic]");
        sb.AppendLine();
        sb.AppendLine($"## Hook (First 3-5 seconds)");
        sb.AppendLine($"[Immediately grab attention with intrigue, surprise, or promised value. Be specific, not vague.]");
        sb.AppendLine();
        sb.AppendLine($"## Introduction (Next 10-15 seconds)");
        sb.AppendLine($"[Build context and preview the journey. Create anticipation.]");
        sb.AppendLine();
        sb.AppendLine($"## [3-5 Content Sections with Descriptive Headers]");
        sb.AppendLine($"[Each section should:");
        sb.AppendLine($" - Have a clear purpose and payoff");
        sb.AppendLine($" - Include specific examples or demonstrations");
        sb.AppendLine($" - Maintain momentum toward the conclusion");
        sb.AppendLine($" - Suggest visual moments with [VISUAL: description]]");
        sb.AppendLine();
        sb.AppendLine($"## Conclusion");
        sb.AppendLine($"[Powerful summary and clear call-to-action. Leave viewers with lasting value.]");
        sb.AppendLine();

        // Quality requirements
        sb.AppendLine($"QUALITY REQUIREMENTS:");
        sb.AppendLine($"- Every sentence must serve the story and add value");
        sb.AppendLine($"- Use specific, memorable examples and analogies");
        sb.AppendLine($"- Vary sentence length and structure naturally");
        sb.AppendLine($"- Include 2-3 pattern interrupts or surprising turns");
        sb.AppendLine($"- Mark key visual moments with [VISUAL: brief description]");
        sb.AppendLine($"- Ensure the script sounds natural when read aloud");
        sb.AppendLine($"- Build emotional resonance appropriate to the topic");

        return sb.ToString();
    }

    /// <summary>
    /// Get tone-specific creative guidelines
    /// </summary>
    private static string GetToneSpecificGuidelines(string tone)
    {
        return tone.ToLowerInvariant() switch
        {
            "informative" or "educational" => @"- Lead with curiosity and discovery, not dry facts
- Use the 'curiosity gap' technique to maintain engagement
- Include surprising facts or counterintuitive insights
- Make complex topics accessible through clear analogies
- Build understanding progressively, not all at once",

            "narrative" or "storytelling" => @"- Use story structure with clear setup, conflict, and resolution
- Include sensory details and emotional beats
- Create characters or perspectives the audience can connect with
- Use 'show don't tell' principles where possible
- Build suspense and anticipation naturally",

            "humorous" or "entertaining" => @"- Use wit and clever observations, not forced jokes
- Include relatable situations and reactions
- Timing is crucial - set up punchlines properly
- Balance humor with substance
- Use callbacks and running gags if appropriate",

            "dramatic" => @"- Build tension and stakes progressively
- Use vivid, evocative language
- Include emotional peaks and valleys
- Create investment in outcomes
- Use the power of silence and pauses (mark with [PAUSE])",

            "conversational" => @"- Write as if talking to a friend over coffee
- Use 'you' and 'we' to create connection
- Include personal touches and relatable moments
- Ask engaging questions (then answer them)
- Keep energy up with natural enthusiasm",

            "professional" => @"- Maintain authority while remaining accessible
- Use industry-specific insights and expertise
- Include data and examples that demonstrate credibility
- Balance formality with engagement
- Provide actionable, professional-grade insights",

            _ => @"- Focus on authentic, engaging delivery
- Use natural language patterns
- Include specific, memorable content
- Maintain viewer interest throughout
- Provide clear value and takeaways"
        };
    }

    /// <summary>
    /// Get pacing description for better AI understanding
    /// </summary>
    private static string GetPacingDescription(PacingEnum pacing)
    {
        return pacing switch
        {
            PacingEnum.Chill => "Relaxed and contemplative (~120-140 WPM), with longer pauses for reflection",
            PacingEnum.Conversational => "Natural conversation pace (~150-165 WPM), comfortable and engaging",
            PacingEnum.Fast => "Energetic and dynamic (~180-200 WPM), keeping momentum high",
            _ => "Standard conversational pace"
        };
    }

    /// <summary>
    /// Get density description for better AI understanding
    /// </summary>
    private static string GetDensityDescription(DensityEnum density)
    {
        return density switch
        {
            DensityEnum.Sparse => "Light and accessible, with plenty of breathing room between concepts",
            DensityEnum.Balanced => "Good balance of depth and accessibility, moderately detailed",
            DensityEnum.Dense => "Information-rich and detailed, for engaged audiences seeking depth",
            _ => "Balanced information density"
        };
    }

    /// <summary>
    /// Estimate word count based on duration and pacing
    /// </summary>
    private static int EstimateWordCount(PlanSpec spec)
    {
        int baseWpm = spec.Pacing switch
        {
            PacingEnum.Chill => 130,
            PacingEnum.Conversational => 157,
            PacingEnum.Fast => 190,
            _ => 157
        };

        double densityMultiplier = spec.Density switch
        {
            DensityEnum.Sparse => 0.85,
            DensityEnum.Balanced => 1.0,
            DensityEnum.Dense => 1.15,
            _ => 1.0
        };

        return (int)(spec.TargetDuration.TotalMinutes * baseWpm * densityMultiplier);
    }

    /// <summary>
    /// System prompt for visual asset selection with emphasis on coherence and quality
    /// </summary>
    public static string GetSystemPromptForVisualSelection()
    {
        return @"You are an expert visual director for video content. Your role is to select or generate visuals that:

VISUAL QUALITY PRINCIPLES:
- Match the narrative and emotional tone of each scene
- Create visual coherence and consistency throughout the video
- Avoid generic stock footage clichés
- Use visuals that enhance understanding and engagement
- Create visual variety while maintaining a cohesive style

SELECTION CRITERIA:
- Relevance: Direct connection to the script content
- Quality: Professional, well-composed, properly lit
- Emotion: Matches the emotional tone and energy level
- Originality: Avoid overused or cliché imagery
- Coherence: Fits with the overall visual style of the video

VISUAL STORYTELLING:
- Use close-ups for emotional moments
- Use wide shots for context and scope
- Use motion and action to maintain engagement
- Consider color psychology and mood
- Think about transitions between shots

Provide specific, actionable search terms or generation prompts that will yield high-quality, relevant visuals.";
    }

    /// <summary>
    /// Build a visual selection prompt with content awareness
    /// </summary>
    public static string BuildVisualSelectionPrompt(string sceneHeading, string sceneContent, string tone, int sceneIndex)
    {
        return BuildVisualSelectionPrompt(sceneHeading, sceneContent, tone, sceneIndex, null);
    }

    /// <summary>
    /// Build a visual selection prompt with visual-text synchronization metadata
    /// Enhanced version that includes timing markers, cognitive load context, and synchronization requirements
    /// </summary>
    public static string BuildVisualSelectionPrompt(
        string sceneHeading, 
        string sceneContent, 
        string tone, 
        int sceneIndex,
        NarrationSegment? syncData)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"SELECT VISUALS FOR SCENE {sceneIndex + 1}:");
        sb.AppendLine($"Scene Title: {sceneHeading}");
        sb.AppendLine();
        sb.AppendLine($"Script Content:");
        sb.AppendLine(sceneContent);
        sb.AppendLine();
        sb.AppendLine($"Tone: {tone}");
        sb.AppendLine();

        if (syncData != null)
        {
            sb.AppendLine($"VISUAL-TEXT SYNCHRONIZATION DATA:");
            sb.AppendLine($"- Narration Complexity: {syncData.NarrationComplexity:F1}/100");
            sb.AppendLine($"- Target Visual Complexity: {(100 - syncData.NarrationComplexity):F1}/100 (inversely balanced)");
            sb.AppendLine($"- Cognitive Load Score: {syncData.CognitiveLoadScore:F1}/100 (target: <75)");
            sb.AppendLine($"- Narration Rate: {syncData.NarrationRate:F0} WPM");
            sb.AppendLine($"- Information Density: {syncData.InformationDensity}");
            sb.AppendLine($"- Timing: {syncData.StartTime:hh\\:mm\\:ss\\.ff} - {syncData.EndTime:hh\\:mm\\:ss\\.ff} (Duration: {syncData.Duration.TotalSeconds:F1}s)");
            
            if (syncData.KeyConcepts.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"KEY CONCEPTS REQUIRING VISUAL SUPPORT:");
                foreach (var concept in syncData.KeyConcepts)
                {
                    sb.AppendLine($"  • {concept.Text} ({concept.Type}) - Importance: {concept.Importance:F0}/100");
                    sb.AppendLine($"    Time: +{concept.TimeOffset.TotalSeconds:F1}s");
                    sb.AppendLine($"    Visualization: {concept.SuggestedVisualization}");
                    if (concept.RequiresMetaphor)
                        sb.AppendLine($"    Note: Abstract concept - requires concrete visual metaphor");
                    if (concept.SuggestsMotion)
                        sb.AppendLine($"    Note: Action verb - suggests motion/animation");
                }
            }

            if (syncData.VisualRecommendations.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"RECOMMENDED VISUAL TREATMENTS:");
                foreach (var rec in syncData.VisualRecommendations)
                {
                    sb.AppendLine($"  • {rec.ContentType}: {rec.Description}");
                    sb.AppendLine($"    Timing: +{rec.StartTime.TotalSeconds:F1}s for {rec.Duration.TotalSeconds:F1}s");
                    sb.AppendLine($"    Complexity: {rec.VisualComplexity:F0}/100");
                    sb.AppendLine($"    Priority: {rec.Priority:F0}/100");
                    
                    if (rec.BRollKeywords.Count > 0)
                    {
                        sb.AppendLine($"    B-Roll Keywords: {string.Join(", ", rec.BRollKeywords)}");
                    }

                    if (rec.Metadata != null)
                    {
                        sb.AppendLine($"    Visual Metadata:");
                        sb.AppendLine($"      - Camera: {rec.Metadata.CameraAngle}, {rec.Metadata.ShotType}");
                        sb.AppendLine($"      - Composition: {rec.Metadata.CompositionRule}");
                        sb.AppendLine($"      - Focus Point: {rec.Metadata.FocusPoint}");
                        sb.AppendLine($"      - Lighting: {rec.Metadata.LightingMood}");
                        if (rec.Metadata.ColorScheme.Count > 0)
                        {
                            sb.AppendLine($"      - Color Scheme: {string.Join(", ", rec.Metadata.ColorScheme)}");
                        }
                    }

                    sb.AppendLine($"    Reasoning: {rec.Reasoning}");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"COGNITIVE LOAD BALANCING RULES:");
            sb.AppendLine($"- High narration complexity ({syncData.NarrationComplexity:F0}) → Use SIMPLE, CLEAN visuals");
            sb.AppendLine($"- Low narration complexity → Can use DETAILED, BUSY visuals");
            sb.AppendLine($"- Fast narration rate ({syncData.NarrationRate:F0} WPM) → FEWER visual changes");
            sb.AppendLine($"- Slow narration rate → MORE visual variety");
            sb.AppendLine($"- Ensure visuals SUPPORT, not CONTRADICT narration");
            sb.AppendLine();
        }

        sb.AppendLine($"Provide 3-5 specific search terms or image generation prompts that would yield visuals for this scene.");
        sb.AppendLine($"Each should be:");
        sb.AppendLine($"- Specific and descriptive (not generic stock footage)");
        sb.AppendLine($"- Professional quality");
        sb.AppendLine($"- Emotionally appropriate to the tone");
        sb.AppendLine($"- Visually interesting and aligned with narration");
        
        if (syncData != null)
        {
            sb.AppendLine($"- Complexity level: {(100 - syncData.NarrationComplexity):F0}/100");
            sb.AppendLine($"- Include timing precision markers (±0.5 second accuracy)");
        }
        
        sb.AppendLine();
        
        if (syncData != null)
        {
            sb.AppendLine($"FORMAT: Return as structured JSON:");
            sb.AppendLine($"{{");
            sb.AppendLine($"  \"visuals\": [");
            sb.AppendLine($"    {{");
            sb.AppendLine($"      \"description\": \"Detailed visual description\",");
            sb.AppendLine($"      \"searchTerms\": [\"specific\", \"contextual\", \"keywords\"],");
            sb.AppendLine($"      \"timing\": {{ \"start\": 0.0, \"duration\": 3.0 }},");
            sb.AppendLine($"      \"complexity\": 30,");
            sb.AppendLine($"      \"metadata\": {{");
            sb.AppendLine($"        \"cameraAngle\": \"eye-level\",");
            sb.AppendLine($"        \"shotType\": \"medium-shot\",");
            sb.AppendLine($"        \"composition\": \"rule-of-thirds\",");
            sb.AppendLine($"        \"focusPoint\": \"subject\",");
            sb.AppendLine($"        \"colorScheme\": [\"#hex1\", \"#hex2\"],");
            sb.AppendLine($"        \"lighting\": \"soft\"");
            sb.AppendLine($"      }}");
            sb.AppendLine($"    }}");
            sb.AppendLine($"  ]");
            sb.AppendLine($"}}");
        }
        else
        {
            sb.AppendLine($"Format: One search term per line, starting with 'VISUAL:'");
        }

        return sb.ToString();
    }

    /// <summary>
    /// System prompt for content quality validation
    /// </summary>
    public static string GetSystemPromptForQualityValidation()
    {
        return @"You are a quality control expert for AI-generated video content. Your role is to identify any signs that content feels:
- AI-generated or robotic
- Generic or 'sloppy'
- Lacking in substance or authenticity
- Using cliché patterns or phrases
- Poorly paced or structured

Provide specific, actionable feedback for improvement, focusing on making content feel more human, engaging, and valuable.";
    }

    /// <summary>
    /// Build a quality validation prompt
    /// </summary>
    public static string BuildQualityValidationPrompt(string script, string contentType)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"ANALYZE THIS {contentType.ToUpper()} VIDEO SCRIPT FOR QUALITY:");
        sb.AppendLine();
        sb.AppendLine(script);
        sb.AppendLine();
        sb.AppendLine($"EVALUATION CRITERIA:");
        sb.AppendLine($"1. Authenticity: Does it feel human-written and genuine?");
        sb.AppendLine($"2. Engagement: Will it hold viewer attention throughout?");
        sb.AppendLine($"3. Value: Does it provide real insight or entertainment?");
        sb.AppendLine($"4. Pacing: Is the rhythm and flow natural?");
        sb.AppendLine($"5. Originality: Does it avoid clichés and generic patterns?");
        sb.AppendLine();
        sb.AppendLine($"Provide:");
        sb.AppendLine($"- Overall quality score (0-100)");
        sb.AppendLine($"- Specific issues found (if any)");
        sb.AppendLine($"- Concrete suggestions for improvement");
        sb.AppendLine($"- What works well that should be preserved");

        return sb.ToString();
    }

    /// <summary>
    /// Augment system prompt with tone profile constraints
    /// </summary>
    public static string AugmentSystemPromptWithTone(string baseSystemPrompt, Models.Quality.ToneProfile toneProfile)
    {
        var sb = new StringBuilder();
        sb.AppendLine(baseSystemPrompt);
        sb.AppendLine();
        sb.AppendLine("MANDATORY TONE CONSTRAINTS:");
        sb.AppendLine($"- Vocabulary Level: {toneProfile.VocabularyLevel} (strictly maintain this complexity level)");
        sb.AppendLine($"- Formality: {toneProfile.Formality} (all language must match this formality)");
        sb.AppendLine($"- Humor Style: {toneProfile.Humor} (apply consistently throughout)");
        sb.AppendLine($"- Energy Level: {toneProfile.Energy} (pace and intensity must match)");
        sb.AppendLine($"- Perspective: {toneProfile.Perspective} (maintain consistent point of view)");
        sb.AppendLine();
        sb.AppendLine("TONE GUIDELINES:");
        sb.AppendLine(toneProfile.Guidelines);
        sb.AppendLine();
        
        if (toneProfile.ExamplePhrases.Length > 0)
        {
            sb.AppendLine("EXAMPLE PHRASES THAT MATCH THIS TONE:");
            foreach (var phrase in toneProfile.ExamplePhrases)
            {
                sb.AppendLine($"  ✓ \"{phrase}\"");
            }
            sb.AppendLine();
        }

        if (toneProfile.PhrasesToAvoid.Length > 0)
        {
            sb.AppendLine("PHRASES TO AVOID:");
            foreach (var phrase in toneProfile.PhrasesToAvoid)
            {
                sb.AppendLine($"  ✗ \"{phrase}\"");
            }
            sb.AppendLine();
        }

        sb.AppendLine("TONE CONSISTENCY REQUIREMENT:");
        sb.AppendLine("Every sentence, word choice, and stylistic element must align with these tone constraints.");
        sb.AppendLine("Violations will be flagged and may require rewrites.");

        return sb.ToString();
    }

    /// <summary>
    /// Build script generation prompt with enhanced tone profile integration
    /// </summary>
    public static string BuildScriptGenerationPromptWithTone(
        Brief brief, 
        PlanSpec spec, 
        Models.Quality.ToneProfile toneProfile,
        string? additionalContext = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"CREATE A VIDEO SCRIPT:");
        sb.AppendLine($"Topic: {brief.Topic}");
        sb.AppendLine();

        sb.AppendLine($"TARGET SPECIFICATIONS:");
        sb.AppendLine($"- Duration: {spec.TargetDuration.TotalMinutes:F1} minutes ({EstimateWordCount(spec):N0} words)");
        sb.AppendLine($"- Tone: {brief.Tone}");
        sb.AppendLine($"- Pacing: {GetPacingDescription(spec.Pacing)}");
        sb.AppendLine($"- Content Density: {GetDensityDescription(spec.Density)}");
        sb.AppendLine($"- Language: {brief.Language}");
        
        if (!string.IsNullOrEmpty(brief.Audience))
        {
            sb.AppendLine($"- Target Audience: {brief.Audience}");
        }

        if (!string.IsNullOrEmpty(brief.Goal))
        {
            sb.AppendLine($"- Content Goal: {brief.Goal}");
        }

        sb.AppendLine();

        sb.AppendLine("TONE PROFILE (STRICT REQUIREMENTS):");
        sb.AppendLine($"- Vocabulary Level: {toneProfile.VocabularyLevel}");
        sb.AppendLine($"- Formality: {toneProfile.Formality}");
        sb.AppendLine($"- Humor: {toneProfile.Humor}");
        sb.AppendLine($"- Energy: {toneProfile.Energy}");
        sb.AppendLine($"- Perspective: {toneProfile.Perspective}");
        sb.AppendLine($"- Target WPM: {toneProfile.TargetWordsPerMinute}");
        sb.AppendLine();

        sb.AppendLine("TONE GUIDELINES:");
        sb.AppendLine(toneProfile.Guidelines);
        sb.AppendLine();

        if (toneProfile.ExamplePhrases.Length > 0)
        {
            sb.AppendLine("USE LANGUAGE SIMILAR TO:");
            foreach (var phrase in toneProfile.ExamplePhrases.Take(5))
            {
                sb.AppendLine($"  • \"{phrase}\"");
            }
            sb.AppendLine();
        }

        if (toneProfile.PhrasesToAvoid.Length > 0)
        {
            sb.AppendLine("AVOID LANGUAGE LIKE:");
            foreach (var phrase in toneProfile.PhrasesToAvoid.Take(5))
            {
                sb.AppendLine($"  • \"{phrase}\"");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"SPECIFIC GUIDELINES FOR THIS VIDEO:");
        sb.AppendLine(GetToneSpecificGuidelines(brief.Tone));
        sb.AppendLine();

        if (!string.IsNullOrEmpty(additionalContext))
        {
            sb.AppendLine($"ADDITIONAL CONTEXT:");
            sb.AppendLine(additionalContext);
            sb.AppendLine();
        }

        sb.AppendLine($"REQUIRED STRUCTURE:");
        sb.AppendLine($"# [Compelling, Specific Title - Not Generic]");
        sb.AppendLine();
        sb.AppendLine($"## Hook (First 3-5 seconds)");
        sb.AppendLine($"[Immediately grab attention with intrigue, surprise, or promised value. Be specific, not vague.]");
        sb.AppendLine();
        sb.AppendLine($"## Introduction (Next 10-15 seconds)");
        sb.AppendLine($"[Build context and preview the journey. Create anticipation.]");
        sb.AppendLine();
        sb.AppendLine($"## [3-5 Content Sections with Descriptive Headers]");
        sb.AppendLine($"[Each section should:");
        sb.AppendLine($" - Have a clear purpose and payoff");
        sb.AppendLine($" - Include specific examples or demonstrations");
        sb.AppendLine($" - Maintain momentum toward the conclusion");
        sb.AppendLine($" - Suggest visual moments with [VISUAL: description]]");
        sb.AppendLine();
        sb.AppendLine($"## Conclusion");
        sb.AppendLine($"[Powerful summary and clear call-to-action. Leave viewers with lasting value.]");
        sb.AppendLine();

        sb.AppendLine($"QUALITY REQUIREMENTS:");
        sb.AppendLine($"- Every sentence must serve the story and add value");
        sb.AppendLine($"- Use specific, memorable examples and analogies");
        sb.AppendLine($"- Vary sentence length and structure naturally");
        sb.AppendLine($"- Include 2-3 pattern interrupts or surprising turns");
        sb.AppendLine($"- Mark key visual moments with [VISUAL: brief description]");
        sb.AppendLine($"- Ensure the script sounds natural when read aloud");
        sb.AppendLine($"- Build emotional resonance appropriate to the topic");
        sb.AppendLine($"- MAINTAIN STRICT TONE CONSISTENCY THROUGHOUT (will be validated)");

        return sb.ToString();
    }

    /// <summary>
    /// Augment visual prompt with tone-aligned style keywords
    /// </summary>
    public static string AugmentVisualPromptWithTone(
        string baseVisualPrompt,
        Models.Quality.ToneProfile toneProfile)
    {
        var sb = new StringBuilder();
        sb.AppendLine(baseVisualPrompt);
        sb.AppendLine();
        sb.AppendLine("TONE-ALIGNED VISUAL STYLE:");
        
        if (toneProfile.VisualStyleKeywords.Length > 0)
        {
            sb.AppendLine("Required style keywords:");
            foreach (var keyword in toneProfile.VisualStyleKeywords)
            {
                sb.AppendLine($"  • {keyword}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Visual energy alignment:");
        sb.AppendLine($"- Energy Level: {toneProfile.Energy}");
        sb.AppendLine($"- Formality: {toneProfile.Formality}");
        
        var visualGuidance = toneProfile.Energy switch
        {
            Models.Quality.EnergyLevel.Calm => "Use calm, serene, stable visuals. Slow pans, gentle transitions, peaceful scenes.",
            Models.Quality.EnergyLevel.Moderate => "Use balanced visuals with moderate motion. Natural movement, comfortable pacing.",
            Models.Quality.EnergyLevel.Energetic => "Use dynamic, engaging visuals. Active scenes, purposeful movement, vibrant energy.",
            Models.Quality.EnergyLevel.High => "Use high-energy visuals. Fast cuts when appropriate, bold compositions, intense scenes.",
            _ => "Use appropriate visual energy matching the tone."
        };

        sb.AppendLine($"- Guidance: {visualGuidance}");
        sb.AppendLine();
        sb.AppendLine("Ensure all visual selections reinforce and never contradict the established tone.");

        return sb.ToString();
    }
}
