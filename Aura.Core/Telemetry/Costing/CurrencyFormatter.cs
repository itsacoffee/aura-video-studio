using System;
using System.Globalization;

namespace Aura.Core.Telemetry.Costing;

/// <summary>
/// Centralized currency formatter for consistent cost display across the application
/// Handles formatting, rounding, and locale-aware display
/// </summary>
public static class CurrencyFormatter
{
    /// <summary>
    /// Default currency code used when none is specified
    /// </summary>
    public const string DefaultCurrency = "USD";
    
    /// <summary>
    /// Format a cost value with currency symbol and appropriate decimal places
    /// </summary>
    /// <param name="amount">Amount to format</param>
    /// <param name="currencyCode">ISO 4217 currency code (e.g., "USD", "EUR", "GBP")</param>
    /// <param name="decimalPlaces">Number of decimal places to show (default: 4 for precise cost tracking)</param>
    /// <returns>Formatted string like "$0.0023" or "€1.2500"</returns>
    public static string Format(decimal amount, string currencyCode = DefaultCurrency, int decimalPlaces = 4)
    {
        var symbol = GetCurrencySymbol(currencyCode);
        var rounded = Math.Round(amount, decimalPlaces);
        
        return $"{symbol}{rounded.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}";
    }
    
    /// <summary>
    /// Format a cost value with full currency code instead of symbol
    /// </summary>
    /// <param name="amount">Amount to format</param>
    /// <param name="currencyCode">ISO 4217 currency code</param>
    /// <param name="decimalPlaces">Number of decimal places to show</param>
    /// <returns>Formatted string like "USD 0.0023" or "EUR 1.2500"</returns>
    public static string FormatWithCode(decimal amount, string currencyCode = DefaultCurrency, int decimalPlaces = 4)
    {
        var rounded = Math.Round(amount, decimalPlaces);
        return $"{currencyCode} {rounded.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)}";
    }
    
    /// <summary>
    /// Format a cost value for user display (shorter, more readable)
    /// Uses 2 decimal places for amounts >= 0.01, otherwise 4 decimal places
    /// </summary>
    /// <param name="amount">Amount to format</param>
    /// <param name="currencyCode">ISO 4217 currency code</param>
    /// <returns>Formatted string optimized for readability</returns>
    public static string FormatForDisplay(decimal amount, string currencyCode = DefaultCurrency)
    {
        var symbol = GetCurrencySymbol(currencyCode);
        
        // Use 2 decimal places for amounts >= 1 cent
        if (amount >= 0.01m)
        {
            var rounded = Math.Round(amount, 2);
            return $"{symbol}{rounded.ToString("F2", CultureInfo.InvariantCulture)}";
        }
        
        // Use 4 decimal places for very small amounts
        var preciseRounded = Math.Round(amount, 4);
        return $"{symbol}{preciseRounded.ToString("F4", CultureInfo.InvariantCulture)}";
    }
    
    /// <summary>
    /// Get currency symbol for a given currency code
    /// </summary>
    public static string GetCurrencySymbol(string currencyCode)
    {
        return currencyCode.ToUpperInvariant() switch
        {
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            "JPY" => "¥",
            "CNY" => "¥",
            "INR" => "₹",
            "AUD" => "A$",
            "CAD" => "C$",
            "CHF" => "Fr",
            "SEK" => "kr",
            "NZD" => "NZ$",
            _ => currencyCode + " "
        };
    }
    
    /// <summary>
    /// Parse a formatted currency string back to decimal
    /// Handles various formats like "$0.0023", "USD 0.0023", "0.0023"
    /// </summary>
    public static decimal Parse(string formattedAmount)
    {
        if (string.IsNullOrWhiteSpace(formattedAmount))
            return 0m;
            
        // Remove common currency symbols and whitespace
        var cleaned = formattedAmount
            .Replace("$", "")
            .Replace("€", "")
            .Replace("£", "")
            .Replace("¥", "")
            .Replace("₹", "")
            .Replace("Fr", "")
            .Replace("kr", "")
            .Trim();
        
        // Remove currency codes (3 uppercase letters at start)
        if (cleaned.Length > 3 && char.IsLetter(cleaned[0]))
        {
            var potentialCode = cleaned.Substring(0, 3);
            if (potentialCode.All(char.IsUpper))
            {
                cleaned = cleaned.Substring(3).Trim();
            }
        }
        
        if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        
        throw new FormatException($"Cannot parse '{formattedAmount}' as a currency amount");
    }
    
    /// <summary>
    /// Check if two currency amounts are equal within an acceptable tolerance
    /// (useful for comparing estimated vs actual costs)
    /// </summary>
    /// <param name="amount1">First amount</param>
    /// <param name="amount2">Second amount</param>
    /// <param name="tolerancePercent">Acceptable difference percentage (default: 5%)</param>
    /// <returns>True if amounts are within tolerance</returns>
    public static bool AreEqual(decimal amount1, decimal amount2, decimal tolerancePercent = 5m)
    {
        if (amount1 == 0m && amount2 == 0m)
            return true;
            
        if (amount1 == 0m || amount2 == 0m)
            return false;
            
        var larger = Math.Max(amount1, amount2);
        var smaller = Math.Min(amount1, amount2);
        var percentDiff = ((larger - smaller) / larger) * 100m;
        
        return percentDiff <= tolerancePercent;
    }
}
