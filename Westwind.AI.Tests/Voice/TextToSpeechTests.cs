using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Westwind.AI.Configuration;
using Westwind.AI.Images;
using Westwind.AI.Voice;
using Westwind.Utilities;

namespace Westwind.AI.Tests.Voice
{
    [TestClass]
    public class TextToSpeechTests
    {
        public TextToSpeechTests()
        {
            // Load confingurations from disk
            Configurations = OpenAiConnectionConfiguration.Load();

            // Note: for Azure you need a separate deployment for Dall-E-3 specific models
            Connection = Configurations.ActiveConnection; // Configurations["Azure OpenAi Dall-E"];
            // Connection = Configurations["OpenAI Dall-E"];

            if (Connection == null)
                throw new InvalidOperationException("No Dall-E-3 configuration found.");

            ImagePrompt.DefaultImageStoragePath = Path.GetFullPath("images/GeneratedImages");
        }

        public OpenAiConnection Connection { get; set; }
        public OpenAiConnectionConfiguration Configurations { get; set; }

        [TestMethod]
        public async Task TextToSpeechTest()
        {
            string file = @"c:\temp\Test.mp3";
            var generator = new OpenAiTextToSpeech(Connection);

            bool result = await generator.ConvertTextToSpeechFileAsync("The sky is below and the ground is above, the judges are criminals the wolf's a dove", "HelloWorld.mp3");

            Assert.IsTrue(result, generator.ErrorMessage);

            ShellUtils.GoUrl(file);

        }
    }
}
