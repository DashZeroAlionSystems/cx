namespace CX.Engine.ChatAgents;

public class ChatLoadContext
{
    public readonly BinaryReader Br;
    public readonly int Version;

    public ChatLoadContext(BinaryReader br, int version)
    {
        Br = br;
        Version = version;
    }
}