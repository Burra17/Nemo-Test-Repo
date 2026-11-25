using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Starter.Api.Features.Todos;

namespace Starter.Tests.Features.Todos;

/// <summary>
/// MB-1.2: listan, de försenade och statistiken visar bara det egna teamets uppgifter. En egen testklass, så att
/// testet som spolar fram klockan inte påverkar testerna i TeamIsolationTests.
/// </summary>
public sealed class TeamListsTests(TodosApiTests.ApiFactory factory) : IClassFixture<TodosApiTests.ApiFactory>
{
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private DateOnly Today => DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);

    private HttpClient ClientFor(string team)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Team", team);
        return client;
    }

    private async Task<TodoResponse> CreateAsEkonomi(string title, DateOnly? dueDate = null)
    {
        var created = await ClientFor("ekonomi").PostAsJsonAsync("/api/todos", new { title, dueDate });
        return (await created.Content.ReadFromJsonAsync<TodoResponse>(Json))!;
    }

    [Fact]
    [Trait("Abuse", "MB-1.2")]
    public async Task The_list_does_not_contain_another_teams_todos()
    {
        // Arrange
        await CreateAsEkonomi("Ekonomis hemliga lista");

        // Act
        var todos = await ClientFor("salj").GetFromJsonAsync<List<TodoResponse>>("/api/todos", Json);

        // Assert
        Assert.DoesNotContain(todos!, t => t.Title == "Ekonomis hemliga lista");
    }

    [Fact]
    [Trait("Abuse", "MB-1.2")]
    public async Task The_overdue_list_does_not_contain_another_teams_todos()
    {
        // Arrange: ekonomi har en uppgift som ska vara klar idag, och sedan går det två dagar
        await CreateAsEkonomi("Ekonomis försenade uppgift", Today);
        factory.Clock.Advance(TimeSpan.FromDays(2));

        // Act
        var overdue = await ClientFor("salj").GetFromJsonAsync<List<OverdueTodoResponse>>("/api/todos/overdue", Json);

        // Assert
        Assert.DoesNotContain(overdue!, t => t.Title == "Ekonomis försenade uppgift");
    }

    [Fact]
    [Trait("Abuse", "MB-1.2")]
    public async Task The_stats_do_not_count_another_teams_todos()
    {
        // Arrange: säljs siffror innan ekonomi gör något
        var before = await ClientFor("salj").GetFromJsonAsync<DeadlineStatsResponse>("/api/todos/stats", Json);

        // Act: ekonomi blir klar med en uppgift som hade ett förfallodatum
        var todo = await CreateAsEkonomi("Ekonomis klara uppgift", Today);
        await ClientFor("ekonomi").PutAsync($"/api/todos/{todo.Id}/done", null);

        // Assert: säljs siffror är oförändrade
        var after = await ClientFor("salj").GetFromJsonAsync<DeadlineStatsResponse>("/api/todos/stats", Json);
        Assert.Equal(before, after);
    }
}
