namespace CX.Engine.Common.Xml;

public class CxmlCircularDependencyException : Exception
{
    public CxmlCircularDependencyException(string message) : base(message)
    {
    }
}