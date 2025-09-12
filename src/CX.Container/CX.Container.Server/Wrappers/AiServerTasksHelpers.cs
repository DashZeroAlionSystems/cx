using System.Text;

internal static class AiServerTasksHelpers
{
    public static Stream GenerateStreamFromString(string s)
    {
        try
        {
            MemoryStream stream = new();
            StreamWriter writer = new(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
}