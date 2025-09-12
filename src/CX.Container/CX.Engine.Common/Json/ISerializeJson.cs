using System.Text;
using System.Text.Json;

namespace CX.Engine.Common.Json;

public interface ISerializeJson
{
    void Serialize(Utf8JsonWriter jw);
}

public static class ISerializeJsonExt
{
    public static string GetJsonString(this ISerializeJson src)
    {
        using var ms = new MemoryStream();
        using var jw = new Utf8JsonWriter(ms);
        src.Serialize(jw);
        jw.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public static Memory<byte> GetJsonMemory(this ISerializeJson src)
    {
        using var ms = new MemoryStream();
        using var jw = new Utf8JsonWriter(ms);
        src.Serialize(jw);
        jw.Flush();
        return ms.ToMemory();
    }
    
    public static HttpContent GetHttpContent(this ISerializeJson src)
    {
        var content = new ReadOnlyMemoryContent(src.GetJsonMemory());
        content.Headers.ContentType = new("application/json");
        return content;
    }
}