## Creating a "Program-style" script

The primary approach is to write Top-Level statements, ie:

```csharp
var p = new Person
{
    Id = 1,
    Name = "John Doe"
};

p.Dump();

class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

To have a "Program" style script and a static `Main()` entry point, put it in a `partial class Program`:

```csharp
partial class Program
{
    static void Main()
    {
        var p = new Person
        {
            Id = 1,
            Name = "John Doe"
        };

        p.Dump();
    }
}

class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

Another option without the class wrapper is:

```csharp
void Main(string[] args)
{
    var p = new Person
    {
        Id = 1,
        Name = "John Doe"
    };

    p.Dump();
}

Main(args);

class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```