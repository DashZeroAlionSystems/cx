using System.Text;
using System.Text.Json;
using CX.Engine.Common;
using CX.Engine.Common.Db;
using CX.Engine.Common.PostgreSQL;
using Npgsql;

namespace CX.Clients.Afriforum.Domain;

public class SakenetwerkRepo
{
    private readonly PostgreSQLClient _client;

    public SakenetwerkRepo(IServiceProvider sp, SakenetwerkAssistantOptions options)
    {
        options.Validate();
        _client = sp.GetRequiredNamedService<PostgreSQLClient>(options.PostgreSQLClientName);
    }

    public async Task<List<string>> GetCitiesAsync()
    {
        return (await _client.ListStringAsync("SELECT DISTINCT city FROM sakenetwerk_besighede WHERE city IS NOT NULL"))
            .ToList()!;
    }

    public async Task<List<string>> GetCitiesAsync(string[] cityILikes)
    {
        return (await _client.ListStringAsync($"SELECT DISTINCT city FROM sakenetwerk_besighede WHERE city ILIKE ANY({cityILikes})"))
            .ToList()!;
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        return (await _client.ListStringAsync(
            """
            SELECT DISTINCT category
            FROM sakenetwerk_besighede
            CROSS JOIN LATERAL jsonb_array_elements_text(categories::jsonb) AS category
            WHERE jsonb_typeof(categories::jsonb) = 'array'
              AND category IS NOT NULL
            ORDER BY category;
            """)).ToList()!;
    }

    public async Task<List<string>> GetTagsAsync()
    {
        return (await _client.ListStringAsync(
            """
            SELECT DISTINCT tag
            FROM sakenetwerk_besighede
            CROSS JOIN LATERAL jsonb_array_elements_text(tags::jsonb) AS tag
            WHERE jsonb_typeof(tags::jsonb) = 'array'
              AND tag IS NOT NULL
            ORDER BY tag;
            """)).ToList()!;
    }

    public record IdCityProvince(Guid Id, string City, string Province);
    public Task<List<IdCityProvince>> GetIdCityProvinceAsync() =>
        _client.ListAsync<IdCityProvince>(
            "SELECT id, city, province FROM sakenetwerk_besighede",
            row => new(row.GetGuid(0), row.GetNullable<string>(1), row.GetNullable<string>(2)));

    public record IdNameCategoriesTags(Guid Id, string Name, string[] Categories, string[] Tags);
    public Task<List<IdNameCategoriesTags>> GetIdNameCategoriesTagsAsync() =>
        _client.ListAsync<IdNameCategoriesTags>(
            "SELECT id, name, categories, tags FROM sakenetwerk_besighede",
            row =>
            {
                try
                {
                    return new(row.GetGuid(0), row.GetString(1),
                        JsonSerializer.Deserialize<string[]>(row.GetNullable<string>(2) ?? "null"),
                        JsonSerializer.Deserialize<string[]>(row.GetNullable<string>(3) ?? "null"));
                }
                catch
                {
                    return new(row.GetGuid(0), row.GetString(1),
                        [row.GetNullable<string>(2)!],
                        [row.GetNullable<string>(3)!]);
                }
            })!;

    public async Task SetCategoriesAndTagsAsync(IdNameCategoriesTags row)
    {
        var cats = (row.Categories?.Length ?? 0) == 0 ? null : JsonSerializer.Serialize(row.Categories);
        var tags = (row.Tags?.Length ?? 0) == 0 ? null : JsonSerializer.Serialize(row.Tags);

        await _client.ExecuteAsync($"UPDATE sakenetwerk_besighede SET categories = {cats}::jsonb, tags = {tags}::jsonb WHERE id = {row.Id}");
    }

    public Task UpdateCityNameAsync(string oldName, string newName) =>
        _client.ExecuteAsync($"UPDATE sakenetwerk_besighede SET city = {newName} WHERE city = {oldName}");

    public Task UpdateProvinceAsync(Guid id, string newName) =>
        _client.ExecuteAsync($"UPDATE sakenetwerk_besighede SET province = {newName} WHERE id = {id}");

    public async Task<List<string>> GetRowsAsync(SakenetwerkFilter filter)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
                      SELECT row_to_json(t)
                      FROM (
                      SELECT name, email, telephone, website_url, discount, is_online, address_1, address_2, 
                             suburb, city, postal_code, province, long, lat 
                      FROM sakenetwerk_besighede
                      WHERE listing_status = 'Goedgekeur'
                      """);

        var cmd = new NpgsqlCommand();

        if (!string.IsNullOrWhiteSpace(filter.NameLike))
        {
            sb.AppendLine("AND name ILIKE @NameILIKE");
            cmd.Parameters.AddWithValue("NameILIKE", '%' + filter.NameLike + '%');
        }

        if (filter.CityLike?.Length > 0)
        {
            sb.AppendLine("AND city ILIKE ANY(@CityILIKE)");
            cmd.Parameters.AddWithValue("CityILike", filter.CityLike);
        }

        if (filter.Provinces?.Length > 0)
        {
            sb.AppendLine("AND province = ANY(@Province)");
            cmd.Parameters.AddWithValue("Province", filter.Provinces);
        }

        if (filter.Categories?.Length > 0 && !filter.Categories.Contains("Any"))
        {
            sb.AppendLine("AND categories::jsonb ?| @Categories");
            cmd.Parameters.AddWithValue("Categories", filter.Categories);
        }
        
        if (filter.Tags?.Length > 0  && !filter.Tags.Contains("Any"))
        {
            sb.AppendLine("AND tags::jsonb ?| @Tags");
            cmd.Parameters.AddWithValue("Tags", filter.Tags);
        }
        
        if (!string.IsNullOrWhiteSpace(filter.EmailLike))
        {
            sb.AppendLine("AND email ILIKE @EmailILIKE");
            cmd.Parameters.AddWithValue("EmailILIKE", '%' + filter.EmailLike + '%');
        }

        if (!string.IsNullOrWhiteSpace(filter.PhoneNumber))
        {
            sb.AppendLine("AND regexp_replace(telephone, '[^0-9]', '', 'g') ILIKE ('%' || regexp_replace(@PhoneILIKE, '[^0-9]', '', 'g') || '%')");
            cmd.Parameters.AddWithValue("PhoneILIKE", filter.PhoneNumber);
        }
        
        if (!string.IsNullOrWhiteSpace(filter.UrlLike))
        {
            sb.AppendLine("AND website_url ILIKE @UrlILIKE");
            cmd.Parameters.AddWithValue("UrlILIKE", filter.UrlLike);
        }
        
        sb.AppendLine("LIMIT 300");
        sb.AppendLine(") t");

        cmd.CommandText = sb.ToString();

        return (await _client.ListStringAsync(cmd)).ToList()!;
    }
}