using System.Text.Json;
using CX.Container.Server.Databases;
using CX.Container.Server.Domain;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Resources;
using CX.Engine.Archives.Pinecone;
using CX.Engine.Assistants.Channels;
using CX.Engine.Assistants.Walter1;
using CX.Engine.Common;
using CX.Engine.Common.Db;
using CX.Engine.Common.DistributedLocks;
using CX.Engine.Common.PostgreSQL;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CX.Container.Server.Controllers.v1;

[ApiController]
[Route("api/pinecone-namespaces")]
[ApiVersion("1.0")]
public sealed class PineconeNamespacesController : ControllerBase
{
    [NotNull] private readonly AelaDbContext _dbContext;
    private readonly DistributedLockService _distributedLockService;
    private readonly PostgreSQLClient _sql;

    public PineconeNamespacesController(IServiceProvider sp, [NotNull] AelaDbContext dbContext, [NotNull] DistributedLockService distributedLockService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _distributedLockService = distributedLockService ?? throw new ArgumentNullException(nameof(distributedLockService));
        _sql = sp.GetRequiredNamedService<PostgreSQLClient>("pg_default");
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private class ProjectDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Thumbnail { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private class NamespaceListing
    {
        public string Key { get; set; }
        public PineconeNamespaceOptions Value { get; set; }
        public List<ProjectDto> UsedByProjects { get; set; }
    }

    [HttpGet("", Name = "GetNamespaces")]
    [Authorize]
    public async Task<ActionResult> GetNamespacesAsync()
    {
        var projectsTask = _dbContext.Projects.ToListAsync();
        var namespacesTask = _sql.ListAsync<NamespaceListing>("SELECT key AS Key, value AS Value FROM config_pinecone_namespaces", row => new()
        {
            Key = row.Get<string>("key"),
            Value = JsonSerializer.Deserialize<PineconeNamespaceOptions>(row.Get<string>("value"))
        });

        var projects = await projectsTask;
        var res = await namespacesTask;

        foreach (var row in res)
            row.UsedByProjects = projects.Where(p => p.Namespace == row.Key).Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Thumbnail = p.Thumbnail
            }).ToList();

        return new OkObjectResult(res);
    }

    [HttpPut("{id}", Name = "AddNamespaces")]
    [Authorize]
    public async Task<ActionResult> AddOrUpdateNamespaceAsync(string id, [FromBody] PineconeNamespaceOptions options)
    {
        await using var _ = await _distributedLockService.UseAsync(Consts.ApiLock);

        if (!StringValidators.IsValidAlphaNumericUnderscoreStartingWithLetter(id))
            return BadRequest(new
                { ErrorCode = 1, Message = "Namespace id must contain only alphanumeric characters or underscores and start with a letter or underscore" });

        if (options == null)
            return BadRequest(new { ErrorCode = 2, Message = "options must not be null" });
        
        try
        {
            options.Validate();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        
        if (options.Namespace.Length > 30)
            return BadRequest(new { ErrorCode = 3, Message = "Namespace must be 30 characters or less" });

        var value = JsonSerializer.Serialize(options);

        await _sql.ExecuteAsync(
            $"INSERT INTO config_pinecone_namespaces (key, value) VALUES ({id}, {value}::jsonb) ON CONFLICT (key) DO UPDATE SET value = {value}::jsonb");

        await _sql.ExecuteAsync(
            $$"""
               INSERT INTO config_channels (key, value) 
               VALUES ({{"pinecone_namespace_" + id}}, {{
                   JsonSerializer.Serialize(new ChannelOptions
                   {
                       DisplayName = id,
                       AssistantName = "walter1.default",
                       ShowInUI = true,
                       Overrides = [ new Walter1AssistantOptionsOverrides
                       {
                           RemoveArchives = ["pinecone.default"],
                           AddArchives = [$"pinecone-namespace.{id}"]
                       }]
                   })
               }}::jsonb)
               ON CONFLICT (key) DO NOTHING;
               """
        );
        
        return NoContent();
    }

    [HttpDelete("{id}", Name = "DeleteNamespaces")]
    [Authorize]
    public async Task<ActionResult> DeleteNamespaceAsync(string id)
    {
        await using var _ = await _distributedLockService.UseAsync(Consts.ApiLock);

        if (!StringValidators.IsValidAlphaNumericUnderscoreStartingWithLetter(id))
            return BadRequest(new
            {
                ErrorCode = 1,
                Message = "Namespace id must contain only alphanumeric characters or underscores and start with a letter or underscore"
            });

        if (await _dbContext.Projects.Where(proj => proj.Namespace == id).AnyAsync())
            return BadRequest(new { ErrorCode = 2, Message = "Namespace is in use by one or more projects" });

        await _sql.ExecuteAsync($"DELETE FROM config_pinecone_namespaces WHERE key = {id}");

        await _sql.ExecuteAsync(
            $$"""
            DELETE FROM config_channels WHERE key = {{"pinecone_namespace_" + id}}
            """);

        return NoContent();
    }
}