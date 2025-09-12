using System.Data.Common;
using Dapper;

namespace CX.Engine.Common.Db;

public static class DbDataReaderExt
{
    public static T GetNullable<T>(this DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? default : reader.GetFieldValue<T>(ordinal);
    }
    
    public static T Get<T>(this DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetFieldValue<T>(ordinal);
    }
    
    public static bool ColumnExists(this DbDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i) == columnName)
                return true;
        }
        return false;
    }
    
    public static T GetNullable<T>(this DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? default : reader.GetFieldValue<T>(ordinal);
    }
    
    public static T Get<T>(this DbDataReader reader, int ordinal)
    {
        return reader.GetFieldValue<T>(ordinal);
    }
    
    public static T Dapper<T>(this DbDataReader rdr)
    {
        var deserializer = SqlMapper.GetTypeDeserializer(typeof(T), rdr);
        return (T)deserializer(rdr);
    }
    
    public static byte[] GetBytes(this DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? default : reader.GetFieldValue<byte[]>(ordinal);
    }
}