namespace CX.Engine.Common;

public interface IValidatorFor<in T>
{
    void Validate(T opts);
}