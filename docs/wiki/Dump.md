# Data Dumping

Use the powerful `Dump()` method to visualize almost any object or value in the output pane.

## Usage

Use the `.Dump()` extension method:

```csharp
"some text".Dump();
DateTime.Now.Dump();
new MyClass().Dump();
new List<MyClass> { new MyClass() }.Dump();
```

You can alternatively use the `Util` class:

```csharp
Util.Dump(new MyClass());
```

`Dump()` returns the object/value being dumped

```csharp
var personsOver30 = Persons.Where(p => p.Age > 30).Dump();
```

`Dump()` is chainable

```csharp
var names = Persons
  .Where(p => p.Age > 30)
  .Dump()
  .Select(p => p.Name);
```

## Parameters

The `Dump()` method has the following optional parameters.

`title`: shows a title text above the output.

```csharp
Persons.Where(p => p.Age > 30).Dump("Persons above the age of 30");
```

`css`: CSS class(es) to add to the output. You can use any Bootstrap v5 classes.

```csharp
Logs.Where(l => l.Level == "WRN").Dump(css: "card text-bg-warning w-25");
```

> :bulb: You can also define your own CSS classes in `Settings > Styles` and use them here!

`clear`: removes the output after the specified number of milliseconds.

```csharp
while (true)
{
    Stocks.First(s => s.Name == "AAPL").Dump(clear: 2000);
    await Task.Delay(2010);
}
```

`code`: syntax-highlight an output string using the specified language code.

```csharp
"""
<root>
  <list>
    <item>Airplane</item>
    <item>Broom</item>
    <item>Carpet</item>
  </list>
</parent>
""".Dump(code: "xml");
```

This is powered by the excellent [highlight.js](https://github.com/highlightjs/highlight.js) library which supports an
exhaustive [list of languages](https://github.com/highlightjs/highlight.js?tab=readme-ov-file#supported-languages). You
can also just use `auto` which will auto-detect the language.

## Media Files

To dump media files, use the built-in `Image`, `Audio` and `Video` types from the `NetPad.Media` namespace.

```csharp
using NetPad.Media;

new Image("/path/to/image/png").Dump();
Image.FromPath("/path/to/image.png").Dump();
Image.FromUri(new Uri("https://web.com/image.png")).Dump();
Image.FromBase64("data:image/png;base64,/9j/4AAQSkZJRgABAQAA...").Dump();
Image.FromBytes(File.ReadAllBytes("/path/to/image.png"), "image/png").Dump();

// Same thing for Audio and Video classes
Audio.FromPath(...).Dump();
Video.FromPath(...).Dump();
...
```

> **Caution:** using the `FromBytes` method will internally convert the source to a Base64 string and is provided as a
> convenience method.

#### Sizing

Media files will be rendered in their original size, but you can specify display size using the `DisplayWidth` and
`DisplayHeight` properties:

```csharp
new Image("/path/to/img.png")
{
    DisplayWidth = "128px",
    DisplayHeight = "128px"
}.Dump();
```

Or using extension methods:

```csharp
// Individually
Image.FromPath("/path/to/img.png")
  .WithDisplayWidth("100%");
  .WithDisplayHeight("400px");

// Together
Image.FromPath(imageFile).WithDisplaySize("100%", "400px");
```

#### Methods

`Image`, `Audio` and `Video` inherit from the `MediaFile` base class which has these utlitiy methods:

```csharp
var image = Image.FromPath("/path/to/image.png");

// Opens the file with the default application and immediately returns.
image.Open();

// Opens the file with the default application and waits for it to exit.
image.OpenAndWait();
```

## HTML

**Functionality is a bit basic right now, but will be expanded.**

You can dump HTML and create your own interactive views.

```csharp
TextNode.RawText("<h1>Heading 1</h1>").Dump();
```

You need to add the following namepsaces to your script:

```csharp
O2Html
O2Html.Dom
```

> :bulb: More documentation and examples will be added soon. Advanced scenarios like JS interop and Blazor support are
> planned.

### Examples

#### Dump raw HTML:

```csharp
TextNode.RawText("""
<div class="card" style="width: 300px" id="myCard">
  <img src="https://raw.githubusercontent.com/dotnet/brand/main/logo/dotnet-logo.svg" class="card-img-top">
  <div class="card-body">
    <h4 class="card-title text-dark">Got .NET?</h5>
    <h5 class="card-title placeholder-glow">
      <span class="placeholder col-6"></span>
    </h5>
    <p class="card-text placeholder-glow">
      <span class="placeholder col-7"></span>
      <span class="placeholder col-4"></span>
      <span class="placeholder col-4"></span>
      <span class="placeholder col-6"></span>
      <span class="placeholder col-8"></span>
    </p>
    <a class="btn btn-warning col-6" href="https://dotnet.microsoft.com/en-us/download" target="_blank">Download Now!</a>
  </div>
</div>
""").Dump();

TextNode.RawText("""
<script>
  (function() {
      setTimeout(() => document.querySelector("#myCard a.btn-warning").classList.replace("btn-warning", "btn-primary"), 2000);
  })();
</script>
""").Dump();
```

You can also use the `Util` class:

```csharp
Util.RawHtml("<h1>Heading 1</h1>");
```

#### Use a fluent API to construct and dump an `Element`:

```csharp
new Element("div").AddClass("badge bg-info").AddText("Hello World!").Dump();
```

#### Build an interactive `form`:

```csharp
var form = new Element("form")
    .SetAttribute("type", "submit")
    .SetAttribute("onsubmit", "return false;")
    .SetAttribute("style", "max-width: 300px; padding: 1rem; border: 2px solid grey; border-radius: 3px");

form.AddAndGetElement("label")
    .AddClass("form-label")
    .SetAttribute("for", "firstName")
    .AddText("First Name");

form.AddAndGetElement("input")
    .SetId("firstName")
    .AddClass("form-control")
    .SetAttribute("type", "text")
    .SetAttribute("placeholder", "Your first name...");

form.AddAndGetElement("button")
    .SetId("submit-btn")
    .AddClass("btn btn-primary mt-3")
    .AddText("<i class=\"fa fa-solid fa-search\"></i> Submit");

form.AddDivider()
    .AddAndGetElement("p")
    .AddClass("m-3")
    .AddChild(HtmlSerializer.Serialize(new { Name = "John Doe", Age = 35 }));

form.Dump();

TextNode.RawText("""
<script>
  (function() {
      document.getElementById("submit-btn").addEventListener("click", () => {
        const name = document.getElementById("firstName").value;
        if (!name) {
            alert("Please enter your name.");
            return;
        }
        alert("Submitted. Thank you, " + name + "!");
      });
  })();
</script>
""").Dump();
```

#### Create an HTML document from scratch and render it inside an `iframe`:

```csharp
// Let's start by creating our HTML document
var document = new HtmlDocument();

document.AddStyle("""body {font-family: "Ubuntu"; background: #eee}""");

document.Body
    .AddChild(new Element("h1").AddText("Welcome!"))
    .AddAndGetElement("p").AddText("..from beyond the iFrame.");

// Then add an iframe to host our document inside of, and some JavaScript to load it
TextNode.RawText("""<iframe id="foo"></iframe>""").Dump();

TextNode.RawText($$"""
<script>
(function() {
    const iframe = document.getElementById('foo'),
        iframedoc = iframe.contentDocument || iframe.contentWindow.document;

    iframedoc.body.innerHTML = '{{document.ToHtml()}}';
})();
</script>
""").Dump();

// Let's also print out our HTML to take a look at it
document.ToHtml(O2Html.Formatting.Indented).Dump("HTML", code: "html");
```

## `Span<T>` and `ReadOnlySpan<T>`

Dumping `Span<T>` and `ReadOnlySpan<T>` values only works if you dump the Span directly. If the Span is a nested value
inside a collection or object, only basic info about the Span will be rendered.

```csharp
var span = new Span<byte>();
span.Dump(); // will show Span contents

var myObject = new MyObject
{
    SpanProperty = new Span<byte>() // will show basic info about this Span
}.Dump();
```

## Customizing `Dump()`

### Settings

You can control some of the functionality behind `Dump()` from settings.

#### Serialization Depth

When dumping objects there is a limit on how deep the serialization will go. This setting can be configured
in <kbd><kbd>Settings</kbd> > <kbd>Results</kbd> > <kbd>Serialization</kbd> > <kbd>Max Depth</kbd></kbd>.

#### Collection Length

When dumping collections there is a limit on how many items from the collection will be serialized. This setting can be
configured in <kbd><kbd>Settings</kbd> > <kbd>Results</kbd> > <kbd>Serialization</kbd> > <kbd>Max Collection
Length</kbd></kbd>.

!> Setting these properties to a large number might cause performance problems.

### In Code

While **not** implemented yet, the plan is to give users the ability to use code to customize how objects are serialized
and rendered.