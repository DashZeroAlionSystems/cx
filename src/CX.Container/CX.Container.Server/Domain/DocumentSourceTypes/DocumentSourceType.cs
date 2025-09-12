namespace CX.Container.Server.Domain.DocumentSourceTypes;

using Ardalis.SmartEnum;
using CX.Container.Server.Exceptions;

/// <summary>
/// <para>Value Object representing the Source of a Document.</para>
/// <para>The object is immutable.</para>
/// <para>
/// <list type="table">
/// <listheader><term>Valid Document Source Type Values</term><description>Description</description></listheader>
/// <item><term>Blob</term><description>Document is located in Blob Storage.</description></item>
/// <item><term>Site</term><description>Document is located on a web-Site.</description></item>
/// </list>
/// </para>
/// <para>These values are defined by <see cref="DocumentSourceTypeEnum"/>.</para>
/// </summary>
public sealed class DocumentSourceType : ValueObject
{
    private readonly DocumentSourceTypeEnum _documentSourceType;

    /// <summary>
    /// Value of the Value Object.
    /// </summary>
    /// <exception cref="InvalidSmartEnumPropertyName">When attempting to create a DocumentSourceType that is not supported by the SmartEnum.</exception>
    public string Value
    {
        get => _documentSourceType.Name;
        private init
        {
            if (!DocumentSourceTypeEnum.TryFromName(value, true, out var parsed))
                throw new InvalidSmartEnumPropertyName(nameof(Value), value);

            _documentSourceType = parsed;
        }
    }

    private DocumentSourceType(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Create a new instance of the DocumentSourceType from the given string.
    /// </summary>
    /// <param name="value">The value to assign to the new instance.</param>
    /// <returns><see cref="DocumentSourceType"/></returns>
    public static DocumentSourceType Of(string value) => new DocumentSourceType(value);

    public static implicit operator string(DocumentSourceType value) => value.Value;
    public static List<string> ListNames() => DocumentSourceTypeEnum.List.Select(x => x.Name).ToList();

    /// <summary>
    /// Factory Method for creating a new instance of the DocumentSourceType with the value of "Blob".
    /// </summary>
    /// <returns><see cref="DocumentSourceType"/></returns>
    public static DocumentSourceType Blob() => new DocumentSourceType(DocumentSourceTypeEnum.Blob.Name);
    
    /// <summary>
    /// Factory Method for creating a new instance of the DocumentSourceType with the value of "Site".
    /// </summary>
    /// <returns></returns>
    public static DocumentSourceType Site() => new DocumentSourceType(DocumentSourceTypeEnum.Site.Name);

    private DocumentSourceType()
    {
    } // EF Core

    private abstract class DocumentSourceTypeEnum : SmartEnum<DocumentSourceTypeEnum>
    {
        public static readonly DocumentSourceTypeEnum Blob = new BlobType();
        public static readonly DocumentSourceTypeEnum Site = new SiteType();

        protected DocumentSourceTypeEnum(string name, int value) : base(name, value)
        {
        }

        private class BlobType : DocumentSourceTypeEnum
        {
            public BlobType() : base("Blob", 0)
            {
            }
        }

        private class SiteType : DocumentSourceTypeEnum
        {
            public SiteType() : base("Site", 1)
            {
            }
        }
    }
}