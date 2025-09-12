using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;

namespace CX.Engine.Assistants.ArtifactAssists;

public partial class AgentAction
{
    public string Name;
    public readonly SchemaObject Object = new();
    public Delegate Method;
    public string UsageNotes;
    public Func<JsonDocument, Task<string>> OnJsonActionAsync;
    public Func<object, string> FormatResult = DefaultFormatResult;
    public bool DirectExceptions;

    public static string DefaultFormatResult(object value)
    {
        if (value is string s)
            return s;

        if (value is List<string> list)
            return list.ToCappedListString(50, characterSoftCap: 1_000);

        return JsonSerializer.Serialize(value);
    }

    public const string TypeDiscriminatorProperty = "action";

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NameRegex();

    public const int MaxNameLength = 50;

    // Existing constructor
    public AgentAction(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));

        Object.AddProperty(TypeDiscriminatorProperty, PrimitiveTypes.String, choices: [name]);

        if (!NameRegex().IsMatch(name))
            throw new ArgumentException(
                "Name must be alphanumeric and underscores starting with an alphabet character",
                nameof(name));

        if (Name.Length > MaxNameLength)
            throw new ArgumentException($"Name must be less than {MaxNameLength} characters (provided name: {name})", nameof(name));
    }

    public AgentAction(Delegate method)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        if (method.Method == null)
            throw new ArgumentException("Method must not be a lambda expression", nameof(method));

        //get the name of the called method
        var name = method.Method.Name;

        Setup(name, method);
    }

    private void Setup(string name, Delegate method)
    {
        Method = method;

        name = MiscHelpers.CleanMethodName(name);

        // Validate basic name rules
        if (!NameRegex().IsMatch(name))
            throw new ArgumentException(
                $"Name must be alphanumeric and underscores starting with an alphabet character (provided name: {name})",
                nameof(name));

        if (name.Length > MaxNameLength)
            throw new ArgumentException($"Name must be less than {MaxNameLength} characters (provided name: {name})", nameof(name));

        Name = name;

        // Add "$type" property with fixed choice = name
        Object.AddProperty(TypeDiscriminatorProperty, PrimitiveTypes.String, choices: [name]);

        // Examine the method info for parameters and return type
        var methodInfo = method.Method;
        var parameters = methodInfo.GetParameters();
        var returnType = methodInfo.ReturnType;

        // For each parameter, add a corresponding property in the schema (only supported primitives or class objects).
        foreach (var param in parameters)
        {
            if (param.GetCustomAttribute<SemanticIgnoreAttribute>() != null)
                continue;

            var paramType = param.ParameterType;
            string schemaType;

            if (paramType == typeof(string))
            {
                schemaType = PrimitiveTypes.String;
                Object.AddProperty(param.Name!, schemaType);
            }
            else if (paramType == typeof(bool))
            {
                schemaType = PrimitiveTypes.Boolean;
                Object.AddProperty(param.Name!, schemaType);
            }
            else if (paramType == typeof(int)
                     || paramType == typeof(long)
                     || paramType == typeof(short)
                     || paramType == typeof(byte))
            {
                schemaType = PrimitiveTypes.Integer;
                Object.AddProperty(param.Name!, schemaType);
            }
            else if (paramType == typeof(float)
                     || paramType == typeof(double)
                     || paramType == typeof(decimal))
            {
                schemaType = PrimitiveTypes.Number;
                Object.AddProperty(param.Name!, schemaType);
            }
            else if (paramType.IsClass)
            {
                // For class types, add an "object" schema with nested properties
                schemaType = PrimitiveTypes.Object;
                var nestedObject = new SchemaObject().AddPropertiesFrom(paramType);
                Object.AddProperty(param.Name!, schemaType, obj: nestedObject);
            }
            else
            {
                throw new ArgumentException(
                    $"Unsupported parameter type: {paramType.Name}. " +
                    "Only string, bool, int, long, short, byte, float, double, decimal, or class objects are supported."
                );
            }
        }

        // Set up the action to be performed when the JSON is received.
        OnJsonActionAsync = async (jsonDoc) =>
        {
            var root = jsonDoc.RootElement;
            var paramValues = new object[parameters.Length];

            // Gather all parameter values from the JSON
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].GetCustomAttribute<SemanticIgnoreAttribute>() != null)
                    continue;

                var param = parameters[i];
                var propertyValue = root.GetProperty(param.Name!);
                var value = JsonSerializer.Deserialize(propertyValue.GetRawText(), param.ParameterType);
                paramValues[i] = value!;
            }

            try
            {
                // Invoke the delegate with dynamic invocation
                var result = method.DynamicInvoke(paramValues);
                if (result == null)
                    //if void return void
                    if (returnType == typeof(void))
                        return "void";
                    else
                        return "null";

                result = await MiscHelpers.AwaitAnyAsync(result);

                if (result.IsVoid())
                    result = "void";

                // Synchronous result (not a Task)
                return FormatResult(result);
            }
            catch (Exception ex)
            {
                var ue = TryUnpackException(ex);
                
                if (ue != null)
                    throw ue;
                else
                    throw;
            }
        };
    }

    public static Exception TryUnpackException(Exception ex)
    {
        while (true)
        {
            if (ex is TargetInvocationException tie)
            {
                if (tie.InnerException != null)
                    ex = tie.InnerException;
                else
                    break;
            } else if (ex is AggregateException ae && ae.InnerExceptions.Count == 1)
            {
                ex = ae.InnerExceptions[0];
                continue;
            }
            else
                break;
        }

        return ex;
    }

    public string GetCallSignature(JsonDocument args)
    {
        try
        {
            var type = args.RootElement.GetProperty(TypeDiscriminatorProperty).GetString();
            if (type != Name)
                throw new ArgumentException($"Expected type {Name}, got {type}", nameof(args));

            // Build a human-friendly string representation of the call
            var sb = new StringBuilder();
            sb.Append(Name);
            sb.Append('(');

            var first = true;
            foreach (var arg in args.RootElement.EnumerateObject())
            {
                if (arg.Name == TypeDiscriminatorProperty)
                    continue;

                if (!first)
                    sb.Append(", ");

                sb.Append(arg.Name);
                sb.Append(": ");
                sb.Append(JsonSerializer.Serialize(arg.Value));
                first = false;
            }

            sb.Append(')');
            return sb.ToString();
        }
        catch
        {
            return JsonSerializer.Serialize(args);
        }
    }

    public string GetCallSignature()
    {
        // Build a human-friendly string representation of the call
        var sb = new StringBuilder();
        sb.Append(Name);
        sb.Append('(');

        var first = true;
        foreach (var arg in Object.Properties)
        {
            if (arg.Key == TypeDiscriminatorProperty)
                continue;

            if (!first)
                sb.Append(", ");

            sb.Append(arg.Key);
            first = false;
        }

        sb.Append(')');
        return sb.ToString();
    }

    public async Task<OpenAIChatMessage> InvokeAsync(JsonDocument args)
    {
        var type = args.RootElement.GetProperty(TypeDiscriminatorProperty).GetString();
        if (type != Name)
            throw new ArgumentException($"Expected type {Name}, got {type}", nameof(args));

        var sig = GetCallSignature(args);

        if (OnJsonActionAsync == null)
            return new("system", $"> {sig}\r\n< void");

        try
        {
            var reply = await OnJsonActionAsync(args);
            return new(ArtifactAssist.RoleForTools, $"> {sig}\r\n< {reply}");
        }
        catch (Exception ex)
        {
            if (DirectExceptions)
                throw;
            else
                throw new ArtifactException(ex.Message, ex);
        }
    }

    public AgentAction(string name, Delegate method)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        Setup(name, method);
    }
}