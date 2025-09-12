using static CX.Engine.Common.CodeProcessing.TokenType;

namespace CX.Engine.Common.CodeProcessing;

public static class Accessor
{
    public class AccessorContext
    {
        public object Root;
        public string RootName;
    }

    public static Task<object> EvaluateAsync(string accessor, object root, string rootName = null)
    {
        var ctx = new AccessorContext() { Root = root, RootName = rootName };
        var ast = Parse(accessor, rootName);
        return EvaluateAsync(ast, ctx);
    }

    public static async Task<object> EvaluateAsync(ASTNode ast, AccessorContext ctx)
    {
        if (ast is IdentifierNode id)
            return await EvaluateAsync(id, ctx);

        if (ast is PropertyAccessNode prop)
            return await EvalAccessNodePropertyAsync((IdentifierNode)prop.Right, prop.Left, ctx, prop.LeftIsOptional);

        if (ast is ArrayAccessNode arr)
            return await AccessArrayAsync(((ASTConstantNode)arr.Index).Constant.IntegerValue, arr.Left, ctx, arr.LeftIsOptional, arr.IndexIsOptional);
        
        throw new EvaluatorException($"Unexpected AST node type: {ast.GetType().FullName}");
    }

    public static async Task<object> EvaluateAsync(IdentifierNode id, AccessorContext ctx)
    {
        if (id.Id == ctx.RootName)
            return ctx.Root;

        if (ctx.Root == null)
            throw new InvalidOperationException($"{id.Path} is null");
        
        return await AccessObjectPropertyAsync(id, ctx.Root, false);
    }

    public static async Task<object> AccessArrayAsync(int index, ASTNode left, AccessorContext ctx, bool leftIsOptional, bool indexIsOptional)
    {
        var leftValue = await EvaluateAsync(left, ctx);

        if (leftIsOptional && leftValue == null)
            return null;

        return await AccessArrayAsync(index, leftValue, indexIsOptional);
    }

    public static Task<object> AccessArrayAsync(int index, object array, bool indexIsOptional)
    {
        if (array is not IList<object> list)
            throw EvaluatorException.Throw("Array access on non-array object");

        if (index < 0 || index >= list.Count)
            if (indexIsOptional)
                return null;
            else
                throw EvaluatorException.Throw("Array index out of bounds: " + index);

        return Task.FromResult(list[index]);
    }

    public static async Task<object> EvalAccessNodePropertyAsync(IdentifierNode propId, ASTNode left, AccessorContext ctx, bool leftIsOptional)
    {
        var leftValue = await EvaluateAsync(left, ctx);

        if (leftIsOptional && leftValue == null)
            return null;
        
        if (leftValue == null)
            throw new InvalidOperationException($"Cannot access property {propId.Id} of {left.Path}");

        return await AccessObjectPropertyAsync(propId, leftValue, true);
    }

    public static async Task<object> AccessObjectPropertyAsync(IdentifierNode propId, object obj, bool propertyAccessOptional)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        if (obj is IResolveValueAsync sld)
            return await sld.ResolveValueAsync(propId.Id);

        if (obj is IDictionary<string, object> dict)
        {
            if (dict.TryGetValue(propId.Id, out var res))
                return res;

            if (propertyAccessOptional)
                return null;

            EvaluatorException.Throw("Dictionary entry not found: " + propId.Id);
        }

        var type = obj.GetType();

        var fld = type.GetField(propId.Id);
        if (fld != null)
            return fld.GetValue(obj);

        var prop = type.GetProperty(propId.Id);
        if (prop == null)
            throw EvaluatorException.Throw("Property not found: " + propId.Id);

        return prop.GetValue(obj);
    }

    public static ASTNode Parse(string accessor, string rootName = null)
    {
        var tokenizer = new CodeTokenizer(accessor);

        if (CurIsValidForPostIdentifier(tokenizer) && rootName != null)
            return ReadPostIdentifier(tokenizer, new IdentifierNode(rootName) { Path = rootName } );

        var id = tokenizer.ReadIdentifier();
        var idNode = new IdentifierNode(id) { Path = id };

        return ReadPostIdentifier(tokenizer, idNode);
    }

    public static bool CurIsValidForPostIdentifier(CodeTokenizer tokenizer)
    {
        var token = tokenizer.PreviewToken();
        return token.TokenType is Dot or Eof or ArrayStart or QuestionMark;
    }

    private static ASTNode ReadPostIdentifier(CodeTokenizer tokenizer, ASTNode left)
    {
        var next = tokenizer.PreviewToken();

        if (next.TokenType == Eof)
            return left;

        var optional = false;

        if (next.TokenType == QuestionMark)
        {
            tokenizer.ReadToken();
            optional = true;
            next = tokenizer.PreviewToken();
            if (next.TokenType is not Dot and not ArrayStart)
                ParserException.Throw(tokenizer.Index, [Dot, ArrayStart], next.TokenType);
        }

        if (next.TokenType == Dot)
        {
            tokenizer.ReadToken();
            var right = new IdentifierNode(tokenizer.ReadIdentifier());
            var res = new PropertyAccessNode(left, right, optional);
            res.LeftIsOptional |= left.IsOptional;
            res.Path = left.Path + "." + right;
            return ReadPostIdentifier(tokenizer, res);
        }

        if (next.TokenType == ArrayStart)
        {
            tokenizer.ReadToken();
            var optionalIdx = tokenizer.CurrentCharIsQuestionMark;
            if (optionalIdx)
                tokenizer.ReadToken();
            var idx = tokenizer.ReadInteger();
            tokenizer.ReadArrayEnd();

            var res = new ArrayAccessNode(left, new ASTConstantNode(Token.ForInteger(idx)), optional, optionalIdx);
            res.LeftIsOptional |= left.IsOptional;
            res.Path = $"{left.Path}[{idx}]";
            return ReadPostIdentifier(tokenizer, res);
        }

        throw ParserException.Throw(tokenizer.Index, [Dot, ArrayStart, Eof], tokenizer.PreviewToken());
    }
}