using BenchmarkDotNet.Running;
using Primp.Benchmarks;

if (args.Length > 0 && string.Equals(args[0], "compare", StringComparison.OrdinalIgnoreCase))
{
	var compareArgs = args.Skip(1).ToArray();
	var exitCode = await PerfComparisonRunner.RunAsync(compareArgs);
	Environment.Exit(exitCode);
}

BenchmarkSwitcher.FromAssembly(typeof(HttpBenchmarks).Assembly).Run(args);
