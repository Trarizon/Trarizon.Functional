using System.Diagnostics.CodeAnalysis;

namespace Trarizon.Library.Functional.CompilerServices;

public static class GeneratorHelpers
{
    [DoesNotReturn]
    public static void ThrowNullReferenceException()
        => throw new NullReferenceException();

    [DoesNotReturn]
    public static void ThrowUnknownUnionCaseException()
        => throw new ArgumentOutOfRangeException();
}
