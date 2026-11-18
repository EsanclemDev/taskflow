using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TaskManager.Api.Tests;

public class TaskDetailsApiIntegrationTests : IClassFixture<TaskManagerWebApplicationFactory>
{
    // Un host y una SQLite en memoria por clase: los tests de xUnit
    // de la misma clase no se ejecutan en paralelo. Cada test crea
    // usuarios distintos y mantiene el aislamiento por UserId.
    private readonly TaskManagerWebApplicationFactory _factory;

    public TaskDetailsApiIntegrationTests(TaskManagerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateTask_ShouldSaveDetailsAndCreatorAssignee()
    {
        var factory = _factory;
        using var client = factory.CreateHttpsClient();
        var email = await ApiTestHelpers.RegisterAndLoginAsync(client);

        var response = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Preparar pruebas de API",
            description = "Comprobar nuevas propiedades",
            priority = "high",
            category = "backend",
            dueDate = "2026-12-10"
        });
        await ApiTestHelpers.AssertStatusAsync(response, HttpStatusCode.Created);
        using var json = await ApiTestHelpers.ReadJsonAsync(response);
        var task = json.RootElement;
        Assert.Equal("high", task.GetProperty("priority").GetString());
        Assert.Equal("backend", task.GetProperty("category").GetString());
        Assert.Equal("2026-12-10", task.GetProperty("dueDate").GetString());
        Assert.Equal("pending", task.GetProperty("status").GetString());
        Assert.False(task.GetProperty("isCompleted").GetBoolean());
        Assert.Equal(email, task.GetProperty("createdByUserName").GetString());
        Assert.Equal(email, task.GetProperty("assignedToUserName").GetString());
        Assert.Equal(task.GetProperty("userId").GetString(),
            task.GetProperty("createdByUserId").GetString());
        Assert.Equal(task.GetProperty("userId").GetString(),
            task.GetProperty("assignedToUserId").GetString());
        Assert.True(task.GetProperty("updatedAt").GetDateTime() >=
            task.GetProperty("createdAt").GetDateTime());
        Assert.False(task.TryGetProperty("createdByUser", out _));
        Assert.False(task.TryGetProperty("assignedToUser", out _));
    }

    [Fact]
    public async Task CreateTask_WithoutDetails_ShouldUseDefaults()
    {
        var factory = _factory;
        using var client = factory.CreateHttpsClient();
        await ApiTestHelpers.RegisterAndLoginAsync(client);
        using var response = await client.PostAsJsonAsync("/api/tasks",
            new { title = "Default values" });
        using var json = await ApiTestHelpers.ReadJsonAsync(response);
        Assert.Equal("medium", json.RootElement.GetProperty("priority").GetString());
        Assert.Equal("other", json.RootElement.GetProperty("category").GetString());
        Assert.Equal(JsonValueKind.Null,
            json.RootElement.GetProperty("dueDate").ValueKind);
    }

    [Theory]
    [InlineData("invalid", "backend")]
    [InlineData("high", "sales")]
    public async Task CreateTask_WithInvalidDetails_ShouldReturnBadRequest(
        string priority, string category)
    {
        var factory = _factory;
        using var client = factory.CreateHttpsClient();
        await ApiTestHelpers.RegisterAndLoginAsync(client);
        using var response = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Invalid values", priority, category
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_ShouldKeepDetailsWhenOmittedAndClearDueDate()
    {
        var factory = _factory;
        using var client = factory.CreateHttpsClient();
        await ApiTestHelpers.RegisterAndLoginAsync(client);
        using var create = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Original task", priority = "high",
            category = "frontend", dueDate = "2026-12-10"
        });
        using var created = await ApiTestHelpers.ReadJsonAsync(create);
        var id = created.RootElement.GetProperty("id").GetInt32();
        var originalUpdated = created.RootElement.GetProperty("updatedAt").GetDateTime();

        await Task.Delay(30);
        using var update = await client.PutAsJsonAsync($"/api/tasks/{id}", new
        {
            title = "Updated task", description = "Keeps details", isCompleted = true
        });
        await ApiTestHelpers.AssertStatusAsync(update, HttpStatusCode.NoContent);
        using var get = await client.GetAsync($"/api/tasks/{id}");
        using var afterUpdate = await ApiTestHelpers.ReadJsonAsync(get);
        Assert.Equal("high", afterUpdate.RootElement.GetProperty("priority").GetString());
        Assert.Equal("frontend", afterUpdate.RootElement.GetProperty("category").GetString());
        Assert.Equal("2026-12-10", afterUpdate.RootElement.GetProperty("dueDate").GetString());
        Assert.Equal("completed", afterUpdate.RootElement.GetProperty("status").GetString());
        Assert.True(afterUpdate.RootElement.GetProperty("isCompleted").GetBoolean());
        Assert.True(afterUpdate.RootElement.GetProperty("updatedAt").GetDateTime() > originalUpdated);

        using var clear = await client.PutAsJsonAsync($"/api/tasks/{id}", new
        {
            title = "Updated task", priority = "urgent", clearDueDate = true
        });
        await ApiTestHelpers.AssertStatusAsync(clear, HttpStatusCode.NoContent);
        using var getAfterClear = await client.GetAsync($"/api/tasks/{id}");
        using var afterClear = await ApiTestHelpers.ReadJsonAsync(getAfterClear);
        Assert.Equal("urgent", afterClear.RootElement.GetProperty("priority").GetString());
        Assert.Equal(JsonValueKind.Null, afterClear.RootElement.GetProperty("dueDate").ValueKind);
    }

    [Fact]
    public async Task PatchStatus_ShouldKeepCompletionInSync()
    {
        var factory = _factory;
        using var client = factory.CreateHttpsClient();
        await ApiTestHelpers.RegisterAndLoginAsync(client);
        using var create = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Kanban test"
        });
        using var created = await ApiTestHelpers.ReadJsonAsync(create);
        var id = created.RootElement.GetProperty("id").GetInt32();

        foreach (var (status, completed) in new[]
        {
            ("in_progress", false), ("completed", true), ("pending", false)
        })
        {
            using var patch = await client.PatchAsJsonAsync(
                $"/api/tasks/{id}/status", new { status });
            await ApiTestHelpers.AssertStatusAsync(patch, HttpStatusCode.NoContent);
            using var get = await client.GetAsync($"/api/tasks/{id}");
            using var json = await ApiTestHelpers.ReadJsonAsync(get);
            Assert.Equal(status, json.RootElement.GetProperty("status").GetString());
            Assert.Equal(completed, json.RootElement.GetProperty("isCompleted").GetBoolean());
        }
    }

    [Fact]
    public async Task OtherUser_CannotReadModifyOrDeleteTasks()
    {
        var factory = _factory;
        using var owner = factory.CreateHttpsClient();
        using var outsider = factory.CreateHttpsClient();
        await ApiTestHelpers.RegisterAndLoginAsync(owner);
        await ApiTestHelpers.RegisterAndLoginAsync(outsider);

        using var create = await owner.PostAsJsonAsync("/api/tasks", new
        {
            title = "Private task"
        });
        using var json = await ApiTestHelpers.ReadJsonAsync(create);
        var id = json.RootElement.GetProperty("id").GetInt32();

        using var get = await outsider.GetAsync($"/api/tasks/{id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        using var put = await outsider.PutAsJsonAsync($"/api/tasks/{id}", new
        {
            title = "Unauthorized edit"
        });
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        using var patch = await outsider.PatchAsJsonAsync(
            $"/api/tasks/{id}/status", new { status = "completed" });
        Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
        using var delete = await outsider.DeleteAsync($"/api/tasks/{id}");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        using var outsiderList = await outsider.GetAsync("/api/tasks");
        using var outsiderJson = await ApiTestHelpers.ReadJsonAsync(outsiderList);
        Assert.Equal(0, outsiderJson.RootElement.GetArrayLength());
        using var ownerGet = await owner.GetAsync($"/api/tasks/{id}");
        Assert.Equal(HttpStatusCode.OK, ownerGet.StatusCode);
    }
}
