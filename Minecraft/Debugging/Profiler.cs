using BoboEngine;
using System.Diagnostics;

namespace Minecraft.Debugging;

// TODO: Connect to future pie chart!
public class Profiler
{
    private readonly Dictionary<string, Stopwatch> _benchmarks = new();

    private readonly Stack<string> _runningBenchmarks = new();

    public void Push(string id)
    {
        Stopwatch stopwatch;

        if (!_benchmarks.ContainsKey(id))
        {
            stopwatch = new Stopwatch();

            _benchmarks.Add(id, stopwatch);
        }
        else stopwatch = _benchmarks[id];

        stopwatch.Start();
        _runningBenchmarks.Push(id);
    }
    public void Pop()
    {
        string id = _runningBenchmarks.Pop();

        _benchmarks[id].Stop();
    }

    public void PrintResults(string? prefix = null)
    {
        foreach (var benchmark in _benchmarks)
        {
            PrintResult(benchmark, prefix);
        }
    }
    private void PrintResult(KeyValuePair<string, Stopwatch> benchmark, string? prefix = null)
    {
        Engine.Log((prefix ?? "") + $" - {benchmark.Key} {benchmark.Value.Elapsed}");
    }
}