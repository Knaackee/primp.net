using BenchmarkDotNet.Running;
using Primp.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(HttpBenchmarks).Assembly).Run(args);
