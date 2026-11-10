using N;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Trarizon.Library.Functional;
using Trarizon.Library.Functional.CompilerServices;
using Trarizon.Library.Functional.Unions;

Console.WriteLine("Hello, World!");

RefOptional<int> c = default;

c.Select(x => x.ToString());
c.Select(x => x.ToString().AsSpan());

Func<int, string> func = x => x.ToString();
Func<int, string, string> func3 = (x, y) => x.ToString();
c.Select(func);
c.Select("", func3);

Func<int, ReadOnlySpan<char>> func2 = x => x.ToString().AsSpan();
Func<int, string, ReadOnlySpan<char>> func4 = (x, y) => x.ToString().AsSpan();
c.Select(func2);
c.Select("", func4);


c.Zip(c.AsDeref(), (x, y) => x + y);
c.Zip(c, (x, y) => (x, y).ToString().AsSpan());

var uu = new N.O.U();

var str = uu switch
{
    string s => s,
    int i => i.ToString(),
    N.O.D => "D",
};

MyUnion mu = default;


if (U.TryCreate("114514", out var u))
{
    if (u.Is<ulong>(out var i))
    {
        System.Console.WriteLine(i);
    }
}

MyObjectUnion obju = "str";

var text = obju switch
{
    string s => s,
    List<char> chars => new string(chars.ToArray()),
};

if (obju is string objus)
{
    System.Console.WriteLine(objus);
}

System.Console.WriteLine(text);

[TypeUnion(typeof(ulong), typeof(string), typeof(DateTime), typeof(ReadOnlySpan<char>))]
partial struct U;

union BclUnion(string, int)
{
    public string Str => this switch
    {
        string s => s,
        int i => i.ToString(),
    };
}

// MyUnion.TryCreate(JsonDocument.Parse(""), out var u);

// namespace System.Runtime.CompilerServices
// {
//     public interface IUnion
//     {
//         /// <summary>
//         /// Gets the value contained in the union, or <see langword="null" /> if the union has no value.
//         /// </summary>
//         /// <value>
//         /// The current value of the union as one of its case types, or <see langword="null" />.
//         /// </value>
//         object? Value { get; }
//     }
// }

namespace N
{
    partial class O
    {
        [TypeUnion(typeof(int), typeof(string), typeof(D))]
        [Union]
        public partial struct U
        {
            [Obsolete("Use As<object>() instead", true)]
            public object? Value => this.As<object>();
        }

        [TypeUnion(typeof(IDo), typeof(EnumChar),
            ShareInterfaces = UnionShareInterfaceOption.Explicit)]
        partial struct ShareIntf
        {
        }

        interface IDo
        {
            void Do(string str);
        }

        struct EnumChar : IEnumerable<char>, IDo
        {
            public void Do(string str) => throw new NotImplementedException();
            public IEnumerator<char> GetEnumerator() => default!;
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct D : IEquatable<D>
        {
            public bool Equals(D other) => throw new NotImplementedException();

            // public static bool operator ==(D a, D b) => throw new NotImplementedException();
            // public static bool operator !=(D a, D b) => throw new NotImplementedException();
        }
    }
}

[TypeUnion(
    typeof(void),
    typeof(string),
    typeof(int),
    typeof(JsonElement),
    typeof(ReadOnlySpan<char>),
    typeof(IDisposable),
    typeof(float),
    typeof(void*), typeof(int*),
    typeof(void**), typeof(int**),
    GenerateDangerousMembers = true,
    AlwaysGenerateSeparateMethodsForRefStruct = true,
    ShareInterfaces = UnionShareInterfaceOption.Explicit,
    LanguageUnionSupport = true)]
partial struct MyUnion
{
}

[TypeUnion(typeof(string), typeof(List<char>), typeof(int),
    ShareInterfaces = UnionShareInterfaceOption.Explicit,
    LanguageUnionSupport = true)]
partial struct MyObjectUnion
{

}

// [TypeUnion(typeof(T))]
// [TypeUnion<T, int>]
// partial struct MyUnion<T>;