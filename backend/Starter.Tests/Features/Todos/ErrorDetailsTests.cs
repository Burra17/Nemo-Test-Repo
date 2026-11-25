using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Starter.Api.Domain;
using Starter.Api.Features.Todos;

namespace Starter.Tests.Features.Todos;

/// <summary>Kör API:t i Production med ett repository som kraschar, och kontrollerar vad felet avslöjar.</summary>
public sealed class ErrorDetailsTests(ErrorDetailsTests.ProductionFactory factory) :
    IClassFixture<ErrorDetailsTests.ProductionFactory>
{
    [Fact]
    [Trait("Abuse", "MB-5.1")]
    public async Task Unexpected_error_outside_development_returns_500_without_details()
    {
        // Act: listan anropar repositoryt, som kraschar
        var response = await factory.CreateClient().GetAsync("/api/todos");

        // Assert: 500, men inget om vad som gick fel eller hur appen är byggd
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ThrowingTodoRepository.Secret, body);
        Assert.DoesNotContain(nameof(ThrowingTodoRepository), body);
    }

    public sealed class ProductionFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"starter-test-{Guid.NewGuid()}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
            builder.ConfigureTestServices(services => services.AddScoped<ITodoRepository, ThrowingTodoRepository>());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(_databasePath);
        }
    }
}

/// <summary>Kraschar vid varje anrop, så att testet kan framkalla ett oväntat fel.</summary>
internal sealed class ThrowingTodoRepository : ITodoRepository
{
    public const string Secret = "Hemlig databasdetalj: tabellen Todos på C:\\data\\starter.db";

    public Task<IReadOnlyList<Todo>> ListAsync(CancellationToken cancellationToken) => throw Boom();

    public Task<IReadOnlyList<Todo>> ListOverdueAsync(DateOnly today, CancellationToken cancellationToken) => throw Boom();

    public Task<IReadOnlyList<Todo>> ListCompletedWithDueDateAsync(CancellationToken cancellationToken) => throw Boom();

    public Task<int> CountOverdueAsync(DateOnly today, CancellationToken cancellationToken) => throw Boom();

    public Task<IReadOnlyList<Todo>> SearchAsync(string query, CancellationToken cancellationToken) => throw Boom();

    public Task<Todo?> FindAsync(int id, CancellationToken cancellationToken) => throw Boom();

    public Task AddAsync(Todo todo, CancellationToken cancellationToken) => throw Boom();

    public void Remove(Todo todo) => throw Boom();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => throw Boom();

    private static InvalidOperationException Boom() => new(Secret);
}
