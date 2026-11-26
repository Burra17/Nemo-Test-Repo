using BenchmarkDotNet.Running;

namespace Starter.Benchmarks;

/// <summary>
/// En egen klass med Main i stället för top-level statements. Starter.Api har redan en publik klass som heter
/// Program, och två klasser med samma namn ger varningen CS0436.
/// </summary>
public static class BenchmarkProgram
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(BenchmarkProgram).Assembly).Run(args);
}
