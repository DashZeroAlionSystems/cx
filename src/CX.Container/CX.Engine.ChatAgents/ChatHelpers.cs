namespace CX.Engine.ChatAgents;

public static class ChatHelpers
{
    #region Dictionary<string, ChatRespnose>
    /// <summary>
    /// Serializes a dictionary of string to string using a 7-bit encoded int32 length and then the key-value pairs.
    /// </summary>
    /// <param name="bw">The <see cref="BinaryWriter"/> to serialize to.</param>
    /// <param name="dict">The dictionary to serialize.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public static void Write(this BinaryWriter bw, Dictionary<string, ChatResponse> dict)
    {
        ArgumentNullException.ThrowIfNull(bw);
        ArgumentNullException.ThrowIfNull(dict);
        bw.Write7BitEncodedInt(dict.Count);
        foreach (var kvp in dict)
        {
            bw.Write(kvp.Key);
            kvp.Value.Serialize(bw);
        }
    }

    /// <summary>
    /// Populates a dictionary of string to string from a 7-bit encoded int32 length and then the key-value pairs.
    /// </summary>
    /// <param name="dict">The dictionary to populate.</param>
    /// <param name="clc">The <see cref="ChatLoadContext"/> to deserialize from.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public static void PopulateFromReader(this Dictionary<string, ChatResponse> dict, ChatLoadContext clc)
    {
        var br = clc.Br;
        ArgumentNullException.ThrowIfNull(dict);
        ArgumentNullException.ThrowIfNull(br);

        var count = br.Read7BitEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var key = br.ReadString();
            dict[key] = new(clc);
        }
    }
    #endregion
   
}