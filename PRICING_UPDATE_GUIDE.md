# LLM Pricing Update Guide

## Overview

The LLM cost estimation system now uses a **dynamic JSON configuration file** that can be updated without code changes. This allows pricing to stay current as providers update their rates or introduce new models.

## Configuration File Location

The pricing configuration is stored in:
```
Aura.Core/Configuration/llm-pricing.json
```

## Updating Pricing

### 1. Check Current Pricing

Visit the official pricing pages:
- **OpenAI**: https://openai.com/api/pricing/
- **Anthropic**: https://www.anthropic.com/pricing
- **Google Gemini**: https://ai.google.dev/pricing
- **Azure OpenAI**: https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/

### 2. Update the JSON File

Edit `llm-pricing.json` with the new rates:

```json
{
  "version": "2024.12",
  "lastUpdated": "2024-12-15",
  "providers": {
    "openai": {
      "name": "OpenAI",
      "models": {
        "gpt-4o-mini": {
          "inputPrice": 0.150,    // ← Update these values
          "outputPrice": 0.600,   // ← Per 1M tokens in USD
          "contextWindow": 128000,
          "description": "Fast and affordable, good for most tasks"
        }
      }
    }
  }
}
```

### 3. Update Version and Date

```json
{
  "version": "2024.12",        // ← Increment version (YYYY.MM format)
  "lastUpdated": "2024-12-15", // ← Update to current date
  ...
}
```

### 4. Validate JSON

Ensure the JSON is valid:
```bash
# Using jq (if installed)
jq . llm-pricing.json

# Or validate online at https://jsonlint.com/
```

### 5. Test the Changes

```csharp
var estimator = new LlmCostEstimator(logger);

// Verify version updated
Console.WriteLine($"Config version: {estimator.GetConfigVersion()}");
Console.WriteLine($"Last updated: {estimator.GetConfigLastUpdated()}");

// Test cost calculation
var estimate = estimator.CalculateCost(1000, 1000, "gpt-4o-mini");
Console.WriteLine($"Cost: ${estimate.TotalCost:F6}");
```

## Adding New Models

### 1. Add to Appropriate Provider

```json
{
  "providers": {
    "openai": {
      "models": {
        "gpt-5": {                    // ← New model
          "inputPrice": 5.00,         // ← Set pricing
          "outputPrice": 15.00,
          "contextWindow": 200000,    // ← Set context window
          "description": "Next-generation model"
        }
      }
    }
  }
}
```

### 2. Include Experimental/Free Models

For experimental or free models during preview:

```json
{
  "gemini-2.0-flash-exp": {
    "inputPrice": 0.00,           // ← Free during preview
    "outputPrice": 0.00,
    "contextWindow": 1000000,
    "description": "Experimental model (free during preview)"
  }
}
```

## Adding New Providers

### 1. Add Provider Section

```json
{
  "providers": {
    "mistral": {                    // ← New provider
      "name": "Mistral AI",
      "note": "Pricing in USD per 1M tokens",
      "models": {
        "mistral-large": {
          "inputPrice": 4.00,
          "outputPrice": 12.00,
          "contextWindow": 32000,
          "description": "Most capable Mistral model"
        }
      }
    }
  }
}
```

## Configuration Schema

The configuration follows this schema:

```typescript
{
  version: string;           // Format: YYYY.MM
  lastUpdated: string;       // Format: YYYY-MM-DD
  description?: string;
  providers: {
    [providerKey: string]: {
      name: string;
      note?: string;
      models: {
        [modelKey: string]: {
          inputPrice: number;      // USD per 1M tokens
          outputPrice: number;     // USD per 1M tokens
          contextWindow?: number;  // Tokens
          description?: string;
        }
      }
    }
  };
  fallbackModel: {
    inputPrice: number;
    outputPrice: number;
    description: string;
  };
}
```

## Hot Reloading

The system checks for configuration updates every 5 minutes. To force immediate reload:

1. **Restart the application** (recommended)
2. **Wait up to 5 minutes** for automatic detection

## Fallback Behavior

If the configuration file is missing or invalid:
- The system uses built-in fallback pricing (gpt-4o-mini rates)
- A warning is logged
- The application continues to function

## Pricing Validation

After updating pricing:

```csharp
// Test with various models
var testModels = new[] { "gpt-4o", "gpt-4o-mini", "claude-3-5-sonnet-20241022" };

foreach (var model in testModels)
{
    var estimate = estimator.CalculateCost(1000, 1000, model);
    Console.WriteLine($"{model}: ${estimate.TotalCost:F6}");
}

// Compare with expected values
```

## Common Updates

### Monthly Routine Check
```bash
# 1. Check provider websites for updates
# 2. Update llm-pricing.json
# 3. Increment version number
# 4. Test with sample calculations
# 5. Commit changes
git add Aura.Core/Configuration/llm-pricing.json
git commit -m "Update LLM pricing for [Month Year]"
```

### New Model Launch
```bash
# When a provider launches a new model:
# 1. Add model to appropriate provider section
# 2. Include current pricing
# 3. Set context window and description
# 4. Test estimation with new model
```

## Monitoring Pricing Changes

### Set Up Alerts

Create a calendar reminder to check pricing monthly:
- First Monday of each month
- Check all provider pricing pages
- Update configuration if changed
- Test calculations

### Automated Checks (Optional)

Create a script to detect pricing updates:

```bash
#!/bin/bash
# check-pricing-updates.sh

# Check OpenAI pricing page for changes
curl -s https://openai.com/api/pricing/ | sha256sum > /tmp/openai-pricing.hash

# Compare with stored hash
if diff /tmp/openai-pricing.hash /var/cache/openai-pricing.hash; then
    echo "No pricing changes detected"
else
    echo "⚠️  Pricing may have changed - manual review needed"
    # Send notification
fi
```

## Troubleshooting

### Configuration Not Loading

1. Check file exists at expected path
2. Validate JSON syntax
3. Check application logs for errors
4. Verify file permissions

### Incorrect Cost Calculations

1. Verify pricing values are per 1M tokens (not per 1K)
2. Check decimal places (e.g., 0.150 not 150)
3. Ensure model name matches configuration keys
4. Test with known values

### Model Not Found

If you get "Unknown model" warnings:

1. Check model name spelling in configuration
2. Add model to appropriate provider
3. Verify model name normalization

## Example: Complete Update Process

```bash
# 1. Check current version
grep '"version"' Aura.Core/Configuration/llm-pricing.json

# 2. Check provider websites for updates
# (OpenAI, Anthropic, Google, etc.)

# 3. Edit configuration file
vim Aura.Core/Configuration/llm-pricing.json

# 4. Update version and date
# "version": "2024.12" → "2025.01"
# "lastUpdated": "2024-12-01" → "2025-01-15"

# 5. Validate JSON
jq . Aura.Core/Configuration/llm-pricing.json > /dev/null && echo "Valid JSON"

# 6. Test changes
dotnet test --filter "LlmCostEstimatorTests"

# 7. Commit
git add Aura.Core/Configuration/llm-pricing.json
git commit -m "Update LLM pricing for January 2025"
```

## Best Practices

1. **Always update the version number** when making changes
2. **Update the lastUpdated date** to track when pricing was verified
3. **Add descriptions** for new models to help users understand capabilities
4. **Keep context windows accurate** for models that have limits
5. **Document pricing sources** in commit messages
6. **Test calculations** after updates to verify correctness
7. **Check all three major providers** (OpenAI, Anthropic, Google) monthly
8. **Set up reminders** to check pricing regularly

## Future Enhancements

Potential improvements to the pricing system:

- **Automatic fetching** from provider APIs (if available)
- **Price history tracking** for trend analysis
- **Alert system** for significant price changes
- **Regional pricing** support for Azure and other regional services
- **Currency conversion** for non-USD pricing
- **Batch discount** calculations for high-volume usage

---

**Last Updated**: December 2024  
**Maintained By**: Aura Development Team  
**Questions?**: Check provider documentation or open an issue
