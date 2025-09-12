namespace CX.Container.Server.Domain.SourceDocumentStatus;

using Ardalis.SmartEnum;
using CX.Container.Server.Exceptions;
using Amazon.Runtime;


/// <summary>
/// <para>Value Object representing the Status of a <see cref="SourceDocuments.SourceDocument"/>.</para>
/// <para>The object is immutable.</para>
/// <para>
/// <list type="table">
/// <listheader><term>Valid Statuses</term><description>Description</description></listheader>
/// <item><term>PublicBucket</term><description>Document was uploaded to the Public storage.</description></item>
/// <item><term>PrivateBucket</term><description>Document was moved to Private storage.</description></item>
/// <item><term>OCR</term><description>Document submitted for Optical Character Recognition.</description></item>
/// <item><term>Scraping</term><description>Document submitted for Web Scraping.</description></item>
/// <item><term>OCRDone</term><description>OCR has been completed.</description></item>
/// <item><term>Decorating</term><description>Text is being Decorated.</description></item>
/// <item><term>DecoratingDone</term><description>Decoration is complete.</description></item>
/// <item><term>Training</term><description>Document is being used to train the Chat-bot.</description></item>
/// <item><term>TrainingDone</term><description>Training is complete.</description></item>
/// <item><term>QueuedForRetrain</term><description>Perform retraining on the document.</description></item>
/// <item><term>Done</term><description>Document workflow has completed.</description></item>
/// <item><term>Error</term><description>Document workflow has Errors.</description></item>
/// </list>
/// </para>
/// <para>These values are defined by <see cref="SourceDocumentStatusEnum"/>.</para>
/// </summary>
public sealed class SourceDocumentStatus : ValueObject
{
    private readonly SourceDocumentStatusEnum _sourceDocumentStatus;
    
    /// <summary>
    /// Value of the Status.
    /// </summary>
    /// <exception cref="InvalidSmartEnumPropertyName">When attempting to assign an invalid Document Status.</exception>
    public string Value
    {
        get => _sourceDocumentStatus.Name;
        init
        {
            if (!SourceDocumentStatusEnum.TryFromName(value, true, out var parsed))
                throw new InvalidSmartEnumPropertyName(nameof(Value), value);

            _sourceDocumentStatus = parsed;
        }
    }

    public SourceDocumentStatus(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Factory method for creating a <see cref="SourceDocumentStatus"/> object.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static SourceDocumentStatus Of(string value) => new(value);
    public static implicit operator string(SourceDocumentStatus value) => value.Value;
    public static implicit operator SourceDocumentStatus(string value) => Of(value);
    public static List<string> ListNames() => SourceDocumentStatusEnum.List.Select(x => x.Name).ToList();

    /// <summary>
    /// Factory method for creating a <see cref="SourceDocumentStatus"/> object with a value of <see cref="SourceDocumentStatusEnum.PublicBucket"/>.
    /// </summary>
    public static SourceDocumentStatus PublicBucket() => new(SourceDocumentStatusEnum.PublicBucket.Name);
    
    /// <summary>
    /// Factory method for creating a <see cref="SourceDocumentStatus"/> object with a value of <see cref="SourceDocumentStatusEnum.PrivateBucket"/>.
    /// </summary>
    public static SourceDocumentStatus PrivateBucket() => new(SourceDocumentStatusEnum.PrivateBucket.Name);

    /// <summary>
    /// Factory method for creating a <see cref="SourceDocumentStatus"/> object with a value of <see cref="SourceDocumentStatusEnum.OCR"/>.
    /// </summary>
    public static SourceDocumentStatus OCR() => new(SourceDocumentStatusEnum.OCR.Name);

    /// <summary>
    /// Factory method for creating a <see cref="SourceDocumentStatus"/> object with a value of <see cref="SourceDocumentStatusEnum.Scraping"/>.
    /// </summary>
    public static SourceDocumentStatus Scraping() => new(SourceDocumentStatusEnum.Scraping.Name);

    /// <summary>
    /// Factory method for creating a <see cref="SourceDocumentStatus"/> object with a value of <see cref="SourceDocumentStatusEnum.OCRDone"/>.
    /// </summary>
    public static SourceDocumentStatus OCRDone() => new(SourceDocumentStatusEnum.OCRDone.Name);
    
    /// <summary>
    /// Factory method for creating a <see cref="SourceDocumentStatus"/> object with a value of <see cref="SourceDocumentStatusEnum.Decorating"/>.
    /// </summary>
    public static SourceDocumentStatus Decorating() => new(SourceDocumentStatusEnum.Decorating.Name);
    
    /// <summary>
    /// Factory method for creating a <see cref="SourceDocumentStatus"/> object with a value of <see cref="SourceDocumentStatusEnum.DecoratingDone"/>.
    /// </summary>
    public static SourceDocumentStatus DecoratingDone() => new(SourceDocumentStatusEnum.DecoratingDone.Name);
    
    /// <summary>
    /// Factory method for creating a <see cref="SourceDocumentStatus"/> object with a value of <see cref="SourceDocumentStatusEnum.Training"/>.
    /// </summary>
    public static SourceDocumentStatus Training() => new(SourceDocumentStatusEnum.Training.Name);
    
    /// <summary>
    /// Factory method for creating a <see cref="SourceDocumentStatus"/> object with a value of <see cref="SourceDocumentStatusEnum.TrainingDone"/>.
    /// </summary>
    public static SourceDocumentStatus TrainingDone() => new(SourceDocumentStatusEnum.TrainingDone.Name);
    
    /// <summary>
    /// Factory method for creating a <see cref="SourceDocumentStatus"/> object with a value of <see cref="SourceDocumentStatusEnum.QueuedForRetrain"/>.
    /// </summary>
    public static SourceDocumentStatus QueuedForRetrain() => new(SourceDocumentStatusEnum.QueuedForRetrain.Name);

    /// <summary>
    /// Factory method for creating a <see cref="SourceDocumentStatus"/> object with a value of <see cref="SourceDocumentStatusEnum.Done"/>.
    /// </summary>
    public static SourceDocumentStatus Done() => new(SourceDocumentStatusEnum.Done.Name);

    public static SourceDocumentStatus Error() => new(SourceDocumentStatusEnum.Error.Name);

    private SourceDocumentStatus() { } // EF Core

    private abstract class SourceDocumentStatusEnum : SmartEnum<SourceDocumentStatusEnum>
    {
        public static readonly SourceDocumentStatusEnum PublicBucket = new PublicBucketType();
        public static readonly SourceDocumentStatusEnum PrivateBucket = new PrivateBucketType();
        public static readonly SourceDocumentStatusEnum OCR = new OCRType();
        public static readonly SourceDocumentStatusEnum Scraping = new ScrapingType();
        public static readonly SourceDocumentStatusEnum OCRDone = new OCRDoneType();
        public static readonly SourceDocumentStatusEnum Decorating = new DecoratingType();
        public static readonly SourceDocumentStatusEnum DecoratingDone = new DecoratingDoneType();
        public static readonly SourceDocumentStatusEnum Training = new TrainingType();
        public static readonly SourceDocumentStatusEnum TrainingDone = new TrainingDoneType();
        public static readonly SourceDocumentStatusEnum QueuedForRetrain = new QueuedForRetrainType();
        public static readonly SourceDocumentStatusEnum Done = new DoneType();
        public static readonly SourceDocumentStatusEnum Error = new ErrorType();

        protected SourceDocumentStatusEnum(string name, int value) : base(name, value)
        {
        }
        private class PublicBucketType : SourceDocumentStatusEnum
        {
            public PublicBucketType() : base("PublicBucket", 0)
            {
            }
        }

        private class PrivateBucketType : SourceDocumentStatusEnum
        {
            public PrivateBucketType() : base("PrivateBucket", 1)
            {
            }
        }
        private class OCRType : SourceDocumentStatusEnum
        {
            public OCRType() : base("OCR", 2)
            {
            }
        }

        private class ScrapingType : SourceDocumentStatusEnum
        {
            public ScrapingType() : base("Scraping", 3)
            {
            }
        }

        private class OCRDoneType : SourceDocumentStatusEnum
        {
            public OCRDoneType() : base("OCRDone", 4)
            {
            }
        }

        private class DecoratingType : SourceDocumentStatusEnum
        {
            public DecoratingType() : base("Decorating", 5)
            {
            }
        }
        private class DecoratingDoneType : SourceDocumentStatusEnum
        {
            public DecoratingDoneType() : base("DecoratingDone", 6)
            {
            }
        }
        private class TrainingType : SourceDocumentStatusEnum
        {
            public TrainingType() : base("Training", 7)
            {
            }
        }

        private class TrainingDoneType : SourceDocumentStatusEnum
        {
            public TrainingDoneType() : base("TrainingDone", 8)
            {
            }
        }

        private class QueuedForRetrainType : SourceDocumentStatusEnum
        {
            public QueuedForRetrainType() : base("QueuedForRetrain", 9)
            {
            }
        }
        private class DoneType : SourceDocumentStatusEnum
        {
            public DoneType() : base("Done", 10)
            {
            }
        }
        private class ErrorType : SourceDocumentStatusEnum
        {
            public ErrorType() : base("Error", 11)
            {
            }
        }

    }
}