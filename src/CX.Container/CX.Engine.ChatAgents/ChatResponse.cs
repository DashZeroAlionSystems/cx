using System.Text.Json.Serialization;
using CX.Engine.Common;

namespace CX.Engine.ChatAgents;

public class ChatResponse
{
    [JsonInclude] public string Answer = null!;
    [JsonInclude] public AttachmentInfo[] Attachments;
    public string SystemPrompt;
    [JsonInclude] public List<ToolCall> ToolCalls = new();

    public ChatResponse()
    {
    }

    public ChatResponse(ChatLoadContext clc)
    {
        Deserialize(clc);
    }

    public void Serialize(BinaryWriter bw)
    {
        bw.Write(Answer ?? "");
        bw.Write(SystemPrompt ?? "");

        if (Attachments != null)
        {
            bw.Write7BitEncodedInt(Attachments?.Length ?? 0);
            if (Attachments != null)
                foreach (var att in Attachments)
                    att.Serialize(bw);
        }
        else
            bw.Write7BitEncodedInt(0);

        if (ToolCalls != null)
        {
            bw.Write7BitEncodedInt(ToolCalls.Count);
            foreach (var tc in ToolCalls)
                tc.Serialize(bw);
        }
        else
            bw.Write7BitEncodedInt(0);
    }

    public void Deserialize(ChatLoadContext clc)
    {
        var br = clc.Br;
        Answer = br.ReadString();

        if (clc.Version < 2)
        {
            SystemPrompt = "";
            Attachments = null;
            ToolCalls.Clear();
        }
        else
        {
            SystemPrompt = br.ReadString();

            if (SystemPrompt == "")
                SystemPrompt = null;
            
            var count = br.Read7BitEncodedInt();
            Attachments = new AttachmentInfo[count];
            for (var i = 0; i < count; i++)
                Attachments[i] = new(br);

            count = br.Read7BitEncodedInt();
            for (var i = 0; i < count; i++)
                ToolCalls.Add(new(clc));
        }
    }

    public void InflateFromRequest(ChatRequestBase ctx)
    {
        if (Attachments != null)
            foreach (var att in Attachments)
                att.DoGetContentStreamAsync = ctx.Attachments.FirstOrDefault(a => a.IsSameAttachment(att))?.DoGetContentStreamAsync;
    }
}