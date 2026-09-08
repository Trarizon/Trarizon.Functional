namespace Trarizon.Library.Functional.CompilerServices;

/// <summary>
/// Marked on type parameters of As&lt;T>, Is&lt;T>, etc. methods of generated union
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public sealed class GeneratorUseOnlyAttribute : Attribute
{
}
