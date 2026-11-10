using Microsoft.CodeAnalysis.CSharp;

namespace Trarizon.Library.Functional.Generators.TypeUnion;

internal sealed record class Env(
    bool MaybeNull,
    bool UnscopedRef,
    bool UnionAttribute,
    bool IUnion,
    string? TargetFramework,
    LanguageVersion LanguageVersion
)
{
    public bool AllowsRefStruct
    {
        get
        {
            if (TargetFramework is null)
                return false;
#if ROSLYN_4_9_2_OR_GREATER
            if (LanguageVersion < LanguageVersion.CSharp13)
                return false;
#endif
            if (TargetFramework.StartsWith("net"))
            {
                if (Version.TryParse(TargetFramework[3..], out var v))
                    return v.Major >= 9;
            }
            return false;
        }
    }

    public bool GenericMath
    {
        get
        {
            if (TargetFramework is null)
                return false;
            if (TargetFramework.StartsWith("net"))
            {
                if (Version.TryParse(TargetFramework[3..], out var v))
                    return v.Major >= 7;
            }
            return false;
        }
    }

    public bool CollectionExpression
    {
        get
        {
            if (TargetFramework is null)
                return false;
#if ROSLYN_4_9_2_OR_GREATER
            if (LanguageVersion > LanguageVersion.CSharp12)
                return true;
#endif
            return false;
        }
    }
}

