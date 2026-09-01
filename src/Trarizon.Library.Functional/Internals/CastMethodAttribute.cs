#pragma warning disable CS9113

namespace Trarizon.Library.Functional.Internals;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class CastMethodAttribute(byte[] typeTypeParameterIndices, byte[] methodTypeParameterIndices) : Attribute;