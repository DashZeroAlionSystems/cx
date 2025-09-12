using Microsoft.Extensions.Configuration;

namespace CX.Engine.Common;

public interface IValidatableConfiguration
{
    void Validate(IConfigurationSection section);
}