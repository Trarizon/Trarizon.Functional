using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using Trarizon.Library.Roslyn;

namespace Trarizon.Library.Functional.Generators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class GeneratorUseAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(GeneratorUseOnlyAccess);

    private static readonly DiagnosticDescriptor GeneratorUseOnlyAccess = new(
        "TRAFNL0002",
        "Generator-use-only member accessed from non-generated code",
        "The '{0}' is marked as generator-use-only and should not be accessed from non-generated code",
        "Trarizon.Library.Functional",
        DiagnosticSeverity.Error,
        true);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;

            if (!compilation.TryGetTypeByMetadataName(
                "Trarizon.Library.Functional.CompilerServices.GeneratorUseOnlyAttribute",
                out var generatorUseOnlyAttr))
                return;

            context.RegisterOperationAction(
                context => AnalyzeMemberOperation(context, generatorUseOnlyAttr),
                OperationKind.Invocation,
                OperationKind.FieldReference,
                OperationKind.PropertyReference,
                OperationKind.ObjectCreation,
                OperationKind.MethodReference,
                OperationKind.EventReference);

            context.RegisterOperationAction(
                context => AnalyzeTypeOperation(context, generatorUseOnlyAttr),
                OperationKind.VariableDeclarator,
                OperationKind.TypeOf,
                OperationKind.DefaultValue,
                OperationKind.Conversion);
        });
    }

    private static void AnalyzeMemberOperation(OperationAnalysisContext context, INamedTypeSymbol generatorUseOnlyAttr)
    {
        var operation = context.Operation;
        ISymbol? symbol = operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IFieldReferenceOperation fieldRef => fieldRef.Field,
            IPropertyReferenceOperation propRef => propRef.Property,
            IObjectCreationOperation creation => creation.Constructor,
            IMethodReferenceOperation methodRef => methodRef.Method,
            IEventReferenceOperation eventRef => eventRef.Event,
            _ => null,
        };

        if (symbol is null)
            return;

        if (!symbol.TryGetAttributeData(generatorUseOnlyAttr, out _))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            GeneratorUseOnlyAccess,
            operation.Syntax.GetLocation(),
            symbol.ToDisplayString()));
    }

    private static void AnalyzeTypeOperation(OperationAnalysisContext context, INamedTypeSymbol generatorUseOnlyAttr)
    {
        var operation = context.Operation;
        ITypeSymbol? type = operation switch
        {
            IVariableDeclaratorOperation declarator => declarator.Symbol.Type,
            ITypeOfOperation typeOf => typeOf.TypeOperand,
            IDefaultValueOperation defaultValue => defaultValue.Type,
            IConversionOperation conversion => conversion.Type,
            _ => null,
        };

        if (type is null)
            return;

        if (!type.TryGetAttributeData(generatorUseOnlyAttr, out _))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            GeneratorUseOnlyAccess,
            operation.Syntax.GetLocation(),
            type.ToDisplayString()));
    }
}