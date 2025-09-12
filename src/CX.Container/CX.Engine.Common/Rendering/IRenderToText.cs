namespace CX.Engine.Common.Rendering;

public interface IRenderToText
{
    /// <summary>
    /// Must be called within a configured <see cref="TextRenderContext"/>.
    /// </summary>
    public Task RenderToTextAsync();
}