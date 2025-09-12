using System.Text;
using CX.Engine.Common;

public class AnythingToMarkdownCacheEntry
{
    public const int Magic = 0x2807F3FB;

    public string Content;
    public readonly List<string> ExtractionErrors = new();

    public AnythingToMarkdownCacheEntry() { }

    public byte[] GetBytes()
    {
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);
        Serialize(bw);
        bw.Flush();
        return ms.ToArray();
    }

    public void Serialize(BinaryWriter bw)
    {
        bw.Write(Magic);
        bw.Write(1); //Version
        bw.WriteNullable(Content);

        bw.Write7BitEncodedInt(ExtractionErrors.Count);
        foreach (var err in ExtractionErrors)
            bw.Write(err);
    }

    public AnythingToMarkdownCacheEntry(byte[] bytes)
    {
        if (bytes != null)
            Populate(bytes);
    }

    private void Populate(byte[] bytes)
    {
        if (bytes.Length < 9)
        {
            Content = Encoding.UTF8.GetString(bytes);
            return;
        }

        using var ms = new MemoryStream(bytes);
        using var br = new BinaryReader(ms);

        if (br.ReadInt32() != Magic)
        {
            Content = Encoding.UTF8.GetString(bytes);
            return;
        }

        if (br.ReadInt32() != 1)
        {
            Content = Encoding.UTF8.GetString(bytes);
            return;
        }

        Content = br.ReadStringNullable();

        var count = br.Read7BitEncodedInt();
        for (var i = 0; i < count; i++)
            ExtractionErrors.Add(br.ReadString());
    }
}