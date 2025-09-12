namespace CX.Engine.Common.CodeProcessing;

public class EvaluatorException : Exception
{
    public EvaluatorException(string message) : base(message)
    {
    }
    
    public static EvaluatorException Throw(string message) => throw new EvaluatorException(message);
}