
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TaskManager.Api.Tests;

public class TasksApiIntegrationTests : IClassFixture<TaskManagerWebApplicationFactory>
{
    // Un host y una SQLite en memoria por clase: los tests de xUnit
    // de la misma clase no se ejecutan en paralelo. Cada test crea
    // usuarios distintos y mantiene el aislamiento por UserId.
    private readonly TaskManagerWebApplicationFactory _factory;

    public TasksApiIntegrationTests(TaskManagerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private const string FrontendOrigin = "http://localhost:4200";
    private const string TestPassword = "TaskFlow2026!";

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var email = $"test-{Guid.NewGuid():N}@taskflow.local";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                password = TestPassword,
                confirmPassword = TestPassword
            });

        var registerBody =
            await registerResponse.Content.ReadAsStringAsync();

        Assert.True(
            registerResponse.StatusCode == HttpStatusCode.Created,
            $"Registro: {(int)registerResponse.StatusCode}. {registerBody}");

        await LoginAsync(client, email);
    }

    private static async Task LoginAsync(
        HttpClient client,
        string email)
    {
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email,
                password = TestPassword
            });

        var loginBody =
            await loginResponse.Content.ReadAsStringAsync();

        Assert.True(
            loginResponse.StatusCode == HttpStatusCode.OK,
            $"Login: {(int)loginResponse.StatusCode}. {loginBody}");

        using var loginJson = JsonDocument.Parse(loginBody);

        var accessToken = loginJson.RootElement
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static void SetFrontendOrigin(HttpClient client)
    {
        client.DefaultRequestHeaders.Add(
            "Origin",
            FrontendOrigin);
    }

    // =========================================================
    // PRUEBAS ORIGINALES
    // =========================================================

    [Fact]
    public async Task GetTasks_ShouldReturnOk()
    {
        var factory = _factory;
        using var client = factory.CreateHttpsClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var factory = _factory;
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_ShouldReturnSuccess()
    {
        var factory = _factory;
        using var client = factory.CreateHttpsClient();

        await AuthenticateAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/tasks",
            new
            {
                title = "Integration test task",
                description = "Created from xUnit"
            });

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CreateTask_WithInvalidTitle_ShouldReturnBadRequest()
    {
        var factory = _factory;
        using var client = factory.CreateHttpsClient();

        await AuthenticateAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/tasks",
            new
            {
                title = "AB",
                description = "Invalid task"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Task_ShouldSupportFullCrudLifecycle()
    {
        var factory = _factory;
        using var client = factory.CreateHttpsClient();

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

    // =========================================================
    // SEGURIDAD: REVOCACIÓN DE ACCESS TOKENS
    // =========================================================

    [Fact]
    public async Task AccessToken_ShouldBeRejectedAfterLogout()
    {
        var factory = _factory;
        using var client = factory.CreateHttpsClient();

        SetFrontendOrigin(client);

        await AuthenticateAsync(client);

        // El access token debe funcionar antes del logout.
        var beforeLogout = await client.GetAsync("/api/tasks");

        Assert.Equal(
            HttpStatusCode.OK,
            beforeLogout.StatusCode);

        var oldAccessToken =
            client.DefaultRequestHeaders.Authorization;

        Assert.NotNull(oldAccessToken);

        // Revocar la familia mediante logout.
        var logoutResponse = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new { });

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        // Reutilizar el mismo access token.
        client.DefaultRequestHeaders.Authorization =
            oldAccessToken;

        var afterLogout = await client.GetAsync("/api/tasks");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            afterLogout.StatusCode);
    }

    [Fact]
    public async Task AccessToken_ShouldNotCreateTaskAfterLogout()
    {
        var factory = _factory;
        using var client = factory.CreateHttpsClient();

        SetFrontendOrigin(client);

        await AuthenticateAsync(client);

        // Verificar que la sesión funciona.
        var beforeLogout = await client.GetAsync("/api/tasks");

        Assert.Equal(
            HttpStatusCode.OK,
            beforeLogout.StatusCode);

        var oldAccessToken =
            client.DefaultRequestHeaders.Authorization;

        Assert.NotNull(oldAccessToken);

        // Cerrar sesión.
        var logoutResponse = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new { });

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        // Mantener expresamente el token revocado.
        client.DefaultRequestHeaders.Authorization =
            oldAccessToken;

        var response = await client.PostAsJsonAsync(
            "/api/tasks",
            new
            {
                title = "Should never be created",
                description = "Revoked session"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    
    [Fact]
    public async Task Logout_ShouldNotInvalidateIndependentSession()
    {
        var factory = _factory;

        using var clientA = factory.CreateHttpsClient();
        using var clientB = factory.CreateHttpsClient();

        SetFrontendOrigin(clientA);
        SetFrontendOrigin(clientB);

        var email =
            $"independent-{Guid.NewGuid():N}@taskflow.local";

        // Registrar usuario.
        var registerResponse = await clientA.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                password = TestPassword,
                confirmPassword = TestPassword
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        // Login A.
        await LoginAsync(clientA, email);

        // Login B: sesión independiente.
        await LoginAsync(clientB, email);

        // Verificar que ambos access tokens funcionan.
        Assert.Equal(
            HttpStatusCode.OK,
            (await clientA.GetAsync("/api/tasks")).StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            (await clientB.GetAsync("/api/tasks")).StatusCode);

        // Cerrar sesión A.
        var logoutResponse = await clientA.PostAsJsonAsync(
            "/api/auth/logout",
            new { });

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        // Comprobar que A queda revocada.
        var afterA = await clientA.GetAsync("/api/tasks");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            afterA.StatusCode);

        // Comprobar que B sigue activa.
        var afterB = await clientB.GetAsync("/api/tasks");

        Assert.Equal(
            HttpStatusCode.OK,
            afterB.StatusCode);
    }

}
