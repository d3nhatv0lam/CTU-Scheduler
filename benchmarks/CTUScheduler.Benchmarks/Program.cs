using BenchmarkDotNet.Running;

namespace CTUScheduler.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<TimetableGeneratorBenchmarks>(args: args);
    }
}
