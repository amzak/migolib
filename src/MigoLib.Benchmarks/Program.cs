using BenchmarkDotNet.Running;
using MigoLib.Benchmarks;

namespace benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(ParseModelBenchmarks).Assembly);
        }
    }
}