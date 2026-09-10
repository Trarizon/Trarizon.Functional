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
    /// If this union stores the variant of type <typeparamref name="T"/>, it is cast to the type <typeparamref name="T"/>.
    /// </summary>
    T? AsExactly<T>()
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

    /// <summary>
    /// Check if this union stores the variant of type <typeparamref name="T"/>.
    /// </summary>
    bool IsExactly<T>()
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    ;

    /// <summary>
    /// Check if this union stores the variant of type <typeparamref name="T"/>.
    /// If this union stores the variant of type <typeparamref name="T"/>, it is cast to the type <typeparamref name="T"/>.
    /// </summary>
    bool IsExactly<T>([MaybeNullWhen(false)] out T value)
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

    /// <summary>
    /// Get the flag value of the variant type <typeparamref name="TVariant"/>.
    /// </summary>
    static abstract uint GetFlagValue<TVariant>()
#if NET9_0_OR_GREATER
        where TVariant : allows ref struct
#endif
    ;

    /// <summary>
    /// Get the flag value of the variant type <paramref name="type"/>.
    /// </summary>
    /// <returns>
    /// The flag value of the variant type <paramref name="type"/>.
    /// <c>0u</c> if <paramref name="type"/> is not a variant type of this type union.
    /// </returns>
    static abstract uint GetFlagValue(Type type);

    /// <summary>
    /// Get the variant type that the flag value <paramref name="flagValue"/> represents.
    /// </summary>
    /// <returns>
    /// The variant type that the flag value <paramref name="flagValue"/> represents.
    /// <see langword="null"/> if <paramref name="flagValue"/> is <c>0u</c> or if the flag value is not defined.
    /// </returns>
    static abstract Type? GetFlagType(uint flagValue);

    /// <summary>
    /// Check if the flag value <paramref name="flagValue"/> is defined. <br/>
    /// Note that for flag value <c>0u</c>, this returns false.
    /// </summary>
    static abstract bool IsFlagDefined(uint flagValue);

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

    /// <summary>
    /// Try to create a type union from a flag value and a value of type T.<br/>
    /// Type T must be assignable to the type that <paramref name="flagValue"/> represents.
    /// </summary>
    static abstract bool TryCreate<T>(uint flagValue, T value, out TSelf result)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    ;

    static bool ITypeUnion.IsFlagDefined(uint flagValue)
        => flagValue >= 1 && flagValue <= TSelf.VariantTypes.Length;

    static Type? ITypeUnion.GetFlagType(uint flagValue)
    {
        if (!TSelf.IsFlagDefined(flagValue))
            return null;
        return TSelf.VariantTypes[unchecked((int)flagValue - 1)];
    }

#endif
}
