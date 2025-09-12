using System.Text.Json;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.Tracing;
using DiffPlex.DiffBuilder;
using IronPython.Runtime.Operations;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Assistants.ArtifactAssists;

public class ArtifactAssist
{
    private readonly IServiceProvider _sp;

    public static JsonSerializerOptions ArtifactSerializerOptions = new() { WriteIndented = true };

    public const string Property_QuestionResponse = "question_response";
    public const string Property_Action = "next_action";
    public const string ResponseAction_NoAction = nameof(NoAction);
    public const string Property_ExecutionPlan = "execution_plan";

    public const string RoleForTools = "developer";

    public ArtifactAssist([NotNull] IServiceProvider sp)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
    }

    private static void NoAction()
    {
    }

    private async Task<string> ChangeArtifactAsync(ArtifactAssistRequest req, JsonElement key, string changed_artifact)
    {
        if (req.OnChangeArtifactValidateKey != null)
            req.OnChangeArtifactValidateKey(key);

        if (req.OnCleanArtifact != null)
            changed_artifact = req.OnCleanArtifact(changed_artifact);

        var curArtifact = req.StringArtifact;
        var changeAttemptMsg = $"Artifact Change Attempt:\r\n\r\n{changed_artifact}";
        if (req.Logger.IsEnabled(LogLevel.Debug))
        {
            var diff = InlineDiffBuilder.Diff(curArtifact, changed_artifact);
            req.Logger.LogDebug(changeAttemptMsg + "\r\n\r\nDiff:\r\n\r\n" + diff.ToDiffString() + "\r\n\r\nPartial Diff:\r\n\r\n" + diff.ToDiffString(true));
        }

        if (req.OnStringArtifactChangedAsync != null)
        {
            await req.OnStringArtifactChangedAsync(changed_artifact);

            if (req.AddArtifactChangeMessage)
            {
                var diff = InlineDiffBuilder.Diff(curArtifact, changed_artifact);
                return $"An artifact change has been made: {diff.ToDiffString(true)}";
            }

            return "void";
        }

        if (req.UpdateArtifactInRequest)
            req.StringArtifact = changed_artifact;

        return "void";
    }

    public async Task RequestAsync(ArtifactAssistRequest req)
    {
        req.Validate();
        req.Agent ??= _sp.GetRequiredNamedService<IChatAgent>(req.AgentName);
        req.Logger ??= _sp.GetLogger<ArtifactAssist>(req.LoggerName ?? "void");

        var schema = req.Agent.GetSchema(req.SchemaName);
        schema.Object = new();
        if (req.UseExecutionPlan)
            schema.Object.AddProperty(Property_ExecutionPlan, PrimitiveTypes.String);
        schema.Object.AddProperty(Property_QuestionResponse, PrimitiveTypes.String);

        var actions = new List<AgentAction>();
        if (req.AddNoAction)
            actions.Add(NoAction);
        
        if (req.AddChangeArtifactAction)
        {
            var action = new AgentAction(req.ChangeArtifactMethodName)
            {
                OnJsonActionAsync = async args =>
                {
                    var changed_artifact = args.RootElement.GetProperty(req.ChangeArtifactPropertyName).GetRawText();
                    JsonElement key = default;
                    if (req.ChangeArtifactKeyPropertyName != null)
                        key = args.RootElement.GetProperty(req.ChangeArtifactKeyPropertyName);
                    return await ChangeArtifactAsync(req, key, changed_artifact);
                }
            };
            action.UsageNotes = req.Prompt.Actions.ChangeArtifactNotes;

            if (req.SchemaObject != null)
                action.Object.Properties.Add(req.ChangeArtifactPropertyName, new(PrimitiveTypes.Object, obj: req.SchemaObject));
            else
                action.Object.AddProperty(req.ChangeArtifactPropertyName, PrimitiveTypes.String, nullable: true);
            
            if (req.ChangeArtifactKeyPropertyName != null)
                action.Object.AddProperty(req.ChangeArtifactKeyPropertyName, PrimitiveTypes.String);

            actions.Add(action);
        }
        actions.AddRange(req.Actions);
        var typeDefs = new TypeDefinition[actions.Count];
        for (var i = 0; i < actions.Count; i++)
            typeDefs[i] = new(PrimitiveTypes.Object, obj: actions[i].Object);

        {
            var hashSet = new HashSet<string>();
            foreach (var a in actions)
                if (!hashSet.Add(a.Name))
                    throw new InvalidOperationException($"Duplicate action name: {a.Name}");
        }

        if (typeDefs.Length > 0)
            schema.Object.AddProperty(Property_Action, PrimitiveTypes.Object, anyOf: typeDefs);

        req.AfterSchemaBuilt?.Invoke(schema);

        var chatReq = req.Agent.GetRequest();
        chatReq.History.AddRange(req.History);

        req.Prompt.CurrentState.Content = (req.CurrentArtifactDescriptionPrompt + "\r\n\r\n{StringArtifact}").Trim();
        req.Prompt.Context.ChangeArtifactMethodName = req.ChangeArtifactMethodName;
        req.Prompt.Context.StringArtifact = req.StringArtifact ?? "";
        req.Prompt.Actions.RemoveAllBoundActions();
        foreach (var act in actions)
            req.Prompt.Actions.TryAdd(act);
        
        chatReq.SystemPrompt = req.Prompt.GetPrompt();
        chatReq.ReasoningEffort = req.ReasoningEffort;
        
        chatReq.Question = req.Question;
        chatReq.SetResponseSchema(schema);

        var chatRes = await req.Agent.RequestAsync(chatReq);

        if (!string.IsNullOrWhiteSpace(chatReq.Question))
        {
            var userMessage = new OpenAIChatMessage("user", chatReq.Question);
            req.History.Add(userMessage);
            req.OnAddedToHistory?.Invoke(userMessage);
        }

        var answer = JsonDocument.Parse(chatRes.Answer);
        var action_type = typeDefs.Length == 0 ?
                ResponseAction_NoAction
            :
            answer.RootElement.GetProperty(Property_Action).GetProperty(AgentAction.TypeDiscriminatorProperty).GetString();
        var execution_plan = req.UseExecutionPlan ? answer.RootElement.GetProperty(Property_ExecutionPlan).GetString() : null;
        var question_answer = answer.RootElement.GetProperty(Property_QuestionResponse).GetString();

        if (!string.IsNullOrWhiteSpace(execution_plan))
            req.Logger.LogTrace($"Action: {action_type}\r\nExecution plan: {execution_plan}");

        {
            var replyMessage = new OpenAIChatMessage("assistant", question_answer);
            req.History.Add(replyMessage);
            req.OnAddedToHistory?.Invoke(replyMessage);
        }

        if (action_type != ResponseAction_NoAction)
        {
            if (req.ActionsAllowed <= 0)
            {
                var actionMessage = new OpenAIChatMessage(RoleForTools, "Too many actions.  Aborting.");
                req.History.Add(actionMessage);
                req.OnAddedToHistory?.Invoke(actionMessage);
                return;
            }
            req.ActionsAllowed--;
            var actionJson = JsonDocument.Parse(answer.RootElement.GetProperty(Property_Action).GetRawText());
            var action = actions.FirstOrDefault(a => a.Name == action_type) ?? throw new InvalidOperationException(string.Format("Action not found: {0}", action_type));

            try
            {
                await CXTrace.Current.SpanFor(action.Name, actionJson).ExecuteAsync(async span =>
                {
                    req.OnStartAction?.Invoke(action, actionJson);
                    var actionMessage = await action.InvokeAsync(actionJson);
                    span.Output = actionMessage;
                    req.History.Add(actionMessage);
                    req.OnAddedToHistory?.Invoke(actionMessage);
                    if (req.Logger.IsEnabled(LogLevel.Trace))
                        req.Logger.LogTrace(actionMessage.Content);
                });
            }
            catch (ArtifactException ex)
            {
                var sig = action.GetCallSignature(actionJson);
                var failedAttempt = ex.Message;

                if (!req.AllowDuplicateExceptions && req.FailedAttempts.Any(fa => fa == failedAttempt))
                {
                    req.OnDuplicateException?.Invoke(true);
                    return;
                }

                req.FailedAttempts.Add(failedAttempt);                
                var actionMessage = SetActionMessage(req.ArtifactExceptionsAllowed, sig, ex.Message);
                var exceptionRecurrance = req.History.Where(am => 
                    am.Role == actionMessage.Role 
                    && am.Content == actionMessage.Content.replace("\r\nToo many exceptions, aborting.", "")
                ).Count();

                actionMessage.Content += string.Format("\r\nRecurrance: {0}", exceptionRecurrance);
                if (req.DebugMode) actionMessage.Content += string.Format("\r\n{0}", ex.StackTrace);

                req.ArtifactExceptionsAllowed--;
                req.History.Add(actionMessage);
                req.OnAddedToHistory?.Invoke(actionMessage);

                if (req.ArtifactExceptionsAllowed <= 0) return;

                if (req.AllowChaining)
                {
                    req.Question = null;
                    await RequestAsync(req);
                }

                return;
            }

            if (req.AllowChaining && !req.OneShot)
            {
                req.Question = null;
                await RequestAsync(req);
            }

            return;
        }
    }

    private static OpenAIChatMessage SetActionMessage(int exceptionsAllowed, string sig, string exMessage)
    {
        return exceptionsAllowed > 0 
            ? new(RoleForTools, string.Format("> {0}\r\n< Exception: {1}", sig, exMessage))
            : new(RoleForTools, string.Format("> {0}\r\n< Exception: {1}\r\nToo many exceptions, aborting.", sig, exMessage));
    }
}