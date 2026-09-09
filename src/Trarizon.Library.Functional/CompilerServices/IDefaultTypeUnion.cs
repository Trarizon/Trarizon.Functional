using Trarizon.Library.Functional.Unions;

namespace Trarizon.Library.Functional.CompilerServices;

[GeneratorUseOnly]
public interface IDefaultTypeUnion<TSelf> : ITypeUnion<TSelf>
#if NET9_0_OR_GREATER
    where TSelf : IDefaultTypeUnion<TSelf>, allows ref struct
#else
    where TSelf : IDefaultTypeUnion<TSelf>
#endif
{
    static bool ITypeUnion.IsFlagDefined(uint flagValue)
        => flagValue >= 1 && flagValue <= TSelf.VariantTypes.Length;

    static Type? ITypeUnion.GetFlagType(uint flagValue)
    {
        if (!TSelf.IsFlagDefined(flagValue))
            return null;
        return TSelf.VariantTypes[unchecked((int)flagValue - 1)];
    }
}
