using System.Diagnostics.CodeAnalysis;

namespace Trarizon.Library.Functional.Unions;

public interface ITypeUnion
{
    bool IsNull { get; }

    /// <summary>
    /// This behaves like the language feature <c>as</c>.<br/>
    /// Returns <see langword="default(T)"/> if value not matched.
    /// </summary>
    T? As<T>()
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    ;

    /// <summary>
    /// This behaves like the language feature <c>is</c>.<br/>
    /// Check if the value is of the type <typeparamref name="T"/>.
    /// </summary>
    bool Is<T>()
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    ;

    /// <summary>
    /// This behaves like the language feature <c>is</c>.<br/>
    /// Check if the value is of the type <typeparamref name="T"/>.
    /// If the value is of the type <typeparamref name="T"/>, it is cast to the type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    bool Is<T>([MaybeNullWhen(false)] out T value)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    ;

    // Metadata

#if NET7_0_OR_GREATER

    /// <summary>
    /// Get the variant types of this type union.
    /// </summary>
    static abstract ReadOnlySpan<Type> VariantTypes { get; }

#endif
}

public interface ITypeUnion<TSelf> : ITypeUnion
#if NET9_0_OR_GREATER
    where TSelf : ITypeUnion<TSelf>, allows ref struct
#else
    where TSelf : ITypeUnion<TSelf>
#endif
{
#if NET7_0_OR_GREATER

    /// <summary>
    /// Try to create a type union from a value of type T.<br/>
    /// This returns false if T is not exactly one of the variant types.
    /// </summary>
    static abstract bool TryCreate<T>(T value, out TSelf result)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    ;

#endif
}
