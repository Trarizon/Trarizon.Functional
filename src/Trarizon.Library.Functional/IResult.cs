namespace Trarizon.Library.Functional;

public interface IResultErrorVariant<TError>
#if NET9_0_OR_GREATER
    where TError : allows ref struct
#endif
{
    bool IsFailure { get; }
    TError Error { get; }
}

public interface IResult<T, TError> : IOptional<T>, IResultErrorVariant<TError>
#if NET9_0_OR_GREATER
    where T : allows ref struct
    where TError : allows ref struct
#endif
{
    bool IsSuccess { get; }
    bool IResultErrorVariant<TError>.IsFailure => !IsSuccess;
    bool IOptional<T>.HasValue => IsSuccess;
}


#if NET7_0_OR_GREATER

internal interface IResultFactory<TResult, T, TError>
#if NET9_0_OR_GREATER
    where TResult : allows ref struct
    where T : allows ref struct
    where TError : allows ref struct
#endif
{
    TResult Success(T value);
    TResult Failure(TError error);
}

internal interface IResult<T, TError, TSelf> : IResult<T, TError>, IResultFactory<TSelf, T, TError>
#if NET9_0_OR_GREATER
    where T : allows ref struct
    where TError : allows ref struct
    where TSelf : allows ref struct
#endif
{ }

#endif
