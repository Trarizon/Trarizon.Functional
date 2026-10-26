using System.Runtime.CompilerServices;

namespace Trarizon.Library.Functional;

partial struct Optional<T>
{
    public Optional<TResult> Select<TResult>(Func<T, TResult> selector)
        => HasValue ? Optional.Some(selector(Value)) : Optional.None;

    
    public Optional<TResult> Select<TState, TResult>(TState state, Func<T, TState, TResult> selector)
#if NET9_0_OR_GREATER
        where TState : allows ref struct
#endif
        => HasValue ? Optional.Some(selector(Value, state)) : Optional.None;
}

#if REF_MONAD

partial struct RefOptional<T>
{
    public Optional<TResult> Select<TResult>(Func<T, TResult> selector)
        => HasValue ? Optional.Some(selector(Value)) : Optional.None;

    public Optional<TResult> Select<TState, TResult>(TState state, Func<T, TState, TResult> selector)
#if NET9_0_OR_GREATER
        where TState : allows ref struct
#endif
        => HasValue ? Optional.Some(selector(Value, state)) : Optional.None;
}

#endif

#if REF_MONAD

public static partial class OptionalExtensions
{
    public static RefOptional<TResult> Select<T, TResult>(this Optional<T> self, Func<T, TResult> selector)
        where TResult : allows ref struct
        => self.HasValue ? Optional.Some(selector(self.Value)) : Optional.None;

    public static RefOptional<TResult> Select<T, TResult>(this RefOptional<T> self, Func<T, TResult> selector)
        where T : allows ref struct where TResult : allows ref struct
        => self.HasValue ? Optional.Some(selector(self.Value)) : Optional.None;

    #region With State

    public static RefOptional<TResult> Select<T, TState, TResult>(this Optional<T> self, TState state, Func<T, TState, TResult> selector)
        where TState : allows ref struct where TResult : allows ref struct
        => self.HasValue ? Optional.Some(selector(self.Value, state)) : Optional.None;

    public static RefOptional<TResult> Select<T, TState, TResult>(this RefOptional<T> self, TState state, Func<T, TState, TResult> selector)
        where T : allows ref struct where TState : allows ref struct where TResult : allows ref struct
        => self.HasValue ? Optional.Some(selector(self.Value, state)) : Optional.None;

    #endregion
}

#endif
