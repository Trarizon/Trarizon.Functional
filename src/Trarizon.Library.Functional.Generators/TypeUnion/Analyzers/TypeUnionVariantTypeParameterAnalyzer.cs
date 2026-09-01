using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using Trarizon.Library.Roslyn;

namespace Trarizon.Library.Functional.Generators.TypeUnion.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class TypeUnionVariantTypeParameterAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(InvalidTypeArgument);

    private static readonly DiagnosticDescriptor InvalidTypeArgument = new(
        "TRAFNL0101",
        "Invalid type argument for union method",
        "Type argument '{0}' is not a valid variant type of the union '{1}'",
        "Trarizon.Library.Functional.Unions",
        DiagnosticSeverity.Warning,
        true);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;

            if (!compilation.TryGetTypeByMetadataName(
                "Trarizon.Library.Functional.CompilerServices.GeneratedTypeUnionVariantTypeParameterAttribute",
                out var variantTypeParamAttr))
                return;
            if (!compilation.TryGetTypeByMetadataName(
                "Trarizon.Library.Functional.Unions.TypeUnionAttribute",
                out var typeUnionAttr))
                return;

            context.RegisterOperationAction(context =>
            {
                var operation = (IInvocationOperation)context.Operation;

                if (operation.Instance?.Type is not INamedTypeSymbol instanceType)
                    return;

                if (!instanceType.TryGetAttributeData(typeUnionAttr, out var typeUnionAttrData))
                    return;

                var validTypes = typeUnionAttrData.GetTypeUnionVariantTypes();

                var method = operation.TargetMethod;
                for (int i = 0; i < method.TypeParameters.Length; i++)
                {
                    var typeParam = method.TypeParameters[i];
                    if (!typeParam.TryGetAttributeData(variantTypeParamAttr, out var variantAttrData))
                        continue;

                    var typeArg = method.TypeArguments[i];

                    if (typeArg.TypeKind == TypeKind.TypeParameter)
                        continue;

                    var allowsBaseTypes = variantAttrData.GetNamedArgument("AllowsBaseTypes").CastValueOrDefault<bool>();

                    bool isValid;
                    if (allowsBaseTypes)
                    {
                        isValid = IsValidBaseType(typeArg, validTypes, compilation);
                    }
                    else
                    {
                        isValid = validTypes.Contains(typeArg, SymbolEqualityComparer.Default);
                    }

                    if (!isValid)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            InvalidTypeArgument,
                            operation.Syntax.GetLocation(),
                            typeArg.ToDisplayString(),
                            instanceType.ToDisplayString()));
                    }
                }
            }, OperationKind.Invocation);
        });
    }

    private static bool IsValidBaseType(ITypeSymbol typeArg, ImmutableArray<ITypeSymbol> validTypes, Compilation compilation)
    {
        foreach (var validType in validTypes)
        {
            if (SymbolEqualityComparer.Default.Equals(typeArg, validType))
                return true;

            var conversion = compilation.ClassifyConversion(validType, typeArg);
            if (conversion.IsIdentity || conversion.IsBoxing || conversion.IsReference)
                return true;
        }
        return false;
    }
}