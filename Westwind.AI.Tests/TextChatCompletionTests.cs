using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Westwind.AI.Chat;
using Westwind.AI.Chat.Configuration;

namespace Westwind.AI.Tests
{
    [TestClass]
    public class TextChatCompletionTests
    {
        public TextChatCompletionTests()
        {
           Configuration = AiAuthenticationConfiguration.Load().ActiveCredential;
        }

        public IAiCredentials Configuration { get; set; }

        [TestMethod]
        public async Task TranslationTest()
        {

            var translator = new AiTranslator(Configuration);
            translator.ChatHttpClient.CaptureRequestData = true;

            Console.WriteLine("Using: " + Configuration.Name);

            string result = await translator.TranslateText("The sky is below, the ground is above", "en", "de");

            Console.WriteLine(translator.ChatHttpClient.LastRequestJson);
            Console.WriteLine("\n\n" + translator.ChatHttpClient.LastResponseJson);

            Assert.IsNotNull(result, translator.ErrorMessage);
            Console.WriteLine(result);
        }

        [TestMethod]
        public async Task GrammarCheckTest()
        {
            var orig = "Long story short one of the use cases that usually made me grab for the Newtonsoft library was dynamic parsing, but I'm glad to see that at some time at least some minimal support for dynamic parsing was added to the `System.Text.Json.JsonSerializer` class";
            var checker = new AiGrammarChecker(Configuration);
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
            var checker = new AiGrammarChecker(Configuration);
            var result = await checker.CheckGrammarAsDiff(orig);

            Assert.IsNotNull(result, checker.ErrorMessage);

            Console.WriteLine("Original:\n" + orig);

            Console.WriteLine("\nAdjusted:\n");
            Console.WriteLine(result);
        }


    }
}
