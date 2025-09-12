namespace CX.Engine.Common.Embeddings;

public static class EmbeddingHelpers
{
    public static double GetCosineSimilarity(this float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        var dotProduct = 0.0f;
        var magnitudeVector1 = 0.0f;
        var magnitudeVector2 = 0.0f;

        for (var i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitudeVector1 += MathF.Pow(vector1[i], 2);
            magnitudeVector2 += MathF.Pow(vector2[i], 2);
        }

        magnitudeVector1 = MathF.Sqrt(magnitudeVector1);
        magnitudeVector2 = MathF.Sqrt(magnitudeVector2);

        if (magnitudeVector1 == 0 || magnitudeVector2 == 0)
        {
            throw new ArgumentException("Magnitude of at least one vector is zero");
        }

        return dotProduct / (magnitudeVector1 * magnitudeVector2);
    }
}