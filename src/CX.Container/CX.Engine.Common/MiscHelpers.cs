using System.Data.Common;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CX.Engine.Common.Json;
using DiffPlex.DiffBuilder.Model;
using IronPython.Runtime.Exceptions;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SmartFormat;

namespace CX.Engine.Common;

/// <summary>
/// A diverse collection of helper methods.
/// </summary>
public static partial class MiscHelpers
{
    public enum ImageType
    {
        Unknown,
        JPEG,
        BMP,
        GIF,
        PNG
    }

    // Define image signatures as byte arrays
    private static readonly Dictionary<ImageType, byte[]> ImageSignatures = new()
    {
        { ImageType.JPEG, [0xFF, 0xD8, 0xFF] }, // JPEG
        { ImageType.BMP, [0x42, 0x4D] }, // BMP
        { ImageType.GIF, [0x47, 0x49, 0x46, 0x38] }, // GIF
        { ImageType.PNG, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A] } // PNG
    };

    /// <summary>
    /// Adds a range of items to a hash set.
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="dest"/> or <paramref name="source"/> is null.</exception>
    public static void AddRange<TV, TSource>(this HashSet<TV> dest, TSource source) where TSource : IEnumerable<TV>
    {
        ArgumentNullException.ThrowIfNull(dest);
        ArgumentNullException.ThrowIfNull(source);

        foreach (var i in source)
            dest.Add(i);
    }

    public static Memory<byte> ToMemory(this MemoryStream ms) => ms.GetBuffer().AsMemory(0, (int)ms.Length);

    /// <summary>
    /// Copies this stream (from its current position) to a new memory stream.
    /// </summary>
    /// <param name="source">The stream to copy.</param>
    /// <returns>A memory stream with the same content.</returns>
    public static async Task<MemoryStream> CopyToMemoryStreamAsync(this Stream source)
    {
        var memStream = new MemoryStream();
        await source.CopyToAsync(memStream);
        memStream.Position = 0;
        return memStream;
    }

    public static Task<string> GetSHA256Async(this string s) =>
        GetSHA256Async(new MemoryStream(Encoding.UTF8.GetBytes(s)));

    public static string GetSHA256(this string s) => GetSHA256(new MemoryStream(Encoding.UTF8.GetBytes(s)));

    /// <summary>
    /// Returns strings prefixed by sha256_
    /// </summary>
    /// <param name="stream">The stream to compute the SHA256 hash of.</param>
    /// <returns>The SHA256 hash of the stream.</returns>
    public static async Task<string> GetSHA256Async(this Stream stream)
    {
        // Ensure the stream is at the beginning
        stream.Position = 0;

        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        var sb = new StringBuilder();
        sb.Append("sha256_");

        foreach (var b in hashBytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }

    public static async Task<Guid> GetSHA256GuidAsync(this Stream stream)
    {
        // Ensure the stream is at the beginning
        stream.Position = 0;

        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);

        var guidBytes = new byte[16];
        for (var i = 0; i < 16; i++)
            guidBytes[i] = (byte)(hashBytes[i] ^ hashBytes[i + 16]);

        return new(guidBytes);
    }

    public static Guid GetSHA256Guid(this string s)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(new MemoryStream(Encoding.UTF8.GetBytes(s)));

        var guidBytes = new byte[16];
        for (var i = 0; i < 16; i++)
            guidBytes[i] = (byte)(hashBytes[i] ^ hashBytes[i + 16]);

        return new(guidBytes);
    }

    /// <summary>
    /// Returns strings prefixed by sha256_
    /// </summary>
    /// <param name="stream">The stream to compute the SHA256 hash of.</param>
    /// <returns>The SHA256 hash of the stream.</returns>
    public static string GetSHA256(this Stream stream)
    {
        // Ensure the stream is at the beginning
        stream.Position = 0;

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        var sb = new StringBuilder();
        sb.Append("sha256_");

        foreach (var b in hashBytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }

    /// <summary>
    /// Returns strings prefixed by sha256_
    /// </summary>
    /// <param name="bytes">The bytes to compute the SHA256 hash of.</param>
    /// <returns>The SHA256 hash of the stream.</returns>
    public static string GetSHA256(this byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        // Ensure the stream is at the beginning
        stream.Position = 0;

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        var sb = new StringBuilder();
        sb.Append("sha256_");

        foreach (var b in hashBytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }

    private static readonly Regex HyphenatedLineRegex = GetHyphenatedLineRegex();

    public static string CombineHyphenatedLines(this string document)
    {
        // Replace matched patterns with the combined lines
        var combinedDocument = HyphenatedLineRegex.Replace(document, "$1$2");
        return combinedDocument;
    }

    [GeneratedRegex(@"(\w)-\s*\n(\w)", RegexOptions.Compiled)]
    private static partial Regex GetHyphenatedLineRegex();


    private static readonly Regex HashTagRegex = GetHashTagRegex();

    [GeneratedRegex(@"#\w+([-_]\w+)*", RegexOptions.Compiled)]
    private static partial Regex GetHashTagRegex();

    public static string StripHashtags(string input)
    {
        if (input == null)
            return null;

        return HashTagRegex.Replace(input, "").Trim();
    }

    public static HashSet<string> ExtractHashtags(string input)
    {
        // List to hold extracted hashtags
        var hashtags = new HashSet<string>();

        if (input == null)
            return hashtags;

        // Extract hashtags and add them to the list
        foreach (Match match in HashTagRegex.Matches(input))
            hashtags.Add(match.Value.ToLowerInvariant());

        return hashtags;
    }

    /// <summary>
    /// Splits the string, removes empty entries and trims the entries.
    /// </summary>
    public static string[] SplitAt(this string input, string separator)
    {
        if (input == null)
            return [];

        return input.Split([separator], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    public static string DoubleQuoteAndEscape(this string input, string nullString = "\"\"")
    {
        if (input == null)
            return nullString;

        var escapedInput = input.Replace("\"", "\\\"");
        return $"\"{escapedInput}\"";
    }

    public static string SignleQuoteAndEscape(this string input, string nullString = "''")
    {
        if (input == null)
            return nullString;

        var escapedInput = input.Replace("'", "\\'");
        return $"'{escapedInput}'";
    }

    public static string RemoveLeading(this string input, string lead)
    {
        if (input == null)
            return null;

        if (input.StartsWith(lead))
            return input.Substring(lead.Length);

        return input;
    }

    public static string RemoveLeading(this string input, params string[] leads)
    {
        if (input == null)
            return null;

        foreach (var lead in leads)
            input = input.RemoveLeading(lead);

        return input;
    }

    public static void AddRange(this Dictionary<string, string> dest, Dictionary<string, string> source)
    {
        ArgumentNullException.ThrowIfNull(dest);

        if (source == null)
            return;

        foreach (var kvp in source)
            dest[kvp.Key] = kvp.Value;
    }

    public static T GetService<T>(this IServiceProvider sp, bool optional)
    {
        if (optional)
            return sp.GetService<T>();
        else
            return sp.GetRequiredService<T>();
    }

    public static T GetRequiredSection<T>(this IConfiguration config, string mainSectionName, string sectionName)
    {
        var mainSection = config.GetSection(mainSectionName);

        if (!mainSection.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName} for {mainSectionName}.{sectionName}");

        var section = mainSection.GetSection(sectionName);

        if (!section.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName}.{sectionName} of type {typeof(T).Name}");

        return mainSection.GetRequiredSection<T>(sectionName);
    }

    /// <summary>
    /// Will never return null.
    /// </summary>
    [NotNull]
    public static IConfigurationSection GetSection(this IConfiguration config, string mainSectionName, string subSectionName)
    {
        var mainSection = config.GetSection(mainSectionName);
        var section = mainSection.GetSection(subSectionName);
        return section;
    }

    public static string GetSectionPath(this IConfiguration config, string mainSectionName, string subSectionName)
    {
        return config.GetSection(mainSectionName, subSectionName).Path;
    }

    public static bool SectionExists(this IConfiguration config, string mainSectionName, string subSectionName)
    {
        var mainSection = config.GetSection(mainSectionName);

        if (!mainSection.Exists())
            return false;

        var subSection = mainSection.GetSection(subSectionName);

        if (!subSection.Exists())
            return false;

        return true;
    }

    public static bool SectionExists(this IConfiguration config, string mainSectionName)
    {
        var mainSection = config.GetSection(mainSectionName);

        if (!mainSection.Exists())
            return false;

        return true;
    }

    public static T GetSection<T>(this IConfiguration config, string mainSectionName, string sectionName, bool optional)
    {
        if (!optional)
        {
            var mainSection = config.GetSection(mainSectionName);

            if (!mainSection.Exists())
                throw new InvalidOperationException(
                    $"Missing required configuration section {mainSectionName} for {mainSectionName}.{sectionName}");

            var section = mainSection.GetSection(sectionName);

            if (!section.Exists())
                throw new InvalidOperationException(
                    $"Missing required configuration section {mainSectionName}.{sectionName} of type {typeof(T).Name}");

            return mainSection.GetRequiredSection<T>(sectionName);
        }
        else
        {
            var mainSection = config.GetSection(mainSectionName);

            if (!mainSection.Exists())
                return default;

            var section = mainSection.GetSection(sectionName);

            if (!section.Exists())
                return default;

            return mainSection.GetRequiredSection<T>(sectionName);
        }
    }

    public static IConfigurationSection GetRequiredSection(this IConfiguration config, string mainSectionName, string sectionName)
    {
        var mainSection = config.GetSection(mainSectionName);

        if (!mainSection.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName} for {mainSectionName}.{sectionName}");

        var section = mainSection.GetSection(sectionName);

        if (!section.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName}.{sectionName}");

        return mainSection.GetRequiredSection(sectionName);
    }

    public static ValidatedOptionsMonitor Snapshot<T>(this IOptionsMonitor<T> monitor, IConfigurationSection section, Func<T> get, Action<T> set,
        Action<bool> exists, ILogger logger,
        [NotNull] IServiceProvider sp)
    {
        if (sp == null) throw new ArgumentNullException(nameof(sp));
        var res = new SnapshotOptionsMonitor<T>
        {
            Get = get,
            Set = set,
            InitialSet = set,
            Logger = logger,
            Exists = exists,
            MonitoredSection = section,
            Monitor = monitor,
            ServiceProvider = sp
        };
        res.Start();

        return res;
    }

    public static SnapshotOptionsMonitor<T> Snapshot<T>(this IOptionsMonitor<T> monitor, Func<T> get, Action<T> initSet, Action<T> set, ILogger logger,
        [NotNull] IServiceProvider sp) where T : IValidatable
    {
        if (sp == null) throw new ArgumentNullException(nameof(sp));
        var res = new SnapshotOptionsMonitor<T>
        {
            Get = get,
            Monitor = monitor,
            Set = set,
            InitialSet = initSet,
            Logger = logger,
            ServiceProvider = sp
        };
        res.Start();
        return res;
    }

    public static SnapshotOptionsMonitor<T> Snapshot<T>(this IOptionsMonitor<T> monitor, Func<T> get, Action<T> set, ILogger logger, [NotNull] IServiceProvider sp,
        IConfigurationSection section = null)
    {
        if (sp == null) throw new ArgumentNullException(nameof(sp));
        var res = new SnapshotOptionsMonitor<T>
        {
            Get = get,
            Monitor = monitor,
            MonitoredSection = section,
            Set = set,
            InitialSet = set,
            Logger = logger,
            ServiceProvider = sp
        };
        res.Start();
        return res;
    }

    public static IOptionsMonitor<T> MonitorRequiredSection<T>(this IConfiguration config, string mainSectionName, string sectionName) where T : class
    {
        var mainSection = config.GetSection(mainSectionName);

        if (!mainSection.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName} for {mainSectionName}.{sectionName}");

        var section = mainSection.GetSection(sectionName);

        if (!section.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName}.{sectionName}");

        return mainSection.GetRequiredSection(sectionName).GetOptionsMonitor<T>();
    }

    public static (IConfigurationSection section, IOptionsMonitor<T> monitor) MonitorRequiredSectionE<T>(this IConfiguration config, string mainSectionName,
        string sectionName) where T : class
    {
        var mainSection = config.GetSection(mainSectionName);

        if (!mainSection.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName} for {mainSectionName}.{sectionName}");

        var section = mainSection.GetSection(sectionName);

        if (!section.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName}.{sectionName}");

        return (section, mainSection.GetRequiredSection(sectionName).GetOptionsMonitor<T>());
    }

    public static (IConfigurationSection section, IOptionsMonitor<T> monitor) MonitorRequiredSectionE<T>(this IConfiguration config, string mainSectionName,
        string sectionName, Func<IConfigurationSection, IConfigureOptions<T>> configureOptions)
        where T : class
    {
        var mainSection = config.GetSection(mainSectionName);

        if (!mainSection.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName} for {mainSectionName}.{sectionName}");

        var section = mainSection.GetSection(sectionName);

        if (!section.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName}.{sectionName}");

        return (section, mainSection.GetRequiredSection(sectionName).GetOptionsMonitor(configureOptions(section)));
    }

    public static (IConfigurationSection section, IOptionsMonitor<T> monitor) MonitorRequiredSectionE<T>(this IConfiguration config, string mainSectionName,
        Func<IConfigurationSection, IConfigureOptions<T>> configureOptions)
        where T : class
    {
        var mainSection = config.GetSection(mainSectionName);

        if (!mainSection.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName}");

        return (mainSection, mainSection.GetOptionsMonitor(configureOptions(mainSection)));
    }

    public static IOptionsMonitor<T> MonitorSection<T>(this IConfiguration config, string mainSectionName, string sectionName,
        Action<IConfigurationSection, T> configureOptions = null)
        where T : class
    {
        var mainSection = config.GetSection(mainSectionName);
        var section = mainSection.GetSection(sectionName);

        return section.GetOptionsMonitor(new ConfigureOptionsDelegate<T>(opts => { configureOptions?.Invoke(section, opts); }));
    }

    public static IOptionsMonitor<T> MonitorRequiredSection<T>(this IConfiguration config, string mainSectionName, string sectionName,
        Func<IConfigurationSection, IConfigureOptions<T>> configureOptions)
        where T : class
    {
        var mainSection = config.GetSection(mainSectionName);

        if (!mainSection.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName} for {mainSectionName}.{sectionName}");

        var section = mainSection.GetSection(sectionName);

        if (!section.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {mainSectionName}.{sectionName}");

        return section.GetOptionsMonitor(configureOptions(section));
    }

    public static T GetRequiredSection<T>(this IConfiguration config, string sectionName)
    {
        var section = config.GetSection(sectionName);

        if (!section.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {sectionName} of type {typeof(T).Name}");

        var t = section.Get<T>();

        if (t == null)
            throw new InvalidOperationException($"Configuration section {sectionName} is not of type {typeof(T).Name}");

        return t;
    }

    public static (string part, string remainder) SplitAtFirst(this string input, string separator)
    {
        if (input == null)
            return ("", "");

        var index = input.IndexOf(separator, StringComparison.InvariantCultureIgnoreCase);

        if (index == -1)
            return (input, "");

        return (input[..index], input[(index + separator.Length)..]);
    }

    public static string ToIso8601RoundTripString(this DateTime dateTime) =>
        dateTime.ToString("o", CultureInfo.InvariantCulture);

    public static HttpContent GetHttpJsonContent(this ReadOnlyMemory<byte> src)
    {
        var content = new ReadOnlyMemoryContent(src);
        content.Headers.ContentType = new("application/json");
        return content;
    }

    public static HttpContent GetHttpJsonContent(this MemoryStream src) => GetHttpJsonContent(src.ToMemory());

    /// <summary>
    /// Get the first n characters of a string.  If the string is longer than n, it will be trailed by 3 dots.
    /// </summary>
    public static string Preview(this string input, int count)
    {
        if ((input?.Length ?? 0) <= count)
            return input;

        return input?.Substring(0, count) + "...";
    }

    public static string GetEmbeddedResourceAsString(this Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException("Resource not found.", resourceName);

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static ILogger GetLogger(this IServiceProvider sp, string name) =>
        sp.GetRequiredService<ILoggerFactory>().CreateLogger(name);

    public static ILogger GetLogger<T>(this IServiceProvider sp, string name) => sp.GetLogger(typeof(T).FullName + "." + name);
    public static ILogger GetLogger<T>(this IServiceProvider sp) => sp.GetLogger(typeof(T).FullName);

    public static async Task<string> ReadToEndAsync(this Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    public static bool AreEqual(object obj1, object obj2)
    {
        if (ReferenceEquals(obj1, obj2))
            return true;

        if (obj1 == null || obj2 == null)
            return false;

        return obj1.Equals(obj2);
    }

    /// <summary>
    /// Removes an item without maintaining the list's order.
    /// </summary>
    public static void RemoveAtFast<T>(this List<T> lst, int idx)
    {
        var countMin1 = lst.Count - 1;

        if (idx < 0 || idx > countMin1)
            throw new ArgumentOutOfRangeException(nameof(idx));

        lst[idx] = lst[countMin1];
        lst.RemoveAt(countMin1);
    }

    /// <summary>
    /// Removes all matching items without maintaing the list's order.
    /// </summary>
    public static void RemoveAllFast<T>(this List<T> lst, Predicate<T> match)
    {
        for (var i = 0; i < lst.Count; i++)
        {
            if (!match(lst[i]))
                continue;

            lst.RemoveAtFast(i);
            i--;
        }
    }

    public static void DisposeAll<TList>(this TList items) where TList : IEnumerable<IDisposable>
    {
        foreach (var item in items)
            item.Dispose();
    }

    /// <summary>
    /// Designed for managing SlimLocks with a using statement.
    /// </summary>
    public static async ValueTask<SemaphoreSlimDisposable> UseAsync(this SemaphoreSlim slimLock)
    {
        if (slimLock == null)
            return new();
        
        if (!slimLock.Wait(0))
            await slimLock.WaitAsync();
        return new(slimLock);
    }

    public static IConfigurationBuilder AddJsonStrings(this IConfigurationBuilder cb,
        [LanguageInjection(InjectedLanguage.JSON)]
        params string[] jsons)
    {
        foreach (var json in jsons)
            cb.AddJsonString(json);

        return cb;
    }

    public static HashSet<T> ShallowClone<T>(this HashSet<T> src)
    {
        if (src == null)
            return null;

        var res = new HashSet<T>();
        foreach (var item in src)
            res.Add(item);

        return res;
    }

    public static IConfigurationBuilder AddJsonString(this IConfigurationBuilder cb,
        [LanguageInjection(InjectedLanguage.JSON)]
        string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return cb;

        cb.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        return cb;
    }

    public static string RemoveTrailing(this string s, string trail)
    {
        if (s == null)
            return null;

        if (s.EndsWith(trail))
            return s[..^trail.Length];

        return s;
    }

    public static string NullIfWhiteSpace(this string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;

        return s;
    }

    public static bool AddIfNotContains<T>(this List<T> items, T item)
    {
        if (items.Contains(item))
            return false;

        items.Add(item);
        return true;
    }

    public static int CountOccurences(this string source, string value,
        StringComparison comparer = StringComparison.InvariantCultureIgnoreCase)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var count = 0;
        var currentIndex = 0;

        while ((currentIndex = source.IndexOf(value, currentIndex, comparer)) != -1)
        {
            count++;
            currentIndex += value.Length;
        }

        return count;
    }

    public static string Left(this string source, int upTo)
    {
        if (source == null)
            return null;

        if (source.Length <= upTo)
            return source;

        return source[..upTo];
    }

    /// <summary>
    /// Returns a UTF-8 encoded memory stream containing text.
    /// </summary>
    public static MemoryStream AsMemoryStream(this string text)
    {
        if (text == null)
            return new();

        return new(Encoding.UTF8.GetBytes(text));
    }

    public static async Task<TResult[]> Select<TElement, TResult>(this Task<List<TElement>> waitFor,
        Func<TElement, Task<TResult>> action) => await Task.WhenAll((await waitFor).Select(action));

    public static async Task Select<TElement>(this Task<List<TElement>> waitFor,
        Func<TElement, Task> action) => await Task.WhenAll((await waitFor).Select(action));

    public static TaskAwaiter GetAwaiter(this IEnumerable<Task> tasks) => Task.WhenAll(tasks).GetAwaiter();

    public static Task WhenAll(this IEnumerable<ValueTask> tasks)
    {
        async Task AwaitAll()
        {
            var lst = new List<Exception>();
            foreach (var task in tasks)
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    lst.Add(ex);
                }

            if (lst.Count > 0)
                throw new AggregateException(lst);
        }

        return AwaitAll();
    }

    public static Task WhenAll<T>(this IEnumerable<ValueTask<T>> tasks)
    {
        async Task AwaitAll()
        {
            var lst = new List<Exception>();
            foreach (var task in tasks)
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    lst.Add(ex);
                }

            if (lst.Count > 0)
                throw new AggregateException(lst);
        }

        return AwaitAll();
    }

    public static TaskAwaiter GetAwaiter(this IEnumerable<ValueTask> tasks) => tasks.WhenAll().GetAwaiter();
    public static TaskAwaiter GetAwaiter<T>(this IEnumerable<ValueTask<T>> tasks) => tasks.WhenAll().GetAwaiter();

    public static Dictionary<string, object> ToDictionary(this DbDataReader reader, List<string> skipColumns = null)
    {
        // Create a dictionary to hold the column names and their values
        var rowDict = new Dictionary<string, object>();

        // Iterate through each column in the row
        for (var i = 0; i < reader.FieldCount; i++)
        {
            // Get the column name
            var columnName = reader.GetName(i);

            if (skipColumns != null && skipColumns.Contains(columnName))
                continue;

            // Get the value of the column
            var columnValue = reader.IsDBNull(i) ? null : reader.GetValue(i);

            if (columnValue is JArray jArray)
            {
                columnValue = "[" + string.Join(",", jArray.Select(j => '"' + (j is JValue jVal ? jVal.Value as string : j.ToString()) + '"')) + "]";
            }

            // Add the column name and value to the dictionary
            rowDict.Add(columnName, columnValue);
        }

        return rowDict;
    }

    public static string ToJsonString(this DbDataReader reader, List<string> skipColumns = null)
    {
        var dict = reader.ToDictionary(skipColumns);

        // Convert the dictionary to a JSON string
        var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        });

        return json;
    }

    public static string ToSmartFormattedTable(this List<JsonObject> nodes, List<string> skipColumns = null,
        string smartFormatString = null)
    {
        if (nodes == null || nodes.Count == 0)
            return """
                   No Data
                   """;

        var properties = nodes[0].Select(kvp => kvp.Key).ToList();

        if (skipColumns != null)
            properties = properties.Where(p => !skipColumns.Contains(p, StringComparer.InvariantCultureIgnoreCase)).ToList();

        var sb = new StringBuilder();

        smartFormatString ??= "| " + properties.Select(x => x + ": " + "{" + x + "} " + x).Aggregate((x, y) => x + ", " + y) + " |";

        foreach (var node in nodes)
        {
            dynamic expando = new ExpandoObject();
            var expandoDict = (IDictionary<string, object>)expando;
            foreach (var prop in properties)
            {
                if (!node.TryGetPropertyValue(prop, out var value))
                    throw new InvalidOperationException("Missing property in array of similar objects: " + prop);
                expandoDict.Add(prop, value.ToPrimitive());
            }

            sb.Append(Smart.Format(smartFormatString, expando));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string ToMarkdownTable(this List<JsonObject> nodes, List<string> skipColumns = null, Dictionary<string, string> markdownFormats = null)
    {
        if (nodes.Count == 0)
            return """
                   |         |
                   -----------
                   | No data |
                   """;

        var properties = nodes[0].Select(kvp => kvp.Key).ToList();

        if (skipColumns != null)
            properties = properties.Where(p => !skipColumns.Contains(p, StringComparer.InvariantCultureIgnoreCase)).ToList();

        var sb = new StringBuilder();

        sb.Append('|');

        foreach (var prop in properties)
        {
            //if (!node.TryGetPropertyValue(prop, out var value))
            //    throw new InvalidOperationException("Missing property in array of similar objects: " + prop);

            sb.Append(' ');
            sb.Append(prop);
            sb.Append(" |");
        }

        sb.AppendLine();
        sb.AppendLine(new('-', 5));

        foreach (var node in nodes)
        {
            sb.Append("|");
            foreach (var prop in properties)
            {
                if (!node.TryGetPropertyValue(prop, out var value))
                    throw new InvalidOperationException("Missing property in array of similar objects: " + prop);

                sb.Append(' ');
                if (markdownFormats != null && markdownFormats.TryGetValue(prop, out var markdownFormat) && !string.IsNullOrWhiteSpace(markdownFormat))
                    sb.Append(Smart.Format(markdownFormat, value.ToPrimitive()));
                else
                    sb.Append(value.ToPrimitive());
                sb.Append(" |");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static int? TryParseInt(this string s)
    {
        if (s == null)
            return null;

        if (int.TryParse(s, out var i))
            return i;

        return null;
    }

    public static IOptionsMonitor<T> GetOptionsMonitor<T>(this IConfigurationSection section) where T : class
    {
        var services = new ServiceCollection();
        services.Configure<T>(section);
        return services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<T>>();
    }

    public static IOptionsMonitor<TOptions> GetOptionsMonitor<TOptions>(this IConfigurationSection section, IConfigureOptions<TOptions> configureOptions)
        where TOptions : class
    {
        var services = new ServiceCollection();
        services.Configure<TOptions>(section);
        services.AddSingleton(configureOptions);
        return services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<TOptions>>();
    }

    public static MonitoredOptionsSection<TOptions> GetJsonOptionsMonitor<TOptions>(this IConfigurationSection section, ILogger logger, IServiceProvider sp,
        Action<TOptions> afterConfigure = null)
        where TOptions : class
    {
        var services = new ServiceCollection();
        services.Configure<TOptions>(section);
        services.AddSingleton<IConfigureOptions<TOptions>>(new JsonOptionsSetup<TOptions>(section, afterConfigure));
        var monitor = services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<TOptions>>();
        return new(section, monitor, logger, sp);
    }

    public static object ToPrimitive(this JsonNode node)
    {
        if (node is null)
            return null;

        if (node is JsonValue jv)
        {
            switch (jv.GetValueKind())
            {
                case JsonValueKind.String:
                    return jv.GetValue<string>();

                case JsonValueKind.Number:
                    if (jv.TryGetValue<long>(out var l))
                        return l;

                    return jv.GetValue<double>();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return jv.GetValue<bool>();
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Undefined:
                    throw new InvalidOperationException("Cannot convert undefined JsonValueKind to a primitive value.");
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    throw new InvalidOperationException("Cannot convert JsonObject or JsonArray to a primitive value.");
                default:
                    throw new InvalidOperationException($"Unsupported JsonValueKind: {jv.GetValueKind()}");
            }
        }


        if (node is JsonArray or JsonObject)
        {
            throw new InvalidOperationException("Cannot convert JsonObject or JsonArray to a primitive value.");
        }

        throw new InvalidOperationException("Unknown JsonNode type.");
    }

    public static bool TryToPrimitive<T>(this JsonNode node, out T value)
    {
        var prim = node.ToPrimitive();
        try
        {
            value = (T)prim;
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Returns a task that completes when the stopwatch's Elapsed reaches the threshold value.
    /// If the threshold has already been reached a completed task is returned.
    /// </summary>
    public static Task GetTaskForElapsedAt(this Stopwatch sw, TimeSpan threshold, CancellationToken cancellationToken = default)
    {
        var elapsed = sw.Elapsed;

        if (elapsed >= threshold)
            return Task.CompletedTask;

        return Task.Delay(threshold - elapsed, cancellationToken);
    }

    public static ImageType GetImageType(this byte[] fileBytes)
    {
        if (fileBytes == null)
            throw new ArgumentNullException(nameof(fileBytes));

        foreach (var signature in ImageSignatures)
        {
            var sigBytes = signature.Value;
            if (fileBytes.Length < sigBytes.Length)
                continue; // Not enough bytes for this signature

            var isMatch = true;
            for (var i = 0; i < sigBytes.Length; i++)
            {
                if (fileBytes[i] != sigBytes[i])
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch)
                return signature.Key;
        }

        return ImageType.Unknown;
    }

    public static byte[] ConvertImageToJpegAndUpscale(this byte[] inputBytes, double factor)
    {
        if (inputBytes == null || inputBytes.Length == 0)
            throw new ArgumentException("Input byte array cannot be null or empty", nameof(inputBytes));

        // Load the image
        using var image = Image.Load(inputBytes);
        // Calculate new dimensions while keeping aspect ratio
        var newWidth = (int)(image.Width * factor);
        var newHeight = (int)(image.Height * factor);

        image.Mutate(x => x.Resize(newWidth, newHeight));

        // Save the resized image to a memory stream
        using var outputStream = new MemoryStream();

        image.Save(outputStream, JpegFormat.Instance);
        outputStream.Flush();
        return outputStream.ToArray(); // Return the resized image as a byte array
    }

    public static byte[] ConvertImageToPngAndUpscale(this byte[] inputBytes, double factor)
    {
        if (inputBytes == null || inputBytes.Length == 0)
            throw new ArgumentException("Input byte array cannot be null or empty", nameof(inputBytes));

        // Load the image
        using var image = Image.Load(inputBytes);
        // Calculate new dimensions while keeping aspect ratio
        var newWidth = (int)(image.Width * factor);
        var newHeight = (int)(image.Height * factor);

        image.Mutate(x => x.Resize(newWidth, newHeight));

        // Save the resized image to a memory stream
        using var outputStream = new MemoryStream();

        image.Save(outputStream, PngFormat.Instance);
        outputStream.Flush();
        return outputStream.ToArray(); // Return the resized image as a byte array
    }

    public static T Get<T>(this List<T> lst, int index)
    {
        if (index < 0 || index >= lst.Count)
            return default;

        return lst[index];
    }

    public static async void FireAndForget(this Func<Task> t, ILogger logger, string exceptionMessage = null)
    {
        try
        {
            if (t == null)
                return;

            var task = t();
            if (task != null)
                await task;
        }
        catch (Exception e)
        {
            logger.LogError(e, exceptionMessage ?? "Error during FireAndForget invocation");
        }
    }

    public static async void FireAndForget(this Task t, ILogger logger, string exceptionMessage = null)
    {
        try
        {
            if (t == null)
                return;

            await t;
        }
        catch (Exception e)
        {
            logger.LogError(e, exceptionMessage ?? "Error during FireAndForget invocation");
        }
    }

    public static async void FireAndForget(this Task t, Action<string> logger, string exceptionMessage = null) 
    {
        try
        {
            if (t == null)
                return;

            await t;
        }
        catch (Exception e)
        {
            logger?.Invoke((exceptionMessage ?? "Error during FireAndForget invocation") + e);
        }
    }

    public static bool TryParseToJsonElement(this string s, out JsonElement je)
    {
        var rdr = new Utf8JsonReader(Encoding.UTF8.GetBytes(s));
        if (JsonElement.TryParseValue(ref rdr, out var eje))
        {
            je = eje.Value;
            return true;
        }

        je = default;
        return false;
    }

    public static TaskAwaiter GetAwaiter(this TaskCompletionSource tcs) => tcs.Task.GetAwaiter();

    public static string RemoveFileExtension(string s)
    {
        if (s == null)
            return null;

        var idx = s.LastIndexOf('.');
        if (idx == -1)
            return s;

        return s[..idx];
    }

    /// <summary>
    /// NB: returns the same array.
    /// </summary>
    public static float[] RoundAndReplace(this float[] values, int decimals)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = MathF.Round(values[i], decimals);
        return values;
    }

    public static string ToDiffString(this DiffPaneModel diff, bool partial = false)
    {
        var sb = new StringBuilder();
        foreach (var line in diff.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Inserted:
                    sb.AppendLine($"+ {line.Text}");
                    break;
                case ChangeType.Deleted:
                    sb.AppendLine($"- {line.Text}");
                    break;
                default:
                    if (!partial)
                        sb.AppendLine("  " + line.Text);
                    break;
            }
        }

        return sb.ToString();
    }

    public static string Indent(this string input, int count, bool firstLine = true)
    {
        var indents = new string(' ', count);

        var res = input.Replace("\r\n", "\r\n" + indents);

        if (firstLine)
            res = indents + res;

        return res;
    }

    public static string ToCappedListString(this IEnumerable<string> list, int countCap, int characterSoftCap = -1) =>
        ToCappedListString(list.ToList(), countCap, characterSoftCap);

    public static string ToCappedListString(this List<string> list, int countCap, int characterSoftCap = -1)
    {
        var sb = new StringBuilder();
        var first = true;
        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];

            var last = i == list.Count - 1;

            if (!first)
                if (last)
                    sb.Append(" and ");
                else
                    sb.Append(", ");

            sb.Append(item);
            first = false;

            if (characterSoftCap > 0 && sb.Length >= characterSoftCap)
                countCap = i + 1;

            if (i == countCap - 1)
            {
                sb.Append($", and {list.Count - countCap:#,##0} more...");
                break;
            }
        }

        return sb.ToString();
    }

    public static string NormalizeLineEndings(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder(input.Length);

        for (var i = 0; i < input.Length; i++)
        {
            var current = input[i];

            // Handle '\r\n' as a single unit
            if (current == '\r' && i + 1 < input.Length && input[i + 1] == '\n')
            {
                result.Append("\r\n");
                i++; // Skip '\n' since we already handled '\r\n'
            }
            else if (current == '\r' || current == '\n')
            {
                // Normalize standalone '\r' or '\n' to '\r\n'
                result.Append("\r\n");
            }
            else
            {
                // Append normal characters
                result.Append(current);
            }
        }

        return result.ToString();
    }

    public static ValueTask<TValue> IfNull<TValue>(this ValueTask<TValue>? task, TValue defaultValue)
    {
        if (task == null)
            return ValueTask.FromResult(defaultValue);
        else
            return task.Value;
    }

    public static ExpandoObject Clone(this ExpandoObject obj)
    {
        if (obj == null)
            return null;

        var res = new ExpandoObject();
        var cloneDict = (IDictionary<string, object>)res;
        var objDict = (IDictionary<string, object>)obj;

        foreach (var kvp in objDict)
            cloneDict.Add(kvp.Key, kvp.Value);

        return res;
    }

    public static void Merge(this IDictionary<string, object> obj, IDictionary<string, object> other)
    {
        if (other == null)
            return;

        foreach (var kvp in other)
            obj[kvp.Key] = kvp.Value;
    }

    public static object ToPrimitive(this JsonElement el, bool objectsAsDictionaries = false, int count = 0)
    {
        if (count > 10)
            throw new RecursionException(
                $"Recursion detected {JsonSerializer.Serialize(el, new JsonSerializerOptions { WriteIndented = true })}");
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                return el.GetString();
            case JsonValueKind.Number:
                return el.GetDouble();
            case JsonValueKind.True:
            case JsonValueKind.False:
                return el.GetBoolean();
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Undefined:
                throw new InvalidOperationException("Cannot convert undefined JsonValueKind to a primitive value.");
            case JsonValueKind.Object:
            {
                count++;
                if (objectsAsDictionaries)
                {
                    var res = new Dictionary<string, object>();
                    foreach (var prop in el.EnumerateObject())
                        res[prop.Name] = prop.Value.ToPrimitive(true, count);
                    return res;
                }
                else
                    throw new InvalidOperationException("Cannot convert JsonObject to a primitive value.");
            }
            case JsonValueKind.Array:
            {
                var res = new List<object>();
                foreach (var item in el.EnumerateArray())
                    res.Add(item.ToPrimitive());
                return res;
            }
            default:
                throw new InvalidOperationException($"Unsupported JsonValueKind: {el.ValueKind}");
        }
    }

    public static IOptionsMonitor<T> MonitorRequiredSection<T>(this IConfiguration config, string sectionName,
        Func<IConfigurationSection, IConfigureOptions<T>> configureOptions)
        where T : class
    {
        var section = config.GetSection(sectionName);

        if (!section.Exists())
            throw new InvalidOperationException(
                $"Missing required configuration section {sectionName}");

        return section.GetOptionsMonitor(configureOptions(section));
    }

    public static JsonNode ToJsonNode(this JsonDocument jsonDocument) => JsonNode.Parse(JsonSerializer.Serialize(jsonDocument));

    public static JsonSerializerOptions JsonSerializerOptionsHuman = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static void ConfigureNamedOptionsSection<T>(this IServiceCollection sc, IConfiguration configuration, string section) where T : class
    {
        sc.Configure<T>(configuration.GetSection(section));
    }

    /// <summary>
    /// Checks if the filename matches one or more extensions.  Case insensitive.
    /// </summary>
    /// <param name="fileName">The file name to check.</param>
    /// <param name="extensions">Should include periods.</param>
    /// <returns>True if it matches one of the extensions</returns>
    public static bool FilenameHasExtension(this string fileName, params string[] extensions)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        var actualExt = Path.GetExtension(fileName);

        foreach (var ext in extensions)
            if (string.Equals(actualExt, ext, StringComparison.InvariantCultureIgnoreCase))
                return true;

        return false;
    }

    [GeneratedRegex(@"^P(?:(\d+)W)?(?:(\d+)D)?(?:T(?:(\d+(?:\.\d+)?)H)?(?:(\d+(?:\.\d+)?)M)?(?:(\d+(?:\.\d+)?)S)?)?$", RegexOptions.Compiled)]
    private static partial Regex Iso8601DurationRegexGenerator();

    public static Regex Iso8601DurationRegex = Iso8601DurationRegexGenerator();

    public static TimeSpan ParseIso8601Timespan(string isoDuration)
    {
        var match = Iso8601DurationRegex.Match(isoDuration);

        if (!match.Success)
            throw new FormatException("Invalid ISO 8601 duration format.");

        // Parse each group as a double (weeks and days normally are integers, but we parse as double for consistency)
        var weeks = match.Groups[1].Success ? double.Parse(match.Groups[1].Value) : 0;
        var days = match.Groups[2].Success ? double.Parse(match.Groups[2].Value) : 0;
        var hours = match.Groups[3].Success ? double.Parse(match.Groups[3].Value) : 0;
        var minutes = match.Groups[4].Success ? double.Parse(match.Groups[4].Value) : 0;
        var seconds = match.Groups[5].Success ? double.Parse(match.Groups[5].Value) : 0;

        // Convert weeks to days
        days += weeks * 7;

        return TimeSpan.FromDays(days)
               + TimeSpan.FromHours(hours)
               + TimeSpan.FromMinutes(minutes)
               + TimeSpan.FromSeconds(seconds);
    }

    public static int Inc<TKey>(this IDictionary<TKey, int> dictionary, TKey key)
    {
        if (dictionary.TryGetValue(key, out var value))
        {
            dictionary[key] = value + 1;
            return value + 1;
        }

        dictionary[key] = 1;
        return 1;
    }

    public static bool TryParseJson(string s, out JsonNode node)
    {
        try
        {
            node = JsonNode.Parse(s);
            return true;
        }
        catch
        {
            node = null;
            return false;
        }
    }

    public static object ParseJsonOrString(string s)
    {
        if (TryParseJson(s, out var node))
            return node;

        return s;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async Task DoesNothingAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
    }

    private static readonly Task _voidTask = DoesNothingAsync();
    public static Task VoidTask => _voidTask;

    public static readonly object Void = VoidObject.Instance;

    public static bool IsVoid(this object o) => o == VoidObject.Instance;

    public static async ValueTask<object> AwaitAnyAsync(object o, bool recursive)
    {
        if (!recursive)
            return await AwaitAnyAsync(o);

        while (true)
        {
            var lastO = o;
            o = await AwaitAnyAsync(o);
            if (o == lastO)
                return o;
        }
    }

    public static bool IsAwaitable(this object o)
    {
        if (o == null)
            return false;

        if (o is Type)
            throw new InvalidOperationException("This method cannot check if a type is awaitable.");

        var type = o.GetType();
        var awaiter = type.GetMethod("GetAwaiter");
        if (awaiter != null)
        {
            var awaiterType = awaiter.ReturnType;
            if (awaiterType == typeof(TaskAwaiter))
                return true;
            else if (awaiterType.IsGenericType && (awaiterType.GetGenericTypeDefinition() == typeof(TaskAwaiter<>) ||
                                                   awaiterType.GetGenericTypeDefinition() == typeof(ValueTaskAwaiter<>)))
                return true;
        }
        else if (o is Task)
            return true;

        return false;
    }

    public static async ValueTask<object> AwaitAnyAsync(object o)
    {
        if (o == null)
            return null;

        var type = o.GetType();
        var awaiter = type.GetMethod("GetAwaiter");
        if (awaiter != null)
        {
            var awaiterType = awaiter.ReturnType;
            if (awaiterType == typeof(TaskAwaiter))
            {
                await (Task)o;
                var resultProp = type.GetProperty("Result");
                if (resultProp != null)
                {
                    o = resultProp.GetValue(o);

                    if (o?.GetType().FullName == "System.Threading.Tasks.VoidTaskResult")
                        return Void;

                    return o;
                }

                return Void;
            }
            else if (awaiterType.IsGenericType && (awaiterType.GetGenericTypeDefinition() == typeof(TaskAwaiter<>) ||
                                                   awaiterType.GetGenericTypeDefinition() == typeof(ValueTaskAwaiter<>)))
            {
                var resultProp = type.GetProperty("Result");
                if (resultProp != null)
                {
                    o = resultProp.GetValue(o);

                    if (o?.GetType().FullName == "System.Threading.Tasks.VoidTaskResult")
                        return Void;

                    return o;
                }

                return Void;
            }
        }
        else if (o is Task t)
        {
            await t;
            return Void;
        }

        return o;
    }

    /// <summary>
    /// Deserializes the string as JSON if it is valid JSON, otherwise returns the string.
    /// Returns JSON is parsed using <see cref="ToPrimitive(System.Text.Json.Nodes.JsonNode)"/> with objects as dictionaries. 
    /// </summary>
    public static object IfJsonToPrimitive(this string s, bool objectsAsDictionaries = true)
    {
        if (string.IsNullOrWhiteSpace(s) || !s.TryParseToJsonElement(out var je))
            return s;

        return je.ToPrimitive(objectsAsDictionaries);
    }

    public static string CleanMethodName(string name)
    {
        if (name.Contains("|"))
            name = name.Split('|')[0];

        //only the part after >
        if (name.Contains(">"))
            name = name.Split('>')[1];

        if (name.StartsWith("g__"))
            name = name[3..];

        if (name.EndsWith("Async"))
            name = name[..^5];

        return name;
    }

    public static string ToString<T>(this T[] items, string seperator, string lastSeperator = null)
    {
        if (seperator == null)
            throw new ArgumentNullException(nameof(seperator));

        if (lastSeperator == null)
            lastSeperator = seperator;

        if (items.Length == 0)
            return "";

        if (items.Length == 1)
            return items[0].ToString();

        if (items.Length == 2)
            return items[0] + lastSeperator + items[1];

        var sb = new StringBuilder();
        for (var i = 0; i < items.Length - 1; i++)
        {
            sb.Append(items[i]);
            sb.Append(seperator);
        }

        sb.Append(lastSeperator);
        sb.Append(items[^1]);

        return sb.ToString();
    }

    public static void InsertRange(this JsonArray arr, int index, IEnumerable<JsonNode> items)
    {
        if (index < 0 || index > arr.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var i = index;
        foreach (var item in items)
        {
            arr.Insert(i, item);
            i++;
        }
    }

    public static bool HasAttribute<T>(this ParameterInfo par) where T : Attribute => par.GetCustomAttribute<T>() != null;

    public static bool HasAttribute<T>(this ParameterInfo par, out T attr) where T : Attribute
    {
        attr = par.GetCustomAttribute<T>();
        return attr != null;
    }

    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> asyncEnumerable)
    {
        var res = new List<T>();
        await foreach (var val in asyncEnumerable)
            res.Add(val);
        return res;
    }

    /// <summary>
    /// If en contains a single element, only return it's value, otherwise return List from the enumerable.
    /// </summary>
    public static object ToFirstElementOrList<T>(this IEnumerable<T> en)
    {
        if (en == null)
            return default;

        T firstElement = default;
        var isFirst = true;
        var multiple = false;
        var lst = new List<T>();

        foreach (var el in en)
        {
            lst.Add(el);
            if (isFirst)
                firstElement = el;
            else
            {
                multiple = true;
                break;
            }

            isFirst = false;
        }

        if (multiple)
            return lst;
        else
            return firstElement;
    }

    public static bool IsEmptyOrNull<T>(this IEnumerable<T> enumerable) => enumerable == null || !enumerable.Any();

    public const char NBSP = '\u00A0';

    /// <summary>
    /// Removes the common leading indentation (spaces and tabs) from all non-blank lines in the input string.
    /// Each tab is considered as <paramref name="tabSize"/> spaces.
    /// Leading and trailing blank lines are preserved.
    /// </summary>
    /// <param name="input">The input multi-line string.</param>
    /// <param name="tabSize">The number of spaces that a tab character represents. Default is 4.</param>
    /// <returns>A new string with the common indentation removed from each non-blank line.</returns>
    public static string RemoveCommonIndentation(this string input, int tabSize = 4)
    {
        if (input == null)
            return null;

        // Split the string into lines preserving blank lines.
        var lines = input.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);

        // Compute common indentation based on non-blank lines only.
        int GetEffectiveIndent(string line)
        {
            var count = 0;
            foreach (var c in line)
            {
                if (c == ' ')
                {
                    count += 1;
                }
                else if (c == '\t')
                {
                    count += tabSize;
                }
                else
                {
                    break;
                }
            }

            return count;
        }

        var nonBlankLines = lines.Where(line => !string.IsNullOrWhiteSpace(line));
        // If there are no non-blank lines, return the input unchanged.
        if (!nonBlankLines.Any())
        {
            var sb = new StringBuilder();
            for (var i = 0; i < lines.Length; i++)
                sb.AppendLine();
            return sb.ToString();
        }

        var commonIndent = nonBlankLines.Select(GetEffectiveIndent).Min();

        // Function to remove effective indentation from a line.
        string RemoveIndent(string line)
        {
            // Leave blank lines as is.
            if (string.IsNullOrEmpty(line))
                return line;

            var removed = 0;
            var index = 0;
            while (index < line.Length && removed < commonIndent)
            {
                var c = line[index];
                if (c == ' ')
                {
                    removed += 1;
                }
                else if (c == '\t')
                {
                    removed += tabSize;
                }
                else
                {
                    break;
                }

                index++;
            }

            return line.Substring(index);
        }

        // Process every line.
        var trimmedLines = lines.Select(line => RemoveIndent(line));
        return string.Join(Environment.NewLine, trimmedLines);
    }

    public static string ToKebabCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Replace uppercase letters with '-' + lowercase, except at the start
        var kebab = Regex.Replace(
            input,
            "(?<!^)([A-Z])",
            "-$1"
        ).ToLower();

        return kebab;
    }

    public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        foreach (var item in items)
            dict.Add(item);
    }

    public static void Push<TValue>(this List<TValue> lst, TValue value) => lst.Add(value);

    public static TValue Pop<TValue>(this List<TValue> lst)
    {
        if (lst.Count == 0)
            throw new InvalidOperationException("Cannot pop from an empty list.");
        var value = lst[^1];
        lst.RemoveAt(lst.Count - 1);
        return value;
    }

    public static int StringLength(this List<StringBuilder> lst)
    {
        var length = 0;
        foreach (var sb in lst)
            length += sb.Length;
        return length;
    }

    public static void EnsureStartsOnNewLine(this List<StringBuilder> builders, int openLines = 0)
    {
        if (builders == null || builders.Count == 0)
            throw new InvalidOperationException("No StringBuilders provided.");

        if (builders.StringLength() == 0)
            return;

        var lineFeeds = builders.CountTrailingLinefeeds();

        for (var i = 0; i < openLines + 1 - lineFeeds; i++)
            builders[^1].AppendLine();
    }

    public static int CountTrailingLinefeeds(this List<StringBuilder> builders)
    {
        // If no builders are provided, then the joined string is empty.
        if (builders == null || builders.Count == 0)
            return 0;

        // Start at the last builder and its last character.
        var b = builders.Count - 1;
        var i = builders[b].Length - 1;
        var newlineCount = 0;

        // Loop backwards through the overall concatenated string.
        while (b >= 0 && i >= 0)
        {
            var current = builders[b][i];

            if (current == '\n')
            {
                // Look for a preceding '\r' to form a Windows newline.
                var paired = false;
                int pb = b, pi = i;

                // Move to the previous character in the overall sequence.
                if (pi > 0)
                {
                    pi--;
                }
                else
                {
                    pb--;
                    if (pb >= 0 && builders[pb].Length > 0)
                    {
                        pi = builders[pb].Length - 1;
                    }
                    else
                    {
                        pi = -1;
                    }
                }

                if (pi >= 0 && pb >= 0 && builders[pb][pi] == '\r')
                {
                    // We have a "\r\n" sequence.
                    paired = true;
                    newlineCount++;
                    // Now consume the '\r' as well.
                    // Set the new current position to the character before the '\r'
                    b = pb;
                    i = pi; // currently at '\r'
                    if (i > 0)
                    {
                        i--;
                    }
                    else
                    {
                        // Move to the previous builder if available.
                        b--;
                        if (b >= 0)
                        {
                            i = builders[b].Length - 1;
                        }
                        else
                        {
                            i = -1;
                        }
                    }
                }

                if (!paired)
                {
                    // A lone '\n' counts as one linefeed.
                    newlineCount++;
                    if (i > 0)
                    {
                        i--;
                    }
                    else
                    {
                        b--;
                        if (b >= 0)
                        {
                            i = builders[b].Length - 1;
                        }
                        else
                        {
                            i = -1;
                        }
                    }
                }
            }
            else if (current == '\r')
            {
                // A lone '\r' counts as one linefeed.
                newlineCount++;
                if (i > 0)
                {
                    i--;
                }
                else
                {
                    b--;
                    if (b >= 0)
                    {
                        i = builders[b].Length - 1;
                    }
                    else
                    {
                        i = -1;
                    }
                }
            }
            else
            {
                // As soon as a non-linefeed character is reached, stop scanning.
                break;
            }
        }

        return newlineCount;
    }

    public static T Peek<T>(this List<T> lst)
    {
        if (lst == null || lst.Count == 0)
            return default;

        return lst[^1];
    }

    public static bool EndsWithNLinefeeds(this List<StringBuilder> builders, int requiredCount) => builders.CountTrailingLinefeeds() == requiredCount;

    public static string ToValidOpenAISchemaName(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "once_off_schema";

        // Replace invalid characters with underscores
        var validName = Regex.Replace(input, @"[^a-zA-Z0-9_-]", "_");

        // Ensure the name doesn't start with a digit
        if (char.IsDigit(validName[0]))
            validName = "_" + validName;

        return validName.Left(30);
    }

    public static T GetAttribute<T>(this ParameterInfo par, bool inherit = true) where T : Attribute => par.GetCustomAttribute<T>(inherit);

    public static T GetAttribute<T>(this PropertyInfo property, bool inherit = true, bool interfaces = true) where T : Attribute
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        var attribute = property.GetCustomAttribute<T>(inherit);
        if (attribute != null)
            return attribute;

        if (property.DeclaringType != null && interfaces)
        {
            foreach (var iface in property.DeclaringType.GetInterfaces())
            {
                var interfaceProperty = iface.GetProperty(property.Name);
                if (interfaceProperty != null)
                {
                    attribute = interfaceProperty.GetCustomAttribute<T>();
                    if (attribute != null)
                        return attribute;
                }
            }
        }

        return null;
    }

    public static T GetAttribute<T>(this FieldInfo field, bool inherit = true, bool interfaces = true) where T : Attribute
    {
        if (field == null)
            throw new ArgumentNullException(nameof(field));

        var attribute = field.GetCustomAttribute<T>(inherit);
        if (attribute != null)
            return attribute;

        if (field.DeclaringType != null && interfaces)
        {
            foreach (var iface in field.DeclaringType.GetInterfaces())
            {
                var interfaceProperty = iface.GetField(field.Name);
                if (interfaceProperty != null)
                {
                    attribute = interfaceProperty.GetCustomAttribute<T>();
                    if (attribute != null)
                        return attribute;
                }
            }
        }

        return null;
    }
    
    public static T GetAttribute<T>(this MethodInfo method, bool inherit = true, bool interfaces = true) where T : Attribute
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        var attribute = method.GetCustomAttribute<T>(inherit);
        if (attribute != null)
            return attribute;

        if (method.DeclaringType != null && interfaces)
        {
            foreach (var iface in method.DeclaringType.GetInterfaces())
            {
                var interfaceProperty = iface.GetMethod(method.Name);
                if (interfaceProperty != null)
                {
                    attribute = interfaceProperty.GetCustomAttribute<T>();
                    if (attribute != null)
                        return attribute;
                }
            }
        }

        return null;
    }

    public static void Add<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, TValue value)
    {
        if (!dict.TryGetValue(key, out var lst))
            dict[key] = lst = [];
        lst.Add(value);
    }
    
    public static JsonDocument ToJsonDocument(this JsonNode jsonNode)
    {
        if (jsonNode == null)
            return null;
            
        var jsonString = jsonNode.ToJsonString();
        var jsonDocument = JsonDocument.Parse(jsonString);
        return jsonDocument;
    }

    public static string ToYesNo(this bool b) => b ? "Yes" : "No";

    public static void Add<T>(this List<Task> tasks, Func<T> func) where T: Task
    {
        tasks.Add(func());
    }
}