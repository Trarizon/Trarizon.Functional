using Trarizon.Library.Roslyn.Pipeline;
using Trarizon.Library.Roslyn.Pipeline.Collections;

namespace Trarizon.Library.Functional.Generators.TypeUnion;

sealed record TypeUnionData(
    string FileHintName,
    TypeHierarchyInfo TypeHierarchy,
    string TypeName,
    string TypeFullName,
    string TypeFullyQName,
    SequenceEquatableImmutableArray<VariantData> Variants,
    TypeUnionDataOptions Options
);

sealed record TypeUnionDataOptions(
    bool GenerateDangerousMembers
);
