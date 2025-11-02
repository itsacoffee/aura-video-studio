using System.Linq;
using Aura.Core.Services.Localization;
using Xunit;

namespace Aura.Tests.Localization;

public class LanguageRegistryTests
{
    [Fact]
    public void GetSupportedLanguageCount_ReturnsAtLeast50Languages()
    {
        // Act
        var count = LanguageRegistry.GetSupportedLanguageCount();

        // Assert
        Assert.True(count >= 50, $"Expected at least 50 languages, but found {count}");
    }

    [Fact]
    public void GetAllLanguages_ReturnsLanguages()
    {
        // Act
        var languages = LanguageRegistry.GetAllLanguages();

        // Assert
        Assert.NotNull(languages);
        Assert.NotEmpty(languages);
        Assert.True(languages.Count >= 50);
    }

    [Fact]
    public void GetLanguage_ValidCode_ReturnsLanguage()
    {
        // Act
        var english = LanguageRegistry.GetLanguage("en");
        var spanish = LanguageRegistry.GetLanguage("es");
        var mandarin = LanguageRegistry.GetLanguage("zh");

        // Assert
        Assert.NotNull(english);
        Assert.Equal("English", english.Name);
        
        Assert.NotNull(spanish);
        Assert.Equal("Spanish", spanish.Name);
        
        Assert.NotNull(mandarin);
        Assert.Equal("Chinese (Simplified)", mandarin.Name);
    }

    [Fact]
    public void GetLanguage_InvalidCode_ReturnsNull()
    {
        // Act
        var result = LanguageRegistry.GetLanguage("invalid");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetLanguage_Arabic_IsRightToLeft()
    {
        // Act
        var arabic = LanguageRegistry.GetLanguage("ar");

        // Assert
        Assert.NotNull(arabic);
        Assert.True(arabic.IsRightToLeft);
    }

    [Fact]
    public void GetLanguage_Hebrew_IsRightToLeft()
    {
        // Act
        var hebrew = LanguageRegistry.GetLanguage("he");

        // Assert
        Assert.NotNull(hebrew);
        Assert.True(hebrew.IsRightToLeft);
    }

    [Fact]
    public void GetLanguage_German_HasExpansionFactor()
    {
        // Act
        var german = LanguageRegistry.GetLanguage("de");

        // Assert
        Assert.NotNull(german);
        Assert.True(german.TypicalExpansionFactor > 1.0);
    }

    [Fact]
    public void GetLanguagesByRegion_ReturnsRegionalLanguages()
    {
        // Act
        var europeanLanguages = LanguageRegistry.GetLanguagesByRegion("Europe");

        // Assert
        Assert.NotNull(europeanLanguages);
        Assert.Contains(europeanLanguages, l => l.Code == "de");
        Assert.Contains(europeanLanguages, l => l.Code == "fr");
        Assert.Contains(europeanLanguages, l => l.Code == "it");
    }

    [Fact]
    public void GetRightToLeftLanguages_ReturnsRTLLanguages()
    {
        // Act
        var rtlLanguages = LanguageRegistry.GetRightToLeftLanguages();

        // Assert
        Assert.NotNull(rtlLanguages);
        Assert.Contains(rtlLanguages, l => l.Code == "ar");
        Assert.Contains(rtlLanguages, l => l.Code == "he");
        Assert.Contains(rtlLanguages, l => l.Code == "fa");
        Assert.Contains(rtlLanguages, l => l.Code == "ur");
        Assert.All(rtlLanguages, l => Assert.True(l.IsRightToLeft));
    }

    [Fact]
    public void IsLanguageSupported_ValidCode_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(LanguageRegistry.IsLanguageSupported("en"));
        Assert.True(LanguageRegistry.IsLanguageSupported("es"));
        Assert.True(LanguageRegistry.IsLanguageSupported("zh"));
    }

    [Fact]
    public void IsLanguageSupported_InvalidCode_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(LanguageRegistry.IsLanguageSupported("invalid"));
        Assert.False(LanguageRegistry.IsLanguageSupported("xyz"));
    }

    [Theory]
    [InlineData("en", "English")]
    [InlineData("es", "Spanish")]
    [InlineData("fr", "French")]
    [InlineData("de", "German")]
    [InlineData("it", "Italian")]
    [InlineData("pt", "Portuguese")]
    [InlineData("ru", "Russian")]
    [InlineData("ja", "Japanese")]
    [InlineData("ko", "Korean")]
    [InlineData("zh", "Chinese (Simplified)")]
    [InlineData("ar", "Arabic")]
    [InlineData("hi", "Hindi")]
    public void GetLanguage_MajorLanguages_ReturnsCorrectName(string code, string expectedName)
    {
        // Act
        var language = LanguageRegistry.GetLanguage(code);

        // Assert
        Assert.NotNull(language);
        Assert.Equal(expectedName, language.Name);
    }

    [Fact]
    public void GetLanguage_SpanishVariants_HaveDifferentFormality()
    {
        // Act
        var esES = LanguageRegistry.GetLanguage("es-ES");
        var esMX = LanguageRegistry.GetLanguage("es-MX");
        var esAR = LanguageRegistry.GetLanguage("es-AR");

        // Assert
        Assert.NotNull(esES);
        Assert.NotNull(esMX);
        Assert.NotNull(esAR);
        
        // Spain Spanish is more formal
        Assert.NotEqual(esES.DefaultFormality, esMX.DefaultFormality);
        Assert.NotEqual(esES.DefaultFormality, esAR.DefaultFormality);
    }

    [Fact]
    public void GetLanguage_PortugueseVariants_ExistForBrazilAndPortugal()
    {
        // Act
        var ptBR = LanguageRegistry.GetLanguage("pt-BR");
        var ptPT = LanguageRegistry.GetLanguage("pt-PT");

        // Assert
        Assert.NotNull(ptBR);
        Assert.Equal("Portuguese (Brazil)", ptBR.Name);
        
        Assert.NotNull(ptPT);
        Assert.Equal("Portuguese (Portugal)", ptPT.Name);
    }

    [Fact]
    public void GetLanguage_ChineseVariants_ExistForSimplifiedAndTraditional()
    {
        // Act
        var zhCN = LanguageRegistry.GetLanguage("zh-CN");
        var zhTW = LanguageRegistry.GetLanguage("zh-TW");

        // Assert
        Assert.NotNull(zhCN);
        Assert.Contains("Simplified", zhCN.Name);
        
        Assert.NotNull(zhTW);
        Assert.Contains("Traditional", zhTW.Name);
    }
}
