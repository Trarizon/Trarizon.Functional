using System.CodeDom.Compiler;
using System.Data.Common;
using System.Diagnostics;
using Trarizon.Library.Roslyn.Emitting;

namespace Trarizon.Library.Functional.Generators.TypeUnion.Emitters;

// For type union that all variants are reference type or void.
internal sealed class TUObjectOnlyEmitter : ITypeUnionEmitter
{
    private const string f_obj = "__um_obj";

    public void EmitFields(IndentedTextWriter writer, TypeUnionData data)
    {
        writer.WriteLine($"private readonly object {f_obj};");
    }

    public void EmitConstructorBody(IndentedTextWriter writer, TypeUnionData data, VariantData variant, string arg0)
    {
        writer.WriteLine($"this.{f_obj} = {arg0};");
    }

    public void EmitIsNullPropertyBody(IndentedTextWriter writer, TypeUnionData data)
    {
        writer.WriteLine($"return this.{f_obj} is null;");
    }

    public void EmitExplicitCastMethodBody(IndentedTextWriter writer, TypeUnionData data, VariantData variant, string arg0)
    {
        if (data.Variants.Any(x => x.TypeData.TypeKind is VariantTypeKind.Void))
        {
            writer.WriteMultipleLines($$"""
                if (this.IsVoid)
                    global::Trarizon.Library.Functional.CompilerServices.GeneratorHelpers.ThrowInvalidCastException($"Unable to cast {{data.TypeFullName}} to typeof(T).Name");
                """);
        }
        writer.WriteLine($"return ({variant.TypeData.FullyQName}){arg0}.{f_obj};");
    }

    public void EmitIsMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string typeArg0)
    {
        string notVoidAnd = "";
        if (data.Variants.Any(x => x.TypeData.TypeKind is VariantTypeKind.Void))
        {
            notVoidAnd = "!this.IsVoid && ";
        }
        writer.WriteLine($"return {notVoidAnd}this.{f_obj} is {typeArg0};");
    }

    public void EmitIsExactlyMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, VariantData variant)
    { }

    public void EmitIsPointerMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, IEnumerable<VariantData> variants, int level, string typeArg0)
    { }

    public void EmitIsOutMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string typeArg0, string arg0)
    {
        string notVoidAnd = "";
        if (data.Variants.Any(x => x.TypeData.TypeKind is VariantTypeKind.Void))
        {
            notVoidAnd = "!this.IsVoid && ";
        }
        writer.WriteMultipleLines($$"""
            if ({{notVoidAnd}}this.{{f_obj}} is {{typeArg0}} __var_{{typeArg0}}_{{arg0}})
            {
                {{arg0}} = __var_{{typeArg0}}_{{arg0}};
                return true;
            }
            {{arg0}} = default({{typeArg0}});
            return false;
            """);
    }

    public void EmitIsExactlyOutMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, VariantData variant, string arg0)
    { }

    public void EmitIsPointerOutMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, IEnumerable<VariantData> variants, int level, string typeArg0, string arg0)
    { }

    public void EmitTryCreateMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string typeArg0, string arg0, string arg1)
    {
        Debug.Assert(data.Variants.All(x => x.TypeData.TypeKind is VariantTypeKind.Reference or VariantTypeKind.Void));

        writer.WriteMultipleLines($$"""
            if (typeof({{typeArg0}}).IsValueType)
            {
                {{arg1}} = default({{data.TypeFullyQName}});
                return false;
            }
            """);

        foreach (var variant in data.Variants.Where(x => x.TypeData.TypeKind is not VariantTypeKind.Void))
        {
            writer.WriteMultipleLines($$"""
                if (typeof({{typeArg0}}) == typeof({{variant.TypeData.FullyQName}}))
                {
                    {{arg1}} = new {{data.TypeFullyQName}}({{CodeTemplates.UnsafeAs("T", variant.TypeData.FullyQName, arg0)}});
                    return true;
                }
                """);
        }

        writer.WriteLine($"object __var_obj = {CodeTemplates.UnsafeAs("T", "object", arg0)};");

        foreach (var variant in data.Variants.Where(x => x.TypeData.TypeKind is not VariantTypeKind.Void))
        {
            writer.WriteMultipleLines($$"""
                if (__var_obj is {{variant.TypeData.FullyQName}} __var_t_{{variant.Id}}_{{typeArg0}}_{{arg0}}_{{arg1}})
                {
                    {{arg1}} = new {{data.TypeFullyQName}}(__var_t_{{variant.Id}}_{{typeArg0}}_{{arg0}}_{{arg1}});
                    return true;
                }
                """);
        }
        writer.WriteLine($"{arg1} = default({data.TypeFullyQName});");
        writer.WriteLine($"return false;");
    }

    public void EmitEqualsMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string arg0)
    {
        writer.WriteLine($"if (object.ReferenceEquals(this.{f_obj}, {arg0}.{f_obj}))");
        writer.WriteLine($"    return true;");

        writer.WriteLine($"switch (this.{f_obj}, {arg0}.{f_obj})");
        using (writer.EnterBracketIndentScope('{'))
        {
            foreach (var variant in data.Variants.Where(x => x.TypeData.TypeKind is not VariantTypeKind.Void))
            {
                var type = variant.TypeData.FullyQName;
                writer.WriteLine($"case ({type} __var_l_{variant.Id}, {type} __var_r_{variant.Id}):");
                writer.WriteLine($"    return global::System.Collections.Generic.EqualityComparer<{type}>.Default.Equals(__var_l_{variant.Id}, __var_r_{variant.Id});");
            }
        }
        writer.WriteLine("return false;");
    }

    public void EmitGetHashCodeMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data)
    {
        writer.WriteLine($"return this.{f_obj}?.GetHashCode() ?? 0;");
    }

    public void EmitDangerousGetValueMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string typeArg0)
    {
        writer.WriteLine($"return ref {CodeTemplates.UnsafeAsReadOnlyRef("object", typeArg0, $"this.{f_obj}")};");
    }

    public void EmitDangerousGetVariantValueMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, VariantData variant)
    { }
    public void EmitDangerousGetPointerValueMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data)
    { }

    public void EmitSharedInterfaceDispatch(IndentedTextWriter writer, Env env, TypeUnionData data, bool returnsVoid, Func<string, string> exprReturn)
    {
        if (returnsVoid)
        {
            writer.WriteLine(exprReturn($"this.{f_obj}"));
            writer.WriteLine($"return;");
        }
        else
        {
            writer.WriteLine($"return {exprReturn($"this.{f_obj}")};");
        }
    }

    public void EmitHelperMembers(IndentedTextWriter writer, Env env, TypeUnionData data)
    { }
}
