using System.Text.Json;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.Tracing;
using ChatMessage = CX.Engine.ChatAgents.ChatMessage;

namespace CX.Engine.Assistants;

public class AgentRequest : IDisposable
{
    public string UserId;
    public string SessionId;
    public List<ChatMessage> History = new();
    public SemaphoreSlim FeedbackLock;
    public bool UseCache = true;
    public bool UpdateHistory = true;
    public bool IsKeepAlive => UserId == "keep-alive";
    public bool EligibleForContextAi => UserId != null && SessionId != null && !IsKeepAlive;
    public Components<AgentOverride> Overrides { get; set; }  = new();

    public AgentRequest(bool assignSessionId = true)
    {
        if (assignSessionId)
            SessionId = CXTrace.GetNewSessionId();
        UserId = "no-user";
    }

    public void ForgetHistory()
    {
        SessionId = CXTrace.GetNewSessionId();
        History = [];
    }
   
    public AgentRequest(List<ChatMessage> history, bool useCache = true, bool updateHistory = true)
    {
        History = history ?? [];
        UseCache = useCache;
        UpdateHistory = updateHistory;
    }

    public AgentRequest GetScoped()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var res = new AgentRequest(History, UseCache, UpdateHistory);
        res.UserId = UserId;
        res.SessionId = SessionId;
        return res;
    }

    public void Dispose()
    {
    }

    public AgentRequest HasNoCaching()
    {
        UseCache = false;
        return this;
    }

    public AgentRequest DoesNotUpdateHistory()
    {
        UpdateHistory = false;
        return this;
    }

    public AgentRequest RemoveLastQuestionAndAnswerFromHistory()
    {
        if (History.Count >= 2)
        {
            var newHistory = new List<ChatMessage>(History.Count - 2);
            newHistory.AddRange(History.GetRange(0, History.Count - 2));
            History = newHistory;
        }

        return this;
    }

    public AgentRequest HasNoHistory()
    {
        ForgetHistory();
        return DoesNotUpdateHistory();
    }

    public AssistantAnswer Record(string question, AssistantAnswer answer, List<ToolCall> toolCalls = null)
    {
        if (!UpdateHistory)
            return answer;

        lock (History)
        {
            History.Add(new OpenAIChatMessage("user", question));
            History.Add(new OpenAIChatMessage("assistant", answer?.Answer ?? "", toolCalls));
        }
        
        return answer!;
    }

    public static AgentRequest NoHistoryTest(string userId = "unit-test") => new AgentRequest() { UserId = userId, SessionId = CXTrace.GetNewSessionId() } .HasNoHistory();

    public T GetConversationState<T>()
    {
        if (History.Count == 0)
            return default;

        var history = History[0];
        return JsonSerializer.Deserialize<T>(history.Content);
    }

    public void SetConversationState<T>(T state)
    {
        if (History.Count == 0)
            History.Add(new OpenAIChatMessage("system", ""));
        History[0].Content = JsonSerializer.Serialize(state);
    }

    public void InitConversationState<T>(T state = default)
    {
        if (History.Count == 0)
            History.Add(new OpenAIChatMessage("system", JsonSerializer.Serialize(state)));
    }

    public virtual void Assign(AgentRequest source)
    {
        History = source.History;
        UseCache = source.UseCache;
        UpdateHistory = source.UpdateHistory;
        UserId = source.UserId;
        SessionId = source.SessionId;
        History.Clear();
        History.AddRange(source.History);
        Overrides.Clear();
        Overrides.AddRange(source.Overrides);
    }
}