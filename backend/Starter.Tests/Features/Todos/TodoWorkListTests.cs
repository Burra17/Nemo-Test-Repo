using Starter.Api.Domain;
using Starter.Api.Features.Todos;

namespace Starter.Tests.Features.Todos;

public sealed class TodoWorkListTests
{
    [Fact]
    [Trait("Krav", "AK-3.2")]
    public void Order_puts_todos_without_due_date_last()
    {
        // Arrange: uppgiften utan datum ligger först, så testet inte blir grönt av en slump
        var todos = new List<Todo>
        {
            new() { Title = "Utan datum" },
            new() { Title = "Med datum", DueDate = new DateOnly(2026, 12, 16) },
        };

        // Act
        var ordered = TodoWorkList.Order(todos);

        // Assert
        Assert.Equal(["Med datum", "Utan datum"], ordered.Select(t => t.Title));
    }
}
