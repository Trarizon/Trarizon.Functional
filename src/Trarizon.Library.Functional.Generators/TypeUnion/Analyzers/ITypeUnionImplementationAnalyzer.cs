using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Trarizon.Library.Roslyn;

namespace Trarizon.Library.Functional.Generators.TypeUnion.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class ITypeUnionImplementationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DoNotImplementITypeUnion);

    private static readonly DiagnosticDescriptor DoNotImplementITypeUnion = new(
        "TRAFNL0103",
        "Do not implement ITypeUnion or ITypeUnion<TSelf> manually",
        "The interfaces 'ITypeUnion' and 'ITypeUnion<TSelf>' should only be implemented by source-generated code. Do not implement them manually.",
        "Trarizon.Library.Functional.Unions",
        DiagnosticSeverity.Error,
        true);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;

            if (!compilation.TryGetTypeByMetadataName(
                "Trarizon.Library.Functional.Unions.ITypeUnion",
                out var iTypeUnionSymbol))
                return;

            if (!compilation.TryGetTypeByMetadataName(
                "Trarizon.Library.Functional.Unions.ITypeUnion`1",
                out var iTypeUnionTSelfSymbol))
                return;

            var typeUnionAttr = compilation.GetTypeByMetadataName("Trarizon.Library.Functional.Unions.TypeUnionAttribute");

            context.RegisterSymbolAction(context =>
            {
                var symbol = context.Symbol;

                if (symbol is not INamedTypeSymbol typeSymbol)
                    return;

                foreach (var interfaceSymbol in typeSymbol.Interfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(interfaceSymbol.OriginalDefinition, iTypeUnionSymbol)
                        || SymbolEqualityComparer.Default.Equals(interfaceSymbol.OriginalDefinition, iTypeUnionTSelfSymbol))
                    {
                        // If we can't find the attribute, report anyway to be safe
                        if (typeUnionAttr is null || !typeSymbol.HasAttribute(typeUnionAttr))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DoNotImplementITypeUnion,
                                typeSymbol.Locations.FirstOrDefault()));
                        }
                        return;
                    }
                }
            }, SymbolKind.NamedType);
        });
    }
}