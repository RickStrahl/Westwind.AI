using Westwind.AI.Chat;
using Westwind.AI.Chat.Configuration;
using Westwind.AI.Specialized;
using Westwind.Utilities;

namespace Westwind.AI.Tests.TextCompletions
{

    /// <summary>
    /// Most of these tests use the configuration in _AiAuthenticationConfiguration.json
    /// and the ActiveConnectionIndex to determine which connection is used. To use a different model
    /// adjust that file.
    /// 
    /// Note the file can be encrypted run ConfigurationTests to encrypt the file then paste it into
    /// _AiAuthenticationConfiguration.json in your project. This file will not be checked into source control.    
    /// </summary>
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
            ConnectionMessage();

            var completion = new GenericAiChat(Connection);
            completion.HttpClient.CaptureRequestData = true;
                      
            string result = await completion.Complete(
                "Translate the following from English to German:\nThe sky is below, the ground is above",
                "You are a translator that translates between languages. Return only the translated text.");

            // optionally captured request and response data
            Console.WriteLine(completion.HttpClient.LastRequestJson);
            Console.WriteLine("\n\n" + completion.HttpClient.LastResponseJson);

            Assert.IsNotNull(result, completion.ErrorMessage);
            Console.WriteLine(result);
        }

        [TestMethod]
        public async Task SummarizeCompletionTest()
        {
            ConnectionMessage();

            var completion = new AiTextOperations(Connection);            
            string result = await completion.Summarize(
                textToSummarize, 3);

            Assert.IsNotNull(result, completion.ErrorMessage);
            Console.WriteLine(result);
        }

        [TestMethod]
        public async Task SummarizeFromUrlCompletionTest()
        {
            ConnectionMessage();

            var completion = new AiTextOperations(Connection);

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
            translator.HttpClient.CaptureRequestData = true;

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


        [TestMethod]
        public async Task MultipleMessagesTest()
        {
            ConnectionMessage();

            string bornDate = DateTime.Now.AddYears(-30).ToString("MMMM yyyy");
            string currentDate = DateTime.Now.ToString("MMMM yyyy");

            Console.WriteLine("Born on: "  + bornDate);

            var completion = new GenericAiChat(Connection);

            string result = await completion.Complete( [
                 new OpenAiChatMessage { content = $"You are a helpful assistant that answers generic everyday questions precisely. " +
                                                   $"Today is {DateTime.Now.Date:d}", role = "system"   },
                 new OpenAiChatMessage { content = "Hello what is your name?", role = "assistant"   },
                 new OpenAiChatMessage { content = "My name is Rick", role = "user"   },
                 new OpenAiChatMessage { content = "When were you born?", role = "assistant"   },
                 new OpenAiChatMessage { content = bornDate, role = "user"   },      
                 new OpenAiChatMessage { content = $"How old am I today?", role = "user"   }
             ]) ;

            Assert.IsNotNull(result, completion.ErrorMessage);
            Console.WriteLine(result);
        }



        /// <summary>
        /// Demonstrates using the built-in ChatHistory in an instance chat client 
        /// to maintain context across multiple completion requests (or if you want to 
        /// build an interactive chat client).
        /// (completion.HttpClient.ChatHistory) 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ChatHistoryTest()
        {
            ConnectionMessage();

            string bornDate = DateTime.Now.AddYears(-30).ToString("MMMM yyyy");
            string currentDate = DateTime.Now.ToString("MMMM yyyy");

            Console.WriteLine("Born on: " + bornDate);

            var completion = new GenericAiChat(Connection)
            {
                HttpClient =  
                {
                    CaptureRequestData = true
                }
            };

            // We have to provide the start date, otherwise it uses the AI training date 🤣
            string result = await completion.Complete([               
                new OpenAiChatMessage { content = "You are a helpful assistant that answers generic everyday questions precisely", role = "system"   },
                new OpenAiChatMessage { content = "My name is Rick and I was born in 1966 in Berlin, Germany.\nHow old am I on " + currentDate, role = "user"   },
            ]);

            Assert.IsNotNull(result, completion.ErrorMessage);
            Console.WriteLine(result);

            // continue conversion with the previous in chat history (uses completion.HttpClient.ChatHistory)
            result = await completion.Complete("Tell me about my birth city.", includeHistory: true);

            Assert.IsNotNull(result, completion.ErrorMessage);
            Console.WriteLine(result);

            
            Console.WriteLine("---\n" +completion.HttpClient.LastRequestJson);
            Console.WriteLine("---\n" + completion.HttpClient.LastResponseJson);
        }
    
        void ConnectionMessage()
        {
            Console.WriteLine("Using " + Connection.Name + " (" + Connection.ModelId + ")");
        }


        // This will get rejected by OpenAI/AzureOpenAi for safety viloations! Bah!
        // Works with local Ollama models
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
