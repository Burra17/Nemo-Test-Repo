using Starter.Api.Domain;

namespace Starter.Api.Features.Todos;

public static class TodoDeadlines
{
    private static readonly TimeZoneInfo Stockholm =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    public static bool IsOverdue(Todo todo, DateOnly today) =>
        todo.DueDate != null && todo.DueDate < today && !todo.IsDone;

    public static DateOnly TodayInSweden(TimeProvider clock) => DateInSweden(clock.GetUtcNow());

    public static bool WasCompletedOnTime(DateOnly dueDate, DateTimeOffset completedAt) =>
        DateInSweden(completedAt) <= dueDate;

    private static DateOnly DateInSweden(DateTimeOffset moment)
    {
        var swedishTime = TimeZoneInfo.ConvertTime(moment, Stockholm);
        return DateOnly.FromDateTime(swedishTime.DateTime);
    }
}
