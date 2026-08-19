using Microsoft.CodeAnalysis;
using Trarizon.Library.Roslyn;
using CsTypeKind = Microsoft.CodeAnalysis.TypeKind;

namespace Trarizon.Library.Functional.Generators.TypeUnion;

sealed record VariantData(
    int Id,
    VariantTypeData TypeData,
    int FieldId,
    // Name that can use as identifier in code, MinimalQualifiedFormat maybe with number suffix
    // for pointer type, it is the original type name,
    string ReadableIdentifier
);

record VariantTypeData(
    string FullName,
    string FullyQName,
    string MinimalQName,
    VariantTypeKind TypeKind,
    bool IsRefLikeType,
    bool IsInterface,
    // For types that cannot use EqualityComparer<T> to compare, use this
    VariantTypeEqualityKind EqualityKind,
    // Pointer type has a subtype data
    VariantTypeData? SubtypeData
)
{
    public bool IsObjectDerived => !IsRefLikeType && TypeKind is VariantTypeKind.Managed or VariantTypeKind.Reference or VariantTypeKind.Unmanaged;

    public int PointerLevel => TypeKind is VariantTypeKind.Pointer ? 1 + SubtypeData!.PointerLevel : 0;
    public bool IsNonVoidPointer => TypeKind is VariantTypeKind.Pointer && !(SubtypeData!.TypeKind is VariantTypeKind.Void || SubtypeData.IsVoidPointer);
    public bool IsVoidPointer => TypeKind is VariantTypeKind.Pointer && (SubtypeData!.TypeKind is VariantTypeKind.Void || SubtypeData.IsVoidPointer);
    public VariantTypeData FinalPointerAtType => TypeKind is VariantTypeKind.Pointer ? SubtypeData!.FinalPointerAtType : this;

    public static VariantTypeData Create(ITypeSymbol type)
    {
        var fname = type.ToDisplayString();
        var fqname = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var mqname = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        VariantTypeKind vtk;
        bool isInterface;
        VariantTypeEqualityKind equalityKind;
        VariantTypeData? sub = null;
        if (type.IsReferenceType)
        {
            vtk = VariantTypeKind.Reference;
            isInterface = type.TypeKind is CsTypeKind.Interface;
            equalityKind = VariantTypeEqualityKind.Comparer;
        }
        else if (type.IsUnmanagedType)
        {
            switch (type)
            {
                case { TypeKind: CsTypeKind.Pointer }:
                    sub = Create(((IPointerTypeSymbol)type).PointedAtType);
                    vtk = VariantTypeKind.Pointer;
                    equalityKind = VariantTypeEqualityKind.OperatorEq;
                    break;
                case { SpecialType: SpecialType.System_Void }:
                    vtk = VariantTypeKind.Void;
                    equalityKind = VariantTypeEqualityKind.Unit;
                    break;
                case { TypeKind: CsTypeKind.FunctionPointer }:
                    vtk = VariantTypeKind.FunctionPointer;
                    equalityKind = VariantTypeEqualityKind.OperatorEq;
                    break;
                default:
                    vtk = VariantTypeKind.Unmanaged;
                    equalityKind = type.IsRefLikeType ? GetRefStructNonGenericEqualityKind(type) : VariantTypeEqualityKind.Comparer;
                    break;
            }
            isInterface = false;
        }
        else
        {
            vtk = VariantTypeKind.Managed;
            equalityKind = type.IsRefLikeType ? GetRefStructNonGenericEqualityKind(type) : VariantTypeEqualityKind.Comparer;
            isInterface = false;
        }
        return new(fname, fqname, mqname, vtk, type.IsRefLikeType, isInterface, equalityKind, sub);

        static VariantTypeEqualityKind GetRefStructNonGenericEqualityKind(ITypeSymbol type)
        {
            // Implements IEquatable<T>
            if (type.IsImplementsByFullyQualifiedMetadataName("System.IEquatable`1", out var equatableSymbol) && SymbolEqualityComparer.Default.Equals(type, equatableSymbol.TypeArguments[0]))
                return VariantTypeEqualityKind.IEquatable;
            // Has operator ==
            if (type.GetMembers().OfType<IMethodSymbol>().Any(m => IsOperatorEq(type, m)))
                return VariantTypeEqualityKind.OperatorEq;
            return VariantTypeEqualityKind.None;

            static bool IsOperatorEq(ITypeSymbol type, IMethodSymbol symbol)
            {
                return symbol.MethodKind is MethodKind.UserDefinedOperator
                    && symbol.Name == "op_Equality"
                    && symbol.Parameters.Length == 2
                    && SymbolEqualityComparer.Default.Equals(symbol.Parameters[0].Type, type)
                    && SymbolEqualityComparer.Default.Equals(symbol.Parameters[1].Type, type);
            }
        }
    }
}

enum VariantTypeKind { Managed, Reference, Unmanaged, Pointer, FunctionPointer, Void, }

enum VariantTypeEqualityKind { None, Unit, Comparer, IEquatable, OperatorEq }

static partial class DataExtensions
{
    extension(VariantTypeKind kind)
    {
        public bool IsUnmanaged => kind is VariantTypeKind.Unmanaged || kind.IsPointer;
        public bool IsGenericable => kind is not VariantTypeKind.Void && !kind.IsPointer;
        public bool IsPointer => kind is VariantTypeKind.Pointer or VariantTypeKind.FunctionPointer;
    }

    extension(VariantTypeEqualityKind kind)
    {
        public bool IsValid => kind is not VariantTypeEqualityKind.None;
    }
}