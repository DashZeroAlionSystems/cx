namespace CX.Container.Server.Domain.Emails;

using FluentValidation;

/// <summary>
/// Value Object representing an Email.
/// </summary>
public sealed class Email : ValueObject
{
    /// <summary>
    /// Email address as a string.
    /// </summary>
    public string Value { get; init; }
    
    private Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Value = null;
            return;
        }
        new EmailValidator().ValidateAndThrow(value);
        Value = value;
    }
    
    public static Email Of(string value) => new(value);
    public static implicit operator string(Email value) => value.Value;

    private Email() { } // EF Core
    
    private sealed class EmailValidator : AbstractValidator<string> 
    {
        public EmailValidator()
        {
            RuleFor(email => email).EmailAddress();
        }
    }
}