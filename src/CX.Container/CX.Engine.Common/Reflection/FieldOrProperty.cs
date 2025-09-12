using System.Reflection;

namespace CX.Engine.Common.Reflection;

public struct FieldOrProperty
{
    public FieldInfo Field;
    public PropertyInfo Property;
    
    public FieldOrProperty(FieldInfo field)
    {
        Field = field;
        Property = null;
    }
    
    public FieldOrProperty(PropertyInfo property)
    {
        Field = null;
        Property = property;
    }
    
    public object GetValue(object obj)
    {
        if (Field != null)
            return Field.GetValue(obj);
        else
            return Property.GetValue(obj);
    }
    
    public void SetValue(object obj, object value)
    {
        if (Field != null)
            Field.SetValue(obj, value);
        else
            Property.SetValue(obj, value);
    }
    
    public Type ValueType
    {
        get
        {
            if (Field != null)
                return Field.FieldType;
            else
                return Property.PropertyType;
        }
    }
    
    public string Name
    {
        get
        {
            if (Field != null)
                return Field.Name;
            else
                return Property.Name;
        }
    }

    public bool HasAttribute<T>() where T : Attribute => GetAttribute<T>() != null;
    public bool HasAttribute<T>(out T attr) where T : Attribute
    {
        attr = GetAttribute<T>();
        return attr != null;
    }

    public T GetAttribute<T>(bool inherit = true, bool interfaces = true) where T: Attribute
    {
        if (Field != null)
            return Field.GetAttribute<T>(inherit, interfaces);
        else
            return Property.GetAttribute<T>(inherit, interfaces);
    }
    
    public bool TryGetAttribute<T> (out T attr) where T: Attribute
    {
        attr = GetAttribute<T>();
        return attr != null;
    }
}