using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Starter.Api.Features.Todos;

namespace Starter.Tests.Features.Todos;

public sealed class SearchInjectionTests(TodosApiTests.ApiFactory factory) :
    IClassFixture<TodosApiTests.ApiFactory>
{
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private HttpClient ClientFor(string team)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Team", team);
        return client;
    }

    private static string SearchUrl(string query) => $"/api/todos/search?q={Uri.EscapeDataString(query)}";

    [Fact]
    [Trait("Abuse", "MB-4.1")]
    public async Task Search_with_sql_returns_no_todos_from_other_teams()
    {
        // Arrange: ekonomi har en uppgift som sälj inte får se
        await ClientFor("ekonomi").PostAsJsonAsync("/api/todos", new { title = "Hemlig lönekörning" });

        // Act: sälj söker med SQL i sökfältet
        var response = await ClientFor("salj").GetAsync(SearchUrl("' OR 1=1 --"));

        // Assert: 200, och inga uppgifter alls, eftersom ingen titel innehåller den texten
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var todos = await response.Content.ReadFromJsonAsync<List<TodoResponse>>(Json);
        Assert.Empty(todos!);
    }
}
