using System.ComponentModel;
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

    [DoesNotReturn]
    public static void ThrowInvalidCastException(string message)
        => throw new InvalidCastException(message);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GeneratorUseOnly]
    public readonly struct VoidSentinel;
}
