using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Starter.Tests.Features.Todos;

/// <summary>
/// Sökningen ska ha en gräns för hur många anrop som får göras per minut. Gränsen är konfigurerbar, så att testerna
/// kan använda en liten gräns i stället för att göra tusentals anrop.
/// </summary>
public sealed class RateLimitTests
{
    private const int Limit = 3;

    [Fact]
    [Trait("Abuse", "MB-7.1")]
    public async Task One_search_too_many_returns_429_with_retry_after()
    {
        // Arrange: en egen app per test, så att testerna inte delar på samma räknare
        using var factory = new SmallLimitFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Team", "salj");

        // Act: gör alla tillåtna anrop, och sedan ett till
        for (var i = 0; i < Limit; i++)
        {
            await client.GetAsync("/api/todos/search?q=offert");
        }

        var response = await client.GetAsync("/api/todos/search?q=offert");

        // Assert: 429, och svaret berättar när det går att försöka igen
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
    }

    [Fact]
    [Trait("Abuse", "MB-7.2")]
    public async Task Searches_within_the_limit_return_200()
    {
        // Arrange
        using var factory = new SmallLimitFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Team", "salj");

        // Act och Assert: alla anrop inom gränsen går igenom
        for (var i = 0; i < Limit; i++)
        {
            var response = await client.GetAsync("/api/todos/search?q=offert");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private sealed class SmallLimitFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"starter-test-{Guid.NewGuid()}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
            builder.UseSetting("RateLimiting:SearchPermitLimit", Limit.ToString());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(_databasePath);
        }
    }
}
