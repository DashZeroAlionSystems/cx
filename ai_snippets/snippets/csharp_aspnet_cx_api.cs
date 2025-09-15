using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

// Domain
public record CxUser(int Id, string Email, bool IsActive);

// Repository
public interface IUserRepository {
	Task<CxUser?> GetByIdAsync(int id);
	Task UpsertAsync(CxUser user);
}

public sealed class PgUserRepository : IUserRepository {
	private readonly string _connString;
	public PgUserRepository(string connString) { _connString = connString; }

	public async Task<CxUser?> GetByIdAsync(int id) {
		await using var conn = new NpgsqlConnection(_connString);
		await conn.OpenAsync();
		await using var cmd = new NpgsqlCommand("SELECT id, email, is_active FROM cx_users WHERE id = @id", conn);
		cmd.Parameters.AddWithValue("id", id);
		await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
		if (!await reader.ReadAsync()) return null;
		return new CxUser(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2));
	}

	public async Task UpsertAsync(CxUser user) {
		await using var conn = new NpgsqlConnection(_connString);
		await conn.OpenAsync();
		await using var cmd = new NpgsqlCommand(@"INSERT INTO cx_users(id, email, is_active)
VALUES(@id, @email, @is_active)
ON CONFLICT (id) DO UPDATE SET email = EXCLUDED.email, is_active = EXCLUDED.is_active;", conn);
		cmd.Parameters.AddWithValue("id", user.Id);
		cmd.Parameters.AddWithValue("email", user.Email);
		cmd.Parameters.AddWithValue("is_active", user.IsActive);
		await cmd.ExecuteNonQueryAsync();
	}
}

// Service
public sealed class UserService {
	private readonly IUserRepository _repo;
	public UserService(IUserRepository repo) { _repo = repo; }
	public Task<CxUser?> GetAsync(int id) => _repo.GetByIdAsync(id);
	public Task UpsertAsync(CxUser user) => _repo.UpsertAsync(user);
}

var builder = WebApplication.CreateBuilder(args);
var connString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING") ?? "Host=localhost;Username=postgres;Password=postgres;Database=postgres";

builder.Services.AddSingleton<IUserRepository>(_ => new PgUserRepository(connString));
builder.Services.AddScoped<UserService>();

var app = builder.Build();

app.MapPost("/users", async (UserService svc, CxUser user) => {
	await svc.UpsertAsync(user);
	return Results.NoContent();
});

app.MapGet("/users/{id:int}", async (UserService svc, int id) => {
	var user = await svc.GetAsync(id);
	return user is null ? Results.NotFound(new { error = "Not found" }) : Results.Ok(user);
});

app.Run();