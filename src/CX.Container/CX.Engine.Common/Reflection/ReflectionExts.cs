using System.Reflection;

namespace CX.Engine.Common.Reflection;

public static class ReflectionExts
{
    public static IEnumerable<FieldOrProperty> GetFieldsAndProperties(this Type type, BindingFlags? flags = null)
    {
        var fl = flags ?? BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        
        var fields = type.GetFields(fl).Select(f => new FieldOrProperty(f));
        var properties = type.GetProperties(fl).Select(p => new FieldOrProperty(p));
        return fields.Concat(properties);
    }

    public static IEnumerable<FieldOrProperty> GetFieldsAndPropertiesWithAttribute<T>(this Type type, BindingFlags? flags = null) where T: Attribute
    {
        return GetFieldsAndProperties(type, flags ?? BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(f => f.GetAttribute<T>() != null);
    }
}