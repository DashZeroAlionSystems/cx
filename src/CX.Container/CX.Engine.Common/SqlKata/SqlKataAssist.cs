using System.Text;
using System.Text.RegularExpressions;
using CX.Engine.Common.IronPython;
using CX.Engine.Common.JsonSchemas;
using Cx.Engine.Common.PromptBuilders;
using Microsoft.Scripting;
using SqlKata;
using SqlKata.Compilers;

namespace CX.Engine.Common.SqlKata;

/// <summary>
/// Provides methods to assist with building and executing SQL queries using SqlKata,
/// including dynamic filtering, ordering, and grouping based on provided parameters.
/// </summary>
public class SqlKataAssist
{
    private dynamic _context;
    private SchemaObject _schemaObject = new();
    private Dictionary<string, bool> _properties = new();
    private Dictionary<string, object> _defaults = new();
    private SqlKataFormats _formatters = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlKataAssist"/> class.
    /// Sets up the IronPython context with the sql_kata_filter function.
    /// </summary>
    /// <param name="ironContext">The IronPython request context.</param>
    /// <param name="context">The dynamic context object.</param>
    public SqlKataAssist(dynamic context) =>
        _context = context;

    private object TryGetPropValue(object obj, Type type, object defaultValue)
    {
        try
        {
            obj = Convert.ChangeType(obj, type);
        }
        catch
        {
            obj = defaultValue;
        }
        return obj;
    }

    /// <summary>
    /// Executes the SQL query by applying SELECT, WHERE, ORDER BY, and GROUP BY clauses based on the parameters.
    /// If runQuery is false, returns the compiled SQL string instead of executing the query.
    /// </summary>
    /// <param name="query">The SqlKata query object.</param>
    /// <param name="parameters">A dictionary of parameters for filtering, ordering, and grouping.</param>
    /// <param name="runQuery">Determines whether to execute the query (true) or return the SQL string (false).</param>
    /// <returns>An asynchronous task that returns the query result or the compiled SQL string.</returns>
    /// <exception cref="ArgumentException">Thrown when a required parameter is missing or invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown when a necessary context value is missing.</exception>
    /// <exception cref="ArgumentTypeException">Thrown when the clause type is not supported.</exception>
    public async Task<SqlKataAssistResult> ExecuteScript(Query query, Dictionary<string, object> parameters, bool runQuery = true)
    {
        var result = new SqlKataAssistResult();
        result.Formats = _formatters;
        
        var validOrderBys = new HashSet<string>();
        
        if (!parameters.TryGetValue("Select", out var selectsRaw))
            throw new ArgumentException("Select is not a property of parameters");

        if (selectsRaw is not List<object> selects || !selects.Any() || selects.Count == 0)
            throw new ArgumentException("Select is not a property of parameters");

        foreach (var select in selects)
        {
            if (select is not string)
                throw new ArgumentException("Select.Items is not of type string");
            var selectStr = select as string;
            result.Formats.SelectHandleRename(selectStr);
            if (selectStr.Contains(" AS ", StringComparison.CurrentCultureIgnoreCase))
            {
                validOrderBys.Add(selectStr.SelectToAlias());
                validOrderBys.Add(selectStr.SelectToAggregate());
            }
            else
                validOrderBys.Add(selectStr);

            query.SelectRaw(selectStr);
        }

        result.Formats.Update();
        
        if (!parameters.TryGetValue("Where", out var whereRaw))
            throw new ArgumentException("Where is not a property of parameters");
        if (whereRaw is Dictionary<string, object> whereList)
        {
            foreach (var where in whereList)
            {
                if (where.Value is not Dictionary<string, object>)
                    throw new ArgumentException("Where.Value is not of type Dictionary<string, object>");

                var clause = where.Value as Dictionary<string, object>;
                var whereResult = new List<string>();

                if (clause.ContainsKey("Min") && clause.ContainsKey("Max"))
                {
                    var genericType = _context.WHERE_CONTEXT[where.Key] as Type ?? throw new ArgumentNullException($"{where.Key} not defined in WHERE_CONTEXT");
                    var targetType = genericType.GetGenericArguments()[0];
                    var _default = TryGetPropValue(_defaults[where.Key], targetType, null);
                    var min = TryGetPropValue(clause["Min"], targetType, _default);
                    var max = TryGetPropValue(clause["Max"], targetType, _default);
                    if (_default is not null)
                    {
                        
                        if (targetType == typeof(DateTime))
                        {
                            if (!min.Equals(_default))
                                whereResult.Add($">= '{min}'");

                            if (!max.Equals(_default))
                                whereResult.Add($"<= '{max}'");
                        }
                        else
                        {
                            if (!min.Equals(_default))
                                whereResult.Add($">= {min}");

                            if (!max.Equals(_default))
                                whereResult.Add($"<= {max}");
                        }
                    }
                    else
                    {
                        whereResult.Add($">= {min}");
                        whereResult.Add($"<= {max}");
                    }
                }
                else if (clause.ContainsKey("Value_s") && clause.Values.First() is List<object>)
                {
                    var genericType = _context.WHERE_CONTEXT[where.Key] as Type ?? throw new ArgumentNullException($"{where.Key} not defined in WHERE_CONTEXT");
                    var targetType = genericType.GetGenericArguments()[0];
                    
                    if(targetType != typeof(string[]))
                        throw new ArgumentTypeException("Target type is not supported for multi choice");
                    
                    var value = clause["Value_s"] as List<object>;
                    var _default = _defaults[where.Key] as string;

                    if (_default is not null && value.Any())
                        if(!_default.Equals(value[0] as string))
                            whereResult.Add($" IN ({string.Join(',', value.Select(x => $"'{x as string}'"))})");
                    
                }
                else if (clause.Keys.Contains("Value_s"))
                {
                    var genericType = _context.WHERE_CONTEXT[where.Key] as Type ?? throw new ArgumentNullException($"{where.Key} not defined in WHERE_CONTEXT");
                    var targetType = genericType.GetGenericArguments()[0];

                    var value = Convert.ChangeType(clause["Value_s"], targetType);
                    var _default = Convert.ChangeType(_defaults[where.Key], targetType);
                    if (_default is not null)
                    {
                        if (!_default.Equals(value))
                            whereResult.Add($"= '{value}'");
                                
                    }
                    else
                    {
                        whereResult.Add($"= '{value}'");
                    }
                }
                else
                    throw new ArgumentTypeException("Type of clause is not supported");

                foreach (var statement in whereResult)
                {
                    if(!result.Selections.ContainsKey("Where"))
                        result.Selections.Add("Where", []);
                    result.Selections["Where"].Add($"{where.Key}: {Regex.Replace(statement, @"[=><\s']+", "")}");
                    query.WhereRaw($"{where.Key} {statement}");
                }
            }
        }

        parameters.TryGetValue("OrderBy", out var orderByRaw);

        if (!parameters.TryGetValue("GroupBy", out var groupByRaw))
            throw new ArgumentException("GroupBy is not a property of parameters");

        if (groupByRaw is List<object> groupBys)
        {
            result.Selections.Add("GroupBy", groupBys.Select(x => x as string).ToList());
            foreach (string groupBy in groupBys)
            {
                query.GroupByRaw(groupBy);
                validOrderBys.Add(groupBy);
            }
        }

        if (orderByRaw is Dictionary<string, object> orderBys)
        {
            foreach (var orderBy in orderBys)
            {
                if (orderBy.Value is Dictionary<string, object> order &&
                    order.FirstOrDefault().Value?.ToString() != "NONE")
                {
                    if (validOrderBys.Count == 0 || validOrderBys.Contains(orderBy.Key))
                    {
                        if (!result.Selections.ContainsKey("OrderBy"))
                            result.Selections.Add("OrderBy", []);
                        query.OrderByRaw($"{orderBy.Key} {order.FirstOrDefault().Value as string}");
                        result.Selections["OrderBy"].Add($"{orderBy.Key}: {order.FirstOrDefault().Value as string}");
                    }
                    else
                        throw new ArgumentException($"OrderBy {orderBy.Key} is not a property of aggregate or valid selections");
                }
            }
        }

        if (parameters.TryGetValue("Limit", out var limitRow))
            if(limitRow is double limit)
            {
                var intLim = Convert.ToInt32(limit);
                result.Selections.Add("Limit", [intLim.ToString()]);
                query.Limit(intLim);
            }
        var raw = CompileQuery(query);
        result.Sql = raw;
        if(runQuery)
            result.Results = await query.GetAsync();
        
        return result;
    }

    private string CompileQuery(Query query)
    {
        var compiler = new SqlServerCompiler();
        var sql = compiler.Compile(query);
        return sql.RawSql;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="propertyName">The property Name: Where and OrderBy</param>
    /// <returns></returns>
    public string CompileProperty(string propertyName, int indentSize = 3, int order = 1)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        var indent = new string(' ', indentSize * order);
        var property = _schemaObject.Properties[propertyName];
        foreach(var key in property.Object.Properties.Keys)
            sb.AppendLine($"{indent}{key},");
        
        return sb.ToString();
    }

    /// <summary>
    /// Tries to retrieve a property from the given schema object using a path notation.
    /// </summary>
    /// <param name="path">The path to the property (e.g., "Where->PropertyName->Value").</param>
    /// <param name="act">The schema object to search in.</param>
    /// <param name="val">The found type definition if successful.</param>
    /// <returns>True if the property is found; otherwise, false.</returns>
    private bool TryGetProperty(string path, SchemaObject act, out TypeDefinition val)
    {
        val = null;
        try
        {
            var parts = path.Split("->");
            if (parts.Length == 0) return false;

            val = act.Properties[parts[0]];
            foreach (var part in parts.Skip(1))
                val = val.Object.Properties[part];

            return true;
        }
        catch
        {
            return val != null;
        }
    }

    /// <summary>
    /// Adds a property definition to the schema for the specified SQL functions.
    /// Updates the internal schema object and context for WHERE and ORDER BY clauses.
    /// </summary>
    /// <param name="functions">The list of SQL functions (e.g., Where, OrderBy, GroupBy) that this property supports.</param>
    /// <param name="name">The name of the property.</param>
    /// <param name="type">The data type of the property (e.g., "integer", "number", "string", "boolean").</param>
    /// <param name="isDateTime">Indicates whether the property represents a DateTime value.</param>
    /// <param name="choices">A list of valid choices for the property.</param>
    /// <param name="defaultValue">The default value for the property.</param>
    /// <exception cref="NotImplementedException">Thrown if the provided type is not supported.</exception>
    public void AddProperty(SqlKataRequest req)
    {
        var name = req.Name ?? throw new ArgumentNullException($"Parameter {nameof(req.Name)} cannot be null");
        var type = req.Type ?? throw new ArgumentNullException($"Parameter {nameof(req.Type)} cannot be null");
        var functions = req.Functions;
        var defaultValue = req.DefaultValue;
        var isDateTime = req.IsDate;
        var choices = req.Choices;
        var description = req.Description;
        var format = req.Format;
        var allowMultiple = req.AllowMultiple;
        
        Type contextType;
        if (req.Functions.Contains(SqlKataFunctionType.Where))
        {
            var whereObj = new SchemaObject();
            if (TryGetProperty("Where", _schemaObject, out var whereTypeDef))
                whereObj = whereTypeDef.Object;

            whereObj.AddProperty(name, type);
            var newProperty = new SchemaObject();
            switch (type)
            {
                case "integer":
                    contextType = typeof(MinMaxObj<int>);
                    defaultValue = -1;
                    newProperty.AddPropertiesFrom(contextType);
                    break;
                case "number":
                    contextType = typeof(MinMaxObj<double>);
                    defaultValue = -1d;
                    newProperty.AddPropertiesFrom(contextType);
                    break;
                case "string":
                    if (allowMultiple && !isDateTime)
                    {
                        contextType = typeof(ValueObj<string[]>);
                        newProperty.AddPropertiesFrom(contextType);
                        break;
                    }
                    contextType = isDateTime ? typeof(MinMaxObj<DateTime>) : typeof(ValueObj<string>);
                    newProperty.AddPropertiesFrom(contextType);
                    break;
                case "boolean":
                    contextType = typeof(ValueObj<bool>);
                    newProperty.AddPropertiesFrom(contextType);
                    break;
                default:
                    throw new NotImplementedException($"The type {type} is not implemented.");
            }

            whereObj.Properties[name].Object = newProperty;
            whereObj.Properties[name].Description = description;
            if (whereTypeDef == null)
                _schemaObject.AddProperty("Where", "object", obj: whereObj);
            else
                whereTypeDef.Object = whereObj;

            if (_context == null)
                throw new ArgumentNullException(nameof(_context), "The context is null");

            if (!((IDictionary<string, object>)_context).ContainsKey("WHERE_CONTEXT") || _context.WHERE_CONTEXT == null)
                _context.WHERE_CONTEXT = new Dictionary<string, Type>();

            if (_context.WHERE_CONTEXT is Dictionary<string, Type> whereContext)
                whereContext.Add(name, contextType);
        }

        var orderObj = new SchemaObject();
        if (TryGetProperty("OrderBy", _schemaObject, out var orderTypeDef))
            orderObj = orderTypeDef.Object;
        var newOrderProperty = new SchemaObject();
        newOrderProperty.AddPropertiesFrom<ValueObj<string>>();
        newOrderProperty.Properties.FirstOrDefault().Value.Choices = new List<string> { "ASC", "DESC", "NONE" };
        orderObj.AdditionalProperties ??= newOrderProperty;

        if (orderTypeDef == null)
            _schemaObject.AddProperty(
                "OrderBy",
                "object",
                obj: orderObj
            );

        if (functions.Contains(SqlKataFunctionType.OrderBy))
        {
            orderObj.AddProperty(name, type);
            orderObj.Properties[name].Object = newOrderProperty;
            orderObj.Properties[name].Description = description;
            if(orderTypeDef != null)
                orderTypeDef.Object = orderObj;
        }
        
        if (choices != null)
            AddChoices(name, choices, _schemaObject, defaultValue.ToString());
        _defaults.Add(name, defaultValue);
        _properties.Add(name, functions.Contains(SqlKataFunctionType.GroupBy));
        if(format != null)
            _formatters.Add(name, format);
    }

    /// <summary>
    /// Adds valid choices to the specified property within the schema.
    /// </summary>
    /// <param name="name">The name of the property to which choices are added.</param>
    /// <param name="choices">The list of choices to add.</param>
    /// <param name="act">The schema object where the property exists.</param>
    /// <param name="defaultValue">The default value to include in the choices.</param>
    /// <exception cref="ArgumentNullException">Thrown if the property is not found in the schema.</exception>
    private void AddChoices(string name, List<string> choices, SchemaObject act, string defaultValue)
    {
        if (!TryGetProperty($"Where->{name}->Value", act, out var prop))
            if (!TryGetProperty($"{name}", act, out prop))
                throw new ArgumentNullException($"{name} is not a property of {nameof(act)}.");
        choices.Add(defaultValue);
        prop.Choices.AddRange(choices);
    }

    private void AddRemainder(bool allowLimit = false)
    {
        if(!_schemaObject.Properties.ContainsKey("Select"))
            _schemaObject.AddProperty(
                "Select",
                PrimitiveTypes.Array,
                "An array of strings representing the columns or expressions to be selected in the SQL query. If no selection is provided, you should select all the columns at no point are you allowed to do *. When processing a grouping query, ensure that the SELECT list is constructed to include both the grouping column(s) and any aggregate expressions. The output should be formatted so that the grouping field appears first, followed by the aggregate columns with clear, descriptive aliases. This structure will ensure that the results are displayed in a clean, tabular format with intuitive headers.",
                itemType: PrimitiveTypes.String
            );

        if(!_schemaObject.Properties.ContainsKey("GroupBy"))
            _schemaObject.AddProperty(
                "GroupBy",
                PrimitiveTypes.Array,
                "An array of property names used to group the query results. Only include properties for which grouping is desired.",
                _properties.Where(x => x.Value).Select(x => x.Key).ToList(),
                itemType: PrimitiveTypes.String
            );
        if(allowLimit && !_schemaObject.Properties.ContainsKey("Limit"))
            _schemaObject.AddProperty("Limit", PrimitiveTypes.Integer, "Set the limit for the amount of rows that you want to cap at. If no Limit is required please set the value to null",
                nullable: true);
        
        if(!_schemaObject.Properties.ContainsKey("Reasoning"))
            _schemaObject.AddProperty("Reasoning", "string", "Give a reasoning message");
    }

    /// <summary>
    /// Retrieves the complete schema object representing the properties and structure
    /// for the SQL query parameters including SELECT and GROUP BY configurations.
    /// </summary>
    /// <returns>The schema object detailing the SQL query structure.</returns>
    public SchemaObject GetSchemaObject(bool allowLimit = false)
    {
        AddRemainder(allowLimit);
        return _schemaObject;
    }

    private TreePromptBuilder GetPromptBuilderInternal(SchemaObject schemaObject, TreePromptBuilder builder, int order = -1, string descendentDescription = null, Dictionary<string, object> descendentChoices = null)
    {
        // Base condition: if the schema object is null, there's nothing to add.
        if (schemaObject == null)
            return builder;

        order++;
        // Loop through each property in the schema.
        foreach (var kvp in schemaObject.Properties)
        {
            // Add a prompt section for this property.
            // Here we add the property's key and its description (if available).
            // You can adjust the format as needed.
            var sbs = new StructureDescriptionSection() { Order = order, Header = null };
            var description = kvp.Value.Description;
            if (!string.IsNullOrEmpty(description))
                description += descendentDescription?.Trim();
            else
                description = "No description provided";
            var lstChoices = kvp.Value.Choices;
            var choices = descendentChoices;
            if (lstChoices is { Count: > 0 } && kvp.Value.Object is not null)
            {
                descendentChoices = new() { { "Choices", lstChoices } };
                descendentDescription += ", all valid choices: {Choices:list:|, |, and }";
            }
            else if (lstChoices is { Count: > 0 })
            {
                choices = new(){ { "Choices", lstChoices } };
                description += ", all valid choices: {Choices:list:|, |, and }";
            }
            var context = ((IDictionary<string, object>)_context).ToDictionary(x => x.Key, x => x.Value);
            sbs.Fields.Add(kvp.Key, new(description, choices ?? context));
            builder.Add(sbs);
            if (kvp.Value.Object is { AdditionalProperties: not null })
            {
                var sb = new StringBuilder();
                var header = new PromptContentSection() { Order = order + 1 };
                header.Content = "- Any Additional properties required, should be in the following format";
                builder.Add(header);
                
                var desc = new PromptContentSection() { Order = order + 2 };
                sb.AppendLine("- PropertyName:");
                sb.AppendLine(GetPromptBuilderInternal(kvp.Value.Object.AdditionalProperties, new(), order + 2)
                    .GetPrompt());
                desc.Content = sb.ToString();
                builder.Add(desc);
            }

            // Recursively process nested SchemaObjects if they exist.
            if (kvp.Value.Object != null)
                GetPromptBuilderInternal(kvp.Value.Object, builder, order, descendentDescription, descendentChoices);
            kvp.Value.Description = null;
            
            descendentDescription = null;
            descendentChoices = null;
        }
        
        return builder;
    }

    public TreePromptBuilder GetPromptBuilder()
    {
        GetSchemaObject();
        return GetPromptBuilderInternal(_schemaObject, new TreePromptBuilder());
    }
}
