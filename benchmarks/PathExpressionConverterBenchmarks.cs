using BenchmarkDotNet.Attributes;

namespace Minimal.Behaviors.Wpf.Benchmarks;

[MemoryDiagnoser]
public class PathExpressionConverterBenchmarks
{
    // Target under test
    private readonly PathExpressionConverter _converter = PathExpressionConverter.Instance;

    // Sample object graph
    private SampleEventArgs _source = default!;
    private List<string> _coldPaths = default!;

    // Control iteration sizes
    [Params(1, 10, 100, 1000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        // Build nested structure with arrays/lists
        _source = new SampleEventArgs
        {
            OriginalSource = new SampleNode
            {
                Name = "root",
                Value = 1,
                ItemsArray = new object[2000],
                ItemsList = new List<object>(capacity: 2000),
                Child = new SampleNode
                {
                    Name = "child",
                    Value = 2,
                    Child = new SampleNode
                    {
                        Name = "leaf",
                        Value = 42
                    }
                }
            }
        };

        for (int i = 0; i < _source.OriginalSource!.ItemsArray!.Length; i++)
        {
            var node = new SampleNode { Name = "n" + i, Value = i };
            _source.OriginalSource.ItemsArray[i] = node;
            _source.OriginalSource.ItemsList!.Add(node);
        }

        // Warm token cache for hot-path benchmarks
        _ = _converter.Convert(_source, "OriginalSource");
        _ = _converter.Convert(_source, "OriginalSource.Child.Child.Value");
        _ = _converter.Convert(_source, "OriginalSource.ItemsArray[0]");
        _ = _converter.Convert(_source, "OriginalSource.ItemsList[0]");

        // Prepare distinct paths to force token parsing (cold cache)
        _coldPaths = new List<string>(N);
        for (int i = 0; i < N; i++)
        {
            // Ensure distinct tokenization by varying the index
            _coldPaths.Add($"OriginalSource.ItemsArray[{i}]");
        }
    }

    // -----------------------
    // Hot-path micro-benchmarks
    // -----------------------

    [Benchmark(Baseline = true)]
    public object? SimpleProperty_Hot()
    {
        // Single property hop
        return _converter.Convert(_source, "OriginalSource");
    }

    [Benchmark]
    public object? Nested3Properties_Hot()
    {
        // Three property hops
        return _converter.Convert(_source, "OriginalSource.Child.Child.Value");
    }

    [Benchmark]
    public object? ArrayIndexer_Hot()
    {
        // Property hop + array indexer
        return _converter.Convert(_source, "OriginalSource.ItemsArray[0]");
    }

    [Benchmark]
    public object? ListIndexer_Hot()
    {
        // Property hop + IList indexer
        return _converter.Convert(_source, "OriginalSource.ItemsList[0]");
    }

    [Benchmark]
    public object? InvalidMember_Hot()
    {
        // Missing property should result in null
        return _converter.Convert(_source, "OriginalSource.Missing");
    }

    [Benchmark]
    public object? OutOfRangeIndex_Hot()
    {
        // Out-of-range index should result in null
        return _converter.Convert(_source, "OriginalSource.ItemsArray[999999]");
    }

    // -----------------------
    // Cold-path: force token parsing and dictionary insert
    // -----------------------

    [Benchmark]
    public void Cold_Cache_Tokenization()
    {
        for (int i = 0; i < N; i++)
        {
            _ = _converter.Convert(_source, _coldPaths[i]);
        }
    }

    // -----------------------
    // Mixed scenario: frequent hot hits + sporadic cold misses
    // -----------------------

    [Benchmark]
    public void Mixed_Hot_With_Occasional_Cold()
    {
        for (int i = 0; i < N; i++)
        {
            _ = _converter.Convert(_source, "OriginalSource");
            _ = _converter.Convert(_source, "OriginalSource.Child.Child.Value");
            _ = _converter.Convert(_source, "OriginalSource.ItemsArray[0]");
            _ = _converter.Convert(_source, "OriginalSource.ItemsList[0]");

            if ((i & 0x1F) == 0)
            {
                int idx = i % _source.OriginalSource!.ItemsArray!.Length;
                _ = _converter.Convert(_source, $"OriginalSource.ItemsArray[{idx}]");
            }
        }
    }
}

// Sample objects used by the converter benchmarks

public sealed class SampleEventArgs
{
    public SampleNode? OriginalSource { get; set; }
}

public sealed class SampleNode
{
    public string? Name { get; set; }
    public int Value { get; set; }
    public SampleNode? Child { get; set; }

    public object[]? ItemsArray { get; set; }
    public List<object>? ItemsList { get; set; }
}