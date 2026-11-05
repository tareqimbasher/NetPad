using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace O2Html.Tests;

public class TypeNamesTests
{
    private const string Ns = "O2Html.Tests";

    [Theory]
    [InlineData(typeof(int), "Int32", "System.Int32")]
    [InlineData(typeof(int[,]), "Int32[,]", "System.Int32[,]")]
    [InlineData(typeof(int?), "Int32?", "System.Int32?")]
    [InlineData(typeof(List<>), "List<T>", "System.Collections.Generic.List<T>")]
    [InlineData(
        typeof(Dictionary<string, List<int>>),
        "Dictionary<String, List<Int32>>",
        "System.Collections.Generic.Dictionary<System.String, System.Collections.Generic.List<System.Int32>>")]
    public void NamesTypesLikeCSharp(Type type, string expectedName, string expectedNameWithNamespace)
    {
        AssertNames(type, expectedName, expectedNameWithNamespace);
    }

    [Theory]
    [InlineData(typeof(List<string>[]), "List<String>[]", "System.Collections.Generic.List<System.String>[]")]
    [InlineData(typeof(int?[]), "Int32?[]", "System.Int32?[]")]
    [InlineData(typeof((int, string)[]), "ValueTuple<Int32, String>[]", "System.ValueTuple<System.Int32, System.String>[]")]
    [InlineData(typeof(int[][]), "Int32[][]", "System.Int32[][]")]
    [InlineData(typeof(List<int>[][,]), "List<Int32>[][,]", "System.Collections.Generic.List<System.Int32>[][,]")]
    public void NamesArraysAfterTheirElementType(Type type, string expectedName, string expectedNameWithNamespace)
    {
        AssertNames(type, expectedName, expectedNameWithNamespace);
    }

    [Theory]
    [InlineData(typeof(List<int?>), "List<Int32?>", "System.Collections.Generic.List<System.Int32?>")]
    [InlineData(
        typeof(Dictionary<string, (int, string)?>),
        "Dictionary<String, ValueTuple<Int32, String>?>",
        "System.Collections.Generic.Dictionary<System.String, System.ValueTuple<System.Int32, System.String>?>")]
    public void NamesNullableGenericArgumentsWithAQuestionMark(
        Type type,
        string expectedName,
        string expectedNameWithNamespace)
    {
        AssertNames(type, expectedName, expectedNameWithNamespace);
    }

    [Theory]
    [InlineData(
        typeof(Dictionary<string, object>.KeyCollection),
        "Dictionary<String, Object>.KeyCollection",
        "System.Collections.Generic.Dictionary<System.String, System.Object>.KeyCollection")]
    [InlineData(
        typeof(List<int>.Enumerator),
        "List<Int32>.Enumerator",
        "System.Collections.Generic.List<System.Int32>.Enumerator")]
    [InlineData(
        typeof(GenericOuter<int>.GenericInner<string>),
        "GenericOuter<Int32>.GenericInner<String>",
        Ns + ".GenericOuter<System.Int32>.GenericInner<System.String>")]
    [InlineData(
        typeof(GenericOuter<int>.Inner.Innermost),
        "GenericOuter<Int32>.Inner.Innermost",
        Ns + ".GenericOuter<System.Int32>.Inner.Innermost")]
    [InlineData(
        typeof(Outer.GenericInner<int>.Innermost),
        "GenericInner<Int32>.Innermost",
        Ns + ".Outer.GenericInner<System.Int32>.Innermost")]
    [InlineData(
        typeof(HashSet<List<int>.Enumerator[]>),
        "HashSet<List<Int32>.Enumerator[]>",
        "System.Collections.Generic.HashSet<System.Collections.Generic.List<System.Int32>.Enumerator[]>")]
    public void NamesTypesNestedInGenericTypesWithTheirDeclaringTypes(
        Type type,
        string expectedName,
        string expectedNameWithNamespace)
    {
        AssertNames(type, expectedName, expectedNameWithNamespace);
    }

    [Theory]
    [InlineData(typeof(Outer.Inner), "Inner", Ns + ".Outer.Inner")]
    [InlineData(typeof(Outer.GenericInner<int>), "GenericInner<Int32>", Ns + ".Outer.GenericInner<System.Int32>")]
    public void NamesTypesNestedInNonGenericTypesWithoutTheirDeclaringTypes(
        Type type,
        string expectedName,
        string expectedNameWithNamespace)
    {
        AssertNames(type, expectedName, expectedNameWithNamespace);
    }

    [Fact]
    public void NamesAnonymousTypes()
    {
        var type = new { Id = 1, Name = "" }.GetType();

        AssertNames(type, "AnonymousType<Int32, String>", "AnonymousType<System.Int32, System.String>");
    }

    [Fact]
    public void NamesAnonymousTypesNestedInAnonymousTypes()
    {
        var type = new { Order = 1, Lines = new[] { new { Sku = "A", Qty = (int?)1 } } }.GetType();

        AssertNames(
            type,
            "AnonymousType<Int32, AnonymousType<String, Int32?>[]>",
            "AnonymousType<System.Int32, AnonymousType<System.String, System.Int32?>[]>");
    }

    [Fact]
    public void NamesArraysOfAnonymousTypes()
    {
        var type = new[] { new { Sku = "A", Qty = 1 } }.GetType();

        AssertNames(type, "AnonymousType<String, Int32>[]", "AnonymousType<System.String, System.Int32>[]");
    }

    [Fact]
    public void KeepsTheNameOfAClassThatOnlyLooksLikeAnAnonymousType()
    {
        AssertNames(typeof(MyAnonymousType), "MyAnonymousType", "MyAnonymousType");
    }

    [Fact]
    public void NamesWhereAfterTheSequenceItReturns()
    {
        AssertNamesHiddenSequenceClass(new List<int> { 1 }.Where(i => i > 0).GetType(), "IEnumerable<Int32>");
    }

    [Fact]
    public void NamesSelectAfterTheSequenceItReturns()
    {
        AssertNamesHiddenSequenceClass(new[] { 1 }.Select(i => i.ToString()).GetType(), "IEnumerable<String>");
    }

    [Fact]
    public void NamesSelectOfAnAnonymousTypeAfterTheSequenceItReturns()
    {
        AssertNamesHiddenSequenceClass(
            Enumerable.Range(1, 2).Select(i => new { Id = i }).GetType(),
            "IEnumerable<AnonymousType<Int32>>");
    }

    [Fact]
    public void NamesOrderByAfterTheOrderedSequenceItReturns()
    {
        AssertNamesHiddenSequenceClass(new[] { 2, 1 }.OrderBy(i => i).GetType(), "IOrderedEnumerable<Int32>");
    }

    [Fact]
    public void NamesGroupByAfterTheSequenceOfGroupsItReturns()
    {
        AssertNamesHiddenSequenceClass(
            new[] { "a" }.GroupBy(s => s.Length).GetType(),
            "IEnumerable<IGrouping<Int32, String>>");
    }

    [Fact]
    public void NamesAGroupAfterTheGroupingItIs()
    {
        AssertNamesHiddenSequenceClass(new[] { "a" }.GroupBy(s => s.Length).First().GetType(), "IGrouping<Int32, String>");
    }

    [Fact]
    public void NamesToLookupAfterTheLookupItReturns()
    {
        AssertNamesHiddenSequenceClass(new[] { "a" }.ToLookup(s => s.Length).GetType(), "ILookup<Int32, String>");
    }

    [Fact]
    public void NamesACompilerGeneratedGroupAfterTheGrouping()
    {
        AssertNames(
            typeof(CompilerGeneratedGrouping),
            "IGrouping<Int32, String>",
            Ns + ".TypeNamesTests.CompilerGeneratedGrouping");
    }

    [Fact]
    public void NamesACompilerGeneratedLookupAfterTheLookup()
    {
        AssertNames(
            typeof(CompilerGeneratedLookup),
            "ILookup<Int32, String>",
            Ns + ".TypeNamesTests.CompilerGeneratedLookup");
    }

    [Fact]
    public void NamesAYieldMethodAfterTheSequenceItReturns()
    {
        AssertNamesHiddenSequenceClass(Items().GetType(), "IEnumerable<Int32>");
    }

    [Fact]
    public void NamesAnAsyncIteratorAfterTheSequenceItReturns()
    {
        AssertNamesHiddenSequenceClass(AsyncItems().GetType(), "IAsyncEnumerable<Int32>");
    }

    [Fact]
    public void NamesACompilerGeneratedUntypedSequenceAfterIEnumerable()
    {
        AssertNames(
            typeof(CompilerGeneratedUntypedSequence),
            "IEnumerable",
            Ns + ".TypeNamesTests.CompilerGeneratedUntypedSequence");
    }

    [Fact]
    public void KeepsTheNameOfACompilerGeneratedClassThatIsNotASequence()
    {
        int captured = 1;
        Func<int> func = () => captured;
        var type = func.Target!.GetType();

        Assert.StartsWith("<>c__DisplayClass", TypeNames.GetReadableName(type));
    }

    [Fact]
    public void KeepsTheNameOfTheUsersOwnNonPublicSequenceClass()
    {
        AssertNames(typeof(InternalSequence<int>), "InternalSequence<Int32>", Ns + ".InternalSequence<System.Int32>");
    }

    [Fact]
    public void KeepsTheNameOfAPublicFrameworkSequenceClass()
    {
        AssertNames(
            new[] { 1 }.AsQueryable().GetType(),
            "EnumerableQuery<Int32>",
            "System.Linq.EnumerableQuery<System.Int32>");
    }

    [Fact]
    public void KeepsTheNameOfAPublicFrameworkClassOfTheUsersNonPublicClass()
    {
        AssertNames(
            typeof(List<InternalSequence<int>>),
            "List<InternalSequence<Int32>>",
            "System.Collections.Generic.List<" + Ns + ".InternalSequence<System.Int32>>");
    }

    private static void AssertNames(Type type, string expectedName, string expectedNameWithNamespace)
    {
        Assert.Equal(expectedName, TypeNames.GetReadableName(type));
        Assert.Equal(expectedNameWithNamespace, TypeNames.GetReadableName(type, withNamespace: true));
    }

    private static void AssertNamesHiddenSequenceClass(Type type, string expectedName)
    {
        Assert.Equal(expectedName, TypeNames.GetReadableName(type));

        var nameWithNamespace = TypeNames.GetReadableName(type, withNamespace: true);
        var className = type.Name.Split('`')[0];
        Assert.StartsWith(type.Namespace + ".", nameWithNamespace);
        Assert.Contains("." + className, nameWithNamespace);
    }

    private static IEnumerable<int> Items()
    {
        yield return 1;
    }

    private static async IAsyncEnumerable<int> AsyncItems()
    {
        await Task.Yield();
        yield return 1;
    }

    [CompilerGenerated]
    private class CompilerGeneratedUntypedSequence : IEnumerable
    {
        public IEnumerator GetEnumerator() => Array.Empty<int>().GetEnumerator();
    }

    [CompilerGenerated]
    private class CompilerGeneratedGrouping : IGrouping<int, string>
    {
        public int Key => 0;
        public IEnumerator<string> GetEnumerator() => Enumerable.Empty<string>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [CompilerGenerated]
    private class CompilerGeneratedLookup : ILookup<int, string>
    {
        public int Count => 0;
        public IEnumerable<string> this[int key] => Enumerable.Empty<string>();
        public bool Contains(int key) => false;
        public IEnumerator<IGrouping<int, string>> GetEnumerator() => Enumerable.Empty<IGrouping<int, string>>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

internal class InternalSequence<T> : IEnumerable<T>
{
    public IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class Outer
{
    public class Inner
    {
    }

    public class GenericInner<T>
    {
        public class Innermost
        {
        }
    }
}

public class GenericOuter<T>
{
    public class Inner
    {
        public class Innermost
        {
        }
    }

    public class GenericInner<U>
    {
    }
}
