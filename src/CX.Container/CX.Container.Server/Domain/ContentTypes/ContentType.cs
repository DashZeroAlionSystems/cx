namespace CX.Container.Server.Domain.ContentTypes;

using Ardalis.SmartEnum;
using CX.Container.Server.Exceptions;

/// <summary>
/// <para>Value Object representing the Content Type of a document or file.</para>
/// <para>The object is immutable.</para>
/// <para>
/// <list type="table">
/// <listheader><term>Valid Content Type Options</term><description>Description</description></listheader>
/// <item><term>PlainText</term><description>Plain Text Content</description></item>
/// <item><term>Markdown</term><description>Markdown Content</description></item>
/// <item><term>Html</term><description>HTML Content</description></item>
/// <item><term>Audio</term><description>Audio Content</description></item>
/// <item><term>Video</term><description>Video Content</description></item>
/// <item><term>Image</term><description>Image Content</description></item>
/// </list>
/// </para>
/// <para>These values are defined by <see cref="ContentTypeEnum"/>.</para>
/// </summary>
public sealed class ContentType : ValueObject
{
    private readonly ContentTypeEnum _contentType;

    /// <summary>
    /// Initializes and gets the value of the Content Type.
    /// </summary>
    /// <exception cref="InvalidSmartEnumPropertyName">Thrown when the value is an invalid content type.</exception>
    public string Value
    {
        get => _contentType.Name;
        private init
        {
            if (!ContentTypeEnum.TryFromName(value, true, out var parsed))
                throw new InvalidSmartEnumPropertyName(nameof(Value), value);

            _contentType = parsed;
        }
    }

    private ContentType(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new instance of the Content Type from the given string.
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="InvalidSmartEnumPropertyName">Thrown when the value is an invalid content type.</exception>
    /// <returns></returns>
    public static ContentType Of(string value) => new(value);

    public static implicit operator string(ContentType value) => value.Value;
    public static List<string> ListNames() => ContentTypeEnum.List.Select(x => x.Name).ToList();

    /// <summary>
    /// Create PlainText Content Type.
    /// </summary>
    /// <returns><see cref="ContentType"/></returns>
    public static ContentType PlainText() => new(ContentTypeEnum.PlainText.Name);

    /// <summary>
    /// Create Markdown Content Type
    /// </summary>
    /// <returns><see cref="ContentType"/></returns>
    public static ContentType Markdown() => new(ContentTypeEnum.Markdown.Name);
    
    /// <summary>
    /// Create Html Content Type
    /// </summary>
    /// <returns><see cref="ContentType"/></returns>
    public static ContentType Html() => new(ContentTypeEnum.Html.Name);
    
    /// <summary>
    /// Create Audio Content Type
    /// </summary>
    /// <returns><see cref="ContentType"/></returns>
    public static ContentType Audio() => new(ContentTypeEnum.Audio.Name);
    
    /// <summary>
    /// Create Video Content Type
    /// </summary>
    /// <returns><see cref="ContentType"/></returns>
    public static ContentType Video() => new(ContentTypeEnum.Video.Name);
    
    /// <summary>
    /// Create Image Content Type
    /// </summary>
    /// <returns><see cref="ContentType"/></returns>
    public static ContentType Image() => new(ContentTypeEnum.Image.Name);

    private ContentType()
    {
    } // EF Core

    private abstract class ContentTypeEnum : SmartEnum<ContentTypeEnum>
    {
        public static readonly ContentTypeEnum PlainText = new PlainTextType();
        public static readonly ContentTypeEnum Markdown = new MarkdownType();
        public static readonly ContentTypeEnum Html = new HtmlType();
        public static readonly ContentTypeEnum Audio = new AudioType();
        public static readonly ContentTypeEnum Video = new VideoType();
        public static readonly ContentTypeEnum Image = new ImageType();

        protected ContentTypeEnum(string name, int value) : base(name, value)
        {
        }

        private class PlainTextType : ContentTypeEnum
        {
            public PlainTextType() : base("PlainText", 0)
            {
            }
        }

        private class MarkdownType : ContentTypeEnum
        {
            public MarkdownType() : base("Markdown", 1)
            {
            }
        }

        private class HtmlType : ContentTypeEnum
        {
            public HtmlType() : base("Html", 2)
            {
            }
        }

        private class AudioType : ContentTypeEnum
        {
            public AudioType() : base("Audio", 3)
            {
            }
        }

        private class VideoType : ContentTypeEnum
        {
            public VideoType() : base("Video", 4)
            {
            }
        }

        private class ImageType : ContentTypeEnum
        {
            public ImageType() : base("Image", 5)
            {
            }
        }
    }
}