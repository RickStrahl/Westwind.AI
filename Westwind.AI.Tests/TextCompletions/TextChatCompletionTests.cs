using Westwind.AI.Chat;
using Westwind.AI.Chat.Configuration;
using Westwind.AI.Specialized;
using Westwind.Utilities;

namespace Westwind.AI.Tests.TextCompletions
{
    [TestClass]
    public class TextChatCompletionTests
    {
        IOpenAiConnection Connection { get; set; }
        OpenAiConnectionConfiguration Configuration { get; set; }

        public TextChatCompletionTests()
        {
            Configuration = OpenAiConnectionConfiguration.Load();
            Connection = Configuration.ActiveConnection;
        }

        [TestMethod]
        public async Task GenericCompletionTest()
        {
            var completion = new GenericAiChat(Connection);
            completion.ChatHttpClient.CaptureRequestData = true;

            Console.WriteLine("Using: " + Connection.Name);

            string result = await completion.Complete(
                "Translate the following from English to German:\nThe sky is below, the ground is above",
                "You are a translator that translates between languages. Return only the translated text.");

            // optionally captured request and response data
            Console.WriteLine(completion.ChatHttpClient.LastRequestJson);
            Console.WriteLine("\n\n" + completion.ChatHttpClient.LastResponseJson);

            Assert.IsNotNull(result, completion.ErrorMessage);
            Console.WriteLine(result);
        }

        [TestMethod]
        public async Task SummarizeCompletionTest()
        {
            var completion = new AiTextOperations(Connection);

            Console.WriteLine("Using: " + Connection.Name);

            string result = await completion.Summarize(
                textToSummarize, 3);

            Assert.IsNotNull(result, completion.ErrorMessage);
            Console.WriteLine(result);
        }

        [TestMethod]
        public async Task SummarizeFromUrlCompletionTest()
        {
            var completion = new AiTextOperations(Connection);

            Console.WriteLine("Using: " + Connection.Name);

            string html = await HttpClientUtils.DownloadStringAsync("https://weblog.west-wind.com/posts/2024/Feb/20/Reading-Raw-ASPNET-RequestBody-Multiple-Times");

            // Extract article HTML fragment to reduce size
            html = StringUtils.ExtractString(html, "<article", "</article>", returnDelimiters: true);

            string result = await completion.Summarize(
                html, 3);

            Assert.IsNotNull(result, completion.ErrorMessage);
            Console.WriteLine(result);
        }



        [TestMethod]
        public async Task TranslationTest()
        {
            var translator = new AiTextOperations(Connection);
            translator.ChatHttpClient.CaptureRequestData = true;

            Console.WriteLine("Using: " + Connection.Name);

            string result = await translator.TranslateText("The sky is below, the ground is above", "en", "de");

            //Console.WriteLine(translator.ChatHttpClient.LastRequestJson);
            //Console.WriteLine("\n\n" + translator.ChatHttpClient.LastResponseJson);

            Assert.IsNotNull(result, translator.ErrorMessage);
            Console.WriteLine(result);
        }



        [TestMethod]
        public async Task GrammarCheckTest()
        {
            var orig = "Long story short one of the use cases that usually made me grab for the Newtonsoft library was dynamic parsing, but I'm glad to see that at some time at least some minimal support for dynamic parsing was added to the `System.Text.Json.JsonSerializer` class";
            var checker = new AiTextOperations(Connection);
            var result = await checker.CheckGrammar(orig);

            Assert.IsNotNull(result, checker.ErrorMessage);

            Console.WriteLine("Original:\n" + orig);

            Console.WriteLine("\nAdjusted:\n");
            Console.WriteLine(result);
        }

        [TestMethod]
        public async Task GrammarCheckDiffTest()
        {
            var orig = "Long story short one of the use cases that usually made me grab for the Newtonsoft library was dynamic parsing, but I'm glad to see that at some time at least some minimal support for dynamic parsing was added to the `System.Text.Json.JsonSerializer` class";
            var checker = new AiTextOperations(Connection);
            var result = await checker.CheckGrammarAsDiff(orig);

            Assert.IsNotNull(result, checker.ErrorMessage);

            Console.WriteLine("Original:\n" + orig);

            Console.WriteLine("\nAdjusted:\n");
            Console.WriteLine(result);
        }

        string textToSummarize = """
            For money, for honor
            No one even knows 
            What the hell we’re fighting for
            
            Another proxy war
            Another worthless cause
            Another wave of bloody gore
            
            War is hell
            Everybody knows
            And yet here we are again 
            
            Burn up the money
            Burn up the air
            Scream and shout about how it’s not fair
            
                The War Machine
                  It’s a Wrecking Machine
                The War Machine
                  It’s evil and mean
                The War Machine
                  Never what was foreseen
                The War Machine
                  It’s a Money Machine
                The Money Machine
                  It’s a profit scheme
                The Money Machine
                  The profit's obscene!
            
            Blow shit up
            In the name of peace
            War is the disease
            
            All guts, no glory
            Politician’s dreams
            While a soldier screams
            
            Send kids to war
            It's a corporate racket
            Works out great for the upper tax bracket
            
            Ashes to ashes
            Dust to dust
            There’s nothing and no one left to trust
            
                The War Machine
                  It’s a Wrecking Machine
                The War Machine
                  It’s evil and mean
                The War Machine
                  Never what was foreseen
                The War Machine
                  It’s a Money Machine
                The War Machine
                The War Machine
            """;
    }
}
