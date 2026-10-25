using ZLinq;
using ZLinq.Linq;

namespace Trarizon.Library.Functional.ZLinq;

public static class ValueEnumerableExtensions
{
    /// <summary>
    /// Filters an sequence of optional values and returns a new sequence containing those that has value
    /// </summary>
    public static ValueEnumerable<WhereSelect<TEnumerator, Optional<T>, T>, T> WhereValues<TEnumerator, T>(this ValueEnumerable<TEnumerator, Optional<T>> source) where TEnumerator : struct, IValueEnumerator<Optional<T>>
        => source.Where(x => x.HasValue).Select(x => x.Value);

    public static ValueEnumerable<WhereSelect<TEnumerator, Result<T, TError>, T>, T> WhereValues<TEnumerator, T, TError>(this ValueEnumerable<TEnumerator, Result<T, TError>> source) where TEnumerator : struct, IValueEnumerator<Result<T, TError>>
        => source.Where(x => x.IsSuccess).Select(x => x.Value);

    public static ValueEnumerable<WhereSelect<TEnumerator, Result<T, TError>, TError>, TError> WhereErrors<TEnumerator, T, TError>(this ValueEnumerable<TEnumerator, Result<T, TError>> source) where TEnumerator : struct, IValueEnumerator<Result<T, TError>>
        => source.Where(x => x.IsFailure).Select(x => x.Error);
}
