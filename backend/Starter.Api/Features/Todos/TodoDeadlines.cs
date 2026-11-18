using Starter.Api.Domain;

namespace Starter.Api.Features.Todos;

public static class TodoDeadlines
{
    public static bool IsOverdue(Todo todo, DateOnly today) =>
        todo.DueDate != null && todo.DueDate < today && !todo.IsDone;
}
