namespace CX.Container.Server.Domain.MessageTypes;

using Ardalis.SmartEnum;
using CX.Container.Server.Exceptions;

/// <summary>
/// <para>Value Object representing the Type or Origin of Message.</para>
/// <para>The object is immutable.</para>
/// <para>
/// <list type="table">
/// <listheader><term>Valid Message Type Options</term><description>Description</description></listheader>
/// <item><term>User</term><description>Message sent by the User.</description></item>
/// <item><term>System</term><description>Message sent by the Chat Bot.</description></item>
/// <item><term>Error</term><description>Error Message</description></item>
/// </list>
/// </para>
/// <para>These values are defined by <see cref="MessageTypeEnum"/>.</para>
/// </summary>
public sealed class MessageType : ValueObject
{
    private readonly MessageTypeEnum _messageType;

    /// <summary>
    /// Returns the value of the Message Type.
    /// </summary>
    /// <exception cref="InvalidSmartEnumPropertyName">When attempting to assign an invalid value.</exception>
    public string Value
    {
        get => _messageType.Name;
        private init
        {
            if (!MessageTypeEnum.TryFromName(value, true, out var parsed))
                throw new InvalidSmartEnumPropertyName(nameof(Value), value);

            _messageType = parsed;
        }
    }

    private MessageType(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Factory method to create a new instance of <see cref="MessageType"/> from the given string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns><see cref="MessageType"/></returns>
    public static MessageType Of(string value) => new MessageType(value);

    public static implicit operator string(MessageType value) => value.Value;
    public static List<string> ListNames() => MessageTypeEnum.List.Select(x => x.Name).ToList();

    /// <summary>
    /// Factory method to create a new instance of <see cref="MessageType"/> representing a User Message.
    /// </summary>
    /// <returns><see cref="MessageType"/></returns>
    public static MessageType User() => new MessageType(MessageTypeEnum.User.Name);
    
    /// <summary>
    /// Factory method to create a new instance of <see cref="MessageType"/> representing a System Message.
    /// </summary>
    /// <returns><see cref="MessageType"/></returns>
    public static MessageType System() => new MessageType(MessageTypeEnum.System.Name);
    
    /// <summary>
    /// Factory method to create a new instance of <see cref="MessageType"/> representing an Error.
    /// </summary>
    /// <returns><see cref="MessageType"/></returns>
    public static MessageType Error() => new MessageType(MessageTypeEnum.Error.Name);

    private MessageType()
    {
    } // EF Core

    private abstract class MessageTypeEnum : SmartEnum<MessageTypeEnum>
    {
        public static readonly MessageTypeEnum User = new UserType();
        public static readonly MessageTypeEnum System = new SystemType();
        public static readonly MessageTypeEnum Error = new ErrorType();

        protected MessageTypeEnum(string name, int value) : base(name, value)
        {
        }

        private class UserType : MessageTypeEnum
        {
            public UserType() : base("User", 0)
            {
            }
        }

        private class SystemType : MessageTypeEnum
        {
            public SystemType() : base("System", 1)
            {
            }
        }

        private class ErrorType : MessageTypeEnum
        {
            public ErrorType() : base("Error", 2)
            {
            }
        }
    }
}