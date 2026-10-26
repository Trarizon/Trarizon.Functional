namespace Trarizon.Library.Functional;

partial struct Optional<T>
{
    public Optional<(T, T2)> Zip<T2>(Optional<T2> other)
        => HasValue && other.HasValue ? Optional.Some((Value, other.Value)) : Optional.None;

    // val + val => val
    public Optional<TResult> Zip<T2, TResult>(Optional<T2> other, Func<T, T2, TResult> selector)
        => HasValue && other.HasValue ? Optional.Some(selector(Value, other.Value)) : Optional.None;

#if REF_MONAD

    // val + ref => val
    public Optional<TResult> Zip<T2, TResult>(RefOptional<T2> other, Func<T, T2, TResult> selector)
        where T2 : allows ref struct
        => HasValue && other.HasValue ? Optional.Some(selector(Value, other.Value)) : Optional.None;

#endif
}

#if REF_MONAD

partial struct RefOptional<T>
{
    // ref + val => val
    public Optional<TResult> Zip<T2, TResult>(Optional<T2> other, Func<T, T2, TResult> selector)
        => HasValue && other.HasValue ? Optional.Some(selector(Value, other.Value)) : Optional.None;

    // ref + ref => val
    public Optional<TResult> Zip<T2, TResult>(RefOptional<T2> other, Func<T, T2, TResult> selector)
        => HasValue && other.HasValue ? Optional.Some(selector(Value, other.Value)) : Optional.None;
}

#endif

#if REF_MONAD

public static partial class OptionalExtensions
{
    // val + val => ref
    public static RefOptional<TResult> Zip<T, T2, TResult>(this Optional<T> self, Optional<T2> other, Func<T, T2, TResult> selector)
        where TResult : allows ref struct
        => self.HasValue && other.HasValue ? Optional.Some(selector(self.Value, other.Value)) : Optional.None;

    // val + ref => ref
    public static RefOptional<TResult> Zip<T, T2, TResult>(this Optional<T> self, RefOptional<T2> other, Func<T, T2, TResult> selector)
        where T2 : allows ref struct where TResult : allows ref struct
        => self.HasValue && other.HasValue ? Optional.Some(selector(self.Value, other.Value)) : Optional.None;

    // ref + val => ref
    public static RefOptional<TResult> Zip<T, T2, TResult>(this RefOptional<T> self, Optional<T2> other, Func<T, T2, TResult> selector)
        where T : allows ref struct where TResult : allows ref struct
        => self.HasValue && other.HasValue ? Optional.Some(selector(self.Value, other.Value)) : Optional.None;

    // ref + ref => ref
    public static RefOptional<TResult> Zip<T, T2, TResult>(this RefOptional<T> self, RefOptional<T2> other, Func<T, T2, TResult> selector)
        where T : allows ref struct where T2 : allows ref struct where TResult : allows ref struct
        => self.HasValue && other.HasValue ? Optional.Some(selector(self.Value, other.Value)) : Optional.None;
}

#endif
