using Microsoft.Extensions.Time.Testing;
using Starter.Api.Domain;
using Starter.Api.Features.Todos;

namespace Starter.Tests.Features.Todos;

public sealed class TodoDeadlinesTests
{
    [Fact]
    [Trait("Krav", "AK-2.1")]
    public void A_todo_due_today_is_not_overdue()
    {
        // Arrange
        var today = new DateOnly(2026, 11, 16);
        var todo = new Todo { DueDate = today };

        // Act
        var isOverdue = TodoDeadlines.IsOverdue(todo, today);

        // Assert
        Assert.False(isOverdue);
    }

    [Fact]
    [Trait("Krav", "AK-2.2")]
    public void A_todo_due_yesterday_is_overdue_at_midnight_swedish_time()
    {
        // Arrange: 23:00 UTC är 00:00 i Sverige (UTC+1 i november), alltså redan den 17:e
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 16, 23, 0, 0, TimeSpan.Zero));
        var todo = new Todo { DueDate = new DateOnly(2026, 11, 16) };

        // Act
        var today = TodoDeadlines.TodayInSweden(clock);
        var isOverdue = TodoDeadlines.IsOverdue(todo, today);

        // Assert
        Assert.True(isOverdue);
    }

    [Fact]
    [Trait("Krav", "AK-4.1")]
    public void A_todo_completed_on_the_due_date_at_22_swedish_time_is_on_time()
    {
        // Arrange: 21:00 UTC är 22:00 i Sverige (UTC+1 i november), fortfarande förfallodagen
        var dueDate = new DateOnly(2026, 11, 20);
        var completedAt = new DateTimeOffset(2026, 11, 20, 21, 0, 0, TimeSpan.Zero);

        // Act
        var onTime = TodoDeadlines.WasCompletedOnTime(dueDate, completedAt);

        // Assert
        Assert.True(onTime);
    }

    [Fact]
    [Trait("Krav", "AK-4.2")]
    public void A_todo_completed_after_midnight_swedish_time_is_late()
    {
        // Arrange: 23:30 UTC är 00:30 i Sverige, alltså redan dagen efter förfallodagen.
        // I UTC är det fortfarande den 20:e, så en regel som glömmer svensk tid säger "i tid".
        var dueDate = new DateOnly(2026, 11, 20);
        var completedAt = new DateTimeOffset(2026, 11, 20, 23, 30, 0, TimeSpan.Zero);

        // Act
        var onTime = TodoDeadlines.WasCompletedOnTime(dueDate, completedAt);

        // Assert
        Assert.False(onTime);
    }
}
