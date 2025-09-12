namespace CX.Engine.Common.Conversion;

public class CxConvertException : Exception
{
    public CxConvertException(string message) : base(message)
    {
    }

    public CxConvertException(Exception innerException) : base((innerException ?? throw new ArgumentNullException(nameof(innerException))).Message, innerException)
    {
    }
}