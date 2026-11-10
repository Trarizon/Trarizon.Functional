using System.CodeDom.Compiler;

namespace Trarizon.Library.Functional.Generators.TypeUnion.Emitters;

internal interface ITypeUnionEmitter
{
    void EmitFields(IndentedTextWriter writer, TypeUnionData data);
    void EmitConstructorBody(IndentedTextWriter writer, TypeUnionData data, VariantData variant, string arg0);
    void EmitIsNullPropertyBody(IndentedTextWriter writer, TypeUnionData data);
    void EmitUnionValuePropertyBody(IndentedTextWriter writer, TypeUnionData data);
    void EmitExplicitCastMethodBody(IndentedTextWriter writer, TypeUnionData data, VariantData variant, string arg0);
    void EmitIsMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string typeArg0);
    void EmitIsExactlyMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, VariantData variant);
    void EmitIsPointerMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, IEnumerable<VariantData> variants, int level, string typeArg0);
    void EmitIsOutMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string typeArg0, string arg0);
    void EmitIsExactlyOutMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, VariantData variant, string arg0);
    void EmitIsPointerOutMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, IEnumerable<VariantData> variants, int level, string typeArg0, string arg0);
    void EmitTryCreateMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string typeArg0, string arg0, string arg1);
    void EmitEqualsMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string arg0);
    void EmitGetHashCodeMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data);

    void EmitDangerousGetValueMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, string typeArg0);
    void EmitDangerousGetVariantValueMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data, VariantData variant);
    void EmitDangerousGetPointerValueMethodBody(IndentedTextWriter writer, Env env, TypeUnionData data);

    void EmitHelperMembers(IndentedTextWriter writer, Env env, TypeUnionData data);

    void EmitSharedInterfaceDispatch(IndentedTextWriter writer, Env env, TypeUnionData data, bool returnsVoid, Func<string, string> exprReturn);
}
