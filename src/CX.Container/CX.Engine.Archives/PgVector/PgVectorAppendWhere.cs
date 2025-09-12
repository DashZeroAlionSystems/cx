namespace CX.Engine.Archives.PgVector;

public class PgVectorAppendWhere : IChunkArchiveRetrievalRequestComponent
{
    public string Where;
    public Dictionary<string, object> Parameters = [];

    public PgVectorAppendWhere()
    {
    }

    public PgVectorAppendWhere(string where)
    {
        Where = where;
    }
}