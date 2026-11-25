using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Starter.Api.Features.Todos;

namespace Starter.Tests.Features.Todos;

public sealed class MassAssignmentTests(TodosApiTests.ApiFactory factory) :
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

    private async Task<List<string>> TitlesFor(string team)
    {
        var todos = await ClientFor(team).GetFromJsonAsync<List<TodoResponse>>("/api/todos", Json);
        return todos!.Select(t => t.Title).ToList();
    }

    [Fact]
    [Trait("Abuse", "MB-3.1")]
    public async Task Create_with_another_teams_id_keeps_the_todo_in_my_team()
    {
        // Act: sälj skapar en uppgift och skickar med ett annat teams id, som formuläret aldrig visar
        var response = await ClientFor("salj").PostAsJsonAsync(
            "/api/todos", new { title = "Smugglad av salj", teamId = 2 });

        // Assert: 201, och uppgiften hamnar hos sälj, inte hos det andra teamet
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("Smugglad av salj", await TitlesFor("salj"));
        Assert.DoesNotContain("Smugglad av salj", await TitlesFor("ekonomi"));
    }

    [Fact]
    [Trait("Abuse", "MB-3.2")]
    public async Task Update_with_another_teams_id_keeps_the_todo_in_my_team()
    {
        // Arrange: sälj skapar en egen uppgift
        var created = await ClientFor("salj").PostAsJsonAsync("/api/todos", new { title = "Säljs egen uppgift" });
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>(Json);

        // Act: sälj ändrar den och skickar med ett annat teams id
        await ClientFor("salj").PutAsJsonAsync(
            $"/api/todos/{todo!.Id}", new { title = "Säljs egen uppgift", priority = "Normal", teamId = 2 });

        // Assert: uppgiften tillhör fortfarande sälj
        var afterwards = await ClientFor("salj").GetAsync($"/api/todos/{todo.Id}");
        Assert.Equal(HttpStatusCode.OK, afterwards.StatusCode);
    }

    [Fact]
    [Trait("Abuse", "MB-3.3")]
    public async Task Update_with_isDeleted_does_not_hide_the_todo()
    {
        // Arrange: sälj skapar en egen uppgift
        var created = await ClientFor("salj").PostAsJsonAsync("/api/todos", new { title = "Ska inte kunna gömmas" });
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>(Json);

        // Act: sälj ändrar den och skickar med borttagen-flaggan, som formuläret aldrig visar
        await ClientFor("salj").PutAsJsonAsync(
            $"/api/todos/{todo!.Id}", new { title = "Ska inte kunna gömmas", priority = "Normal", isDeleted = true });

        // Assert: uppgiften syns fortfarande i säljs lista
        Assert.Contains("Ska inte kunna gömmas", await TitlesFor("salj"));
    }
}
