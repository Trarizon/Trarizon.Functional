using System.Runtime.CompilerServices;

namespace Trarizon.Library.Functional;

public static partial class ResultExtensions
{
    public static Result.FailureBuilder<TResultError> SelectError<TError, TResultError>(this Result.FailureBuilder<TError> self, Func<TError, TResultError> selector)
        => Result.Failure(selector(self.Error));

    public static Result<T, TResultError> SelectError<T, TError, TResultError>(this Result<T, TError> self, Func<TError, TResultError> selector)
        => self.IsFailure ? Result.Failure(selector(self.Error)) : Result.Success(self.Value);

#if REF_MONAD

    [OverloadResolutionPriority(-1)]
    public static RefResult.FailureBuilder<TResultError> SelectError<TError, TResultError>(this Result.FailureBuilder<TError> self, RefFunc<TError, TResultError> selector)
        where TResultError : allows ref struct
        => Result.Failure(selector(self.Error));

    public static RefResult.FailureBuilder<TResultError> SelectError<TError, TResultError>(this RefResult.FailureBuilder<TError> self, Func<TError, TResultError> selector)
        where TError : allows ref struct
        where TResultError : allows ref struct
        => Result.Failure(selector(self.Error));

    [OverloadResolutionPriority(-1)]
    public static RefResult<T, TResultError> SelectError<T, TError, TResultError>(this Result<T, TError> self, RefFunc<TError, TResultError> selector)
        where TResultError : allows ref struct
        => self.IsFailure ? Result.Failure(selector(self.Error)) : RefResult.Success(self.Value);

    public static RefResult<T, TResultError> SelectError<T, TError, TResultError>(this RefResult<T, TError> self, Func<TError, TResultError> selector)
        where T : allows ref struct
        where TError : allows ref struct
        where TResultError : allows ref struct
        => self.IsFailure ? Result.Failure(selector(self.Error)) : RefResult.Success(self.Value);

#endif
}
