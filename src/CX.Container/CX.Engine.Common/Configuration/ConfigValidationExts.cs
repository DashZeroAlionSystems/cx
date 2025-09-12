using System.Numerics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace CX.Engine.Common;

public static class ConfigValidationExts
{
    public static void ThrowIfNullOrWhiteSpace(this IConfigurationSection section, string value,
        [CallerArgumentExpression(nameof(value))]
        string propertyName = null,
        string message = null)
    {
        if (section == null)
            throw new ArgumentNullException(nameof(section));
        
        if (propertyName == null)
            throw new ArgumentNullException(nameof(propertyName));

        var path = section.Path;
        if (string.IsNullOrWhiteSpace(value))
            throw new ConfigValidationException(path, propertyName, message ?? $"{propertyName} is required and may not be null or white space in {path}");
    }
    
    public static void ThrowIfNullOrEmpty<T>(this IConfigurationSection section, IEnumerable<T> value,
        [CallerArgumentExpression(nameof(value))]
        string propertyName = null)
    {
        if (section == null)
            throw new ArgumentNullException(nameof(section));
        
        if (propertyName == null)
            throw new ArgumentNullException(nameof(propertyName));

        var path = section.Path;
        if (value == null || !value.Any())
            throw new ConfigValidationException(path, propertyName, $"{propertyName} is required and may not be null or empty in {path}");
    } 

    public static void ThrowIfNull(this IConfigurationSection section, object value, string message = null,
        [CallerArgumentExpression(nameof(value))]
        string propertyName = null)
    {
        if (section == null)
            throw new ArgumentNullException(nameof(section));
        
        message ??= $"{propertyName} is required and may not be null in {section.Path}";
    
        var path = section.Path;

        if (value == null)
            throw new ConfigValidationException(path, propertyName, message);
    }

    public static void ThrowIfZeroOrNegative(this IConfigurationSection section, int value, string message = null,
        [CallerArgumentExpression(nameof(value))]
        string propertyName = null)
    {
        if (section == null)
            throw new ArgumentNullException(nameof(section));
        
        message ??= $"{propertyName} has to be 1 or greater in {section.Path}";
    
        var path = section.Path;

        if (value < 1)
            throw new ConfigValidationException(path, propertyName, message);
    }

    public static void ThrowIfNegative<T>(this IConfigurationSection section, T value, string message = null,
        [CallerArgumentExpression(nameof(value))]
        string propertyName = null)
        where T: INumber<T>
    {
        if (section == null)
            throw new ArgumentNullException(nameof(section));
        
        message ??= $"{propertyName} must be greater than or equal to 0 in {section.Path}";
    
        var path = section.Path;
        
        if (value < T.Zero)
            throw new ConfigValidationException(path, propertyName, message);
    }

    public static void ThrowIfNamedServiceNotFound<T>(this IConfigurationSection section, T value, string name,
        [CallerArgumentExpression(nameof(value))]
        string propertyName = null)
        where T: class
    {
        if (section == null)
            throw new ArgumentNullException(nameof(section));
        
        if (propertyName == null)
            throw new ArgumentNullException(nameof(propertyName));
        
        var path = section.Path;

        if (value == null)
            throw new ConfigValidationException(path, name, $"{propertyName} {name.SignleQuoteAndEscape()} not found for {path}");
    }

    public static void ThrowIfDoesNotExist(this IConfiguration config, string mainSectionName, string subSectionName, string message = null)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        
        var section = config.GetSection(mainSectionName, subSectionName);

        var exists = section.Exists();
        var path = section.Path;

        message ??= $"Missing required configuration section {path}";

        if (!exists)
            throw new ConfigValidationException(path, null, message);
    }

    public static void ThrowIfDoesNotExist([NotNull] this IConfigurationSection section, string message = null)
    {
        var exists = section.Exists();
        var path = section.Path;

        message ??= $"Missing required configuration section {path}";

        if (!exists)
            throw new ConfigValidationException(path, null, message);
    }
    
    /// <summary>
    /// minValue and maxValue are inclusive.
    /// </summary>
    public static void ThrowIfNotInRange(this IConfigurationSection section, double value,
        double minValue,
        double maxValue,
        string message = null,
        [CallerArgumentExpression(nameof(value))]
        string propertyName = null)
    {
        if (section == null)
            throw new ArgumentNullException(nameof(section));
        
        message ??= $"{propertyName} has to be in the range {minValue} - {maxValue} in {section.Path}";
    
        var path = section.Path;

        if (value < minValue || value > maxValue || double.IsNaN(value) || double.IsInfinity(value))
            throw new ConfigValidationException(path, propertyName, message);
    }

}