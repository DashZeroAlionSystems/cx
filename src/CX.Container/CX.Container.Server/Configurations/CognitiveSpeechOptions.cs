namespace CX.Container.Server.Configurations
{
    /// <summary>
    /// Options for Microsoft Cognitive Speech Service 
    /// </summary>
    public class CognitiveSpeechOptions
    {
        /// <summary>
        /// Microsoft Cognitive Speech Service Subscription Key
        /// </summary>
        public string SubscriptionKey { get; set; } = String.Empty;

        /// <summary>
        /// Microsoft Cognitive Speech Service Region
        /// </summary>
        public string Region { get; set; } = String.Empty;

        /// <summary>
        /// Microsoft Cognitive Speech Service Default Language
        /// </summary>
        public string Language { get; set; } = String.Empty;

    }
}
