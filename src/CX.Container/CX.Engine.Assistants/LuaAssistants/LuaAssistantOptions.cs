using CX.Engine.Common;

namespace CX.Engine.Assistants.LuaAssistants;

public class LuaAssistantOptions : IValidatable
{
    public string LuaCore { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(LuaCore))
            throw new ArgumentException($"{nameof(LuaAssistantOptions)}.{nameof(LuaCore)} is required and cannot be empty.");
    }
}