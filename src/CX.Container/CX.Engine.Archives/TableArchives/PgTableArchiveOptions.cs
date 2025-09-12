namespace CX.Engine.Archives.TableArchives;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PgTableArchiveOptions : IValidatableConfiguration
{
    public string PostgreSQLClientName { get; set; }
    public string TableName { get; set; }

    public void Validate(IConfigurationSection section)
    {
        section.ThrowIfNullOrWhiteSpace(PostgreSQLClientName);
        section.ThrowIfNullOrWhiteSpace(TableName);
    }
}