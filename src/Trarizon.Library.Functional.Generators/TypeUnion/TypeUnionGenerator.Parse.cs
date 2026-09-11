using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using Trarizon.Library.Roslyn;
using Trarizon.Library.Roslyn.Pipeline;

namespace Trarizon.Library.Functional.Generators.TypeUnion;

partial class TypeUnionGenerator
{
    private TypeUnionParseInfo? Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context is not
            {
                TargetNode: StructDeclarationSyntax syntax,
                TargetSymbol: INamedTypeSymbol symbol,
                Attributes: [var attr]
            })
            return null;

        var variantTypes = attr.GetConstructorArgument(0).CastArray<ITypeSymbol>();
        return ParseCore(syntax, symbol, attr, variantTypes, cancellationToken);
    }

    private TypeUnionParseInfo? ParseGeneric(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context is not
            {
                TargetNode: StructDeclarationSyntax syntax,
                TargetSymbol: INamedTypeSymbol symbol,
                Attributes: [var attr]
            })
            return null;

        if (attr.AttributeClass is null)
            return null;

        var variantTypes = attr.AttributeClass.TypeArguments;
        return ParseCore(syntax, symbol, attr, variantTypes, cancellationToken);
    }

    private TypeUnionParseInfo? ParseCore(StructDeclarationSyntax syntax, INamedTypeSymbol symbol, AttributeData attr, ImmutableArray<ITypeSymbol> variantTypes, CancellationToken cancellationToken)
    {
        if (variantTypes.Length == 0)
            return null;

        var variantSet = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        int unmanagedIdx = 0;
        int managedIdx = 0;

        var readableNameMap = new Dictionary<string, int>();

        var variantDatas = ImmutableArray.CreateBuilder<VariantParseInfo>(variantTypes.Length);
        var index = 0;
        for (int i = 0; i < variantTypes.Length; i++)
        {
            if (!variantSet.Add(variantTypes[i]))
                continue;

            var type = variantTypes[i];
            uint id = (uint)index + 1;
            index++;

            uint fieldId;
            if (type.IsReferenceType)
            {
                fieldId = default;
            }
            else if (type.IsUnmanagedType)
            {
                var idx = unmanagedIdx++;
                fieldId = (uint)idx;
            }
            else
            {
                var idx = managedIdx++;
                fieldId = (uint)idx;
            }

            var typeData = VariantTypeData.Create(type);

            var data = new VariantParseInfo(
                id, typeData, fieldId, GetUniqueReadableName(type, typeData, readableNameMap)
            );
            variantDatas.Add(data);

            string GetDefaultReadableName(ITypeSymbol type, VariantTypeData data)
            {
                var str = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                var res = (stackalloc char[str.Length]);

                var idx = 0;
                foreach (var c in str.AsSpan())
                {
                    // skip consecutive underscores
                    char printc = '_';
                    if (c is '_' or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
                        printc = c;

                    if (idx > 0 && res[idx - 1] == '_' && printc == '_')
                        continue;
                    res[idx++] = printc;
                }

                // remove trailing underscores
                while (idx > 0 && res[idx - 1] == '_')
                    idx--;

                var start = 0;
                if (data.FinalPointerAtType.TypeKind is VariantTypeKind.FunctionPointer)
                    start = "delegate".Length;

                // remove leading underscores;
                while (start < idx && res[start] == '_')
                    start++;

                return res[start..idx].ToString();
            }

            string GetUniqueReadableName(ITypeSymbol type, VariantTypeData data, Dictionary<string, int> map)
            {
                var name = GetDefaultReadableName(type, data);

                if (!map.TryGetValue(name, out var idx))
                {
                    map.Add(name, 0);
                    return name;
                }

            Inc:
                idx++;
                var resultName = $"{name}_{idx}";
                if (map.ContainsKey(resultName))
                {
                    goto Inc;
                }

                map.Add(resultName, 0);
                return resultName;
            }
        }

        var shareInterfaceOption = attr.GetNamedArgument("ShareInterfaces").CastValueOrDefault<UnionShareInterfaceOption>();

        EquatableImmutableArray<TypeUnionInterfaceParseInfo>? sharedInterfaces = null;
        if (shareInterfaceOption == UnionShareInterfaceOption.Explicit)
        {
            sharedInterfaces = ParseSharedInterfaces(variantTypes);
        }

        return new TypeUnionParseInfo(
            TypeHierarchyInfo.Create(symbol),
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            symbol.ToDisplayString(),
            symbol.Name,
            new TypeUnionDataOptions(
                GenerateDangerousMembers: attr.GetNamedArgument("GenerateDangerousMembers").CastValueOrDefault<bool>(),
                AlwaysGenerateSeparateMethodsForRefStruct: attr.GetNamedArgument("AlwaysGenerateSeparateMethodsForRefStruct").CastValueOrDefault<bool>()
            ),
            variantDatas.DrainToImmutable(),
            sharedInterfaces
        );
    }

    private EquatableImmutableArray<TypeUnionInterfaceParseInfo> ParseSharedInterfaces(ImmutableArray<ITypeSymbol> variantTypes)
    {
        if (variantTypes.Length == 0)
            return [];

        var first = variantTypes[0];

        if (variantTypes.Length == 1)
        {
            if (first.TypeKind is TypeKind.Interface && IsValidInterface(first))
            {
                return [
                    InterfaceToData(first),
                        .. first.AllInterfaces.Where(IsValidInterface).Select(InterfaceToData)
                ];
            }
            else
                return first.AllInterfaces.Where(IsValidInterface).Select(InterfaceToData).ToEquatableImmutableArray();
        }

        var firstInterfaces = first.TypeKind is TypeKind.Interface
            ? first.AllInterfaces.Prepend(first).Where(IsValidInterface)
            : first.AllInterfaces.Where(IsValidInterface);

        var set = new HashSet<ITypeSymbol>(firstInterfaces, SymbolEqualityComparer.Default);
        foreach (var type in variantTypes)
        {
            var intfs = type.TypeKind is TypeKind.Interface
                ? type.AllInterfaces.Prepend(type).Where(IsValidInterface)
                : type.AllInterfaces.Where(IsValidInterface);
            set.IntersectWith(intfs);
        }
        return set.Select(InterfaceToData).ToEquatableImmutableArray();

        // If interface has any static member, we do not implement it
        bool IsValidInterface(ITypeSymbol intfType)
        {
            return intfType.GetMembers().All(x =>
            {
                if (x.IsStatic)
                    return false;
                return true;
            });
        }

        TypeUnionInterfaceParseInfo InterfaceToData(ITypeSymbol type)
        {
            var fqname = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var members = type.GetMembers().Where(x =>
            {
                if (x.IsImplicitlyDeclared)
                    return false;
                if (x is IMethodSymbol m)
                    return m.MethodKind is MethodKind.Ordinary;
                return true;
            });
            return new TypeUnionInterfaceParseInfo(
                fqname,
                members.Select(CollectInterfaceMemberData).ToEquatableImmutableArray());
        }

        TypeUnionInterfaceMemberData CollectInterfaceMemberData(ISymbol symbol)
        {
            if (symbol is IPropertySymbol prop)
            {
                if (prop.IsIndexer)
                {
                    return new TypeUnionInterfaceMemberData(
                        prop.Name,
                        InterfaceMemberKind.Indexer,
                        prop.IsStatic,
                        prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedWithNullableAnnotation),
                        prop switch
                        {
                            { ReturnsByRefReadonly: true } => RefKind.RefReadOnly,
                            { ReturnsByRef: true } => RefKind.Ref,
                            _ => RefKind.None,
                        },
                        prop.Parameters.Select(SelectParameterData).ToEquatableImmutableArray(),
                        prop.ExplicitInterfaceImplementations.Length > 0
                    )
                    {
                        HasGetOrAddAccessor = prop.GetMethod is not null,
                        HasSetOrRemoveAccessor = prop.SetMethod is not null,
                        IsInitAccessor = prop.SetMethod?.IsInitOnly ?? false,
                    };
                }
                else
                {
                    return new TypeUnionInterfaceMemberData(
                        prop.Name,
                        InterfaceMemberKind.Property,
                        prop.IsStatic,
                        prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedWithNullableAnnotation),
                        prop switch
                        {
                            { ReturnsByRefReadonly: true } => RefKind.RefReadOnly,
                            { ReturnsByRef: true } => RefKind.Ref,
                            _ => RefKind.None,
                        },
                        [],
                        prop.ExplicitInterfaceImplementations.Length > 0
                    )
                    {
                        HasGetOrAddAccessor = prop.GetMethod is not null,
                        HasSetOrRemoveAccessor = prop.SetMethod is not null,
                        IsInitAccessor = prop.SetMethod?.IsInitOnly ?? false,
                    };
                }
            }
            if (symbol is IEventSymbol ev)
            {
                return new TypeUnionInterfaceMemberData(
                    ev.Name,
                    InterfaceMemberKind.Event,
                    ev.IsStatic,
                    ev.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedWithNullableAnnotation),
                    RefKind.None,
                    [],
                    ev.ExplicitInterfaceImplementations.Length > 0
                )
                {
                    HasGetOrAddAccessor = ev.AddMethod is not null,
                    HasSetOrRemoveAccessor = ev.RemoveMethod is not null,
                };
            }
            if (symbol is IMethodSymbol m)
            {
                return new TypeUnionInterfaceMemberData(
                    m.Name,
                    InterfaceMemberKind.Method,
                    m.IsStatic,
                    m.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedWithNullableAnnotation),
                    m switch
                    {
                        { ReturnsByRefReadonly: true } => RefKind.RefReadOnly,
                        { ReturnsByRef: true } => RefKind.Ref,
                        _ => RefKind.None,
                    },
                    m.Parameters.Select(SelectParameterData).ToEquatableImmutableArray(),
                    m.ExplicitInterfaceImplementations.Length > 0
                )
                {
                    TypeParameters = m.TypeParameters.Select(x => new TypeParameterInfo(x.Name, x.Variance)).ToEquatableImmutableArray()
                };
            }

            return default;
        }

        ParameterInfo SelectParameterData(IParameterSymbol param)
        {
#if ROSLYN_4_9_2_OR_GREATER
            var @scoped = param.ScopedKind is ScopedKind.None ? "" : "scoped";
#else
            var @scoped = "";
#endif
            return new ParameterInfo(
                param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedWithNullableAnnotation),
                param.RefKind,
                @scoped,
                param.Name
            );
        }
    }
}
