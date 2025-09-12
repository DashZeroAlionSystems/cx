using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace CX.Engine.Common;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public static class CxImageUtils
{
    /// <summary>
    /// Crops a bitmap using a normalized RectangleF (values between 0.0 and 1.0).
    /// </summary>
    /// <param name="source">The source bitmap.</param>
    /// <param name="normalizedRect">The rectangle with normalized coordinates.</param>
    /// <returns>A new cropped bitmap.</returns>
    public static Bitmap CropNormalized(this Bitmap source, RectangleF? normalizedRect)
    {
        if (normalizedRect == null)
            return source;
        
        if (normalizedRect.Value.Left < 0 || normalizedRect.Value.Top < 0 ||
            normalizedRect.Value.Right > 1 || normalizedRect.Value.Bottom > 1 ||
            normalizedRect.Value.Width <= 0 || normalizedRect.Value.Height <= 0)
        {
            throw new ArgumentException("Invalid normalized rectangle.");
        }

        var x = (int)(source.Width * normalizedRect.Value.Left);
        var y = (int)(source.Height * normalizedRect.Value.Top);
        var width = (int)(source.Width * normalizedRect.Value.Width);
        var height = (int)(source.Height * normalizedRect.Value.Height);

        var cropRect = new Rectangle(x, y, width, height);
        return source.Clone(cropRect, source.PixelFormat);
    }
    
    public static Bitmap Scale(this Bitmap source, float scale) => new (source, new((int)(source.Width * scale), (int)(source.Height * scale)));
}