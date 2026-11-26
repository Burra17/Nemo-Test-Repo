using BenchmarkDotNet.Attributes;
using Starter.Api.Domain;
using Starter.Api.Features.Todos;

namespace Starter.Benchmarks;

/// <summary>
/// Mäter sorteringen av arbetslistan (Story 3). Kravet IFK-2: att sortera 1 000 uppgifter ska ta under 1 ms.
/// </summary>
[MemoryDiagnoser]
public class WorkListBenchmarks
{
    private List<Todo> _todos = [];

    [Params(100, 1_000, 10_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Fast seed, så att varje körning sorterar exakt samma data
        var random = new Random(42);
        var firstDay = new DateOnly(2026, 11, 1);

        _todos = Enumerable.Range(1, Count)
            .Select(id => new Todo
            {
                Id = id,
                Title = $"Uppgift {id}",
                IsDone = random.Next(4) == 0,                                       // ungefär var fjärde är klar
                DueDate = random.Next(5) == 0 ? null : firstDay.AddDays(random.Next(60)), // ungefär var femte saknar datum
            })
            .ToList();
    }

    [Benchmark]
    public IReadOnlyList<Todo> OrderWorkList() => TodoWorkList.Order(_todos);
}
