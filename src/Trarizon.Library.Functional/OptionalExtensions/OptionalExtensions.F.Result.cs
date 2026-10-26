namespace Trarizon.Library.Functional;

partial struct Optional<T>
{
    public Result<T, TError> ToResult<TError>(TError error)
        => HasValue ? Result.Success(Value) : Result.Failure(error);

    public Result<T, TError> ToResult<TError>(Func<TError> errorFactory)
        => HasValue ? Result.Success(Value) : Result.Failure(errorFactory());
}

public static partial class OptionalExtensions
{
    public static Result<Optional<T>, TError> Transpose<T, TError>(this Optional<Result<T, TError>> optional)
    {
        if (optional.HasValue)
        {
            ref readonly var result = ref optional.GetValueRefOrDefaultRef();
            return result.IsSuccess ? Optional.Some(result._value) : Result.Failure(result.Error);
        }
        return Result.Success(Optional<T>.None);
    }

#if REF_MONAD

    public static RefResult<T, TError> ToResult<T, TError>(this Optional<T> optional, TError error) where TError : allows ref struct
        => optional.HasValue ? RefResult.Success(optional.Value) : RefResult.Failure(error);

    public static RefResult<T, TError> ToResult<T, TError>(this Optional<T> optional, Func<TError> errorFactory) where TError : allows ref struct
        => optional.HasValue ? RefResult.Success(optional.Value) : RefResult.Failure(errorFactory());

    public static RefResult<T, TError> ToResult<T, TError>(this RefOptional<T> optional, TError error) where T : allows ref struct where TError : allows ref struct
        => optional.HasValue ? RefResult.Success(optional.Value) : RefResult.Failure(error);

    public static RefResult<T, TError> ToResult<T, TError>(this RefOptional<T> optional, Func<TError> errorFactory) where T : allows ref struct where TError : allows ref struct
        => optional.HasValue ? RefResult.Success(optional.Value) : RefResult.Failure(errorFactory());

    public static RefResult<RefOptional<T>, TError> Transpose<T, TError>(this RefOptional<RefResult<T, TError>> optional) where T : allows ref struct where TError : allows ref struct
    {
        if (optional.HasValue)
        {
            ref readonly var result = ref optional.GetValueRefOrDefaultRef();
            return result.IsSuccess ? RefResult.Success(RefOptional.Some(result._value)) : RefResult.Failure(result.Error);
        }
        return RefResult.Success(RefOptional<T>.None);
    }

#endif
}
