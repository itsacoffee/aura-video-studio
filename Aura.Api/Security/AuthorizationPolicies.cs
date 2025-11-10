using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.Api.Security;

/// <summary>
/// Defines authorization policies for the application
/// </summary>
public static class AuthorizationPolicies
{
    // Policy names
    public const string RequireAdminRole = "RequireAdmin";
    public const string RequireUserRole = "RequireUser";
    public const string RequireApiKey = "RequireApiKey";
    public const string RequireValidatedProvider = "RequireValidatedProvider";
    public const string AllowAnonymous = "AllowAnonymous";

    // Claim types
    public const string ApiKeyClaimType = "apikey";
    public const string UserIdClaimType = "sub";
    public const string RoleClaimType = "role";
    public const string ProviderClaimType = "provider";

    /// <summary>
    /// Configures authorization policies
    /// </summary>
    public static void ConfigureAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Admin policy - requires admin role
            options.AddPolicy(RequireAdminRole, policy =>
                policy.RequireRole("Admin", "Administrator"));

            // User policy - requires user or admin role
            options.AddPolicy(RequireUserRole, policy =>
                policy.RequireRole("User", "Admin", "Administrator"));

            // API key policy - requires valid API key
            options.AddPolicy(RequireApiKey, policy =>
                policy.RequireClaim(ApiKeyClaimType));

            // Validated provider policy - requires provider validation
            options.AddPolicy(RequireValidatedProvider, policy =>
                policy.RequireClaim(ProviderClaimType));

            // Default policy - require authentication
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Fallback policy - for endpoints without [Authorize] attribute
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
    }
}

/// <summary>
/// Custom authorization requirement for resource-based authorization
/// </summary>
public class ResourceOwnerRequirement : IAuthorizationRequirement
{
    public string ResourceType { get; }

    public ResourceOwnerRequirement(string resourceType)
    {
        ResourceType = resourceType;
    }
}

/// <summary>
/// Handler for resource owner authorization
/// </summary>
public class ResourceOwnerAuthorizationHandler : AuthorizationHandler<ResourceOwnerRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerRequirement requirement)
    {
        // Get user ID from claims
        var userId = context.User.FindFirst(AuthorizationPolicies.UserIdClaimType)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }

        // In a real application, you would check if the user owns the resource
        // For now, we'll just check if the user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Custom authorization requirement for rate limiting bypass
/// </summary>
public class RateLimitBypassRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Handler for rate limit bypass authorization
/// </summary>
public class RateLimitBypassAuthorizationHandler : AuthorizationHandler<RateLimitBypassRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RateLimitBypassRequirement requirement)
    {
        // Only admins can bypass rate limits
        if (context.User.IsInRole("Admin") || context.User.IsInRole("Administrator"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
