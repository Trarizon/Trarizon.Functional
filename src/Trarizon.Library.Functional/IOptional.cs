namespace Trarizon.Library.Functional;

public interface IOptional<T>
#if NET9_0_OR_GREATER
    where T : allows ref struct
#endif
{
    bool HasValue { get; }
    T Value { get; }
}

#if NET7_0_OR_GREATER

internal interface IOptionalFactory<TSelf, T>
#if NET9_0_OR_GREATER
    where TSelf : allows ref struct
    where T : allows ref struct
#endif
{
    TSelf Some(T value);
    TSelf None();
}

internal interface IOptional<T, TSelf> : IOptional<T>, IOptionalFactory<TSelf, T>
#if NET9_0_OR_GREATER   
    where T : allows ref struct
    where TSelf : allows ref struct
#endif
{ }

#endif
