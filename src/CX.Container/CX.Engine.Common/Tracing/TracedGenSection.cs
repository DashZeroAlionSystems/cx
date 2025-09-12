namespace CX.Engine.Common.Tracing;

public class TracedGenSection
{
    public readonly string GenId;
    public int? RawTotalTokens;
    public int? RawPromptTokens;
    public int? PromptTokens;
    public int? CompletionTokens;
    public int? TotalTokens;
    public int? CachedTokens;
    public int? ReasoningTokens;
    public int? AudioTokens;
    public int? AcceptedPredictionTokens;
    public int? RejectedPredictionTokens;
    public int? XRateLimitLimitRequests;
    public int? XRateLimitLimitTokens;
    public int? XRateLimitRemainingRequests;
    public int? XRateLimitRemainingTokens;
    public string XRateLimitResetRequests;
    public string XRateLimitResetTokens;
    public CXTrace.ObservationLevel Level = CXTrace.ObservationLevel.DEFAULT;

    public object Output;
    public readonly Action<TracedGenSection> OnDone;

    public TracedGenSection(string genId, Action<TracedGenSection> onDone = null)
    {
        GenId = genId;
        OnDone = onDone;
    }
    
    public void Done()
    {
        Output = TracedSpanSection.CleanOutput(Output);
        
        OnDone?.Invoke(this);
    }

    public async Task ExecuteAsync(Func<TracedGenSection, Task> task)
    {
        CXTrace.ObservationId.Value = GenId;
        try
        {
            await task(this);
        }
        catch (Exception ex)
        {
            Output = ex;
            Level = CXTrace.ObservationLevel.ERROR;
            CXTrace.Current.Event(ex);
            throw;
        }
        finally
        {
            Done();
        }
    }
    
    public async Task<T> ExecuteAsync<T>(Func<TracedGenSection, Task<T>> task)
    {
        CXTrace.ObservationId.Value = GenId;
        try
        {
            return await task(this);
        }
        catch (Exception ex)
        {
            Output = ex;
            Level = CXTrace.ObservationLevel.ERROR;
            CXTrace.Current.Event(ex);
            throw;
        }
        finally
        {
            Done();
        }
    }

}