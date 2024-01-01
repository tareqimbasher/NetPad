using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using O2Html.Dom;
using Xunit;
using Xunit.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace O2Html.Tests.Performance;

public class PerformanceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private static readonly Random _random = new();

    public PerformanceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact(Skip = "Adhoc for performance testing")]
    public void LibComparison_Performance_Test()
    {
        const int times = 3;
        const bool preRun = true;

        foreach (var itemsCount in new[] { 1, 10, 100, 1000, 10000 })
        {
            var cars = GetCars(itemsCount);

            _testOutputHelper.WriteLine($"Serializing {itemsCount} Cars");
            Benchmark("System.Text.Json", () => JsonSerializer.Serialize(cars), times, preRun);
            Benchmark("Json.NET", () => JsonConvert.SerializeObject(cars), times, preRun);
            Benchmark("O2HTML", () => HtmlSerializer.Serialize(cars).ToHtml(), times, preRun);
            //Benchmark("Dumpify", () => Dumpify.DumpExtensions.DumpText(cars), times, preRun);
            _testOutputHelper.WriteLine("");
        }
    }

    [Fact(Skip = "Adhoc for performance testing")]
    public void O2HTML_Performance_Tests()
    {
        var cars = GetCars(10000);

        // To cache
        _ = HtmlSerializer.Serialize(cars).ToHtml();

        Node node = null!;

        Benchmark("All", () =>
        {
            Benchmark("Serialize to Element", () => node = HtmlSerializer.Serialize(cars));
            Benchmark("To HTML", () => node.ToHtml());
        }, 3, true);

        // Timings for 10000 Cars:
        // Serialize to Element step is taking the most time (~500ms)
        // ToHtml() step takes less time (~150ms)
        // Conclusion: need to provide option to serialize.NET object to HTML string directly without needing to go through constructing
        // DOM tree objects.
    }

    [Fact(Skip = "Adhoc for performance testing")]
    public void Profiling_Serialize()
    {
        var cars = GetCars(1000);
        HtmlSerializer.Serialize(cars);
    }

    private void Benchmark(string label, Action action, int runTimes = 1, bool preRun = false)
    {
        var stopWatch = new Stopwatch();

        if (preRun)
        {
            stopWatch.Start();

            // Run action first so that if first call caches data that later calls will use,
            // we don't get skewed results for non-cached call
            action();
        }

        double? firstRunMs = stopWatch.IsRunning ? stopWatch.Elapsed.TotalMilliseconds : null;

        var timings = new List<double>();


        for (int i = 0; i < runTimes; i++)
        {
            stopWatch.Restart();
            action();
            timings.Add(stopWatch.Elapsed.TotalMilliseconds);
        }

        stopWatch.Stop();

        timings.Sort();
        var median = timings[timings.Count / 2];

        var result = $"### {label,-25} => MEDIAN: {median}ms | AVG: {Math.Round(timings.Average(), 4)}ms";
        if (firstRunMs.HasValue)
        {
            result += $" | First Run: {firstRunMs}ms";
        }


        _testOutputHelper.WriteLine(result);
    }

    private static List<Car> GetCars(int count)
    {
        var cars = new List<Car>();

        for (int i = 0; i < count; i++)
            cars.Add(new Car());

        return cars;
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    private class Car
    {
        public Car()
        {
            Make = GenerateRandomString(10);
            Model = GenerateRandomString(15);
            Year = 2022;
            CreatedDate = new DateTime(2022, 1, 1);
            Features = new List<Feature>
            {
                new(),
                new(),
                new(),
                new(),
                new()
            };
        }

        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<Feature> Features { get; set; }
    }

    private class Feature
    {
        public Feature()
        {
            Label = GenerateRandomString(15);
            Description = GenerateRandomString(50);
            Included = true;
        }

        public string Label { get; set; }
        public string Description { get; set; }
        public bool Included { get; set; }
    }
}
