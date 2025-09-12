namespace CX.Container.Server.Configurations
{
    /// <summary>
    /// Options for OpenAi
    /// </summary>
    public class OpenAiOptions
    {
        /// <summary>
        /// Open Ai Api key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// OpenAi Url
        /// </summary>
        public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";

        /// <summary>
        /// GPT model name
        /// </summary>
        public string ModelName { get; set; } = "gpt-3.5-turbo";

        /// <summary>
        /// Promt Key
        /// </summary>
        public string PromptKey { get; set; } = "agri_relevance";

        /// <summary>
        /// Promt Subject
        /// </summary>
        public string PromptSubject { get; set; } = "open gpt 3chars";

        /// <summary>
        /// Promt
        /// </summary>
        public string Prompt { get; set; } = "Determine if the text is relevant to agriculture or forestry and if it will be helpful to farmers. Your answer should be an estimated relevance score out of 10, where 0 is not relevant and 10 is very relevant. please just supply a number";
    }
}
