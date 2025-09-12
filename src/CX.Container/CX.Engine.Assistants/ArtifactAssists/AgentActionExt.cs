using System.Linq.Expressions;
using System.Reflection;

namespace CX.Engine.Assistants.ArtifactAssists;

public static class AgentActionExt
{
    public static void Add(this List<AgentAction> lst, Delegate action, bool directExceptions = false)
    {
        lst.Add(new(action) { DirectExceptions = directExceptions });
    }

    public static void Add(this List<AgentAction> lst, params Delegate[] actions) => Add(lst, false, actions);
    
    public static void Add(this List<AgentAction> lst, bool directExceptions, params Delegate[] actions)
    {
        foreach (var action in actions)
            lst.Add(new(action) { DirectExceptions = directExceptions });
    }

    public static Delegate CreateDelegate(this MethodInfo methodInfo, object target) {
        Func<Type[], Type> getType;
        var isAction = methodInfo.ReturnType == typeof(void);
        var types = methodInfo.GetParameters().Select(p => p.ParameterType);

        if (isAction) {
            getType = Expression.GetActionType;
        }
        else {
            getType = Expression.GetFuncType;
            types = types.Concat([methodInfo.ReturnType]);
        }

        if (methodInfo.IsStatic) {
            return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
        }

        return Delegate.CreateDelegate(getType(types.ToArray()), target, methodInfo.Name);
    }

    public static void AddFromObject<T>(this List<AgentAction> lst, T instance, bool directExceptions = false)
    {
        var type = typeof(T);
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var method in methods)
        {
            // Only consider methods decorated with the SemanticActionAttribute.
            if (method.GetCustomAttribute<SemanticActionAttribute>() == null)
                continue;

            lst.Add(method.CreateDelegate(instance), directExceptions);
        }
    }
}