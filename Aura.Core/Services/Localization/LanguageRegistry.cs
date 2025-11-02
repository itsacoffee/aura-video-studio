using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models.Localization;

namespace Aura.Core.Services.Localization;

/// <summary>
/// Registry of supported languages with cultural metadata
/// Supports 50+ languages with regional variants
/// </summary>
public class LanguageRegistry
{
    private static readonly Dictionary<string, LanguageInfo> _languages = new()
    {
        // English variants
        ["en"] = new() { Code = "en", Name = "English", NativeName = "English", Region = "Global", TypicalExpansionFactor = 1.0 },
        ["en-US"] = new() { Code = "en-US", Name = "English (US)", NativeName = "English (United States)", Region = "North America", TypicalExpansionFactor = 1.0 },
        ["en-GB"] = new() { Code = "en-GB", Name = "English (UK)", NativeName = "English (United Kingdom)", Region = "Europe", TypicalExpansionFactor = 1.0 },
        ["en-AU"] = new() { Code = "en-AU", Name = "English (AU)", NativeName = "English (Australia)", Region = "Oceania", TypicalExpansionFactor = 1.0 },
        
        // Spanish variants
        ["es"] = new() { Code = "es", Name = "Spanish", NativeName = "Español", Region = "Global", TypicalExpansionFactor = 1.15 },
        ["es-ES"] = new() { Code = "es-ES", Name = "Spanish (Spain)", NativeName = "Español (España)", Region = "Europe", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.15 },
        ["es-MX"] = new() { Code = "es-MX", Name = "Spanish (Mexico)", NativeName = "Español (México)", Region = "Latin America", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.15 },
        ["es-AR"] = new() { Code = "es-AR", Name = "Spanish (Argentina)", NativeName = "Español (Argentina)", Region = "Latin America", DefaultFormality = FormalityLevel.Informal, TypicalExpansionFactor = 1.15 },
        
        // Portuguese variants
        ["pt"] = new() { Code = "pt", Name = "Portuguese", NativeName = "Português", Region = "Global", TypicalExpansionFactor = 1.15 },
        ["pt-BR"] = new() { Code = "pt-BR", Name = "Portuguese (Brazil)", NativeName = "Português (Brasil)", Region = "Latin America", DefaultFormality = FormalityLevel.Informal, TypicalExpansionFactor = 1.15 },
        ["pt-PT"] = new() { Code = "pt-PT", Name = "Portuguese (Portugal)", NativeName = "Português (Portugal)", Region = "Europe", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.15 },
        
        // French variants
        ["fr"] = new() { Code = "fr", Name = "French", NativeName = "Français", Region = "Global", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.15 },
        ["fr-FR"] = new() { Code = "fr-FR", Name = "French (France)", NativeName = "Français (France)", Region = "Europe", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.15 },
        ["fr-CA"] = new() { Code = "fr-CA", Name = "French (Canada)", NativeName = "Français (Canada)", Region = "North America", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.15 },
        
        // German
        ["de"] = new() { Code = "de", Name = "German", NativeName = "Deutsch", Region = "Europe", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.3, CulturalSensitivities = new() { "Historical references", "Military terminology" } },
        ["de-DE"] = new() { Code = "de-DE", Name = "German (Germany)", NativeName = "Deutsch (Deutschland)", Region = "Europe", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.3 },
        ["de-AT"] = new() { Code = "de-AT", Name = "German (Austria)", NativeName = "Deutsch (Österreich)", Region = "Europe", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.3 },
        
        // Italian
        ["it"] = new() { Code = "it", Name = "Italian", NativeName = "Italiano", Region = "Europe", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.2 },
        
        // Dutch
        ["nl"] = new() { Code = "nl", Name = "Dutch", NativeName = "Nederlands", Region = "Europe", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.1 },
        
        // Russian
        ["ru"] = new() { Code = "ru", Name = "Russian", NativeName = "Русский", Region = "Eastern Europe", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.15 },
        
        // Polish
        ["pl"] = new() { Code = "pl", Name = "Polish", NativeName = "Polski", Region = "Europe", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.2 },
        
        // Ukrainian
        ["uk"] = new() { Code = "uk", Name = "Ukrainian", NativeName = "Українська", Region = "Eastern Europe", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.15 },
        
        // Czech
        ["cs"] = new() { Code = "cs", Name = "Czech", NativeName = "Čeština", Region = "Europe", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.2 },
        
        // Romanian
        ["ro"] = new() { Code = "ro", Name = "Romanian", NativeName = "Română", Region = "Europe", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.15 },
        
        // Swedish
        ["sv"] = new() { Code = "sv", Name = "Swedish", NativeName = "Svenska", Region = "Europe", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.1 },
        
        // Norwegian
        ["no"] = new() { Code = "no", Name = "Norwegian", NativeName = "Norsk", Region = "Europe", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.1 },
        
        // Danish
        ["da"] = new() { Code = "da", Name = "Danish", NativeName = "Dansk", Region = "Europe", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.1 },
        
        // Finnish
        ["fi"] = new() { Code = "fi", Name = "Finnish", NativeName = "Suomi", Region = "Europe", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.2 },
        
        // Greek
        ["el"] = new() { Code = "el", Name = "Greek", NativeName = "Ελληνικά", Region = "Europe", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.2 },
        
        // Turkish
        ["tr"] = new() { Code = "tr", Name = "Turkish", NativeName = "Türkçe", Region = "Middle East", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.15 },
        
        // Mandarin Chinese
        ["zh"] = new() { Code = "zh", Name = "Chinese (Simplified)", NativeName = "简体中文", Region = "Asia", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 0.7 },
        ["zh-CN"] = new() { Code = "zh-CN", Name = "Chinese (Mainland)", NativeName = "简体中文 (中国)", Region = "Asia", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 0.7 },
        ["zh-TW"] = new() { Code = "zh-TW", Name = "Chinese (Traditional)", NativeName = "繁體中文 (台灣)", Region = "Asia", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 0.7 },
        ["zh-HK"] = new() { Code = "zh-HK", Name = "Chinese (Hong Kong)", NativeName = "繁體中文 (香港)", Region = "Asia", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 0.7 },
        
        // Japanese
        ["ja"] = new() { Code = "ja", Name = "Japanese", NativeName = "日本語", Region = "Asia", DefaultFormality = FormalityLevel.VeryFormal, TypicalExpansionFactor = 0.8, CulturalSensitivities = new() { "Honorifics critical", "Indirect communication preferred" } },
        
        // Korean
        ["ko"] = new() { Code = "ko", Name = "Korean", NativeName = "한국어", Region = "Asia", DefaultFormality = FormalityLevel.VeryFormal, TypicalExpansionFactor = 0.9, CulturalSensitivities = new() { "Hierarchical language levels", "Age and status important" } },
        
        // Vietnamese
        ["vi"] = new() { Code = "vi", Name = "Vietnamese", NativeName = "Tiếng Việt", Region = "Asia", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.0 },
        
        // Thai
        ["th"] = new() { Code = "th", Name = "Thai", NativeName = "ไทย", Region = "Asia", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.0 },
        
        // Indonesian
        ["id"] = new() { Code = "id", Name = "Indonesian", NativeName = "Bahasa Indonesia", Region = "Asia", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.1 },
        
        // Malay
        ["ms"] = new() { Code = "ms", Name = "Malay", NativeName = "Bahasa Melayu", Region = "Asia", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.1 },
        
        // Tagalog
        ["tl"] = new() { Code = "tl", Name = "Tagalog", NativeName = "Tagalog", Region = "Asia", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.1 },
        
        // Hindi
        ["hi"] = new() { Code = "hi", Name = "Hindi", NativeName = "हिन्दी", Region = "Asia", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.15 },
        
        // Bengali
        ["bn"] = new() { Code = "bn", Name = "Bengali", NativeName = "বাংলা", Region = "Asia", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.15 },
        
        // Tamil
        ["ta"] = new() { Code = "ta", Name = "Tamil", NativeName = "தமிழ்", Region = "Asia", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.2 },
        
        // Telugu
        ["te"] = new() { Code = "te", Name = "Telugu", NativeName = "తెలుగు", Region = "Asia", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.2 },
        
        // Urdu
        ["ur"] = new() { Code = "ur", Name = "Urdu", NativeName = "اردو", Region = "Asia", IsRightToLeft = true, DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.15 },
        
        // Arabic variants
        ["ar"] = new() { Code = "ar", Name = "Arabic", NativeName = "العربية", Region = "Middle East", IsRightToLeft = true, DefaultFormality = FormalityLevel.VeryFormal, TypicalExpansionFactor = 1.25, CulturalSensitivities = new() { "Religious references", "Gender roles", "Conservative values" } },
        ["ar-SA"] = new() { Code = "ar-SA", Name = "Arabic (Saudi Arabia)", NativeName = "العربية (السعودية)", Region = "Middle East", IsRightToLeft = true, DefaultFormality = FormalityLevel.VeryFormal, TypicalExpansionFactor = 1.25 },
        ["ar-EG"] = new() { Code = "ar-EG", Name = "Arabic (Egypt)", NativeName = "العربية (مصر)", Region = "Middle East", IsRightToLeft = true, DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.25 },
        
        // Hebrew
        ["he"] = new() { Code = "he", Name = "Hebrew", NativeName = "עברית", Region = "Middle East", IsRightToLeft = true, DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.1 },
        
        // Persian
        ["fa"] = new() { Code = "fa", Name = "Persian", NativeName = "فارسی", Region = "Middle East", IsRightToLeft = true, DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.2 },
        
        // Swahili
        ["sw"] = new() { Code = "sw", Name = "Swahili", NativeName = "Kiswahili", Region = "Africa", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.15 },
        
        // Amharic
        ["am"] = new() { Code = "am", Name = "Amharic", NativeName = "አማርኛ", Region = "Africa", DefaultFormality = FormalityLevel.Formal, TypicalExpansionFactor = 1.2 },
        
        // Zulu
        ["zu"] = new() { Code = "zu", Name = "Zulu", NativeName = "isiZulu", Region = "Africa", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.15 },
        
        // Afrikaans
        ["af"] = new() { Code = "af", Name = "Afrikaans", NativeName = "Afrikaans", Region = "Africa", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.1 },
        
        // Hausa
        ["ha"] = new() { Code = "ha", Name = "Hausa", NativeName = "Hausa", Region = "Africa", DefaultFormality = FormalityLevel.Neutral, TypicalExpansionFactor = 1.15 },
    };

    /// <summary>
    /// Get language information by code
    /// </summary>
    public static LanguageInfo? GetLanguage(string languageCode)
    {
        return _languages.TryGetValue(languageCode, out var language) ? language : null;
    }

    /// <summary>
    /// Get all supported languages
    /// </summary>
    public static IReadOnlyList<LanguageInfo> GetAllLanguages()
    {
        return _languages.Values.OrderBy(l => l.Name).ToList();
    }

    /// <summary>
    /// Get languages by region
    /// </summary>
    public static IReadOnlyList<LanguageInfo> GetLanguagesByRegion(string region)
    {
        return _languages.Values
            .Where(l => l.Region.Equals(region, StringComparison.OrdinalIgnoreCase))
            .OrderBy(l => l.Name)
            .ToList();
    }

    /// <summary>
    /// Check if language is supported
    /// </summary>
    public static bool IsLanguageSupported(string languageCode)
    {
        return _languages.ContainsKey(languageCode);
    }

    /// <summary>
    /// Get right-to-left languages
    /// </summary>
    public static IReadOnlyList<LanguageInfo> GetRightToLeftLanguages()
    {
        return _languages.Values
            .Where(l => l.IsRightToLeft)
            .OrderBy(l => l.Name)
            .ToList();
    }

    /// <summary>
    /// Get language count
    /// </summary>
    public static int GetSupportedLanguageCount()
    {
        return _languages.Count;
    }
}
