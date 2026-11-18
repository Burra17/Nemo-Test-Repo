using Starter.Api.Domain;
using Starter.Api.Shared;

namespace Starter.Api.Features.Todos;

/// <summary>The rules for todos live here, not in the controller and not in the database.</summary>
public sealed class TodoService(ITodoRepository repository, TimeProvider clock)
{
    public async Task<OperationResult<IReadOnlyList<TodoResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var todos = await repository.ListAsync(cancellationToken);
        return OperationResult<IReadOnlyList<TodoResponse>>.Ok(todos.Select(ToResponse).ToList());
    }

    public async Task<OperationResult<TodoResponse>> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length == 0)
        {
            return OperationResult<TodoResponse>.Invalid("Title is required.");
        }

        if (title.Length > TodoRules.MaxTitleLength)
        {
            return OperationResult<TodoResponse>.Invalid($"Title can be at most {TodoRules.MaxTitleLength} characters.");
        }

        var todo = new Todo { Title = title, CreatedAt = clock.GetUtcNow(), DueDate = request.DueDate };
        await repository.AddAsync(todo, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<TodoResponse>.Ok(ToResponse(todo));
    }

    public async Task<OperationResult<TodoResponse>> MarkDoneAsync(int id, CancellationToken cancellationToken)
    {
        var todo = await repository.FindAsync(id, cancellationToken);
        if (todo is null)
        {
            return OperationResult<TodoResponse>.NotFound($"Todo {id} was not found.");
        }

        todo.IsDone = true;
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<TodoResponse>.Ok(ToResponse(todo));
    }

    public async Task<OperationResult<IReadOnlyList<OverdueTodoResponse>>> ListOverdueAsync(CancellationToken cancellationToken)
    {
        var today = TodoDeadlines.TodayInSweden(clock);
        var todos = await repository.ListOverdueAsync(today, cancellationToken);
        return OperationResult<IReadOnlyList<OverdueTodoResponse>>.Ok(todos.Select(t => ToOverdueResponse(t, today)).ToList());
    }

    private static TodoResponse ToResponse(Todo todo) => new(todo.Id, todo.Title, todo.IsDone, todo.CreatedAt, todo.DueDate);

    private static OverdueTodoResponse ToOverdueResponse(Todo todo, DateOnly today) =>
        new(todo.Id, todo.Title, todo.DueDate!.Value, today.DayNumber - todo.DueDate!.Value.DayNumber);
}
