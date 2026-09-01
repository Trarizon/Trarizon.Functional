using System.Collections.Immutable;

namespace Trarizon.Library.Functional.Generators;

internal static class Polyfills
{
    public static ImmutableArray<T> DrainToImmutable<T>(this ImmutableArray<T>.Builder builder)
    {
        if (builder.Count == builder.Capacity)
        {
            return builder.MoveToImmutable();
        }
        else
        {
            var array = builder.ToImmutableArray();
            builder.Clear();
            return array;
        }
    }
}
