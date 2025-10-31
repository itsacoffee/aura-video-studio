using System.Collections.Generic;
using Aura.Core.Models.Audience;

namespace Aura.Core.Services.Audience;

/// <summary>
/// Preset audience profile templates for common audience types
/// </summary>
public static class AudienceProfileTemplates
{
    /// <summary>
    /// Get all available templates
    /// </summary>
    public static List<AudienceProfile> GetAllTemplates()
    {
        return new List<AudienceProfile>
        {
            CreateStudentsTemplate(),
            CreateBusinessProfessionalsTemplate(),
            CreateTechEnthusiastsTemplate(),
            CreateParentsTemplate(),
            CreateSeniorsTemplate(),
            CreateHobbyistsTemplate(),
            CreateCompleteBeginnersTemplate(),
            CreateDomainExpertsTemplate(),
            CreateHealthcareWorkersTemplate(),
            CreateEducatorsTemplate()
        };
    }

    /// <summary>
    /// Students (18-24, in college/university)
    /// </summary>
    public static AudienceProfile CreateStudentsTemplate()
    {
        return new AudienceProfileBuilder("Students")
            .SetAgeRange(AgeRange.YoungAdults)
            .SetEducation(EducationLevel.InProgress)
            .SetExpertise(ExpertiseLevel.Novice)
            .SetIncomeBracket(IncomeBracket.LowIncome)
            .SetTechnicalComfort(TechnicalComfort.Moderate)
            .SetLearningStyle(LearningStyle.Multimodal)
            .SetAttentionSpan(AttentionSpan.Medium)
            .AddInterests("Learning", "Career development", "Technology", "Social media")
            .AddPainPoint("Limited time due to coursework")
            .AddPainPoint("Limited budget")
            .AddMotivation("Career preparation")
            .AddMotivation("Academic success")
            .SetDescription("College and university students seeking knowledge and career advancement")
            .AsTemplate()
            .AddTags("education", "young-adults", "career")
            .Build();
    }

    /// <summary>
    /// Business Professionals (25-44, corporate environment)
    /// </summary>
    public static AudienceProfile CreateBusinessProfessionalsTemplate()
    {
        return new AudienceProfileBuilder("Business Professionals")
            .SetAgeRange(25, 44, "Professional Age (25-44)")
            .SetEducation(EducationLevel.BachelorDegree)
            .SetIndustry("Business & Finance")
            .SetExpertise(ExpertiseLevel.Intermediate)
            .SetIncomeBracket(IncomeBracket.MiddleIncome)
            .SetTechnicalComfort(TechnicalComfort.Moderate)
            .SetLearningStyle(LearningStyle.ReadingWriting)
            .SetAttentionSpan(AttentionSpan.Short)
            .AddInterests("Leadership", "Management", "Productivity", "Networking")
            .AddPainPoint("Limited time during workday")
            .AddPainPoint("Need actionable insights quickly")
            .AddMotivation("Career advancement")
            .AddMotivation("Skill development")
            .SetCulturalBackground(style: CommunicationStyle.Professional)
            .SetDescription("Corporate professionals seeking efficient skill development and career growth")
            .AsTemplate()
            .AddTags("business", "professional", "corporate")
            .Build();
    }

    /// <summary>
    /// Tech Enthusiasts (20-40, tech-savvy)
    /// </summary>
    public static AudienceProfile CreateTechEnthusiastsTemplate()
    {
        return new AudienceProfileBuilder("Tech Enthusiasts")
            .SetAgeRange(20, 40, "Tech Age (20-40)")
            .SetEducation(EducationLevel.BachelorDegree)
            .SetIndustry("Technology")
            .SetExpertise(ExpertiseLevel.Advanced)
            .SetTechnicalComfort(TechnicalComfort.TechSavvy)
            .SetLearningStyle(LearningStyle.Visual)
            .SetAttentionSpan(AttentionSpan.Long)
            .AddInterests("Programming", "AI/ML", "Cloud computing", "DevOps", "Cybersecurity")
            .AddPainPoint("Keeping up with rapid technology changes")
            .AddPainPoint("Finding in-depth technical content")
            .AddMotivation("Stay current with technology trends")
            .AddMotivation("Expand technical expertise")
            .SetDescription("Technology enthusiasts and professionals seeking advanced technical content")
            .AsTemplate()
            .AddTags("technology", "programming", "advanced")
            .Build();
    }

    /// <summary>
    /// Parents (25-45, family-focused)
    /// </summary>
    public static AudienceProfile CreateParentsTemplate()
    {
        return new AudienceProfileBuilder("Parents")
            .SetAgeRange(25, 45, "Parenting Age (25-45)")
            .SetEducation(EducationLevel.SomeCollege)
            .SetExpertise(ExpertiseLevel.Intermediate)
            .SetIncomeBracket(IncomeBracket.MiddleIncome)
            .SetTechnicalComfort(TechnicalComfort.BasicUser)
            .SetLearningStyle(LearningStyle.Visual)
            .SetAttentionSpan(AttentionSpan.Short)
            .AddInterests("Parenting", "Education", "Family activities", "Health", "Budgeting")
            .AddPainPoint("Very limited free time")
            .AddPainPoint("Balancing work and family")
            .AddMotivation("Better parenting skills")
            .AddMotivation("Family wellbeing")
            .SetDescription("Parents seeking practical advice for family life and child-rearing")
            .AsTemplate()
            .AddTags("family", "parenting", "lifestyle")
            .Build();
    }

    /// <summary>
    /// Seniors 55+ (older adults)
    /// </summary>
    public static AudienceProfile CreateSeniorsTemplate()
    {
        return new AudienceProfileBuilder("Seniors 55+")
            .SetAgeRange(AgeRange.Seniors)
            .SetEducation(EducationLevel.HighSchool)
            .SetExpertise(ExpertiseLevel.Novice)
            .SetTechnicalComfort(TechnicalComfort.NonTechnical)
            .SetLearningStyle(LearningStyle.Auditory)
            .SetAttentionSpan(AttentionSpan.Medium)
            .AddInterests("Health", "Retirement planning", "Hobbies", "Travel", "Family")
            .AddPainPoint("Technology can be overwhelming")
            .AddPainPoint("Need clear, simple instructions")
            .AddMotivation("Stay active and engaged")
            .AddMotivation("Learn new skills at own pace")
            .SetAccessibilityNeeds(
                requiresCaptions: true,
                requiresLargeText: true,
                requiresSimplifiedLanguage: true)
            .SetDescription("Older adults seeking accessible, easy-to-understand content")
            .AsTemplate()
            .AddTags("seniors", "accessibility", "simple")
            .Build();
    }

    /// <summary>
    /// Hobbyists (all ages, passionate about specific interests)
    /// </summary>
    public static AudienceProfile CreateHobbyistsTemplate()
    {
        return new AudienceProfileBuilder("Hobbyists")
            .SetAgeRange(18, 65, "Adult (18-65)")
            .SetEducation(EducationLevel.SomeCollege)
            .SetExpertise(ExpertiseLevel.Intermediate)
            .SetTechnicalComfort(TechnicalComfort.Moderate)
            .SetLearningStyle(LearningStyle.Visual)
            .SetAttentionSpan(AttentionSpan.Long)
            .AddInterests("DIY projects", "Crafts", "Collecting", "Photography", "Gardening")
            .AddPainPoint("Finding quality tutorials")
            .AddPainPoint("Need step-by-step guidance")
            .AddMotivation("Master hobby skills")
            .AddMotivation("Create quality projects")
            .SetDescription("Enthusiasts pursuing hobbies and creative interests")
            .AsTemplate()
            .AddTags("hobbies", "creative", "diy")
            .Build();
    }

    /// <summary>
    /// Complete Beginners (no prior knowledge)
    /// </summary>
    public static AudienceProfile CreateCompleteBeginnersTemplate()
    {
        return new AudienceProfileBuilder("Complete Beginners")
            .SetAgeRange(18, 50, "Adult (18-50)")
            .SetExpertise(ExpertiseLevel.CompleteBeginner)
            .SetTechnicalComfort(TechnicalComfort.BasicUser)
            .SetLearningStyle(LearningStyle.Visual)
            .SetAttentionSpan(AttentionSpan.Short)
            .AddInterests("Learning new skills", "Personal development")
            .AddPainPoint("Overwhelmed by complex information")
            .AddPainPoint("Need very basic starting point")
            .AddMotivation("Learn something new")
            .AddMotivation("Build confidence")
            .SetAccessibilityNeeds(requiresSimplifiedLanguage: true)
            .SetDescription("Complete beginners with no prior knowledge seeking foundational understanding")
            .AsTemplate()
            .AddTags("beginner", "introductory", "simple")
            .Build();
    }

    /// <summary>
    /// Domain Experts (advanced professionals in specific field)
    /// </summary>
    public static AudienceProfile CreateDomainExpertsTemplate()
    {
        return new AudienceProfileBuilder("Domain Experts")
            .SetAgeRange(30, 60, "Experienced (30-60)")
            .SetEducation(EducationLevel.MasterDegree)
            .SetExpertise(ExpertiseLevel.Expert)
            .SetTechnicalComfort(TechnicalComfort.Expert)
            .SetLearningStyle(LearningStyle.ReadingWriting)
            .SetAttentionSpan(AttentionSpan.Long)
            .AddInterests("Advanced techniques", "Research", "Innovation", "Industry trends")
            .AddPainPoint("Need cutting-edge, detailed content")
            .AddPainPoint("Most content too basic")
            .AddMotivation("Stay at forefront of field")
            .AddMotivation("Deep dive into complex topics")
            .SetCulturalBackground(style: CommunicationStyle.Professional)
            .SetDescription("Expert professionals seeking advanced, detailed content in their domain")
            .AsTemplate()
            .AddTags("expert", "advanced", "professional")
            .Build();
    }

    /// <summary>
    /// Healthcare Workers (medical professionals)
    /// </summary>
    public static AudienceProfile CreateHealthcareWorkersTemplate()
    {
        return new AudienceProfileBuilder("Healthcare Workers")
            .SetAgeRange(25, 55, "Professional Age (25-55)")
            .SetEducation(EducationLevel.BachelorDegree)
            .SetIndustry("Healthcare")
            .SetProfession("Healthcare Professional")
            .SetExpertise(ExpertiseLevel.Advanced)
            .SetIncomeBracket(IncomeBracket.UpperMiddleIncome)
            .SetTechnicalComfort(TechnicalComfort.Moderate)
            .SetLearningStyle(LearningStyle.Multimodal)
            .SetAttentionSpan(AttentionSpan.Short)
            .AddInterests("Medical advances", "Patient care", "Clinical procedures", "Healthcare technology")
            .AddPainPoint("Very limited time due to demanding schedules")
            .AddPainPoint("Need evidence-based, accurate information")
            .AddMotivation("Improve patient outcomes")
            .AddMotivation("Stay current with medical knowledge")
            .SetCulturalBackground(style: CommunicationStyle.Professional)
            .SetDescription("Medical professionals seeking efficient, evidence-based continuing education")
            .AsTemplate()
            .AddTags("healthcare", "medical", "professional")
            .Build();
    }

    /// <summary>
    /// Educators (teachers, instructors)
    /// </summary>
    public static AudienceProfile CreateEducatorsTemplate()
    {
        return new AudienceProfileBuilder("Educators")
            .SetAgeRange(25, 60, "Teaching Age (25-60)")
            .SetEducation(EducationLevel.BachelorDegree)
            .SetIndustry("Education")
            .SetProfession("Teacher/Instructor")
            .SetExpertise(ExpertiseLevel.Advanced)
            .SetIncomeBracket(IncomeBracket.MiddleIncome)
            .SetTechnicalComfort(TechnicalComfort.Moderate)
            .SetLearningStyle(LearningStyle.Multimodal)
            .SetAttentionSpan(AttentionSpan.Medium)
            .AddInterests("Teaching methods", "Educational technology", "Curriculum development", "Student engagement")
            .AddPainPoint("Finding quality educational resources")
            .AddPainPoint("Adapting to different learning styles")
            .AddMotivation("Improve teaching effectiveness")
            .AddMotivation("Help students succeed")
            .SetDescription("Teachers and instructors seeking professional development and teaching resources")
            .AsTemplate()
            .AddTags("education", "teaching", "professional")
            .Build();
    }
}
