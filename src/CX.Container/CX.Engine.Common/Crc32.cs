namespace CX.Engine.Common;

public static class Crc32
{
    private static readonly uint[] Crc32Table = InitializeCrc32Table();

    // Precomputed CRC32 table for faster computation
    private static uint[] InitializeCrc32Table()
    {
        var polynomial = 0xEDB88320;
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            var crc = i;
            for (var j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ polynomial;
                else
                    crc >>= 1;
            }
            table[i] = crc;
        }
        return table;
    }

    /// <summary>
    /// Computes the CRC32 checksum of the string using UTF-16 encoding directly.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The CRC32 checksum as a uint.</returns>
    public static unsafe int GetCrc32(this string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var crc = 0xFFFFFFFF;

        fixed (char* ptr = input)
        {
            // Treat the string as a sequence of bytes (UTF-16)
            var bytePtr = (byte*)ptr;
            var byteLength = input.Length * sizeof(char);

            for (var i = 0; i < byteLength; i++)
            {
                var index = (byte)((crc ^ bytePtr[i]) & 0xFF);
                crc = (crc >> 8) ^ Crc32Table[index];
            }
        }

        return (int)~crc;
    }    
}