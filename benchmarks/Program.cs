using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Minimal.Behaviors.Wpf.Benchmarks;

internal class Program
{
    static void Main()
    {
        var version = typeof(Behavior).Assembly
            .GetName().Version?.ToString() ?? "1.0.0";
        var config = DefaultConfig.Instance
            .WithArtifactsPath($@"benchmarks\results\{version}")
            .WithOption(ConfigOptions.DisableOptimizationsValidator, true);
        BenchmarkRunner.Run<PathExpressionConverterBenchmarks>(config);
        Console.ReadKey();
    }
}
