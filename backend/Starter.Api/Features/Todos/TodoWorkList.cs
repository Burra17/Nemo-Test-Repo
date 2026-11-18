using Starter.Api.Domain;

namespace Starter.Api.Features.Todos;

public static class TodoWorkList
{
    public static IReadOnlyList<Todo> Order(IEnumerable<Todo> todos) =>
        todos
            .OrderBy(t => t.DueDate is null)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .ToList();
}
