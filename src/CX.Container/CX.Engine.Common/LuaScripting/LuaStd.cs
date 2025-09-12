using MoonSharp.Interpreter;

namespace CX.Engine.Common.LuaScripting;

public class LuaStd : ILuaCoreLibrary
{
    public void Setup(LuaInstance instance)
    {
        var script = instance.Script;
        
        script.Globals["DateTime"] = UserData.CreateStatic(typeof(DateTime));
        script.Globals["TimeSpan"] = UserData.CreateStatic(typeof(TimeSpan));
        script.Globals["MiscHelpers"] = UserData.CreateStatic(typeof(MiscHelpers));
        script.Globals["LuaLogger"] = UserData.CreateStatic(typeof(LuaLogger));
    }
}