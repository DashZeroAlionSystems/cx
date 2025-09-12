namespace CX.Engine.ChatAgents;

public static class ChatRequestBaseExt
{
    public static ChatRequestBase WithResponseType<TResponse>(this ChatRequestBase request, IChatAgent chatAgent)
    {
        var schema = chatAgent.GetSchema(nameof(TResponse));
        schema.Object.AddPropertiesFrom<TResponse>();
        request.SetResponseSchema(schema);
        return request;
    }
}