using System.Text.Json.Serialization;
using CX.Engine.Common;

namespace CX.Engine.Assistants.ScheduledQuestions
{
    [Flags]
    public enum RunningDays
    {
        None = 0,
        Monday = 1 << 0,      // 1
        Tuesday = 1 << 1,     // 2
        Wednesday = 1 << 2,   // 4
        Thursday = 1 << 3,    // 8
        Friday = 1 << 4,      // 16
        Saturday = 1 << 5,    // 32
        Sunday = 1 << 6,      // 64
        Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,  // 31
        All = Weekdays | Saturday | Sunday                            // 127
    }
    
    public class ScheduledQuestionAgentOptions : IValidatable
    {
        public string AssistantName { get; set; } = string.Empty;
        public string QuestionQuery { get; set; } = string.Empty;
        public string PostgreSQLClientName { get; set; } = string.Empty;
        public string IntervalPerQuestion { get; set; } = string.Empty;
        public int UTCStartHour { get; set; } = int.MinValue;
        public int UTCEndHour { get; set; } = int.MaxValue;
        public string Username { get; set; } = "user";
        [JsonConverter(typeof(JsonStringEnumConverter<RunningDays>))]
        public RunningDays RunDays { get; set; } = RunningDays.None;

        public void Validate()
        {
            // 1. Validate AssistantName
            if (string.IsNullOrWhiteSpace(AssistantName))
            {
                throw new ArgumentException(
                    $"{nameof(ScheduledQuestionAgentOptions)}.{nameof(AssistantName)} is required and cannot be empty.");
            }

            // 2. Validate PostgreSQLClientName
            if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
            {
                throw new ArgumentException(
                    $"{nameof(ScheduledQuestionAgentOptions)}.{nameof(PostgreSQLClientName)} is required and cannot be empty.");
            }

            // 3. Validate IntervalPerQuestion
            if (string.IsNullOrWhiteSpace(IntervalPerQuestion))
            {
                throw new ArgumentException(
                    $"{nameof(ScheduledQuestionAgentOptions)}.{nameof(IntervalPerQuestion)} is required and cannot be empty.");
            }

            // 4. Ensure IntervalPerQuestion is a valid TimeSpan and within acceptable range
            if (!TimeSpan.TryParse(IntervalPerQuestion, out var interval))
            {
                throw new ArgumentException(
                    $"{nameof(ScheduledQuestionAgentOptions)}.{nameof(IntervalPerQuestion)} must be a valid TimeSpan format.");
            }

            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentException(
                    $"{nameof(ScheduledQuestionAgentOptions)}.{nameof(IntervalPerQuestion)} must be greater than zero.");
            }

            if (interval > TimeSpan.FromDays(1))
            {
                throw new ArgumentException(
                    $"{nameof(ScheduledQuestionAgentOptions)}.{nameof(IntervalPerQuestion)} must be less than or equal to 24 hours.");
            }

            // 5. Validate UTCStartHour and UTCEndHour are within 0 to 23
            if (UTCStartHour < 0 || UTCStartHour > 24)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(UTCStartHour), 
                    $"{nameof(ScheduledQuestionAgentOptions)}.{nameof(UTCStartHour)} should be between 0 and 24 inclusive.");
            }

            if (UTCEndHour < 0 || UTCEndHour > 24)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(UTCEndHour), 
                    $"{nameof(ScheduledQuestionAgentOptions)}.{nameof(UTCEndHour)} should be between 0 and 24 inclusive.");
            }

            // 6. Validate that UTCStartHour is less than or equal to UTCEndHour
            if (UTCStartHour > UTCEndHour)
            {
                throw new ArgumentException(
                    $"{nameof(ScheduledQuestionAgentOptions)}.{nameof(UTCStartHour)} should be less than or equal to {nameof(UTCEndHour)}.");
            }

            // 7. Validate Username
            if (string.IsNullOrWhiteSpace(Username))
            {
                throw new ArgumentException(
                    $"{nameof(ScheduledQuestionAgentOptions)}.{nameof(Username)} cannot be null or empty.");
            }
        }
    }
}
