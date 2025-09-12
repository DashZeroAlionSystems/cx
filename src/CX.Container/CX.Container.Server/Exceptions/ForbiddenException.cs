namespace CX.Container.Server.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base() { }
    
    public ForbiddenAccessException(string message) : base(message) { }
}