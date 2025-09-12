namespace CX.Engine.Common;

public static class SerializationHelpers
{
    #region double[]

    /// <summary>
    /// Serializes a float array using a 7-bit encoded int32 length and then the floats.
    /// </summary>
    /// <param name="bw">The <see cref="BinaryWriter"/> to serialize to.</param>
    /// <param name="arr">The array to serialize.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public static void Write(this BinaryWriter bw, float[] arr)
    {
        ArgumentNullException.ThrowIfNull(bw);
        ArgumentNullException.ThrowIfNull(arr);

        bw.Write7BitEncodedInt(arr.Length);
        foreach (var d in arr)
            bw.Write(d);
    }

    /// <summary>
    /// Deserializes a float array from a 7-bit encoded int32 length and then the floats. 
    /// </summary>
    /// <param name="br">The <see cref="BinaryReader"/> to deserialize from.</param>
    /// <exception cref="ArgumentNullException">If the <see cref="BinaryReader"/> is null.</exception>
    /// <returns>The deserialized float array.</returns>
    public static float[] ReadFloatArray(this BinaryReader br)
    {
        ArgumentNullException.ThrowIfNull(br);

        var count = br.Read7BitEncodedInt();
        var arr = new float[count];
        for (var i = 0; i < arr.Length; i++)
            arr[i] = br.ReadSingle();

        return arr;
    }

    #endregion

    #region Dictionary<string, float[]>

    /// <summary>
    /// Serializes a dictionary of string to float arrays using a 7-bit encoded int32 length and then the key-value pairs.
    /// </summary>
    /// <param name="bw">The <see cref="BinaryWriter"/> to serialize to.</param>
    /// <param name="dict">The dictionary to serialize.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public static void Write(this BinaryWriter bw, Dictionary<string, float[]> dict)
    {
        ArgumentNullException.ThrowIfNull(bw);
        ArgumentNullException.ThrowIfNull(dict);

        bw.Write7BitEncodedInt(dict.Count);
        foreach (var kvp in dict)
        {
            bw.Write(kvp.Key);
            bw.Write(kvp.Value);
        }
    }

    /// <summary>
    /// Deserializes a dictionary of string to float arrays from a 7-bit encoded int32 length and then the key-value pairs.
    /// Does not clear the dictionary before populating it.
    /// </summary>
    /// <param name="dict">The dictionary to populate.</param>
    /// <param name="br">The <see cref="BinaryReader"/> to deserialize from.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public static void PopulateFromReader(this Dictionary<string, float[]> dict, BinaryReader br)
    {
        ArgumentNullException.ThrowIfNull(dict);
        ArgumentNullException.ThrowIfNull(br);

        var count = br.Read7BitEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var key = br.ReadString();
            dict[key] = br.ReadFloatArray();
        }
    }

    #endregion

    #region Dictionary<string, string>

    /// <summary>
    /// Serializes a dictionary of string to string using a 7-bit encoded int32 length and then the key-value pairs.
    /// </summary>
    /// <param name="bw">The <see cref="BinaryWriter"/> to serialize to.</param>
    /// <param name="dict">The dictionary to serialize.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public static void Write(this BinaryWriter bw, Dictionary<string, string> dict)
    {
        ArgumentNullException.ThrowIfNull(bw);
        ArgumentNullException.ThrowIfNull(dict);
        bw.Write7BitEncodedInt(dict.Count);
        foreach (var kvp in dict)
        {
            bw.Write(kvp.Key);
            bw.Write(kvp.Value);
        }
    }

    /// <summary>
    /// Populates a dictionary of string to string from a 7-bit encoded int32 length and then the key-value pairs.
    /// </summary>
    /// <param name="dict">The dictionary to populate.</param>
    /// <param name="br">The <see cref="BinaryReader"/> to deserialize from.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    public static void PopulateFromReader(this Dictionary<string, string> dict, BinaryReader br)
    {
        ArgumentNullException.ThrowIfNull(dict);
        ArgumentNullException.ThrowIfNull(br);

        var count = br.Read7BitEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var key = br.ReadString();
            dict[key] = br.ReadString();
        }
    }

    #endregion

    #region Nullable String
    public static string ReadStringNullable(this BinaryReader br)
    {
        if (br.ReadBoolean())
            return br.ReadString();
        else
            return null;
    }

    public static void WriteNullable(this BinaryWriter bw, string s)
    {
        bw.Write(s != null);
        if (s != null)
            bw.Write(s);
    }
    #endregion
}