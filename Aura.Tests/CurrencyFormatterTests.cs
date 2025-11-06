using Aura.Core.Telemetry.Costing;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for centralized currency formatter
/// </summary>
public class CurrencyFormatterTests
{
    [Fact]
    public void Format_USD_CorrectSymbolAndPrecision()
    {
        // Arrange
        var amount = 0.0023m;
        
        // Act
        var formatted = CurrencyFormatter.Format(amount, "USD", 4);
        
        // Assert
        Assert.Equal("$0.0023", formatted);
    }
    
    [Fact]
    public void Format_EUR_CorrectSymbol()
    {
        // Arrange
        var amount = 1.2500m;
        
        // Act
        var formatted = CurrencyFormatter.Format(amount, "EUR", 4);
        
        // Assert
        Assert.Equal("€1.2500", formatted);
    }
    
    [Fact]
    public void Format_GBP_CorrectSymbol()
    {
        // Arrange
        var amount = 0.5000m;
        
        // Act
        var formatted = CurrencyFormatter.Format(amount, "GBP", 4);
        
        // Assert
        Assert.Equal("£0.5000", formatted);
    }
    
    [Fact]
    public void Format_TwoDecimalPlaces_Rounds()
    {
        // Arrange
        var amount = 1.2567m;
        
        // Act
        var formatted = CurrencyFormatter.Format(amount, "USD", 2);
        
        // Assert
        Assert.Equal("$1.26", formatted);
    }
    
    [Fact]
    public void FormatWithCode_USD_ShowsCode()
    {
        // Arrange
        var amount = 0.0023m;
        
        // Act
        var formatted = CurrencyFormatter.FormatWithCode(amount, "USD", 4);
        
        // Assert
        Assert.Equal("USD 0.0023", formatted);
    }
    
    [Fact]
    public void FormatForDisplay_LargeAmount_UsesTwoDecimals()
    {
        // Arrange
        var amount = 5.6789m;
        
        // Act
        var formatted = CurrencyFormatter.FormatForDisplay(amount, "USD");
        
        // Assert
        Assert.Equal("$5.68", formatted);
    }
    
    [Fact]
    public void FormatForDisplay_SmallAmount_UsesFourDecimals()
    {
        // Arrange
        var amount = 0.0023m;
        
        // Act
        var formatted = CurrencyFormatter.FormatForDisplay(amount, "USD");
        
        // Assert
        Assert.Equal("$0.0023", formatted);
    }
    
    [Fact]
    public void GetCurrencySymbol_USD_ReturnsDollar()
    {
        // Act
        var symbol = CurrencyFormatter.GetCurrencySymbol("USD");
        
        // Assert
        Assert.Equal("$", symbol);
    }
    
    [Fact]
    public void GetCurrencySymbol_UnknownCurrency_ReturnsCurrencyCode()
    {
        // Act
        var symbol = CurrencyFormatter.GetCurrencySymbol("XYZ");
        
        // Assert
        Assert.Equal("XYZ ", symbol);
    }
    
    [Fact]
    public void Parse_WithDollarSign_ParsesCorrectly()
    {
        // Arrange
        var formatted = "$0.0023";
        
        // Act
        var amount = CurrencyFormatter.Parse(formatted);
        
        // Assert
        Assert.Equal(0.0023m, amount);
    }
    
    [Fact]
    public void Parse_WithCurrencyCode_ParsesCorrectly()
    {
        // Arrange
        var formatted = "USD 1.2500";
        
        // Act
        var amount = CurrencyFormatter.Parse(formatted);
        
        // Assert
        Assert.Equal(1.2500m, amount);
    }
    
    [Fact]
    public void Parse_PlainNumber_ParsesCorrectly()
    {
        // Arrange
        var formatted = "0.0023";
        
        // Act
        var amount = CurrencyFormatter.Parse(formatted);
        
        // Assert
        Assert.Equal(0.0023m, amount);
    }
    
    [Fact]
    public void Parse_EmptyString_ReturnsZero()
    {
        // Act
        var amount = CurrencyFormatter.Parse("");
        
        // Assert
        Assert.Equal(0m, amount);
    }
    
    [Fact]
    public void Parse_InvalidFormat_ThrowsException()
    {
        // Arrange
        var formatted = "invalid";
        
        // Act & Assert
        Assert.Throws<System.FormatException>(() => CurrencyFormatter.Parse(formatted));
    }
    
    [Fact]
    public void AreEqual_SameAmounts_ReturnsTrue()
    {
        // Act
        var result = CurrencyFormatter.AreEqual(1.00m, 1.00m);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void AreEqual_WithinTolerance_ReturnsTrue()
    {
        // Arrange - 3% difference
        var amount1 = 1.00m;
        var amount2 = 1.03m;
        
        // Act
        var result = CurrencyFormatter.AreEqual(amount1, amount2, tolerancePercent: 5m);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void AreEqual_OutsideTolerance_ReturnsFalse()
    {
        // Arrange - 10% difference
        var amount1 = 1.00m;
        var amount2 = 1.10m;
        
        // Act
        var result = CurrencyFormatter.AreEqual(amount1, amount2, tolerancePercent: 5m);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void AreEqual_BothZero_ReturnsTrue()
    {
        // Act
        var result = CurrencyFormatter.AreEqual(0m, 0m);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void AreEqual_OneZero_ReturnsFalse()
    {
        // Act
        var result = CurrencyFormatter.AreEqual(0m, 1.00m);
        
        // Assert
        Assert.False(result);
    }
}
