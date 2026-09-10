#pragma warning disable CS9113 // Parameter is unread.

namespace Trarizon.Library.Functional.CompilerServices;

/// <summary>
/// Marked on type parameters of As&lt;T>, Is&lt;T>, etc. methods of generated union
/// </summary>
[AttributeUsage(AttributeTargets.GenericParameter)]
public sealed class GeneratedTypeUnionVariantTypeParameterAttribute(
    bool allowsBaseTypes
) : Attribute;
