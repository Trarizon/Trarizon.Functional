namespace Trarizon.Library.Functional.Generators.Internals;

sealed class EquatableBox<T>(T value) : IEquatable<EquatableBox<T>> where T : IEquatable<T>
{
    public T Value = value;

    public bool Equals(EquatableBox<T> other) => Value.Equals(other.Value);
}
