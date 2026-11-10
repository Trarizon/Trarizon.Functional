using System.Reflection;

namespace Trarizon.Library.Functional.Unions;

/// <summary>
/// Type union metadata.
/// </summary>
/// <remarks>
/// The implementation depends on the detail of generator, it may work incorrectly for custom type that implements ITypeUnion.
/// </remarks>
public static partial class TypeUnionMetadata<T> where T : ITypeUnion<T>
{
#if !NET7_0_OR_GREATER
    private static Type[] _variantTypes;
#endif
    public static ReadOnlySpan<Type> VariantTypes
#if NET7_0_OR_GREATER
            => T.VariantTypes;
#else
            => _variantTypes;
#endif

#if !NET7_0_OR_GREATER

    static TypeUnionMetadata()
    {
        var metadata = typeof(T).GetCustomAttribute<TypeUnionAttribute>();
        _variantTypes = metadata.Types.Distinct().ToArray();
    }

#endif
}