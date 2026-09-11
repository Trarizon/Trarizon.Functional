using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Trarizon.Library.Roslyn;

namespace Trarizon.Library.Functional.Generators.TypeUnion.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class TypeUnionAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(VariantTypeCannotBeSelf, DuplicateTypeUnionAttribute, DuplicateVariantType);

    private static readonly DiagnosticDescriptor VariantTypeCannotBeSelf = new(
        "TRAFNL0104",
        "Variant type cannot be the type union itself",
        "The variant type '{0}' cannot be the type union itself. A type union cannot contain itself as a variant.",
        "Trarizon.Library.Functional.Unions",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor DuplicateTypeUnionAttribute = new(
        "TRAFNL0105",
        "Duplicate TypeUnionAttribute",
        "The type '{0}' is already marked with TypeUnionAttribute. A type can only be marked with one TypeUnionAttribute.",
        "Trarizon.Library.Functional.Unions",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor DuplicateVariantType = new(
        "TRAFNL0106",
        "Duplicate variant type",
        "The variant type '{0}' is duplicated in the TypeUnionAttribute. Each variant type should appear only once.",
        "Trarizon.Library.Functional.Unions",
        DiagnosticSeverity.Info,
        true);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;

            var typeUnionAttr = compilation.GetTypeByMetadataName("Trarizon.Library.Functional.Unions.TypeUnionAttribute");
            var typeUnionAttr2 = compilation.GetTypeByMetadataName("Trarizon.Library.Functional.Unions.TypeUnionAttribute`2");

            if (typeUnionAttr is null && typeUnionAttr2 is null)
                return;

            var typeUnionAttrs = new INamedTypeSymbol?[] {
                typeUnionAttr,
                typeUnionAttr2
            }.Where(x => x is not null)!.ToArray<INamedTypeSymbol>();

            context.RegisterSymbolAction(context =>
            {
                var symbol = context.Symbol;

                if (symbol is not INamedTypeSymbol typeSymbol)
                    return;

                Action_CheckDuplicateAttributes(context, typeSymbol, typeUnionAttrs, out var attrData);
                if (attrData is null)
                    return;

                var variantTypes = attrData.GetTypeUnionVariantTypes();
                Action_CheckVariantTypeBeingSelf(context, typeSymbol, attrData, variantTypes.AsSpan());
                Action_CheckDuplicateVariantTypes(context, typeSymbol, attrData, variantTypes.AsSpan());
            }, SymbolKind.NamedType);
        });
    }

    // Actions

    private void Action_CheckDuplicateAttributes(SymbolAnalysisContext context, INamedTypeSymbol typeSymbol, ReadOnlySpan<INamedTypeSymbol> typeUnionAttrs,
        out AttributeData? firstAttributeData)
    {
        // Check for duplicate TypeUnionAttribute
        firstAttributeData = null;
        foreach (var attr in typeSymbol.GetAttributes())
        {
            if (attr.AttributeClass is null)
                continue;

            if (IsTypeUnionAttribute(attr.AttributeClass, typeUnionAttrs))
            {
                if (firstAttributeData is null)
                {
                    firstAttributeData = attr;
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DuplicateTypeUnionAttribute,
                        attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None,
                        typeSymbol.ToDisplayString()
                    ));
                }
            }
        }
    }

    private void Action_CheckVariantTypeBeingSelf(SymbolAnalysisContext context, INamedTypeSymbol typeSymbol, AttributeData attr, ReadOnlySpan<ITypeSymbol> variantTypes)
    {
        // Check for variant type being the type itself
        foreach (var variantType in variantTypes)
        {
            if (SymbolEqualityComparer.Default.Equals(variantType, typeSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    VariantTypeCannotBeSelf,
                    attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None,
                    variantType.ToDisplayString()));
                return;
            }
        }
    }

    private void Action_CheckDuplicateVariantTypes(SymbolAnalysisContext context, INamedTypeSymbol typeSymbol, AttributeData attr, ReadOnlySpan<ITypeSymbol> variantTypes)
    {
        var visited = new Dictionary<ITypeSymbol, bool>(SymbolEqualityComparer.Default);
        foreach (var variantType in variantTypes)
        {
            if (!visited.TryGetValue(variantType, out var isRepeated))
            {
                visited.Add(variantType, false);
                continue;
            }
            if (!isRepeated)
            {
                // first duplicate
                visited[variantType] = true;
                context.ReportDiagnostic(Diagnostic.Create(
                    DuplicateVariantType,
                    attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None,
                    variantType.ToDisplayString()));
                continue;
            }
        }
    }

    // Utils

    private static bool IsTypeUnionAttribute(INamedTypeSymbol attrClass, params ReadOnlySpan<INamedTypeSymbol> typeUnionAttrs)
    {
        foreach (var typeUnionAttr in typeUnionAttrs)
        {
            if (SymbolEqualityComparer.Default.Equals(attrClass.OriginalDefinition, typeUnionAttr))
                return true;
        }
        return false;
    }
}