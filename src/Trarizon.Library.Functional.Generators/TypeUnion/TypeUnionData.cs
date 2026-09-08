using Trarizon.Library.Functional.Generators.Internals;
using Trarizon.Library.Roslyn.CSharp;
using Trarizon.Library.Roslyn.Pipeline;

namespace Trarizon.Library.Functional.Generators.TypeUnion;

record struct TypeUnionParseInfo(
    TypeHierarchyInfo TypeHierarchy,
    string TypeFQName,
    string TypeFName,
    string TypeName,
    TypeUnionDataOptions Options,
    EquatableImmutableArray<VariantParseInfo> Variants,
    EquatableImmutableArray<TypeUnionInterfaceParseInfo>? SharedInterfaces,
    ExceptionInfo? Exception = null
) : IParseInfo
{
    public readonly string FileHintName => CodeHelpers.ToFileNameString(TypeFQName.AsSpan("global::".Length));
}

sealed record TypeUnionData(
    TypeHierarchyInfo TypeHierarchy,
    string TypeName,
    string TypeFullName,
    string TypeFullyQName,
    EquatableImmutableArray<VariantData> Variants,
    EquatableImmutableArray<TypeUnionInterfaceParseInfo>? SharedInterfaces,
    TypeUnionDataOptions Options
)
{
    public bool IsRefStruct => Variants.Any(x => x.TypeData.IsRefLikeType);

    public static TypeUnionData Create(TypeUnionParseInfo parseInfo)
    {
        return new TypeUnionData(
            parseInfo.TypeHierarchy,
            parseInfo.TypeName,
            parseInfo.TypeFName,
            parseInfo.TypeFQName,
            parseInfo.Variants.Select(x => VariantData.Create(x)).ToEquatableImmutableArray(),
            parseInfo.SharedInterfaces,
            parseInfo.Options
        );
    }
}

record struct TypeUnionDataOptions(
    bool GenerateDangerousMembers,
    bool AlwaysGenerateSeparateMethodsForRefStruct
);
