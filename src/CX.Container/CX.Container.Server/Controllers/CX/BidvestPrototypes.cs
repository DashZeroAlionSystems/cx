using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CX.Container.Server.Common;
using CX.Engine.Common;
using CX.Engine.Common.ACL;
using CX.Engine.Common.PostgreSQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CX.Container.Server.Controllers.CX;

public class BidvestRequest : IPopulateFromDbDataReader
{
    [JsonPropertyName("water_meter")]
    public string[] WaterMeter { get; set; }
    [JsonPropertyName("municipality_name")]
    public string MunicipalityName { get; set; }
    [JsonPropertyName("invoice_number")]
    public string InvoiceNumber { get; set; }
    [JsonPropertyName("invoice_date")]
    public DateTime InvoiceDate { get; set; }
    [JsonPropertyName("killowat_volts_amps_meter")]
    public string[] KillowatVoltsMmeter { get; set; }
    [JsonPropertyName("address")]
    public string[] Address { get; set; }
    [JsonPropertyName("sanitation_meter")]
    public string[] SanitationMeter { get; set; }
    [JsonPropertyName("metrics")]
    public string[] Metrics { get; set; }
    [JsonPropertyName("tariff_codes")]
    public string[] TariffCodes { get; set; }
    [JsonPropertyName("electricity_meter")]
    public string[] ElectricityMeter { get; set; }
    [JsonPropertyName("total_charges")]
    public string[] TotalCharges { get; set; }

    public void PopulateFromDbDataReader(DbDataReader reader)
    {
        var row = JsonSerializer.Deserialize<BidvestRequest>(reader.ToJsonString());
        WaterMeter = row.WaterMeter;
        MunicipalityName = row.MunicipalityName;
        InvoiceNumber = row.InvoiceNumber;
        InvoiceDate = row.InvoiceDate;
        TariffCodes = row.TariffCodes;
        ElectricityMeter = row.ElectricityMeter;
        TotalCharges = row.TotalCharges;
        KillowatVoltsMmeter = row.KillowatVoltsMmeter;
        Metrics = row.Metrics;
        Address = row.Address;
        SanitationMeter = row.SanitationMeter;
    }
}

public class BidvesResponse
{
    public bool Valid { get; set; }
    public string Message { get; set; }
}


[ApiController]
[Route("api/bidvest-prototype")]
[ApiVersion("1.0")]
public class BidvestPrototypesController(
    IServiceProvider sp) : ControllerBase
{
    private PostgreSQLClient _client = sp.GetRequiredNamedService<PostgreSQLClient>("pg_default");
    
    [HttpPost]
    public async Task<ActionResult<BidvesResponse>> CheckValidity([FromBody] BidvestRequest request)
    {
        var sql = $"SELECT * FROM  bidvest_prototype WHERE  water_meter && ARRAY['{string.Join("', '", request.WaterMeter)}']::text[]";
        var rows = await _client.ListAsync<BidvestRequest>(sql);

        rows = rows.OrderBy(x => x.InvoiceDate).ToList();
        var row = rows.LastOrDefault();

        /*if (!row?.Address.Equals(request.Address) ?? false)
            return Ok(new BidvesResponse()
            {
                Message = "Addresses do not equal previous results",
                Valid = false
            });*/
        
        if (row?.Metrics is not null)
        {
            foreach (var metric in row?.Metrics)
            {
                var _metric = metric.Split(':')[0];
                var reg = new Regex(@"\b(?<val>\d+(?:[.,]\d+)?)\s*(?:kl|kwh)\b",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var match = reg.Match(metric);
                var rmatch = reg.Match(request.Metrics.FirstOrDefault(x => x.Contains(_metric)) ?? string.Empty);
                if (string.IsNullOrWhiteSpace(match.Groups[1].Value) || string.IsNullOrWhiteSpace(rmatch.Groups[1].Value))
                    continue;
                var value = double.Parse(match.Groups[1].Value);
                var rvalue = double.Parse(rmatch.Groups[1].Value);
                if (value * 0.9 > rvalue ||
                    value * 1.1 < rvalue)
                    return Ok(new BidvesResponse()
                    {
                        Message = $"{_metric} for metric is below/above tolerance level",
                        Valid = false
                    });
            }
        }

        if (row?.TotalCharges is not null)
            foreach (var charge in row?.TotalCharges)
            {
                var _charge = charge.Split(':').FirstOrDefault();
                var reg = new Regex(
                    //  ↓↓↓  use a verbatim string so you don’t double-escape
                    @"\bR\s?([0-9]{1,3}(?:,[0-9]{3})*|[0-9]+)(?:\.[0-9]{2})?\b",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var match = reg.Match(charge);
                var rmatch = reg.Match(request.TotalCharges.FirstOrDefault(x => x.Contains(_charge, StringComparison.InvariantCulture)) ?? string.Empty);
                if (string.IsNullOrWhiteSpace(match.Groups[1].Value) || string.IsNullOrWhiteSpace(rmatch.Groups[1].Value))
                    continue;
                var value = double.Parse(match.Groups[1].Value);
                var rvalue = double.Parse(rmatch.Groups[1].Value);
                
                if (value * 0.9 > rvalue||
                    value * 1.1 < rvalue)
                    return Ok(new BidvesResponse()
                    {
                        Message = $"{_charge} for charge is below/above tolerance level",
                        Valid = false
                    });
            }

        return Ok(new BidvesResponse()
        {
            Message = "Valid invoince",
            Valid = true
        });
    }
}