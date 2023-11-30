# O2HTML

O2HTML is a powerful C# library designed to seamlessly serialize .NET objects or
value types to HTML, providing a straightforward way to represent complex data structures 
visually. It offers the flexibility to generate HTML strings or create an HTML DOM 
object, acting as a lightweight virtual DOM. Effortlessly transform 
your .NET objects into structured, customizable HTML, simplifying the process of 
integrating data into web applications.

This library powers [NetPad](https://github.com/tareqimbasher/NetPad).

TODO before publishing a nuget package:
- Add instructions and usage examples
- Review public API. Notable change candidates
  - Offer a way to go from a .NET value to HTML string directly without needing to serialize to a HTML DOM object 
    structure first. When a DOM object structure is not needed, going to HTML directly will greatly increase 
    performance in both speed and memory usage, possibly by 4-5x.
