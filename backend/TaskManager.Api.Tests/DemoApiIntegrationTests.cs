using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Api.Data;
using TaskManager.Api.Services;
using Xunit;

namespace TaskManager.Api.Tests;

public class DemoApiIntegrationTests
{
    [Fact]
    public async Task DemoLogin_ShouldCreateTwelveTasksWithExpectedStates()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var client = factory.CreateHttpsClient();
        await ApiTestHelpers.StartDemoAsync(client);

        using var meResponse = await client.GetAsync("/api/auth/me");
        using var me = await ApiTestHelpers.ReadJsonAsync(meResponse);
        Assert.True(me.RootElement.GetProperty("isDemo").GetBoolean());
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null,
            me.RootElement.GetProperty("demoExpiresAt").ValueKind);
        var userId = me.RootElement.GetProperty("id").GetString();

        using var response = await client.GetAsync("/api/tasks");
        using var json = await ApiTestHelpers.ReadJsonAsync(response);
        var tasks = json.RootElement.EnumerateArray().ToList();
        Assert.Equal(12, tasks.Count);
        Assert.Equal(4, tasks.Count(t => t.GetProperty("status").GetString() == "pending"));
        Assert.Equal(4, tasks.Count(t => t.GetProperty("status").GetString() == "in_progress"));
        Assert.Equal(4, tasks.Count(t => t.GetProperty("status").GetString() == "completed"));
        Assert.All(tasks, task =>
        {
            Assert.Equal(userId, task.GetProperty("userId").GetString());
            Assert.Equal(userId, task.GetProperty("createdByUserId").GetString());
            Assert.Equal(userId, task.GetProperty("assignedToUserId").GetString());
            Assert.False(string.IsNullOrWhiteSpace(
                task.GetProperty("priority").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(
                task.GetProperty("category").GetString()));
        });
    }

    [Fact]
    public async Task DemoReset_ShouldRestoreSamplesWithoutTouchingOtherDemo()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var demoA = factory.CreateHttpsClient();
        using var demoB = factory.CreateHttpsClient();
        await ApiTestHelpers.StartDemoAsync(demoA);
        await ApiTestHelpers.StartDemoAsync(demoB);

        using var initialAResponse = await demoA.GetAsync("/api/tasks");
        using var initialA = await ApiTestHelpers.ReadJsonAsync(initialAResponse);
        var firstId = initialA.RootElement[0].GetProperty("id").GetInt32();
        using var deleted = await demoA.DeleteAsync($"/api/tasks/{firstId}");
        await ApiTestHelpers.AssertStatusAsync(deleted, HttpStatusCode.NoContent);
        using var created = await demoA.PostAsJsonAsync("/api/tasks", new
        {
            title = "Task added by demo A"
        });
        await ApiTestHelpers.AssertStatusAsync(created, HttpStatusCode.Created);

        using var reset = await demoA.PostAsJsonAsync("/api/demo/reset", new { });
        await ApiTestHelpers.AssertStatusAsync(reset, HttpStatusCode.NoContent);

        using var aResponse = await demoA.GetAsync("/api/tasks");
        using var a = await ApiTestHelpers.ReadJsonAsync(aResponse);
        Assert.Equal(12, a.RootElement.GetArrayLength());
        Assert.DoesNotContain(a.RootElement.EnumerateArray(),
            t => t.GetProperty("title").GetString() == "Task added by demo A");

        using var bResponse = await demoB.GetAsync("/api/tasks");
        using var b = await ApiTestHelpers.ReadJsonAsync(bResponse);
        Assert.Equal(12, b.RootElement.GetArrayLength());
        Assert.NotEqual(a.RootElement[0].GetProperty("userId").GetString(),
            b.RootElement[0].GetProperty("userId").GetString());
    }

    [Fact]
    public async Task NormalUser_ShouldNotBeAllowedToResetDemo()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var client = factory.CreateHttpsClient();
        await ApiTestHelpers.RegisterAndLoginAsync(client);
        using var create = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "My own task"
        });
        await ApiTestHelpers.AssertStatusAsync(create, HttpStatusCode.Created);
        using var reset = await client.PostAsJsonAsync("/api/demo/reset", new { });
        Assert.Equal(HttpStatusCode.Forbidden, reset.StatusCode);
        using var get = await client.GetAsync("/api/tasks");
        using var tasks = await ApiTestHelpers.ReadJsonAsync(get);
        Assert.Equal(1, tasks.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task ExpiredDemoCleanup_ShouldRemoveOnlyExpiredDemo()
    {
        using var factory = new TaskManagerWebApplicationFactory();
        using var demo = factory.CreateHttpsClient();
        using var normal = factory.CreateHttpsClient();
        await ApiTestHelpers.StartDemoAsync(demo);
        await ApiTestHelpers.RegisterAndLoginAsync(normal);

        using var meResponse = await demo.GetAsync("/api/auth/me");
        using var me = await ApiTestHelpers.ReadJsonAsync(meResponse);
        var demoId = me.RootElement.GetProperty("id").GetString()!;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Users.Where(u => u.Id == demoId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    u => u.DemoExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
        }

        using (var scope = factory.Services.CreateScope())
        {
            var cleanup = scope.ServiceProvider.GetRequiredService<DemoAccountService>();
            Assert.Equal(1, await cleanup.DeleteExpiredAsync(CancellationToken.None));
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await db.Users.AnyAsync(u => u.Id == demoId));
            Assert.False(await db.Tasks.AnyAsync(t => t.UserId == demoId));
            Assert.False(await db.RefreshSessions.AnyAsync(s => s.UserId == demoId));
        }

        using var demoAfterCleanup = await demo.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, demoAfterCleanup.StatusCode);
        using var normalStillWorks = await normal.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.OK, normalStillWorks.StatusCode);
    }
}
