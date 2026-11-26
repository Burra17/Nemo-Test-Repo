using System.Net;
using System.Net.Http.Json;
using Starter.Api.Features.Todos;

namespace Starter.Tests.Features.Todos;

/// <summary>
/// Saras mätvärde direkt ur API:t (Story 4). En egen testklass, så att statistiken bara räknar de uppgifter som
/// testerna här skapar.
/// </summary>
public sealed class StatsApiTests(TodosApiTests.ApiFactory factory) : IClassFixture<TodosApiTests.ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    [Trait("Krav", "AK-4.1")]
    [Trait("Krav", "AK-4.3")]
    public async Task Stats_have_no_rate_until_a_todo_is_done_and_then_count_it_as_on_time()
    {
        // Arrange: den falska klockan står på 2026-11-16 12:00 UTC
        var today = new DateOnly(2026, 11, 16);

        // Act och Assert: innan något är klart finns ingen andel (AK-4.3)
        var before = await _client.GetFromJsonAsync<TodoStatsResponse>("/api/todos/stats");
        Assert.Null(before!.OnTimeRate);

        // Act: en uppgift som ska vara klar idag blir klar idag
        var created = await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest("Skicka offert", today));
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>();
        await _client.PutAsync($"/api/todos/{todo!.Id}/done", null);
        var after = await _client.GetFromJsonAsync<TodoStatsResponse>("/api/todos/stats");

        // Assert: den räknas som klar i tid, och andelen är 100 % (AK-4.1)
        Assert.Equal(1, after!.CompletedOnTime);
        Assert.Equal(0, after.CompletedLate);
        Assert.Equal(1.0, after.OnTimeRate);
    }

    [Fact]
    [Trait("Krav", "AK-4.4")]
    public async Task Marking_an_unknown_todo_as_done_returns_404()
    {
        // Act
        var response = await _client.PutAsync("/api/todos/999/done", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
