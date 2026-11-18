using Microsoft.Extensions.Time.Testing;
using Starter.Api.Domain;
using Starter.Api.Features.Todos;
using Starter.Api.Shared;

namespace Starter.Tests.Features.Todos;

public sealed class TodoServiceTests
{
    private readonly FakeTodoRepository _repository = new();
    private readonly TodoService _service;
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero));

    public TodoServiceTests()
    {
        _service = new TodoService(_repository, _clock);
    }

    [Fact]
    public async Task Create_trims_title_and_saves_the_todo()
    {
        var result = await _service.CreateAsync(new CreateTodoRequest("  Buy milk  "), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Buy milk", result.Data!.Title);
        Assert.Single(_repository.Saved);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_returns_validation_error_when_title_is_empty(string title)
    {
        var result = await _service.CreateAsync(new CreateTodoRequest(title), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.Empty(_repository.Saved);
    }

    [Fact]
    public async Task Create_returns_validation_error_when_title_is_too_long()
    {
        var tooLong = new string('x', TodoRules.MaxTitleLength + 1);

        var result = await _service.CreateAsync(new CreateTodoRequest(tooLong), CancellationToken.None);

        Assert.Equal(ErrorKind.Validation, result.Kind);
    }

    [Fact]
    [Trait("Krav", "AK-1.2")]
    public async Task Create_returns_validation_error_when_due_date_has_passed()
    {
        // Arrange
        var dueDate = new DateOnly(2026, 11, 15); // klockan står på 2026-11-16, så det här är igår

        // Act
        var result = await _service.CreateAsync(new CreateTodoRequest("Skicka offert", dueDate), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.Empty(_repository.Saved);
    }

    [Fact]
    public async Task MarkDone_returns_not_found_for_unknown_id()
    {
        var result = await _service.MarkDoneAsync(42, CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }

    [Fact]
    [Trait("Krav", "AK-2.2")]
    public async Task ListOverdue_returns_a_todo_due_yesterday_with_one_day_late()
    {
        // Arrange: klockan står på 2026-11-16, så den 15:e är igår
        var todo = new Todo { Title = "Skicka offert", DueDate = new DateOnly(2026, 11, 15) };
        await _repository.AddAsync(todo, CancellationToken.None);

        // Act
        var result = await _service.ListOverdueAsync(CancellationToken.None);

        // Assert
        var overdue = Assert.Single(result.Data!);
        Assert.Equal("Skicka offert", overdue.Title);
        Assert.Equal(new DateOnly(2026, 11, 15), overdue.DueDate);
        Assert.Equal(1, overdue.DaysLate);
    }

    [Fact]
    [Trait("Krav", "AK-2.3")]
    public async Task ListOverdue_does_not_return_a_done_todo()
    {
        // Arrange: klockan står på 2026-11-16, så den 15:e är igår
        await _repository.AddAsync(new Todo { Title = "Klar", DueDate = new DateOnly(2026, 11, 15), IsDone = true }, CancellationToken.None);
        await _repository.AddAsync(new Todo { Title = "Inte klar", DueDate = new DateOnly(2026, 11, 15) }, CancellationToken.None);

        // Act
        var result = await _service.ListOverdueAsync(CancellationToken.None);

        // Assert
        var overdue = Assert.Single(result.Data!);
        Assert.Equal("Inte klar", overdue.Title);
    }

    [Fact]
    [Trait("Krav", "AK-2.4")]
    public async Task ListOverdue_does_not_return_a_todo_without_due_date()
    {
        // Arrange: en uppgift utan förfallodatum, och en som är sen (klockan står på 2026-11-16)
        await _repository.AddAsync(new Todo { Title = "Ingen deadline" }, CancellationToken.None);
        await _repository.AddAsync(new Todo { Title = "Sen", DueDate = new DateOnly(2026, 11, 15) }, CancellationToken.None);

        // Act
        var result = await _service.ListOverdueAsync(CancellationToken.None);

        // Assert
        var overdue = Assert.Single(result.Data!);
        Assert.Equal("Sen", overdue.Title);
    }
}
