using System.Runtime.CompilerServices;
using System.Text.Json;
using Trarizon.Library.Functional;
using Trarizon.Library.Functional.Unions;

Console.WriteLine("Hello, World!");

Optional.Of(1).Cast<string>();
Result.Success<int, string>(1).Cast<string,int>();

IEnumerable<int> a = [];

a.OfType<string>();


struct A { }

namespace N
{
    partial class O
    {
        [TypeUnion(typeof(int), typeof(string))]
        partial struct U
        {

        }
    }
}

[TypeUnion(
    typeof(void),
    typeof(string),
    typeof(int),
    typeof(JsonElement),
    typeof(ReadOnlySpan<char>),
    typeof(float),
    typeof(void*), typeof(int*),
    AlwaysGenerateSeperateMethodsForRefStruct = true)]
partial struct MyUnion;