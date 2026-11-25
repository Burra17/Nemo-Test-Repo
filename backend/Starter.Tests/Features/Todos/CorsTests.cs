namespace Starter.Tests.Features.Todos;

public sealed class CorsTests(TodosApiTests.ApiFactory factory) : IClassFixture<TodosApiTests.ApiFactory>
{
    private async Task<HttpResponseMessage> GetFrom(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/todos");
        request.Headers.Add("Origin", origin);
        return await factory.CreateClient().SendAsync(request);
    }

    [Fact]
    [Trait("Abuse", "MB-6.1")]
    public async Task Another_website_is_not_allowed_to_read_the_response()
    {
        // Act: webbläsaren på en ond sida anropar API:t
        var response = await GetFrom("https://evil.example");

        // Assert: svaret säger inget om att den sidan får läsa det
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    [Trait("Abuse", "MB-6.2")]
    public async Task Our_own_frontend_is_allowed_to_read_the_response()
    {
        // Act: vår egen frontend anropar API:t
        var response = await GetFrom("http://localhost:5173");

        // Assert: svaret tillåter just vår frontend, inte alla
        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
