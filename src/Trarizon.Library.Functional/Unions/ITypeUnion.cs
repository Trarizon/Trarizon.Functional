namespace Trarizon.Library.Functional.Unions;

public interface ITypeUnion
{
    bool IsNull { get; }
    
    T? As<T>()
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    ;

    bool Is<T>()
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    ;
    
    bool Is<T>(out T? value)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    ;
}
