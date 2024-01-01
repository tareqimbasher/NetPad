using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace O2Html.Tests;

public class TestData
{
    public static IEnumerable<object?[]> RepresentedAsStringsValues()
    {
        return new[]
        {
            new object[] { "value" },
            new object[] { 'v' },
            new object[] { 1 },
            new object[] { 2.2 },
            new object[] { 3.2m },
            new object[] { 4.2f },
            new object[] { 5L },
            new object[] { (int?)1 },
            new object[] { (double?)2.2 },
            new object[] { (decimal?)3.2m },
            new object[] { (float?)4.2f },
            new object[] { (long?)5L },
            new object[] { true },
            new object[] { (byte)6 },
            new object[] { (sbyte)7 },
            new object[] { (nint)8 },
            new object[] { Guid.NewGuid() },
            new object[] { DateTime.UtcNow },
            new object[] { DateTime.UtcNow - DateTime.UtcNow.AddHours(1) },
            new object[] { DateOnly.MinValue },
            new object[] { TimeOnly.MinValue },
            new object[] { DateTimeOffset.UtcNow },
            new object[] { BindingFlags.Public },
            new object[] { BindingFlags.Public | BindingFlags.Instance },
            new object[] { JsonCommentHandling.Allow },
            new object[] { new FormattableType() },
            new object[] { new Exception("Some error") },
            new object[] { typeof(string) },
            new object[] { Version.Parse("1.2.3.4") },
        };
    }

    public static IEnumerable<object?[]> ObjectValues()
    {
        return new[]
        {
            new object[] { null! },
            new object[] { new { Name = "John", Age = 32 } },
            new object[] { new Customer("John", 32) },
            new object[] { new Product("Computer", 995.5m) },
            new object[] { new Uri("https://www.github.com") },
            new object[] { DBNull.Value },
            new object[] { CultureInfo.CurrentCulture },
            new object[] { new MemoryStream() },
            new object[] { StringComparer.Ordinal },
            new object[] { new Action<int>(i => { ++i; }) },
            new object[] { Task.FromResult(new Customer("name", 33)) },
            new object[] { new Regex(@"\b(?<word>\w+)\s+(\k<word>)\b") },
            new object[] { new Regex(@"\b(?<word>\w+)\s+(\k<word>)\b").Matches("The the quick brown fox  fox jumps over the lazy dog dog.")[0].Groups[0] },
            new object[] { Process.GetCurrentProcess() },
            new object[] { Assembly.GetExecutingAssembly() },
            new object[] { new System.Net.Mail.MailAddress("test@test.com", "Test Man") },
            new object[] { new System.Web.HttpUtility() },
#pragma warning disable SYSLIB0026
            new object[] { new System.Security.Cryptography.X509Certificates.X509Certificate() },
#pragma warning restore SYSLIB0026
            new object[] { new System.Net.NetworkCredential("username", "password") },
            new object[] { new System.Net.NetworkCredential("username", "password").SecurePassword },
            new object[] { new Stopwatch() },
            new object[] { new System.Timers.Timer() },
            new object[] { new System.Threading.Timer(_ => { }) },
            new object[] { new Socket(SocketType.Stream, ProtocolType.Tcp) },
        };
    }

    public static IEnumerable<object?[]> CollectionValues()
    {
        return new[]
        {
            new object[] { new List<string> { "one", "two" } },
            new object[] { new[] { "one", "two" } },
            new object[] { new HashSet<string> { "one", "two" } },
            new object[] { new Dictionary<string, int> { { "one", 1 }, { "two", 2 } } },
            new object[] { new ConcurrentDictionary<string, int>(new Dictionary<string, int> { { "one", 1 }, { "two", 2 } }) },
            new object[] { new Queue<string>(new[] { "one", "two" }) },
            new object[] { new Stack<string>(new[] { "one", "two" }) },
            new object[] { new BitArray(5, false) },
            new object[] { new byte[] { 5, 3 } },
            new object[] { new ObservableCollection<string> { "str", "str 2" } },
            new object[] { new Regex(@"\b(?<word>\w+)\s+(\k<word>)\b").Matches("The the quick brown fox  fox jumps over the lazy dog dog.") },
            new object[] { new Regex(@"\b(?<word>\w+)\s+(\k<word>)\b").Matches("The the quick brown fox  fox jumps over the lazy dog dog.")[0].Groups },
        };
    }

    public static IEnumerable<object?[]> TwoDimensionalArrayValues()
    {
        return new[]
        {
            new object[] { new[,] { { 1, 2 }, { 3, 4 }, { 5, 6 }, { 7, 8 } } },
            new object[] { new[,] { { "one", "two" }, { "three", "four" } } },
            new object[] { new[,] { { new Customer(), new Customer() }, { new Customer(), new Customer() } } },
        };
    }

    public static IEnumerable<object?[]> TupleValues()
    {
        return new[]
        {
            new object[] { (1, 2) },
            new object[] { (1, 2, 3, 4) },
            new object[] { ("one", "two") },
            new object[] { (new Customer(), new Customer(), new Customer(), new Customer()) },
            new object[] { (new Customer(), 4.3m, new DateTime(), "str") },
        };
    }

    public static IEnumerable<object?[]> MemoryValues()
    {
        return new[]
        {
            new object[] { new Memory<int>(new[] { 2, 4, 6 }) },
            new object[] { new ReadOnlyMemory<string>(new[] { "one", "two" }) },
        };
    }

    public static IEnumerable<object?[]> XmlNodeValues()
    {
        var xmlDocument = new XmlDocument();
        xmlDocument.InnerXml = GetXml();

        return new[]
        {
            new object[] { xmlDocument },
        };
    }

    public static IEnumerable<object?[]> XNodeValues()
    {
        return new[]
        {
            new object[] { XDocument.Parse(GetXml()) },
            new object[] { XElement.Parse(GetXml()) },
        };
    }

    public static IEnumerable<object?[]> JsonDocumentValues()
    {
        return new []
        {
            new object[] { JsonDocument.Parse(@"[{""Name"": ""John Smith"", ""Age"": 33}]") },
        };
    }

    public static IEnumerable<object?[]> FileSystemInfoValues()
    {
        return new[]
        {
            new object[] { new FileInfo("/etc/os-release") },
            new object[] { new DirectoryInfo("/etc") },
        };
    }

    public static IEnumerable<object?[]> DataTableValues()
    {
        return new[]
        {
            new object[] { GetDataTable("Table 1") },
        };
    }

    public static IEnumerable<object?[]> DataSetValues()
    {
        var dataSet = new DataSet("Set name");

        dataSet.Tables.Add(GetDataTable("Table 1"));
        dataSet.Tables.Add(GetDataTable("Table 2"));
        dataSet.Tables.Add(GetDataTable("Table 3"));

        return new[]
        {
            new object[] { dataSet },
        };
    }

    public static IEnumerable<object?[]> All()
    {
        var data = new List<object?[]>();

        data.AddRange(RepresentedAsStringsValues());
        data.AddRange(ObjectValues());
        data.AddRange(CollectionValues());
        data.AddRange(TwoDimensionalArrayValues());
        data.AddRange(TupleValues());
        data.AddRange(MemoryValues());
        data.AddRange(XmlNodeValues());
        data.AddRange(XNodeValues());
        data.AddRange(FileSystemInfoValues());
        data.AddRange(DataTableValues());
        data.AddRange(DataSetValues());

        return data;
    }


    private static string GetXml()
    {
        return "<root><children><child attr=\"test\">Value 1</child><child attr=\"test\"></child></children></root>";
    }

    private static DataTable GetDataTable(string tableName)
    {
        var dt = new DataTable(tableName);

        dt.Columns.Add("string", typeof(string));
        dt.Columns.Add("integer", typeof(int));
        dt.Columns.Add("double", typeof(int));
        dt.Columns.Add("Nullable(decimal)", typeof(decimal));
        dt.Columns.Add("Customer", typeof(Customer));
        dt.Columns.Add("XNode", typeof(XNode));
        dt.Columns.Add("Tuple", typeof(ValueTuple<int, Customer>));
        dt.Columns.Add("2-D array", typeof(float[,]));

        var row = dt.NewRow();
        dt.Rows.Add(row);

        row.ItemArray = new object?[]
        {
            "str",
            4,
            5.6,
            (decimal?)null,
            new Customer("name", 33),
            XElement.Parse(GetXml()),
            (5, new Customer()),
            new[,] { { 3.5F, 2F }, { 0F, 1.1F } }
        };

        return dt;
    }

    class FormattableType : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return "formatted string representation";
        }
    }

    class Customer
    {
        public Customer()
        {
        }

        public Customer(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public string? Name { get; }
        public int? Age { get; }
    }

    record Product(string Name, decimal Price);
}
