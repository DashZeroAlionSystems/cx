using Microsoft.Extensions.Configuration.UserSecrets;

namespace CX.Engine.Common;

public static class SecretsProvider
{
    public static readonly string SecretsId = "cx.engine";
    public static string SecretsPath => Path.GetDirectoryName(PathHelper.GetSecretsPathFromSecretsId(SecretsId))!;

    public static string GetPath(string secretName, bool throwIfNotFound = true)
    {
        if (Path.DirectorySeparatorChar == '/')
            secretName = secretName.Replace("\\", "/");
        
        var filePath = Path.Combine(SecretsPath, secretName);
        if (File.Exists(filePath) || !throwIfNotFound)
            return filePath;

        throw new FileNotFoundException($"Secret {secretName} not found at {filePath}");
    }

    public static string Get(string secretName)
    {
        if (Path.DirectorySeparatorChar == '/')
            secretName = secretName.Replace("\\", "/");
        
        var filePath = Path.Combine(SecretsPath, secretName);
        if (File.Exists(filePath))
            return File.ReadAllText(filePath);

        throw new FileNotFoundException($"Secret {secretName} not found at {filePath}");
    }
}