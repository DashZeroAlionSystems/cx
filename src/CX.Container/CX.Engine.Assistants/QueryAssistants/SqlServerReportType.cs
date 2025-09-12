using System.Data;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.SqlServer;

namespace CX.Engine.Assistants.QueryAssistants;

public enum SqlServerReportType
{
  String,
  Boolean,
  Date,
  DateTime,
  Integer,
  Number,
  Object,
}

public static class SqlServerReportTypeExt
{
  public static SqlDbType ToSqlDbType(this SqlServerReportType type)
  {
    return type switch
    {
      SqlServerReportType.String => SqlDbType.NVarChar,
      SqlServerReportType.Integer => SqlDbType.Int,
      SqlServerReportType.Boolean => SqlDbType.Bit,
      SqlServerReportType.Number => SqlDbType.Float,
      SqlServerReportType.Date => SqlDbType.DateTime,
      SqlServerReportType.DateTime => SqlDbType.DateTime,
      SqlServerReportType.Object => SqlDbType.NVarChar,
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
  }

  public static string ToSchemaType(this SqlServerReportType type)
  {
    return type switch
    {
      SqlServerReportType.String => PrimitiveTypes.String,
      SqlServerReportType.Integer => PrimitiveTypes.Integer,
      SqlServerReportType.Boolean => PrimitiveTypes.Boolean,
      SqlServerReportType.Number => PrimitiveTypes.Number,
      SqlServerReportType.Date => PrimitiveTypes.String,
      SqlServerReportType.DateTime => PrimitiveTypes.String,
      SqlServerReportType.Object => PrimitiveTypes.Object,
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
  }
}