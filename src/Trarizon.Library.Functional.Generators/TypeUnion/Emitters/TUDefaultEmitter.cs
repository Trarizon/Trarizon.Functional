using System.CodeDom.Compiler;
using System.Diagnostics;
using Trarizon.Library.Roslyn.Emitting;

namespace Trarizon.Library.Functional.Generators.TypeUnion.Emitters;

internal sealed class TUDefaultEmitter : ITypeUnionEmitter
{
    private const string f_flag = "__um_flag";
    private const string f_obj = "__um_obj";
    private const string f_managed_pfx = "__um_managed";
    private const string f_unmanaged = "__um_unmanaged";
    private const string t_unmanaged = "__ut_Unmanaged";
    private const string f_ptr = "_ptr";
    private const string f_unmanaged_pfx = "_";

    private string ExprVariantFieldAccess(VariantData variant, string @this = "this")
    {
        return variant.TypeData.TypeKind switch
        {
            VariantTypeKind.FunctionPointer => $"{@this}.{f_unmanaged}.{f_ptr}",
            VariantTypeKind.Managed => $"{@this}.{f_managed_pfx}{variant.FieldId}",
            VariantTypeKind.Pointer => $"{@this}.{f_unmanaged}.{f_ptr}",
            VariantTypeKind.Reference => $"{@this}.{f_obj}",
            VariantTypeKind.Unmanaged => $"{@this}.{f_unmanaged}.{f_unmanaged_pfx}{variant.FieldId}",
            var v => throw new NotSupportedException($"TypeKind {v} does not have related field"),
        };
    }

    private string ExprVariantRefToT(VariantData variant, string targetType = "T", string @this = "this")
    {
        var fieldFQTypeName = variant.TypeData.TypeKind switch
        {
            VariantTypeKind.Reference => "object",
            { IsPointer: true } => "global::System.IntPtr",
            _ => variant.TypeData.FullyQName,
        };
        if (fieldFQTypeName == targetType)
            return ExprVariantFieldAccess(variant, @this);
        return $"global::System.Runtime.CompilerServices.Unsafe.As<{fieldFQTypeName}, {targetType}>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<{fieldFQTypeName}>(in {ExprVariantFieldAccess(variant, @this)}))";
    }

    private string ExprVariantToT(VariantData variant, string? targetType = null, string @this = "this")
    {
        if (!variant.TypeData.TypeKind.IsPointer)
            return ExprVariantRefToT(variant, targetType ?? "T", @this);
        if (targetType is null)
        {
            var chars = (stackalloc char[variant.TypeData.PointerLevel + 1]);
            chars.Fill('*');
            chars[0] = 'T';
            targetType = chars.ToString();
        }
        return $"({targetType}){ExprVariantFieldAccess(variant, @this)}";
    }

    public void EmitFields(IndentedTextWriter writer, TypeUnionData data)
    {
        var flagType = data.Variants.Length switch
        {
            <= byte.MaxValue => "byte",
            <= short.MaxValue => "ushort",
            _ => "uint",
        };
        writer.WriteLine($"private readonly {flagType} {f_flag};");

        if (data.Variants.Any(x => x.TypeData.TypeKind is VariantTypeKind.Reference))
        {
            writer.WriteLine($"private readonly object? {f_obj};");
        }
        foreach (var variant in data.Variants.Where(x => x.TypeData.TypeKind is VariantTypeKind.Managed))
        {
            writer.WriteLine($"private readonly {variant.TypeData.FullyQName} {f_managed_pfx}{variant.FieldId};");
        }
        if (data.Variants.Any(x => x.TypeData.TypeKind.IsUnmanaged))
        {
            writer.WriteLine($"private readonly {t_unmanaged} {f_unmanaged};");
        }
    }

    public void EmitConstructorBody(IndentedTextWriter writer, TypeUnionData data, VariantData variant, string arg0)
    {
        writer.WriteLine($"this.{f_flag} = {variant.Id};");

        if (variant.TypeData.TypeKind is VariantTypeKind.Void)
        { }
        else if (variant.TypeData.TypeKind.IsPointer)
        {
            writer.WriteLine($"{ExprVariantFieldAccess(variant)} = (global::System.IntPtr){arg0};");
        }
        else
        {
            writer.WriteLine($"{ExprVariantFieldAccess(variant)} = {arg0};");
        }
    }

    public void EmitIsNullPropertyBody(IndentedTextWriter writer, TypeUnionData data)
    {
        writer.WriteLine($"return this.{f_flag} == 0;");
    }

    public void EmitUnionValuePropertyBody(IndentedTextWriter writer, TypeUnionData data)
    {
        writer.WriteLine($"switch (this.{f_flag})");
        using (writer.EnterBracketIndentScope('{'))
        {
            foreach (var variant in data.Variants)
            {
                writer.WriteLine($"case {variant.Id}:");
                if (variant.TypeData.TypeKind is VariantTypeKind.Reference)
                    writer.WriteLine($"    return this.{f_obj};");
                else
                    writer.WriteLine($"    return {ExprVariantToT(variant, variant.TypeData.FullyQName)};");
            }
        }
        writer.WriteLine($"return null;");
    }

    public void EmitExplicitCastMethodBody(IndentedTextWriter writer, TypeUnionData data, VariantData variant, string arg0)
    {
        writer.WriteMultipleLines($$"""
            if ({{arg0}}.{{f_flag}} != {{variant.Id}})
                global::Trarizon.Library.Functional.CompilerServices.GeneratorHelpers.ThrowInvalidCastException($"Unable to cast {{data.TypeFullName}} to {{variant.TypeData.FullName}}");
            return {{ExprVariantToT(variant, variant.TypeData.FullName, arg0)}};
            """);
    }

    public void EmitIsMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string typeArg0)
    {
        writer.WriteLine($"switch (this.{f_flag})");
        using (writer.EnterBracketIndentScope('{'))
        {
            foreach (var variant in data.Variants.Where(x => x.TypeData.TypeKind.IsGenericable))
            {
                if (env.AllowsRefStruct || !variant.TypeData.IsRefLikeType)
                {
                    writer.WriteLine($"case {variant.Id}:");
                    using (writer.EnterBracketIndentScope('{'))
                    {
                        writer.WriteLine($"if (typeof({typeArg0}) == typeof({variant.TypeData.FullyQName}))");
                        writer.WriteLine($"    return true;");
                        if (variant.TypeData.IsRefLikeType)
                            writer.WriteLine($"return false;");
                        else
                            writer.WriteLine($"return {ExprVariantToT(variant, variant.TypeData.FullName)} is {typeArg0};");
                    }
                }
            }
        }
        writer.WriteLine($"return false;");
    }

    public void EmitIsExactlyMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, VariantData variant)
    {
        writer.WriteLine($"return this.{f_flag} == {variant.Id};");
    }

    public void EmitIsPointerMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, IEnumerable<VariantData> variants, int level, string typeArg0)
    {
        var stars = new string('*', level);
        foreach (var variant in variants)
        {
            if (env.AllowsRefStruct || !variant.TypeData.FinalPointerAtType.IsRefLikeType)
            {
                writer.WriteLine($"if (typeof({typeArg0}{stars}) == typeof({variant.TypeData.FullyQName}))");
                writer.WriteLine($"    return this.{f_flag} == {variant.Id};");
            }
        }
        writer.WriteLine($"return false;");
    }

    public void EmitIsOutMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string typeArg0, string arg0)
    {
        writer.WriteLine($"switch (this.{f_flag})");
        using (writer.EnterBracketIndentScope('{'))
        {
            foreach (var variant in data.Variants.Where(x => x.TypeData.TypeKind.IsGenericable))
            {
                if (env.AllowsRefStruct || !variant.TypeData.IsRefLikeType)
                {
                    writer.WriteLine($"case {variant.Id}:");
                    using (writer.EnterBracketIndentScope('{'))
                    {
                        writer.WriteLine($"if (typeof({typeArg0}) == typeof({variant.TypeData.FullyQName}))");
                        writer.WriteLine($"{{");
                        writer.WriteLine($"    {arg0} = {ExprVariantToT(variant)};");
                        writer.WriteLine($"    return true;");
                        writer.WriteLine($"}}");
                        if (!variant.TypeData.IsRefLikeType)
                        {
                            writer.WriteLine($"if ({ExprVariantToT(variant, variant.TypeData.FullyQName)} is {typeArg0} __var_{typeArg0}_{arg0})");
                            writer.WriteLine($"{{");
                            writer.WriteLine($"    {arg0} = __var_{typeArg0}_{arg0};");
                            writer.WriteLine($"    return true;");
                            writer.WriteLine($"}}");
                        }
                        writer.WriteLine($"break;");
                    }
                }
            }
        }
        writer.WriteLine($"{arg0} = default({typeArg0});");
        writer.WriteLine("return false;");
    }

    public void EmitIsExactlyOutMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, VariantData variant, string arg0)
    {
        writer.WriteMultipleLines($$"""
            if (this.{{f_flag}} == {{variant.Id}})
            {
                {{arg0}} = {{ExprVariantToT(variant, variant.TypeData.FullyQName)}};
                return true;
            }
            {{arg0}} = default({{variant.TypeData.FullyQName}});
            return false;
            """);
    }

    public void EmitIsPointerOutMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, IEnumerable<VariantData> variants, int level, string typeArg0, string arg0)
    {
        var stars = new string('*', level);
        foreach (var variant in variants)
        {
            if (env.AllowsRefStruct || !variant.TypeData.FinalPointerAtType.IsRefLikeType)
            {
                writer.WriteLine($"if (typeof({typeArg0}{stars}) == typeof({variant.TypeData.FullyQName}))");
                using (writer.EnterBracketIndentScope('{'))
                {
                    writer.WriteMultipleLines($$"""
                        if (this.{{f_flag}} == {{variant.Id}})
                        {
                            {{arg0}} = {{ExprVariantToT(variant, $"{typeArg0}{stars}")}};
                            return true;
                        }
                        {{arg0}} = default({{typeArg0}}{{stars}});
                        return false;
                        """);
                }
            }
        }
        writer.WriteLine($"{arg0} = default(T{stars});");
        writer.WriteLine($"return false;");
    }

    public void EmitTryCreateMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string typeArg0, string arg0, string arg1)
    {
        foreach (var variant in data.Variants.Where(x => x.TypeData.TypeKind.IsGenericable))
        {
            writer.WriteMultipleLines($$"""
                if (typeof({{typeArg0}}) == typeof({{variant.TypeData.FullyQName}}))
                {
                    {{arg1}} = new {{data.TypeFullyQName}}(global::System.Runtime.CompilerServices.Unsafe.As<{{typeArg0}}, {{variant.TypeData.FullyQName}}>(ref {{arg0}}));
                    return true;
                }
                """);
        }
        writer.WriteLine($"{arg1} = default({data.TypeFullyQName});");
        writer.WriteLine($"return false;");
    }

    public void EmitEqualsMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string arg0)
    {
        bool requiresEqHelper = false;
        var allowsRefStruct = env.AllowsRefStruct ? ", allows ref struct" : "";

        writer.WriteMultipleLines($$"""
            if (this.{{f_flag}} != other.{{f_flag}})
                return false;
            """);
        writer.WriteLine();

        writer.WriteLine($"switch (this.{f_flag})");
        using (writer.EnterBracketIndentScope('{'))
        {
            writer.WriteLine($"case 0: return true;");
            foreach (var variant in data.Variants)
            {
                string returnExpr = variant.TypeData.EqualityKind switch
                {
                    VariantTypeEqualityKind.Unit => "true",
                    VariantTypeEqualityKind.Comparer => $"global::System.Collections.Generic.EqualityComparer<{variant.TypeData.FullyQName}>.Default.Equals({ExprVariantToT(variant, variant.TypeData.FullyQName)}, {ExprVariantToT(variant, variant.TypeData.FullyQName, "other")})",
                    VariantTypeEqualityKind.IEquatable => $"__lm_IEquatable_Equals({ExprVariantToT(variant, variant.TypeData.FullyQName)}, {ExprVariantToT(variant, variant.TypeData.FullyQName, "other")})",
                    VariantTypeEqualityKind.OperatorEq => $"{ExprVariantToT(variant, variant.TypeData.FullyQName)} == {ExprVariantToT(variant, variant.TypeData.FullyQName, "other")}",
                    _ => null!,
                };
                requiresEqHelper = variant.TypeData.EqualityKind == VariantTypeEqualityKind.IEquatable;
                Debug.Assert(returnExpr is not null);

                writer.WriteLine($"case {variant.Id}: return {returnExpr};");
            }
        }
        writer.WriteLine("return false;");

        if (requiresEqHelper)
        {
            writer.WriteLine();
            writer.WriteMultipleLines($"""
                static bool __lm_IEquatable_Equals<__TEq>(__TEq left, __TEq right) where __TEq : IEquatable<__TEq>{allowsRefStruct}
                    => left.Equals(right);
                """);
        }
    }

    public void EmitGetHashCodeMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data)
    {
        writer.WriteLine("unchecked");
        using (writer.EnterBracketIndentScope('{'))
        {
            writer.WriteLine($"int hash = 17;");
            writer.WriteLine($"hash = (hash * 397) ^ this.{f_flag}.GetHashCode();");
            writer.WriteLine($"switch (this.{f_flag})");
            using (writer.EnterBracketIndentScope('{'))
            {
                foreach (var variant in data.Variants)
                {
                    switch (variant.TypeData.TypeKind)
                    {
                        case VariantTypeKind.Void:
                            break;
                        case { IsPointer: true }:
                            writer.WriteLine($"case {variant.Id}: hash = (hash * 397) ^ {ExprVariantFieldAccess(variant)}.GetHashCode(); break;");
                            break;
                        default:
                            writer.WriteLine($"case {variant.Id}: hash = (hash * 397) ^ {ExprVariantToT(variant, variant.TypeData.FullyQName)}.GetHashCode(); break;");
                            break;
                    }
                }
            }
            writer.WriteLine($"return hash;");
        }
    }

    public void EmitDangerousGetValueMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string typeArg0)
    {
        if (data.Variants.Any(x => x.TypeData.TypeKind is VariantTypeKind.Reference))
        {
            writer.WriteLine($"if (!typeof(T).IsValueType)");
            writer.WriteLine($"    return ref global::System.Runtime.CompilerServices.Unsafe.As<object, {typeArg0}>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<object>(in this.{f_obj}));");
        }

        foreach (var item in data.Variants.Where(x => x.TypeData.TypeKind is VariantTypeKind.Managed or VariantTypeKind.Unmanaged))
        {
            if (env.AllowsRefStruct || !item.TypeData.IsRefLikeType)
            {
                writer.WriteMultipleLines($$"""
                    if (typeof(T) == typeof({{item.TypeData.FullyQName}}))
                        return ref {{ExprVariantRefToT(item)}};
                    """);
            }
        }
        writer.WriteLine("return ref global::System.Runtime.CompilerServices.Unsafe.NullRef<T>();");
    }

    public void EmitDangerousGetVariantValueMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, VariantData variant)
    {
        writer.WriteLine($"return ref {ExprVariantRefToT(variant, variant.TypeData.FullyQName)};");
    }

    public void EmitDangerousGetPointerValueMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data)
    {
        writer.WriteLine($"return ref *(void**)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<{t_unmanaged}>(in this.{f_unmanaged}));");
    }

    public void EmitSharedInterfaceDispatch(IndentedTextWriter writer, Env env, TypeUnionData data, bool returnsVoid, Func<string, string> exprReturn)
    {
        writer.WriteLine($"switch (this.{f_flag})");
        using (writer.EnterBracketIndentScope('{'))
        {
            writer.WriteLine($"case 0:");
            writer.WriteLine($"    {CodeTemplates.ThrowNRE()};");
            writer.WriteLine($"    {(returnsVoid ? "return;" : "return default!;")}");
            foreach (var variant in data.Variants)
            {
                writer.WriteLine($"case {variant.Id}:");
                if (returnsVoid)
                {
                    writer.WriteLine($"    {exprReturn(ExprVariantToT(variant, variant.TypeData.FullyQName))};");
                    writer.WriteLine($"    return;");
                }
                else
                {
                    writer.WriteLine($"    return {exprReturn(ExprVariantToT(variant, variant.TypeData.FullyQName))};");
                }
            }
        }
        writer.WriteLine($"{CodeTemplates.ThrowUUCE()};");
        if (returnsVoid)
            writer.WriteLine($"return;");
        else
            writer.WriteLine($"return default!;");
    }

    public void EmitHelperMembers(IndentedTextWriter writer, Env env, TypeUnionData data)
    {
        var @ref = data.Variants.Any(x => x.TypeData.TypeKind.IsUnmanaged && x.TypeData.IsRefLikeType) ? "ref " : "";

        writer.WriteLine($"[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]");
        writer.WriteLine(Utils.GeneratedCodeAttributeList);
        writer.WriteLine($"private {@ref}struct {t_unmanaged}");
        using (writer.EnterBracketIndentScope('{'))
        {
            foreach (var variant in data.Variants.Where(v => v.TypeData.TypeKind is VariantTypeKind.Unmanaged))
            {
                writer.WriteLine($"[global::System.Runtime.InteropServices.FieldOffset(0)]");
                writer.WriteLine($"public {variant.TypeData.FullyQName} {f_unmanaged_pfx}{variant.FieldId};");
            }
            if (data.Variants.Any(x => x.TypeData.TypeKind.IsPointer))
            {
                writer.WriteLine($"[global::System.Runtime.InteropServices.FieldOffset(0)]");
                writer.WriteLine($"public global::System.IntPtr {f_ptr};");
            }
        }
    }
}
