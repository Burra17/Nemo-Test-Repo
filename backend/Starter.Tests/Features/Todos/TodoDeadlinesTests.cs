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
}
