using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;

namespace Aura.Cli.Commands;

/// <summary>
/// CLI command for managing API keys
/// Commands: set, list, test, rotate, delete, export, import
/// </summary>
public class KeysCommand : ICommand
{
    private readonly ILogger<KeysCommand> _logger;
    private readonly ISecureStorageService _secureStorage;
    private readonly IKeyValidationService _keyValidator;

    public KeysCommand(
        ILogger<KeysCommand> logger,
        ISecureStorageService secureStorage,
        IKeyValidationService keyValidator)
    {
        _logger = logger;
        _secureStorage = secureStorage;
        _keyValidator = keyValidator;
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return ExitCodes.InvalidArguments;
        }

        var subCommand = args[0].ToLowerInvariant();

        try
        {
            return subCommand switch
            {
                "set" => await SetKeyAsync(args.Skip(1).ToArray()),
                "list" => await ListKeysAsync(),
                "test" => await TestKeyAsync(args.Skip(1).ToArray()),
                "rotate" => await RotateKeyAsync(args.Skip(1).ToArray()),
                "delete" => await DeleteKeyAsync(args.Skip(1).ToArray()),
                "export" => await ExportKeysAsync(args.Skip(1).ToArray()),
                "import" => await ImportKeysAsync(args.Skip(1).ToArray()),
                "help" or "--help" or "-h" => ShowHelpAndExit(),
                _ => ShowErrorAndHelp($"Unknown subcommand: {subCommand}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing keys command: {SubCommand}", subCommand);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.UnhandledException;
        }
    }

    private async Task<int> SetKeyAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Error: 'set' requires provider name and API key");
            Console.Error.WriteLine("Usage: aura keys set <provider> <api-key>");
            Console.Error.WriteLine("Example: aura keys set openai sk-proj-abc123...");
            return ExitCodes.InvalidArguments;
        }

        var provider = args[0];
        var apiKey = args[1];

        try
        {
            Console.WriteLine($"Setting API key for provider: {provider}");
            
            await _secureStorage.SaveApiKeyAsync(provider, apiKey);
            
            var masked = SecretMaskingService.MaskApiKey(apiKey);
            Console.WriteLine($"✓ API key saved securely: {masked}");
            Console.WriteLine($"  Provider: {provider}");
            Console.WriteLine($"  Status: Encrypted at rest");
            
            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting API key for {Provider}", provider);
            Console.Error.WriteLine($"Error saving API key: {ex.Message}");
            return ExitCodes.RuntimeError;
        }
    }

    private async Task<int> ListKeysAsync()
    {
        try
        {
            var providers = await _secureStorage.GetConfiguredProvidersAsync();

            if (providers.Count == 0)
            {
                Console.WriteLine("No API keys configured.");
                Console.WriteLine("Use 'aura keys set <provider> <api-key>' to add a key.");
                return ExitCodes.Success;
            }

            Console.WriteLine($"Configured providers ({providers.Count}):");
            Console.WriteLine();

            foreach (var provider in providers.OrderBy(p => p))
            {
                var apiKey = await _secureStorage.GetApiKeyAsync(provider);
                var masked = SecretMaskingService.MaskApiKey(apiKey);
                Console.WriteLine($"  • {provider,-15} {masked}");
            }

            Console.WriteLine();
            Console.WriteLine("Use 'aura keys test <provider>' to validate a key.");
            
            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing API keys");
            Console.Error.WriteLine($"Error listing API keys: {ex.Message}");
            return ExitCodes.RuntimeError;
        }
    }

    private async Task<int> TestKeyAsync(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Error: 'test' requires provider name");
            Console.Error.WriteLine("Usage: aura keys test <provider>");
            Console.Error.WriteLine("Example: aura keys test openai");
            return ExitCodes.InvalidArguments;
        }

        var provider = args[0];

        try
        {
            var apiKey = await _secureStorage.GetApiKeyAsync(provider);
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.Error.WriteLine($"Error: No API key configured for {provider}");
                Console.Error.WriteLine($"Use 'aura keys set {provider} <api-key>' to add a key.");
                return ExitCodes.InvalidConfiguration;
            }

            Console.WriteLine($"Testing API key for provider: {provider}");
            Console.WriteLine($"Key: {SecretMaskingService.MaskApiKey(apiKey)}");
            Console.WriteLine();

            var result = await _keyValidator.TestApiKeyAsync(provider, apiKey, default);

            if (result.IsValid)
            {
                Console.WriteLine($"✓ {result.Message}");
                
                if (result.Details.Count > 0)
                {
                    Console.WriteLine("  Details:");
                    foreach (var detail in result.Details)
                    {
                        Console.WriteLine($"    {detail.Key}: {detail.Value}");
                    }
                }
                
                return ExitCodes.Success;
            }
            else
            {
                Console.Error.WriteLine($"✗ {result.Message}");
                
                if (result.Details.Count > 0)
                {
                    Console.Error.WriteLine("  Details:");
                    foreach (var detail in result.Details)
                    {
                        Console.Error.WriteLine($"    {detail.Key}: {detail.Value}");
                    }
                }
                
                return ExitCodes.RuntimeError;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing API key for {Provider}", provider);
            Console.Error.WriteLine($"Error testing API key: {ex.Message}");
            return ExitCodes.RuntimeError;
        }
    }

    private async Task<int> RotateKeyAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Error: 'rotate' requires provider name and new API key");
            Console.Error.WriteLine("Usage: aura keys rotate <provider> <new-api-key>");
            Console.Error.WriteLine("Example: aura keys rotate openai sk-proj-new123...");
            return ExitCodes.InvalidArguments;
        }

        var provider = args[0];
        var newApiKey = args[1];

        try
        {
            var hasExisting = await _secureStorage.HasApiKeyAsync(provider);
            
            if (!hasExisting)
            {
                Console.Error.WriteLine($"Error: No existing API key found for {provider}");
                Console.Error.WriteLine($"Use 'aura keys set {provider} <api-key>' to add a new key.");
                return ExitCodes.InvalidConfiguration;
            }

            Console.WriteLine($"Rotating API key for provider: {provider}");
            Console.Write("Testing new key... ");

            var testResult = await _keyValidator.TestApiKeyAsync(provider, newApiKey, default);
            
            if (!testResult.IsValid)
            {
                Console.WriteLine("✗ FAILED");
                Console.Error.WriteLine($"Error: New API key failed validation: {testResult.Message}");
                Console.Error.WriteLine("Key was not rotated. Please verify the new key and try again.");
                return ExitCodes.RuntimeError;
            }

            Console.WriteLine("✓ OK");
            Console.Write("Saving new key... ");

            await _secureStorage.SaveApiKeyAsync(provider, newApiKey);
            
            Console.WriteLine("✓ DONE");
            Console.WriteLine();
            Console.WriteLine($"✓ API key rotated successfully");
            Console.WriteLine($"  Provider: {provider}");
            Console.WriteLine($"  New key: {SecretMaskingService.MaskApiKey(newApiKey)}");
            
            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating API key for {Provider}", provider);
            Console.Error.WriteLine($"Error rotating API key: {ex.Message}");
            return ExitCodes.RuntimeError;
        }
    }

    private async Task<int> DeleteKeyAsync(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Error: 'delete' requires provider name");
            Console.Error.WriteLine("Usage: aura keys delete <provider>");
            Console.Error.WriteLine("Example: aura keys delete openai");
            return ExitCodes.InvalidArguments;
        }

        var provider = args[0];

        try
        {
            var hasKey = await _secureStorage.HasApiKeyAsync(provider);
            
            if (!hasKey)
            {
                Console.Error.WriteLine($"Error: No API key found for {provider}");
                return ExitCodes.InvalidConfiguration;
            }

            Console.Write($"Delete API key for {provider}? (y/N): ");
            var confirmation = Console.ReadLine()?.Trim().ToLowerInvariant();
            
            if (confirmation != "y" && confirmation != "yes")
            {
                Console.WriteLine("Cancelled.");
                return ExitCodes.Success;
            }

            await _secureStorage.DeleteApiKeyAsync(provider);
            
            Console.WriteLine($"✓ API key deleted for {provider}");
            
            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API key for {Provider}", provider);
            Console.Error.WriteLine($"Error deleting API key: {ex.Message}");
            return ExitCodes.RuntimeError;
        }
    }

    private async Task<int> ExportKeysAsync(string[] args)
    {
        Console.Error.WriteLine("Error: Export functionality not yet implemented");
        Console.Error.WriteLine("This feature will be added in a future update.");
        return ExitCodes.NotImplemented;
    }

    private async Task<int> ImportKeysAsync(string[] args)
    {
        Console.Error.WriteLine("Error: Import functionality not yet implemented");
        Console.Error.WriteLine("This feature will be added in a future update.");
        return ExitCodes.NotImplemented;
    }

    private void ShowHelp()
    {
        Console.WriteLine("Aura Keys Management");
        Console.WriteLine();
        Console.WriteLine("Securely manage API keys for external providers");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  aura keys <subcommand> [options]");
        Console.WriteLine();
        Console.WriteLine("SUBCOMMANDS:");
        Console.WriteLine("  set <provider> <key>    Set or update an API key");
        Console.WriteLine("  list                    List all configured providers");
        Console.WriteLine("  test <provider>         Test a provider's API key");
        Console.WriteLine("  rotate <provider> <key> Rotate an existing API key");
        Console.WriteLine("  delete <provider>       Delete an API key");
        Console.WriteLine("  export [options]        Export configuration (coming soon)");
        Console.WriteLine("  import <file>           Import configuration (coming soon)");
        Console.WriteLine("  help                    Show this help message");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  aura keys set openai sk-proj-abc123...");
        Console.WriteLine("  aura keys list");
        Console.WriteLine("  aura keys test openai");
        Console.WriteLine("  aura keys rotate openai sk-proj-new456...");
        Console.WriteLine("  aura keys delete elevenlabs");
        Console.WriteLine();
        Console.WriteLine("SUPPORTED PROVIDERS:");
        Console.WriteLine("  openai, anthropic, gemini, google, elevenlabs,");
        Console.WriteLine("  stabilityai, stability, playht, azure");
        Console.WriteLine();
        Console.WriteLine("SECURITY:");
        Console.WriteLine("  • All keys are encrypted at rest");
        Console.WriteLine("  • Windows: DPAPI encryption (CurrentUser scope)");
        Console.WriteLine("  • Linux/macOS: AES-256 encryption with machine key");
        Console.WriteLine("  • Keys are never logged or displayed in full");
    }

    private int ShowHelpAndExit()
    {
        ShowHelp();
        return ExitCodes.Success;
    }

    private int ShowErrorAndHelp(string error)
    {
        Console.Error.WriteLine($"Error: {error}");
        Console.Error.WriteLine();
        ShowHelp();
        return ExitCodes.InvalidArguments;
    }
}
