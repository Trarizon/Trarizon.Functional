namespace Trarizon.Library.Functional;

public static partial class ResultExtensions
{
    public static Optional<T> ToOptional<T, TError>(this Result<T, TError> result)
    => result.IsSuccess ? Optional.Some(result._value) : Optional.None;

    public static Optional<T> ToOptional<T>(this Result.SuccessBuilder<T> result)
        => Optional.Some(result.Value);

    public static Optional<TError> ToOptionalError<T, TError>(this Result<T, TError> result)
        => result.IsFailure ? Optional.Some(result.Error) : Optional.None;

    public static Optional<TError> ToOptionalError<TError>(this Result.FailureBuilder<TError> result)
        => Optional.Some(result.Error);

    public static Optional<Result<T, TError>> Transpose<T, TError>(this Result<Optional<T>, TError> result)
    {
        if (result.IsSuccess)
        {
            ref readonly var optional = ref result._value;
            return optional.HasValue ? Optional.Some(Result.Success<T, TError>(optional.Value)) : Optional.None;
        }
        else
        {
            return Optional.Some(Result.Failure<T, TError>(result.Error));
        }
    }

#if REF_MONAD

    public static RefOptional<T> ToOptional<T, TError>(this RefResult<T, TError> result) where T : allows ref struct where TError : allows ref struct
        => result.IsSuccess ? RefOptional.Some(result._value) : RefOptional<T>.None;

    public static RefOptional<T> ToOptional<T>(this RefResult.SuccessBuilder<T> result) where T : allows ref struct
        => RefOptional.Some(result.Value);

    public static RefOptional<TError> ToOptionalError<T, TError>(this RefResult<T, TError> result) where T : allows ref struct where TError : allows ref struct
        => result.IsFailure ? RefOptional.Some(result.Error) : RefOptional<TError>.None;

    public static RefOptional<TError> ToOptionalError<TError>(this RefResult.FailureBuilder<TError> result) where TError : allows ref struct
        => RefOptional.Some(result.Error);

    public static RefOptional<RefResult<T, TError>> ToOptional<T, TError>(this RefResult<RefOptional<T>, TError> result) where T : allows ref struct where TError : allows ref struct
    {
        if (result.IsSuccess)
        {
            return result._value.TryGetValue(out var value) ? RefOptional.Some(RefResult.Success<T, TError>(value)) : RefOptional<RefResult<T, TError>>.None;
        }
        else
        {
            return RefOptional.Some(RefResult.Failure<T, TError>(result.Error));
        }
    }

#endif
}
