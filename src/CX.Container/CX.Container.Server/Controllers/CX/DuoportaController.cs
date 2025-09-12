using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Aela.Server.Domain;
using Aela.Server.Domain.Duoporta;
using CX.Container.Domain.Duoporta;
using CX.Container.Server.Common;
using CX.Container.Server.Domain;
using CX.Container.Server.Extensions.Services;
using CX.Engine.Common.ACL;
using CX.Engine.SharedOptions;
using Microsoft.Extensions.Options;
using JetBrains.Annotations;

namespace Aela.Server.Controllers.CX
{
    [Authorize]
    [ApiController]
    [Route("api/duoporta")]
    [ApiVersion("1.0")]
    public sealed class DuoportaController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<StructuredDataOptions> _structuredDataOptions;
        private readonly IOptionsMonitor<DuoportaOptions> _duoportaOptions;
        private readonly ACLService _aclService;

        public DuoportaController(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<StructuredDataOptions> structuredDataOptions,
            [NotNull] IOptionsMonitor<DuoportaOptions> duoportaOptions,
            ACLService aclService)
        {
            _httpClientFactory = httpClientFactory;
            _structuredDataOptions = structuredDataOptions;
            _duoportaOptions = duoportaOptions ?? throw new ArgumentNullException(nameof(duoportaOptions));
            _aclService = aclService;
        }

        private StringContent CreateDuoportaAuthBody()
        {
            var credentials = new
            {
                client_id = _duoportaOptions.CurrentValue.ClientId,
                api_key = _duoportaOptions.CurrentValue.ApiKey
            };

            return new(
                JsonSerializer.Serialize(credentials),
                Encoding.UTF8,
                "application/json");
        }

        [HttpPost]
        [RequiresAtLeastUserRole]
        [Route("brands")]
        public async Task<IActionResult> GetBrandsAsync()
        {
            // Authorization handled by RequiresAtLeastUserRole attribute

            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"{_duoportaOptions.CurrentValue.BaseUrl}/brands",
                CreateDuoportaAuthBody());

            var content = await response.Content.ReadAsStringAsync();
            
            return new ContentResult
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        [HttpPost]
        [RequiresAtLeastUserRole]
        [Route("ranges/{brandId}")]
        public async Task<IActionResult> GetRangesAsync(string brandId)
        {
            // Authorization handled by RequiresAtLeastUserRole attribute

            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"{_duoportaOptions.CurrentValue.BaseUrl}/ranges/{Uri.EscapeDataString(brandId)}",
                CreateDuoportaAuthBody());

            var content = await response.Content.ReadAsStringAsync();
            
            return new ContentResult
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        [HttpPost]
        [RequiresAtLeastUserRole]
        [Route("models/{rangeId}")]
        public async Task<IActionResult> GetModelsAsync(string rangeId)
        {
            // Authorization handled by RequiresAtLeastUserRole attribute

            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"{_duoportaOptions.CurrentValue.BaseUrl}/models/{Uri.EscapeDataString(rangeId)}",
                CreateDuoportaAuthBody());

            var content = await response.Content.ReadAsStringAsync();
            
            return new ContentResult
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        [HttpPost]
        [RequiresAtLeastUserRole]
        [Route("specifications/{duoportaId}")]
        public async Task<IActionResult> GetSpecificationsAsync(string duoportaId)
        {
            // Authorization handled by RequiresAtLeastUserRole attribute

            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"{_duoportaOptions.CurrentValue.BaseUrl}/specifications/{Uri.EscapeDataString(duoportaId)}",
                CreateDuoportaAuthBody());

            var content = await response.Content.ReadAsStringAsync();
            
            return new ContentResult
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        [HttpPost]
        [RequiresAtLeastUserRole]
        [Route("features/{duoportaId}")]
        public async Task<IActionResult> GetFeaturesAsync(string duoportaId)
        {
            // Authorization handled by RequiresAtLeastUserRole attribute

            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"{_duoportaOptions.CurrentValue.BaseUrl}/features/{Uri.EscapeDataString(duoportaId)}",
                CreateDuoportaAuthBody());

            var content = await response.Content.ReadAsStringAsync();
            
            return new ContentResult
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        [HttpPost]
        [RequiresAtLeastUserRole]
        [Route("images/{duoportaId}")]
        public async Task<IActionResult> GetImagesAsync(string duoportaId)
        {
            // Authorization handled by RequiresAtLeastUserRole attribute

            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"{_duoportaOptions.CurrentValue.BaseUrl}/images/{Uri.EscapeDataString(duoportaId)}",
                CreateDuoportaAuthBody());

            var content = await response.Content.ReadAsStringAsync();
            
            return new ContentResult
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        [HttpPost]
        [RequiresAtLeastUserRole]
        [Route("mmlookup/{mmCode}")]
        public async Task<IActionResult> GetByMMCodeAsync(string mmCode)
        {
            // Authorization handled by RequiresAtLeastUserRole attribute

            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"{_duoportaOptions.CurrentValue.BaseUrl}/mmlookup/{Uri.EscapeDataString(mmCode)}",
                CreateDuoportaAuthBody());

            var content = await response.Content.ReadAsStringAsync();
            
            return new ContentResult
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        
         [HttpPost]
        [RequiresAtLeastUserRole]
        [Route("mmlookup/{mmCode}/full")]
         public async Task<IActionResult> GetFullMMCodeDataAsync(string mmCode)
        {
            // Authorization handled by RequiresAtLeastUserRole attribute

            using var client = _httpClientFactory.CreateClient();
            
            // First get the MM lookup to extract derivative, make, and model
            var mmLookupResponse = await client.PostAsync(
                $"{_duoportaOptions.CurrentValue.BaseUrl}/mmlookup/{Uri.EscapeDataString(mmCode)}",
                CreateDuoportaAuthBody());

            var mmLookupContent = await mmLookupResponse.Content.ReadAsStringAsync();

            if (!mmLookupResponse.IsSuccessStatusCode)
            {
                return new ContentResult
                {
                    Content = mmLookupContent,
                    ContentType = "application/json",
                    StatusCode = (int)mmLookupResponse.StatusCode
                };
            }

            // Deserialize the initial lookup response
            var mmLookupData = JsonSerializer.Deserialize<JsonElement>(mmLookupContent);
            var firstVehicle = mmLookupData.EnumerateArray().FirstOrDefault();
            
            if (firstVehicle.ValueKind == JsonValueKind.Undefined)
            {
                return NotFound(new { Message = "No vehicle data found" });
            }

            // Extract derivative, make, and model information
            JsonElement? derivative = null;
            JsonElement? model = null;
            JsonElement? make = null;

            if (firstVehicle.TryGetProperty("derivative", out var derivativeElement))
            {
                derivative = derivativeElement;
            }

            if (firstVehicle.TryGetProperty("model", out var modelElement))
            {
                model = modelElement;
            }

            if (firstVehicle.TryGetProperty("make", out var makeElement))
            {
                make = makeElement;
            }

            if (!firstVehicle.TryGetProperty("fields", out var fields))
            {
                return NotFound(new { Message = "No fields found in response" });
            }

            // Prepare the additional context fields
            var additionalContextFields = new List<JsonElement>();

            // Helper method to create a field
            JsonElement CreateField(string name, string value, int? id = null)
            {
                string jsonString;
                if (id.HasValue)
                {
                    jsonString = $"{{\"name\":\"{name}\",\"value\":\"{value}\",\"id\":{id}}}";
                }
                else
                {
                    jsonString = $"{{\"name\":\"{name}\",\"value\":\"{value}\"}}";
                }
                using var doc = JsonDocument.Parse(jsonString);
                return doc.RootElement.Clone();
            }

            // Add additional context fields from initial lookup
            if (derivative.HasValue && derivative.Value.TryGetProperty("id", out var derivativeIdElement) && 
                derivative.Value.TryGetProperty("name", out var derivativeNameElement))
            {
                int derivativeId = derivativeIdElement.GetInt32();
                string derivativeName = derivativeNameElement.GetString();
                
                additionalContextFields.Add(CreateField("Derivative", derivativeName, derivativeId));
            }

            if (model.HasValue && model.Value.TryGetProperty("id", out var modelIdElement) && 
                model.Value.TryGetProperty("name", out var modelNameElement))
            {
                int modelId = modelIdElement.GetInt32();
                string modelName = modelNameElement.GetString();
                
                additionalContextFields.Add(CreateField("Model", modelName, modelId));
            }

            if (make.HasValue && make.Value.TryGetProperty("id", out var makeIdElement) && 
                make.Value.TryGetProperty("name", out var makeNameElement))
            {
                int makeId = makeIdElement.GetInt32();
                string makeName = makeNameElement.GetString();
                
                additionalContextFields.Add(CreateField("Make", makeName, makeId));
            }

            // Find duoporta ID from fields
            var duoportaIdField = fields.EnumerateArray()
                .FirstOrDefault(f => 
                    f.TryGetProperty("name", out var name) && 
                    name.GetString() == "duoporta record ID");

            if (duoportaIdField.ValueKind == JsonValueKind.Undefined || 
                !duoportaIdField.TryGetProperty("value", out var duoportaIdValue))
            {
                return NotFound(new { Message = "No duoporta record ID found" });
            }

            var duoportaId = duoportaIdValue.GetString();

            // Get specifications
            var specsResponse = await client.PostAsync(
                $"{_duoportaOptions.CurrentValue.BaseUrl}/specifications/{Uri.EscapeDataString(duoportaId)}",
                CreateDuoportaAuthBody());

            // Get features
            var featuresResponse = await client.PostAsync(
                $"{_duoportaOptions.CurrentValue.BaseUrl}/features/{Uri.EscapeDataString(duoportaId)}",
                CreateDuoportaAuthBody());

            // Combine the responses
            var specsContent = await specsResponse.Content.ReadAsStringAsync();
            var featuresContent = await featuresResponse.Content.ReadAsStringAsync();

            var specs = JsonSerializer.Deserialize<JsonElement>(specsContent);
            var features = JsonSerializer.Deserialize<JsonElement>(featuresContent);

            // Combine all fields, with additional context fields at the top
            var allFields = additionalContextFields
                .Concat(specs.EnumerateArray())
                .Concat(features.EnumerateArray())
                .ToArray();
            
            return new ContentResult
            {
                Content = JsonSerializer.Serialize(allFields),
                ContentType = "application/json",
                StatusCode = 200
            };
        }
        private int ExtractId(string value)
        {
            var match = System.Text.RegularExpressions.Regex.Match(value, @"\d+");
            return match.Success ? int.Parse(match.Value) : 0;
        }
    }
}