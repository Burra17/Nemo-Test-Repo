using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Starter.Api.Features.Todos;

namespace Starter.Tests.Features.Todos;

public sealed class TeamIsolationTests(TodosApiTests.ApiFactory factory) :
    IClassFixture<TodosApiTests.ApiFactory>
{

    // Samma JSON-inställningar som i TodosApiTests: prioriteten skickas som text ("High"), inte som ett tal.
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

    private HttpClient ClientFor(string team)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Team", team);
        return client;
    }

    private async Task<TodoResponse> CreateAsEkonomi(string title)
    {
        var created = await ClientFor("ekonomi").PostAsJsonAsync("/api/todos", new { title });
        return (await created.Content.ReadFromJsonAsync<TodoResponse>(Json))!;
    }

    [Fact]
    [Trait("Abuse", "MB-1.1")]
    public async Task Another_teams_todo_returns_404_and_no_data()
    {
        // Arrange: ekonomi skapar en egen uppgift
        var created = await ClientFor("ekonomi").PostAsJsonAsync("/api/todos", new { title = "Hemlig lönekörning" });
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>(Json);

        // Act: sälj försöker hämta den med dess id
        var response = await ClientFor("salj").GetAsync($"/api/todos/{todo!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("Hemlig lönekörning", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    [Trait("Abuse", "MB-2.1")]
    public async Task Marking_another_teams_todo_done_returns_404()
    {
        // Arrange
        var todo = await CreateAsEkonomi("Betala leverantörer");

        // Act: sälj försöker bocka av ekonomis uppgift
        var response = await ClientFor("salj").PutAsync($"/api/todos/{todo.Id}/done", null);

        // Assert: 404, och uppgiften är fortfarande inte klar när ekonomi tittar
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var afterwards = await ClientFor("ekonomi").GetFromJsonAsync<TodoResponse>($"/api/todos/{todo.Id}", Json);
        Assert.False(afterwards!.IsDone);
    }
}
