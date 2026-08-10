using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using Trarizon.Library.Roslyn;

namespace Trarizon.Library.Functional.Generators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class MonadCastAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [InvalidCast];

    private static readonly DiagnosticDescriptor InvalidCast = new(
        "TRAFNL0001",
        "Invalid cast",
        "Cast type '{0}' to '{1}' will alway fail",
        "Trarizon.Library.Functional",
        DiagnosticSeverity.Warning,
        true);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;

            if (!compilation.TryGetTypeByMetadataName("Trarizon.Library.Functional.Internals.CastMethodAttribute", out var attrSymbol))
                return;

            context.RegisterOperationAction(AnalysisAction(attrSymbol), OperationKind.Invocation);
        });
    }

    private Action<OperationAnalysisContext> AnalysisAction(INamedTypeSymbol attrTypeSymbol)
    {
        return context =>
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.Instance?.Type is not INamedTypeSymbol instanceType)
                return;

            if (!operation.TargetMethod.OriginalDefinition.TryGetAttributeData(attrTypeSymbol, out var attr))
                return;

            var typeTypPrmIndices = attr.GetConstructorArgument(0).CastArray<int>();
            var methodTypePrmIndices = attr.GetConstructorArgument(1).CastArray<int>();

            var length = Math.Min(typeTypPrmIndices.Length, methodTypePrmIndices.Length);
            for (int i = 0; i < length; i++)
            {
                var fromType = instanceType.TypeArguments[typeTypPrmIndices[i]];
                var toType = operation.TargetMethod.TypeArguments[methodTypePrmIndices[i]];

                if (MaybeCastable(fromType, toType, context.Compilation) is false)
                    context.ReportDiagnostic(CreateDiagnostic(operation, fromType, toType));
            }
        };
    }

    private static bool? MaybeCastable(ITypeSymbol from, ITypeSymbol to, Compilation compilation)
    {
        if (from.TypeKind is TypeKind.Unknown or TypeKind.Error or TypeKind.TypeParameter)
            return null;
        if (to.TypeKind is TypeKind.Unknown or TypeKind.Error or TypeKind.TypeParameter)
            return null;

        var conversion = compilation.ClassifyConversion(from, to);
        if (conversion.IsIdentity || conversion.IsBoxing || conversion.IsUnboxing || conversion.IsReference)
            return true;
        return false;
    }

    private Diagnostic CreateDiagnostic(IInvocationOperation operation, ITypeSymbol from, ITypeSymbol to)
    {
        return Diagnostic.Create(InvalidCast, operation.Syntax.GetLocation(), from.ToDisplayString(), to.ToDisplayString());
    }
}