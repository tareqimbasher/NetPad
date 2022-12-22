using System;
using System.Collections.Generic;
using System.Linq;
using O2Html.Dom;
using Xunit;
using Xunit.Abstractions;

namespace O2Html.Tests.Performance;

public class PerformanceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private static readonly Random _random = new Random();

    public PerformanceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact(Skip = "Adhoc for performance testing")]
    public void LibComparison_Performance_Test()
    {
        var cars = GetCars(1000);

        Benchmark("STJ", () => System.Text.Json.JsonSerializer.Serialize(cars));
        Benchmark("Json.NET", () => Newtonsoft.Json.JsonConvert.SerializeObject(cars));
        Benchmark("O2HTML", () => HtmlConvert.Serialize(cars).ToHtml());
    }

    [Fact(Skip = "Adhoc for performance testing")]
    public void O2HTML_Performance_Tests()
    {
        var cars = GetCars(1000);

        Node node = null!;

        Benchmark("All", () =>
        {
            Benchmark("Serialize to Element", () => node = HtmlConvert.Serialize(cars));
            Benchmark("To HTML", () => node.ToHtml());
        });
    }

    [Fact(Skip = "Adhoc for performance testing")]
    public void Profiling_Serialize()
    {
        var cars = GetCars(1000);
        HtmlConvert.Serialize(cars);
    }

    private void Benchmark(string label, Action action, int runTimes = 1)
    {
        var timings = new List<double>();

        for (int i = 0; i < runTimes; i++)
        {
            var start = DateTime.Now;
            action();
            timings.Add((DateTime.Now - start).TotalMilliseconds);
        }

        timings.Sort();
        var median = timings[timings.Count / 2];

        _testOutputHelper.WriteLine($"### {label.PadRight(25)} => MEDIAN: {median} | AVG: {timings.Average()}");
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

    class Car
    {
        public Car()
        {
            Make = GenerateRandomString(10);
            Model = GenerateRandomString(15);
            Year = 2022;
            CreatedDate = new DateTime(2022, 1, 1);
            Features = new List<Feature>()
            {
                new Feature(),
                new Feature(),
                new Feature(),
                new Feature(),
                new Feature(),
            };
        }

        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<Feature> Features { get; set; }
    }

    class Feature
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
