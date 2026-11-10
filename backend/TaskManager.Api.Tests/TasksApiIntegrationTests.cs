
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TaskManager.Api.Tests;

public class TasksApiIntegrationTests
{
    [Fact]
    public async Task GetTasks_ShouldReturnOk()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_ShouldReturnSuccess()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var client = factory.CreateClient();

        var task = new
        {
            title = "Integration test task",
            description = "Created from xUnit"
        };

        var response = await client.PostAsJsonAsync("/api/tasks", task);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CreateTask_WithInvalidTitle_ShouldReturnBadRequest()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var client = factory.CreateClient();

        var task = new
        {
            title = "AB",
            description = "Invalid task"
        };

        var response = await client.PostAsJsonAsync("/api/tasks", task);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Task_ShouldSupportFullCrudLifecycle()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var client = factory.CreateClient();

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

        var id = created.RootElement.GetProperty("id").GetInt32();

        // READ
        var getResponse = await client.GetAsync($"/api/tasks/{id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

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
        var deleteResponse = await client.DeleteAsync($"/api/tasks/{id}");

        Assert.True(deleteResponse.IsSuccessStatusCode);

        // VERIFY DELETION
        var deletedResponse = await client.GetAsync($"/api/tasks/{id}");

        Assert.Equal(HttpStatusCode.NotFound, deletedResponse.StatusCode);
    }
}
