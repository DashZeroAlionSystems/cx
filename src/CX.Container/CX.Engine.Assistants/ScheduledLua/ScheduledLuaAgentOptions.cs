using System.Text.Json.Serialization;
using CX.Engine.Assistants.ScheduledQuestions;
using CX.Engine.Common;

namespace CX.Engine.Assistants.ScheduledLua;

public class ScheduledLuaAgentOptions : IValidatable
{
    public string LuaCore { get; set; } = string.Empty;
    public string SetupLua { get; set; } = string.Empty;
    public string RunLua { get; set; } = string.Empty;
    public TimeSpan IntervalPerRun { get; set; }
    public int UTCStartHour { get; set; } = int.MinValue;
    public int UTCEndHour { get; set; } = int.MaxValue;
    [JsonConverter(typeof(JsonStringEnumConverter<RunningDays>))]
    public RunningDays RunDays { get; set; } = RunningDays.None;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(LuaCore))
        {
            throw new ArgumentException(
                $"{nameof(ScheduledLuaAgentOptions)}.{nameof(LuaCore)} is required and cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(RunLua))
        {
            throw new ArgumentException(
                $"{nameof(ScheduledLuaAgentOptions)}.{nameof(RunLua)} is required and cannot be empty.");
        }

        if (IntervalPerRun <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                $"{nameof(ScheduledLuaAgentOptions)}.{nameof(IntervalPerRun)} must be greater than zero.");
        }

        if (IntervalPerRun > TimeSpan.FromDays(1))
        {
            throw new ArgumentException(
                $"{nameof(ScheduledLuaAgentOptions)}.{nameof(IntervalPerRun)} must be less than or equal to 24 hours.");
        }

        if (UTCStartHour < 0 || UTCStartHour > 24)
        {
            throw new ArgumentOutOfRangeException(
                nameof(UTCStartHour), 
                $"{nameof(ScheduledLuaAgentOptions)}.{nameof(UTCStartHour)} should be between 0 and 24 inclusive.");
        }

        if (UTCEndHour < 0 || UTCEndHour > 24)
        {
            throw new ArgumentOutOfRangeException(
                nameof(UTCEndHour), 
                $"{nameof(ScheduledLuaAgentOptions)}.{nameof(UTCEndHour)} should be between 0 and 24 inclusive.");
        }

        if (UTCStartHour > UTCEndHour)
        {
            throw new ArgumentException(
                $"{nameof(ScheduledLuaAgentOptions)}.{nameof(UTCStartHour)} should be less than or equal to {nameof(UTCEndHour)}.");
        }
    }
}