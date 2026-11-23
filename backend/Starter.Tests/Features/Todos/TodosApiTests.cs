using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Starter.Api.Features.Todos;

namespace Starter.Tests.Features.Todos;

/// <summary>Starts the real API in memory, with its own throwaway SQLite file, and talks to it over HTTP.</summary>
public sealed class TodosApiTests : IClassFixture<TodosApiTests.ApiFactory>
{
    private readonly HttpClient _client;

    public TodosApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_then_get_returns_the_created_todo()
    {
        var created = await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest("Write a test"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var todos = await _client.GetFromJsonAsync<List<TodoResponse>>("/api/todos");

        Assert.Contains(todos!, t => t.Title == "Write a test");
    }

    [Fact]
    public async Task Post_with_empty_title_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Krav", "AK-1.2")]
    public async Task Post_with_a_due_date_in_the_past_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest("Send offer", new DateOnly(2026, 11, 15)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Krav", "AK-1.1")]
    public async Task Post_with_a_due_date_today_returns_201_with_the_date()
    {
        // Arrange: den falska klockan står på 2026-11-16, så den 16:e är idag.
        // Med den riktiga klockan har datumet redan passerat och API:t svarar 400,
        // så testet bevisar också att klockan byts ut.
        var request = new CreateTodoRequest("Skicka offert", new DateOnly(2026, 11, 16));

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.Equal(new DateOnly(2026, 11, 16), created!.DueDate);
    }

    [Fact]
    [Trait("Krav", "AK-3.1")]
    public async Task Get_returns_todos_in_worklist_order()
    {
        // Arrange: den tidigare uppgiften skapas först, så den har lägre id.
        // Sorteras listan på id (nyaste först) kommer den sist, sorteras den på datum kommer den först.
        await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest("Worklist tidigare", new DateOnly(2026, 11, 17)));
        await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest("Worklist senare", new DateOnly(2026, 11, 20)));

        // Act
        var todos = await _client.GetFromJsonAsync<List<TodoResponse>>("/api/todos");

        // Assert: andra tester delar databasen, så bara våra två uppgifter jämförs
        var mine = todos!.Where(t => t.Title.StartsWith("Worklist")).Select(t => t.Title);
        Assert.Equal(["Worklist tidigare", "Worklist senare"], mine);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero));

        private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"starter-test-{Guid.NewGuid()}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
            builder.ConfigureTestServices(services =>
                services.AddSingleton<TimeProvider>(Clock));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(_databasePath);
        }
    }
}
