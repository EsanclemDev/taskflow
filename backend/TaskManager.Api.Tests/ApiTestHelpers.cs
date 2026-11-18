using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TaskManager.Api.Tests;

internal static class ApiTestHelpers
{
    private const string Password = "TaskFlow2026!";

    public static async Task<string> RegisterAndLoginAsync(HttpClient client)
    {
        var email = $"user-{Guid.NewGuid():N}@taskflow.local";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = Password,
            confirmPassword = Password
        });
        await AssertStatusAsync(register, HttpStatusCode.Created);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = Password
        });
        await ReadAndAuthorizeAsync(client, login);
        return email;
    }

    public static async Task StartDemoAsync(HttpClient client)
    {
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "Origin", "http://localhost:4200");
        var response = await client.PostAsJsonAsync("/api/auth/demo", new { });
        await ReadAndAuthorizeAsync(client, response);
    }

    public static async Task ReadAndAuthorizeAsync(HttpClient client,
        HttpResponseMessage response)
    {
        await AssertStatusAsync(response, HttpStatusCode.OK);
        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode,
            $"HTTP {(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text);
    }

    public static async Task AssertStatusAsync(HttpResponseMessage response,
        HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected,
            $"Esperado {(int)expected}, recibido {(int)response.StatusCode}: {body}");
    }
}
