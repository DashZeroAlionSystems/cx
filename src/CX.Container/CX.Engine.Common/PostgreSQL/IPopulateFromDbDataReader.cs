using System.Data.Common;

namespace CX.Engine.Common.PostgreSQL;

public interface IPopulateFromDbDataReader
{
    void PopulateFromDbDataReader(DbDataReader reader);
}