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

    public static uint GetFlagValue<TVariant>()
#if NET9_0_OR_GREATER
        where TVariant : allows ref struct
#endif
#if NET7_0_OR_GREATER
        => T.GetFlagValue<TVariant>();
#else
        => GetFlagValue(typeof(TVariant));
#endif

    public static uint GetFlagValue(Type type)
    {
        var types = VariantTypes;
        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] == type)
            {
                return unchecked((uint)i + 1);
            }
        }
        return 0u;
    }

    public static Type? GetFlagType(uint flagValue)
    {
        if (!IsFlagDefined(flagValue))
            return null;
        return VariantTypes[unchecked((int)flagValue - 1)];
    }

    public static bool IsFlagDefined(uint flagValue)
        => flagValue >= 1 && flagValue <= VariantTypes.Length;

#if !NET7_0_OR_GREATER

    static TypeUnionMetadata()
    {
        var metadata = typeof(T).GetCustomAttribute<TypeUnionAttribute>();
        _variantTypes = metadata.Types.Distinct().ToArray();
    }

#endif
}