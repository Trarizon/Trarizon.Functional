using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Trarizon.Library.Functional;
using Trarizon.Library.Functional.Unions;

Console.WriteLine("Hello, World!");

Optional.Of(1).Cast<string>();
Result.Success<int, string>(1).Cast<string, int>();
Optional.Of(1).OfType<string>();

IEnumerable<int> a = [];

a.OfType<string>();


struct A { }


namespace System.Runtime.CompilerServices
{
    public interface IUnion
    {
        /// <summary>
        /// Gets the value contained in the union, or <see langword="null" /> if the union has no value.
        /// </summary>
        /// <value>
        /// The current value of the union as one of its case types, or <see langword="null" />.
        /// </value>
        object? Value { get; }
    }
}

namespace N
{
    partial class O
    {
        [TypeUnion(typeof(int), typeof(string), typeof(D))]
        partial struct U
        {

        }

        ref struct D : IEquatable<D>
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
    // typeof(ReadOnlySpan<char>),
    typeof(float),
    typeof(void*), typeof(int*),
    GenerateDangerousMembers = true,
    AlwaysGenerateSeparateMethodsForRefStruct = true)]
partial struct MyUnion : IEquatable<MyUnion>
{
}