using Starter.Api.Domain;

namespace Starter.Api.Features.Todos;

public static class TodoDeadlines
{
    private static readonly TimeZoneInfo Stockholm =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    public static bool IsOverdue(Todo todo, DateOnly today) =>
        todo.DueDate != null && todo.DueDate < today && !todo.IsDone;

    public static DateOnly TodayInSweden(TimeProvider clock)
    {
        var swedishNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Stockholm);
        return DateOnly.FromDateTime(swedishNow.DateTime);
    }
}
