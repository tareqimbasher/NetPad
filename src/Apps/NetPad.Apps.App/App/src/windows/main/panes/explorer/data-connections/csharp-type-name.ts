const keywords = new Map([
    ["Boolean", "bool"],
    ["Byte", "byte"],
    ["SByte", "sbyte"],
    ["Char", "char"],
    ["Decimal", "decimal"],
    ["Double", "double"],
    ["Single", "float"],
    ["Int16", "short"],
    ["UInt16", "ushort"],
    ["Int32", "int"],
    ["UInt32", "uint"],
    ["Int64", "long"],
    ["UInt64", "ulong"],
    ["IntPtr", "nint"],
    ["UIntPtr", "nuint"],
    ["Object", "object"],
    ["String", "string"],
]);

/**
 * Converts a CLR type name to the keyword C# uses for it (`Int32` → `int`, `Byte[]` → `byte[]`). A name
 * C# has no keyword for is returned unchanged.
 */
export function csharpTypeName(clrTypeName: string): string {
    return clrTypeName.replace(/^\w+/, name => keywords.get(name) ?? name);
}
