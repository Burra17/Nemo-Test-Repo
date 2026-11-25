namespace Starter.Tests.Features.Todos;

/// <summary>Varje svar från API:t ska be webbläsaren att vara försiktig (MB-8).</summary>
public sealed class SecurityHeadersTests(TodosApiTests.ApiFactory factory) : IClassFixture<TodosApiTests.ApiFactory>
{
    [Theory]
    [Trait("Abuse", "MB-8.1")]
    [Trait("Abuse", "MB-8.2")]
    [Trait("Abuse", "MB-8.3")]
    [Trait("Abuse", "MB-8.4")]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "no-referrer")]
    [InlineData("Cross-Origin-Resource-Policy", "same-site")]
    public async Task Every_response_has_the_security_header(string header, string expected)
    {
        // Act: ett helt vanligt anrop
        var response = await factory.CreateClient().GetAsync("/api/todos");

        // Assert: headern finns, med rätt värde
        Assert.True(response.Headers.TryGetValues(header, out var values), $"Headern {header} saknas.");
        Assert.Equal(expected, values.Single());
    }
}
