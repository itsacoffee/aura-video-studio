using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Base class for API integration tests with HTTP helpers
/// </summary>
public abstract class ApiIntegrationTestBase : IntegrationTestBase
{
    protected readonly JsonSerializerOptions JsonOptions;

    protected ApiIntegrationTestBase(WebApplicationFactory<Program> factory) : base(factory)
    {
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// GET request helper
    /// </summary>
    protected async Task<TResponse?> GetAsync<TResponse>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    /// <summary>
    /// POST request helper
    /// </summary>
    protected async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest request)
    {
        var response = await Client.PostAsJsonAsync(url, request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    /// <summary>
    /// PUT request helper
    /// </summary>
    protected async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest request)
    {
        var response = await Client.PutAsJsonAsync(url, request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    /// <summary>
    /// DELETE request helper
    /// </summary>
    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        var response = await Client.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
        return response;
    }

    /// <summary>
    /// Assert that endpoint returns expected status code
    /// </summary>
    protected async Task AssertStatusCode(string url, System.Net.HttpStatusCode expectedStatus)
    {
        var response = await Client.GetAsync(url);
        Assert.Equal(expectedStatus, response.StatusCode);
    }
}
