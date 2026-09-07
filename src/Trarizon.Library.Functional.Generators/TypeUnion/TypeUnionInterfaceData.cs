using Microsoft.CodeAnalysis;
using Trarizon.Library.Roslyn.Pipeline;

namespace Trarizon.Library.Functional.Generators.TypeUnion;

public enum UnionShareInterfaceOption { Disabled, Explicit, }

record struct TypeUnionInterfaceParseInfo(
    string TypeFQName,
    EquatableImmutableArray<TypeUnionInterfaceMemberData> Members
);

record struct TypeUnionInterfaceMemberData(
    string Name,
    InterfaceMemberKind Kind,
    bool IsStatic, // The property is currently have no use, as interface with static member will not be generated
    string ReturnTypeFQName,
    RefKind ReturnTypeRefKind,
    EquatableImmutableArray<ParameterInfo> Parameters,
    bool ExplicitImplemented
)
{
    public bool HasGetOrAddAccessor { get; init; }
    public bool HasSetOrRemoveAccessor { get; init; }
    public bool IsInitAccessor { get; init; } // If true, HasSetOrRemoveAccessor must be true
    public EquatableImmutableArray<TypeParameterInfo> TypeParameters { get; init; } = [];

    public bool ReturnsVoid => ReturnTypeFQName == "void";
}

record struct ParameterInfo(
    string TypeFQName,
    RefKind RefKind,
    string? NonRefModifiers,
    string Name
);

record struct TypeParameterInfo(
    string Name,
    VarianceKind Variance
);

enum InterfaceMemberKind { Invalid, Property, Indexer, Event, Method };
