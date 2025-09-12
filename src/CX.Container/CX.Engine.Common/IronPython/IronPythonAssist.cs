using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using CX.Engine.Common.SqlKata;
using JetBrains.Annotations;
using SqlKata;
using SqlKata.Execution;

namespace CX.Engine.Common.IronPython;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class IronPythonAssist
{
    public static async Task<object> ChainResult(object a, Func<object, object> b)
    {
        a = await MiscHelpers.AwaitAnyAsync(a, true);

        return await MiscHelpers.AwaitAnyAsync(b(a), true);
    }
    
    public static async Task<IEnumerable<dynamic>> GetAsync(this Query query) => await query.GetAsync<object>();
    public static async Task<IEnumerable<string>> GetStringAsync(this Query query) => await query.GetAsync<string>();

}