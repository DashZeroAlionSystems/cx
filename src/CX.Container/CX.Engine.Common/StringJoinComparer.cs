namespace CX.Engine.Common;

public class StringJoinComparer<T> : IEqualityComparer<T> 
    where T: class, IEnumerable<string>
{
    public bool Equals(T x, T y)
    {
        if (x == null || y == null) return x == y; // Handle null cases
        return string.Join(",", x) == string.Join(",", y);
    }

    public int GetHashCode(T obj)
    {
        return string.Join(",", obj).GetHashCode();
    }
}