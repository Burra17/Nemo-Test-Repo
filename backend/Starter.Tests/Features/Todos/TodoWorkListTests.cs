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

    [Fact]
    [Trait("Krav", "AK-3.1")]
    public void Order_sorts_by_due_date_with_the_earliest_first()
    {
        // Arrange: blandad ordning, så testet inte blir grönt av en slump
        var todos = new List<Todo>
        {
            new() { Title = "Imorgon", DueDate = new DateOnly(2026, 11, 17) },
            new() { Title = "Igår", DueDate = new DateOnly(2026, 11, 15) },
            new() { Title = "Idag", DueDate = new DateOnly(2026, 11, 16) },
        };

        // Act
        var ordered = TodoWorkList.Order(todos);

        // Assert
        Assert.Equal(["Igår", "Idag", "Imorgon"], ordered.Select(t => t.Title));
    }

    [Fact]
    [Trait("Krav", "AK-3.1")]
    public void Order_puts_the_oldest_todo_first_when_due_dates_are_equal()
    {
        // Arrange: samma datum, och den som skapades sist (högst id) ligger först
        var todos = new List<Todo>
        {
            new() { Id = 2, Title = "Skapad sist", DueDate = new DateOnly(2026, 11, 16) },
            new() { Id = 1, Title = "Skapad först", DueDate = new DateOnly(2026, 11, 16) },
        };

        // Act
        var ordered = TodoWorkList.Order(todos);

        // Assert
        Assert.Equal(["Skapad först", "Skapad sist"], ordered.Select(t => t.Title));
    }

    [Fact]
    [Trait("Krav", "AK-3.4")]
    public void Order_puts_done_todos_last()
    {
        // Arrange: den klara uppgiften har det tidigaste datumet, så utan regeln kommer den först
        var todos = new List<Todo>
        {
            new() { Title = "Klar", IsDone = true, DueDate = new DateOnly(2026, 11, 15) },
            new() { Title = "Inte klar", DueDate = new DateOnly(2026, 11, 17) },
        };

        // Act
        var ordered = TodoWorkList.Order(todos);

        // Assert
        Assert.Equal(["Inte klar", "Klar"], ordered.Select(t => t.Title));
    }
}
