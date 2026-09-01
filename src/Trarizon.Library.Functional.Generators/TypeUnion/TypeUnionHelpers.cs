using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Trarizon.Library.Roslyn;

namespace Trarizon.Library.Functional.Generators.TypeUnion;

static class TypeUnionHelpers
{
    /// <summary>
    /// The call site ensures that the attribute is a type union attribute.
    /// </summary>
    /// <param name="attribute"></param>
    /// <returns></returns>
    public static ImmutableArray<ITypeSymbol> GetTypeUnionVariantTypes(this AttributeData attribute)
    {
        var symbol = attribute.AttributeClass;
        if (symbol == null)
        {
            return ImmutableArray<ITypeSymbol>.Empty;
        }

        if (symbol.Arity == 0)
        {
            return attribute.GetConstructorArgument(0).CastArray<ITypeSymbol>();
        }

        return symbol.TypeArguments;
    }
}
