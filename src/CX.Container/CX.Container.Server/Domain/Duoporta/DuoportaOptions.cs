// DuoportaOptions.cs
using JetBrains.Annotations;

namespace CX.Container.Domain.Duoporta
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class DuoportaOptions
    {
        public string ClientId { get; set; }
        public string ApiKey { get; set; } 
        public string BaseUrl { get; set; }
    }
}