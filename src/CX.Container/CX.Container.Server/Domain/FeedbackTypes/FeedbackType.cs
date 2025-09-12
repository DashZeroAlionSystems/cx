namespace CX.Container.Server.Domain.FeedbackTypes;

using Ardalis.SmartEnum;
using CX.Container.Server.Exceptions;

/// <summary>
/// <para>Value Object representing the type of Feedback a user can provide on a <see cref="Messages.Message"/>.</para>
/// <para>The object is immutable.</para>
/// <para>
/// <list type="table">
/// <listheader><term>Valid Feedback Type Values</term><description>Description</description></listheader>
/// <item><term>None</term><description>No Feedback</description></item>
/// <item><term>Positive</term><description>Positive Feedback</description></item>
/// <item><term>Negative</term><description>Negative Feedback</description></item>
/// </list>
/// </para>
/// <para>These values are defined by <see cref="FeedbackTypeEnum"/>.</para>
/// </summary>
public sealed class FeedbackType : ValueObject
{
    private readonly FeedbackTypeEnum _feedbackType;

    /// <summary>
    /// The value of the Feedback Type.
    /// </summary>
    /// <exception cref="InvalidSmartEnumPropertyName">Thrown when attempting to assign a value not supported by <see cref="FeedbackTypeEnum"/></exception>
    public string Value
    {
        get => _feedbackType.Name;
        private init
        {
            if (!FeedbackTypeEnum.TryFromName(value, true, out var parsed))
                throw new InvalidSmartEnumPropertyName(nameof(Value), value);

            _feedbackType = parsed;
        }
    }

    private FeedbackType(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Create FeedbackType from the given string.
    /// </summary>
    /// <param name="value">The FeedbackType's value.</param>
    /// <returns><see cref="_feedbackType"/></returns>
    public static FeedbackType Of(string value) => new FeedbackType(value);

    public static implicit operator string(FeedbackType value) => value.Value;
    public static List<string> ListNames() => FeedbackTypeEnum.List.Select(x => x.Name).ToList();

    /// <summary>
    /// Create a new instance of the FeedbackType with the value of "None".
    /// </summary>
    /// <returns><see cref="FeedbackType"/></returns>
    public static FeedbackType None() => new FeedbackType(FeedbackTypeEnum.None.Name);
    
    /// <summary>
    /// Create a new instance of the FeedbackType with the value of "Positive".
    /// </summary>
    /// <returns><see cref="FeedbackType"/></returns>
    public static FeedbackType Positive() => new FeedbackType(FeedbackTypeEnum.Positive.Name);
    
    /// <summary>
    /// Create a new instance of the FeedbackType with the value of "Negative".
    /// </summary>
    /// <returns><see cref="FeedbackType"/></returns>
    public static FeedbackType Negative() => new FeedbackType(FeedbackTypeEnum.Negative.Name);

    private FeedbackType()
    {
    } // EF Core

    private abstract class FeedbackTypeEnum : SmartEnum<FeedbackTypeEnum>
    {
        public static readonly FeedbackTypeEnum None = new NoneType();
        public static readonly FeedbackTypeEnum Positive = new PositiveType();
        public static readonly FeedbackTypeEnum Negative = new NegativeType();

        protected FeedbackTypeEnum(string name, int value) : base(name, value)
        {
        }

        private class NoneType : FeedbackTypeEnum
        {
            public NoneType() : base("None", 0)
            {
            }
        }

        private class PositiveType : FeedbackTypeEnum
        {
            public PositiveType() : base("Positive", 1)
            {
            }
        }

        private class NegativeType : FeedbackTypeEnum
        {
            public NegativeType() : base("Negative", 2)
            {
            }
        }
    }
}