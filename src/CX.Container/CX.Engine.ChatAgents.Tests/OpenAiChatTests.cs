using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.ChatAgents.OpenAI.Schemas;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using CXLibTests.Resources;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace CXLibTests;

public class OpenAiChatTests : TestBase
{
    private OpenAIChatAgent _openAiChatAgent = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _openAiChatAgent = sp.GetRequiredNamedService<OpenAIChatAgent>("GPT-4o-mini");
    }

    [Fact]
    public Task PickOneTestAsync() => Builder.RunAsync(async () =>
    {
        var req = _openAiChatAgent.GetRequest("Reply with a single word.  Which is typically red? Apple, Banana or Lettuce?");
        var res = await _openAiChatAgent.RequestAsync(req);
        Assert.Equal("Apple", res.Answer);
    });

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    [Semantic("Contextualizes a user question")]
    private class QuestionContext
    {
        [Semantic("Reasoning about the category of this question")]
        public string CategoryReasoning { get; set; } = null!;

        [Semantic("The category of the question")]
        public string Category { get; set; } = null!;

        [Semantic("The question, contextualized with chat history in mind")]
        public string Question { get; set; } = null!;

        [Semantic(
            """
            The user's emotions towards the chat agent.  
            We deal with negative topics regularly, we only wish to know if the user is happy with the chatbot's performance.
            """, choices: [ "Positive", "Negative", "Neutral" ])]
        public string ChatbotImpression { get; set; } = null!;
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    [Semantic("Class to container answer")]
    private class Answer
    {
        [Semantic("The answer.")] 
        public string[] BestMethods { get; set; } = null!;
    }

    [Fact]
    public Task ContextualizeTestAsync() => Builder.RunAsync(async () =>
    {
        var req = _openAiChatAgent.GetRequest("""
                                                     This affects my house's value significantly.  
                                                     What can I do?
                                                     """);
        req.SystemPrompt =
            "Given a chat history and the latest user question which might reference context in the chat history, formulate a standalone question which can be understood without the chat history or any further context. Do NOT answer the question, just reformulate it if needed and otherwise return it as is. Ensure that the formulated question is in English.  Fix any spelling and grammar mistakes that you encounter.";
            req.History.AddRange(
            [
                new OpenAIChatMessage("user", "The municipality has allowed the construction of a sewage pump next to my house.")
            ]
            );
            req.ResponseFormat =
                new OpenAISchema<QuestionContext>()
                    .Constrain(nameof(QuestionContext.Category), ["Legal", "Municipal", "Municipal Law"]);
            var res = await _openAiChatAgent.GetResponseAsync<QuestionContext>(req);

        Assert.Equal("Municipal Law", res.Category);
    });

    [Fact]
    public Task ImageTestAsync() => Builder.RunAsync(async () =>
    {
        // Make a request to the OpenAI API to describe the image
        var req = _openAiChatAgent.GetRequest(
            "Here's an image of a page to transcribe.  Include descriptions of images and screenshots on the page and maintain the order of content as best you can.  Do not mention that this is transcibed in your answer.",
            systemPrompt: "Describe provided images with exact wording.");
        await req.AttachImageAsync(this.GetResource(Resource.page_5_jpg));
        var res = await _openAiChatAgent.RequestAsync(req);


        // Assert that the the OpenAI API provided a response
        Assert.NotNull(res.Answer);
    });

    public OpenAiChatTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(SecretNames.OpenAIChatAgents);
        Builder.AddServices((sc, config) => { sc.AddOpenAIChatAgents(config); });
    }
}