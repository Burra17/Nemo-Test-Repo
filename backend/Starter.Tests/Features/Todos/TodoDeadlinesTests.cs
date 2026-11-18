using Starter.Api.Domain;
using Starter.Api.Features.Todos;

namespace Starter.Tests.Features.Todos;

public sealed class TodoDeadlinesTests
{
    [Fact]
    [Trait("Krav", "AK-2.1")]
    public void A_todo_due_today_is_not_overdue_at_23_59()
    {
        // Arrange
        var todo = new Todo { DueDate = new DateTime(2026, 11, 16) };
        var now = new DateTime(2026, 11, 16, 23, 59, 0);

        // Act
        var isOverdue = TodoDeadlines.IsOverdue(todo, now);

        // Assert
        Assert.False(isOverdue);
    }
}
