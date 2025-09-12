using JetBrains.Annotations;

namespace CX.Engine.SharedOptions
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class StructuredDataOptions
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
    }
}
