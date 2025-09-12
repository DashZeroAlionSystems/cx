namespace CX.Engine.Common;

public static class HexUtils
{
    public static byte[] HexStringToBytes(this string hex)
    {
        // Remove any spaces or colons from the hex string
        hex = hex.Replace(" ", "").Replace(":", "");

        // Ensure the hex string has an even length
        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have an even length.");

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < hex.Length; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        
        return bytes;
    }
    
    public static double HexStringToFloat(this string hex)
    {
        // Convert the hex string to a byte array
        var bytes = hex.HexStringToBytes();

        // Ensure the byte array has the correct length for a float (4 bytes)
        if (bytes.Length != 4)
            throw new ArgumentException("Hex string must represent a 4-byte float.");

        // Convert the byte array to a float
        return BitConverter.ToSingle(bytes, 0);
    }

    public static double HexStringToTFloat(this string hex)
    {
        // Convert the hex string to a byte array
        var bytes = hex.HexStringToBytes();

        // Ensure the byte array has the correct length for a TFloat (10 bytes)
        if (bytes.Length != 10)
            throw new ArgumentException($"Hex string must represent a 10-byte TFloat (found {bytes.Length} bytes).");

        // Convert the byte array to a double using ParseExt80
        return ParseExt80(bytes);
    }

    public static double ParseExt80(byte[] bytes)
    {
        if (bytes == null || bytes.Length != 10)
            throw new ArgumentException("Input must be exactly 10 bytes.");

        // Extract components
        var fractionBits = BitConverter.ToUInt64(bytes, 0);
        var byte8 = bytes[8];
        var byte9 = bytes[9];

        var sign = (byte9 & 0x80) != 0;
        var exponentRaw = ((byte9 & 0x7F) << 8) | byte8;

        if (exponentRaw == 0 && fractionBits == 0)
            return 0;

        const int bias = 16383;
        var exponent = exponentRaw - bias;

        var integerBit = (fractionBits >> 63) & 1;
        var fraction = fractionBits & 0x7FFFFFFFFFFFFFFF;
        var significand = (integerBit << 63) | fraction;

        // Use decimal for precision
        var mantissa = (decimal)significand / (decimal)(1UL << 63);
        var value = mantissa * (decimal)Math.Pow(2, exponent);

        return (double)(sign ? -value : value);
    }

    public static byte HexToByte(string hex)
    {
        if (hex.Length != 2)
            throw new ArgumentException("Hex string must be 2 characters long.");

        return Convert.ToByte(hex, 16);
    }
    
    public static int HexToInt(this string hex)
    {
        var negative = false;
        
        if (hex.StartsWith("-"))
        {
            negative = true;
            hex = hex.Substring(1);
        }

        if (hex.StartsWith("+"))
            hex = hex.Substring(1);

        hex = hex.PadLeft(8, '0');
        if (hex.Length != 8)
            throw new ArgumentException("Hex string must be 8 or less characters long.");
        
        return (negative ? -1 : 1) * Convert.ToUInt16(hex, 16);
    }
}