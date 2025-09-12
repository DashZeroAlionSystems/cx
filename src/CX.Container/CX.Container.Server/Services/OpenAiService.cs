using CX.Container.Server.Configurations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;
using CX.Engine.ChatAgents.OpenAI;
using JetBrains.Annotations;

namespace CX.Container.Server.Services;

public class OpenAiService
{
    private readonly OpenAiOptions _options;

    public OpenAiService(IOptions<OpenAiOptions> options)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ExtractedInformation> SummarizeTextAsync(string combinedText)
    {
        var extractedInfo = new ExtractedInformation();

        try
        {
            // Initialize HttpClient for OpenAI
            using var httpClient = new HttpClient();

            string apiKey = _options.ApiKey;

            // Set HTTP headers for OpenAI API
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            // Define OpenAI GPT-3.5 API endpoint
            string apiUrl = _options.ApiUrl;

            // Define GPT-3.5 model name
            string modelName = _options.ModelName;

            // Define prompts for each attribute          
            var attributePrompts = new Dictionary<string, (string prompt, int maxTokens)>
            {
                { "title", ("open gpt 20chars \"Analyze text and extract a title for the article in the text. Return the title in plain text without any special characters or the word Title to start off with. \"", 10) },
                { "description", ("open gpt 50chars \"Analyze text and extract a description for the article in the text\"", 45) },
                { "author", ("open gpt 15chars \"Analyze text and extract the author for the article in the text. If the author could not be determined, only write the word 'unknown', do not write a sentence.\"", 5) },
                //{ "source", ("open gpt 15chars \"Analyze text and extract a source for the article in the text. If the source could not be determined, only write the word 'unknown', do not write a sentence.\"", 7) },
                { "publication", ("open gpt 4chars \"Analyze the text and extract a published date, write only the year in which it was published. If the published date could not be determined, only write the word 'unkown', do not write a sentence\"", 5) },
                { "language", ("open gpt 10chars \"Analyze text and determine language of the article in the text, answer with only the language name\"", 4) },                
                { "keywords", ("open gpt 40chars \"Analyze text and extract three keywords for the article in the text. Your answer should be comma separated with no special characters.\"", 10) },
                { "tags", ("open gpt 30chars \"Analyze text and extract two tag words for the article in the text. Your answer should be comma separated with no special characters.\"", 8) },
                { _options.PromptKey, ($"{_options.PromptSubject} \"{_options.Prompt}\"", 2) }
            };

            // Initial user message with the combinedText
            var conversation = new List<object>
            {
                new { role = "user", content = combinedText },                        
            };

            // Loop through each attribute and generate value using GPT-3.5
            foreach (var attribute in attributePrompts.Keys)
            {
                try
                {
                    var (prompt, maxTokens) = attributePrompts[attribute];

                    conversation.Add(new { role = "assistant", content = prompt });

                    // Prepare request data
                    var requestData = new
                    {
                        model = modelName,
                        messages = conversation,
                        temperature = 0.5,
                        max_tokens = maxTokens
                    };

                    // Convert request data to JSON
                    var requestDataJson = System.Text.Json.JsonSerializer.Serialize(requestData);

                    // Create content with the request data
                    var content = new StringContent(requestDataJson, Encoding.UTF8, "application/json");

                    // Send request to OpenAI API
                    var response = await httpClient.PostAsync(apiUrl, content).ConfigureAwait(false);

                    // Handle OpenAI response
                    if (response.IsSuccessStatusCode)
                    {
                        // Read response content
                        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        var responseData = JsonConvert.DeserializeObject<OpenAIResponse>(responseBody);

                        // Extract generated text from OpenAI response
                        var generatedText = responseData.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

                        // Update ExtractedInformation with generated attribute value
                        if (attribute.Equals(_options.PromptKey, StringComparison.OrdinalIgnoreCase))
                        {
                            // Convert the generated text to an integer for agri_relevance
                            if (int.TryParse(generatedText, out int relevance))
                            {
                                extractedInfo.AgriRelevance = relevance;
                            }
                        }
                        else
                        {
                            // Use reflection to set the value of the corresponding property in ExtractedInformation
                            var property = typeof(ExtractedInformation).GetProperty(attribute, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                            if (property != null)
                            {
                                property.SetValue(extractedInfo, generatedText);
                            }
                        }
                    }
                    else
                    {
                        // Handle error from OpenAI
                        Console.WriteLine($"Error from OpenAI: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing attribute '{attribute}': {ex.Message}");
                }
                finally
                {
                    // Remove the last assistant message from the conversation
                    conversation.RemoveAt(conversation.Count - 1);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in processing extracted information: " + ex.Message);
        }

        return extractedInfo;
    }

    public class OpenAIResponse
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public long Created { get; set; }
        public string Model { get; set; }
        public List<Choice> Choices { get; set; }
        public Usage Usage { get; set; }
        public string SystemFingerprint { get; set; }
    }

    public class Choice
    {
        public int Index { get; set; }
        public Message Message { get; set; }
        public object Logprobs { get; set; }
        public string FinishReason { get; set; }
    }

    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    public class ExtractedInformation
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Source { get; set; }
        public string Publication { get; set; }
        public string Language { get; set; }
        public int AgriRelevance { get; set; }
        public string FileName { get; set; }
        public string S3Key { get; set; }
        public string Keywords { get; set; }
        public string Tags { get; set; }
    }
}