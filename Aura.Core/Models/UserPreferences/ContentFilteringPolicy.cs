using System;
using System.Collections.Generic;

namespace Aura.Core.Models.UserPreferences;

/// <summary>
/// Content filtering policy with granular user-defined thresholds
/// Allows complete control over what content is acceptable
/// </summary>
public class ContentFilteringPolicy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Global Filter Control
    public bool FilteringEnabled { get; set; } = true;
    public bool AllowOverrideAll { get; set; } = false; // Unrestricted mode
    
    // Profanity Filtering
    public ProfanityFilterLevel ProfanityFilter { get; set; } = ProfanityFilterLevel.Moderate;
    public List<string> CustomBannedWords { get; set; } = new();
    public List<string> CustomAllowedWords { get; set; } = new(); // Override defaults
    
    // Violence and Gore (0-10 scale)
    public int ViolenceThreshold { get; set; } = 3;
    public bool BlockGraphicContent { get; set; } = true;
    
    // Sexual Content (0-10 scale)
    public int SexualContentThreshold { get; set; } = 1;
    public bool BlockExplicitContent { get; set; } = true;
    
    // Controversial Topics
    public List<string> BannedTopics { get; set; } = new();
    public List<string> AllowedControversialTopics { get; set; } = new();
    
    // Political Content
    public PoliticalContentPolicy PoliticalContent { get; set; } = PoliticalContentPolicy.NeutralOnly;
    public string? PoliticalContentGuidelines { get; set; }
    
    // Religious Content
    public ReligiousContentPolicy ReligiousContent { get; set; } = ReligiousContentPolicy.RespectfulOnly;
    public string? ReligiousContentGuidelines { get; set; }
    
    // Drug and Alcohol References
    public SubstancePolicy SubstanceReferences { get; set; } = SubstancePolicy.Moderate;
    
    // Hate Speech (always blocked by default, but can have exceptions)
    public bool BlockHateSpeech { get; set; } = true;
    public List<string> HateSpeechExceptions { get; set; } = new();
    
    // Copyright Concerns
    public CopyrightPolicy CopyrightPolicy { get; set; } = CopyrightPolicy.Strict;
    
    // Allow/Block Lists
    public List<string> BlockedConcepts { get; set; } = new();
    public List<string> AllowedConcepts { get; set; } = new();
    public List<string> BlockedPeople { get; set; } = new();
    public List<string> AllowedPeople { get; set; } = new();
    public List<string> BlockedBrands { get; set; } = new();
    public List<string> AllowedBrands { get; set; } = new();
    
    // Metadata
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// Profanity filter levels
/// </summary>
public enum ProfanityFilterLevel
{
    Off = 0,
    Mild = 1,
    Moderate = 2,
    Strict = 3,
    Custom = 4
}

/// <summary>
/// Political content policy
/// </summary>
public enum PoliticalContentPolicy
{
    Off = 0,
    NeutralOnly = 1,
    AllowAll = 2,
    Custom = 3
}

/// <summary>
/// Religious content policy
/// </summary>
public enum ReligiousContentPolicy
{
    Off = 0,
    RespectfulOnly = 1,
    AllowAll = 2,
    Custom = 3
}

/// <summary>
/// Substance reference policy
/// </summary>
public enum SubstancePolicy
{
    Block = 0,
    Moderate = 1,
    Allow = 2,
    Custom = 3
}

/// <summary>
/// Copyright policy
/// </summary>
public enum CopyrightPolicy
{
    Strict = 0,
    Moderate = 1,
    UserAssumesRisk = 2
}
