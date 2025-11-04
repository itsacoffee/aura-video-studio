using System.Collections.Generic;
using Aura.Core.Models;

namespace Aura.Core.Services;

/// <summary>
/// Builder for creating standard refinement rubrics
/// </summary>
public static class RefinementRubricBuilder
{
    /// <summary>
    /// Get default rubrics for script evaluation
    /// </summary>
    public static List<RefinementRubric> GetDefaultRubrics()
    {
        return new List<RefinementRubric>
        {
            BuildClarityRubric(),
            BuildCoherenceRubric(),
            BuildTimingRubric(),
            BuildEngagementRubric(),
            BuildAudienceAlignmentRubric()
        };
    }

    /// <summary>
    /// Build clarity rubric
    /// </summary>
    public static RefinementRubric BuildClarityRubric()
    {
        return new RefinementRubric
        {
            Name = "Clarity",
            Description = "Measures how clear and understandable the script is",
            Weight = 0.25,
            TargetThreshold = 85.0,
            Criteria = new List<RubricCriterion>
            {
                new RubricCriterion
                {
                    Name = "Language Simplicity",
                    Description = "Uses clear, concise language appropriate for the audience",
                    ScoringGuideline = "100: Crystal clear, no jargon. 50: Some complex terms. 0: Confusing or overly technical.",
                    ExcellentExamples = new List<string>
                    {
                        "Uses everyday language and explains complex concepts with analogies",
                        "Sentences are short and direct",
                        "Technical terms are defined immediately"
                    },
                    PoorExamples = new List<string>
                    {
                        "Heavy use of undefined jargon",
                        "Run-on sentences that confuse the main point",
                        "Assumes prior knowledge without explanation"
                    }
                },
                new RubricCriterion
                {
                    Name = "Visual Clarity",
                    Description = "Script supports visual storytelling with clear scene descriptions",
                    ScoringGuideline = "100: Every scene is easily visualizable. 50: Some vague descriptions. 0: Abstract with no visual cues.",
                    ExcellentExamples = new List<string>
                    {
                        "Clear scene transitions with visual anchors",
                        "Describes what viewer should see",
                        "Supports B-roll and graphics naturally"
                    },
                    PoorExamples = new List<string>
                    {
                        "No visual description or cues",
                        "Abstract concepts without concrete examples",
                        "Unclear scene transitions"
                    }
                }
            }
        };
    }

    /// <summary>
    /// Build coherence rubric
    /// </summary>
    public static RefinementRubric BuildCoherenceRubric()
    {
        return new RefinementRubric
        {
            Name = "Coherence",
            Description = "Measures logical flow and narrative structure",
            Weight = 0.25,
            TargetThreshold = 85.0,
            Criteria = new List<RubricCriterion>
            {
                new RubricCriterion
                {
                    Name = "Logical Flow",
                    Description = "Ideas progress logically from one to the next",
                    ScoringGuideline = "100: Perfect logical progression. 50: Some jumps in logic. 0: Disconnected ideas.",
                    ExcellentExamples = new List<string>
                    {
                        "Each point builds on the previous one",
                        "Smooth transitions between topics",
                        "Clear beginning, middle, and end"
                    },
                    PoorExamples = new List<string>
                    {
                        "Random jumping between topics",
                        "No clear structure or progression",
                        "Abrupt transitions that confuse viewers"
                    }
                },
                new RubricCriterion
                {
                    Name = "Narrative Arc",
                    Description = "Has a compelling story structure",
                    ScoringGuideline = "100: Strong hook, development, climax, resolution. 50: Basic structure. 0: No discernible arc.",
                    ExcellentExamples = new List<string>
                    {
                        "Opens with compelling hook",
                        "Builds tension or curiosity",
                        "Satisfying conclusion with key takeaway"
                    },
                    PoorExamples = new List<string>
                    {
                        "No hook or introduction",
                        "Flat delivery without progression",
                        "Abrupt or missing conclusion"
                    }
                }
            }
        };
    }

    /// <summary>
    /// Build timing rubric
    /// </summary>
    public static RefinementRubric BuildTimingRubric()
    {
        return new RefinementRubric
        {
            Name = "Timing",
            Description = "Measures fit between content and target duration",
            Weight = 0.20,
            TargetThreshold = 85.0,
            Criteria = new List<RubricCriterion>
            {
                new RubricCriterion
                {
                    Name = "Word Count Fit",
                    Description = "Word count aligns with target duration (150 words/minute)",
                    ScoringGuideline = "100: Within 5% of target. 75: Within 15%. 50: Within 25%. 0: More than 25% off.",
                    ExcellentExamples = new List<string>
                    {
                        "2-minute video has 280-320 words",
                        "Pacing matches intended delivery speed",
                        "Natural breaks for visual elements"
                    },
                    PoorExamples = new List<string>
                    {
                        "Far too long for target duration",
                        "Too brief and rushed",
                        "No consideration for pauses or visual breaks"
                    }
                },
                new RubricCriterion
                {
                    Name = "Information Density",
                    Description = "Content density appropriate for target duration and pacing",
                    ScoringGuideline = "100: Perfect balance. 50: Too dense or too sparse. 0: Completely mismatched.",
                    ExcellentExamples = new List<string>
                    {
                        "Key points have time to breathe",
                        "Not overwhelming or boring",
                        "Matches specified pacing style"
                    },
                    PoorExamples = new List<string>
                    {
                        "Cramming too many ideas",
                        "Repetitive and drawn out",
                        "Mismatched with pacing specification"
                    }
                }
            }
        };
    }

    /// <summary>
    /// Build engagement rubric
    /// </summary>
    public static RefinementRubric BuildEngagementRubric()
    {
        return new RefinementRubric
        {
            Name = "Engagement",
            Description = "Measures viewer engagement and retention potential",
            Weight = 0.15,
            TargetThreshold = 85.0,
            Criteria = new List<RubricCriterion>
            {
                new RubricCriterion
                {
                    Name = "Hook Strength",
                    Description = "Opening immediately captures attention",
                    ScoringGuideline = "100: Irresistible hook. 50: Adequate intro. 0: Boring or missing hook.",
                    ExcellentExamples = new List<string>
                    {
                        "Starts with surprising fact or question",
                        "Creates immediate curiosity",
                        "Clear value proposition in first 10 seconds"
                    },
                    PoorExamples = new List<string>
                    {
                        "Generic or cliché opening",
                        "Slow buildup with no payoff",
                        "No clear reason to keep watching"
                    }
                },
                new RubricCriterion
                {
                    Name = "Pattern Interrupts",
                    Description = "Uses variety to maintain attention",
                    ScoringGuideline = "100: Multiple engagement techniques. 50: Some variety. 0: Monotonous throughout.",
                    ExcellentExamples = new List<string>
                    {
                        "Questions, surprises, or reveals",
                        "Rhythm changes and pacing variety",
                        "Strategic use of analogies or examples"
                    },
                    PoorExamples = new List<string>
                    {
                        "Flat, monotonous delivery",
                        "No surprises or variety",
                        "Predictable from start to finish"
                    }
                }
            }
        };
    }

    /// <summary>
    /// Build audience alignment rubric
    /// </summary>
    public static RefinementRubric BuildAudienceAlignmentRubric()
    {
        return new RefinementRubric
        {
            Name = "AudienceAlignment",
            Description = "Measures how well content matches target audience",
            Weight = 0.15,
            TargetThreshold = 85.0,
            Criteria = new List<RubricCriterion>
            {
                new RubricCriterion
                {
                    Name = "Language Level",
                    Description = "Vocabulary and complexity match audience knowledge",
                    ScoringGuideline = "100: Perfect match. 50: Somewhat off. 0: Completely mismatched.",
                    ExcellentExamples = new List<string>
                    {
                        "Beginner content uses simple terms",
                        "Expert content uses appropriate terminology",
                        "Explains concepts at right depth"
                    },
                    PoorExamples = new List<string>
                    {
                        "Too simple for expert audience",
                        "Too complex for beginners",
                        "Assumes wrong knowledge level"
                    }
                },
                new RubricCriterion
                {
                    Name = "Relevance",
                    Description = "Content addresses audience needs and interests",
                    ScoringGuideline = "100: Highly relevant. 50: Somewhat relevant. 0: Not relevant.",
                    ExcellentExamples = new List<string>
                    {
                        "Addresses known pain points",
                        "Uses relatable examples",
                        "Provides actionable value"
                    },
                    PoorExamples = new List<string>
                    {
                        "Generic content not tailored to audience",
                        "Misses audience concerns",
                        "No clear relevance or value"
                    }
                }
            }
        };
    }
}
