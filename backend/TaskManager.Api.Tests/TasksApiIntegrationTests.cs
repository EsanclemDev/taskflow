
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TaskManager.Api.Tests;

public class TasksApiIntegrationTests
{
    private static async Task AuthenticateAsync(
        HttpClient client)
    {
        var email =
            $"test-{Guid.NewGuid():N}@taskflow.local";

        const string password = "TaskFlow2026!";

        // REGISTER
        var registerResponse = await client.PostAsJsonAsync(
    "/api/auth/register",
    new
    {
        email,
        password,
        confirmPassword = password
    });

        var registerBody =
            await registerResponse.Content.ReadAsStringAsync();

        Assert.True(
            registerResponse.StatusCode == HttpStatusCode.Created,
            $"El registro devolvió {(int)registerResponse.StatusCode} " +
            $"({registerResponse.StatusCode}). Respuesta: {registerBody}"
        );

        // LOGIN
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email,
                password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using var loginJson = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var accessToken = loginJson.RootElement
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
    }

    [Fact]
    public async Task GetTasks_ShouldReturnOk()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_ShouldReturnSuccess()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var task = new
        {
            title = "Integration test task",
            description = "Created from xUnit"
        };

        var response = await client.PostAsJsonAsync(
            "/api/tasks",
            task);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CreateTask_WithInvalidTitle_ShouldReturnBadRequest()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var task = new
        {
            title = "AB",
            description = "Invalid task"
        };

        var response = await client.PostAsJsonAsync(
            "/api/tasks",
            task);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Task_ShouldSupportFullCrudLifecycle()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        // CREATE
        var createResponse = await client.PostAsJsonAsync(
            "/api/tasks",
            new
            {
                title = "Original task",
                description = "Before update"
            });

        Assert.True(createResponse.IsSuccessStatusCode);

        using var created = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        var id = created.RootElement
            .GetProperty("id")
            .GetInt32();

        // READ
        var getResponse = await client.GetAsync(
            $"/api/tasks/{id}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        // UPDATE
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/tasks/{id}",
            new
            {
                title = "Updated task",
                description = "After update",
                isCompleted = true
            });

        Assert.True(updateResponse.IsSuccessStatusCode);

        // DELETE
        var deleteResponse = await client.DeleteAsync(
            $"/api/tasks/{id}");

        Assert.True(deleteResponse.IsSuccessStatusCode);

        // VERIFY DELETION
        var deletedResponse = await client.GetAsync(
            $"/api/tasks/{id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            deletedResponse.StatusCode);
    }
}
