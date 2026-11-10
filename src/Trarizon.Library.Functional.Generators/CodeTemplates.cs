namespace Trarizon.Library.Functional.Generators;

internal static class CodeTemplates
{
    public static string UnsafeAsReadOnlyRef(string sourceType, string targetType, string arg0)
    {
        return $"global::System.Runtime.CompilerServices.Unsafe.As<{sourceType}, {targetType}>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<{sourceType}>(in {arg0}))";
    }
    public static string UnsafeAs(string sourceType, string targetType, string arg0)
    {
        return $"global::System.Runtime.CompilerServices.Unsafe.As<{sourceType}, {targetType}>(ref {arg0})";
    }

    public static string ThrowNRE()
        => "global::Trarizon.Library.Functional.CompilerServices.GeneratorHelpers.ThrowNullReferenceException()";

    public static string ThrowUUCE()
        => "global::Trarizon.Library.Functional.CompilerServices.GeneratorHelpers.ThrowUnknownUnionCaseException()";
}
