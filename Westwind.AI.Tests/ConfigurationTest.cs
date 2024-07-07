using Westwind.AI.Chat.Configuration;
using Westwind.Utilities;

namespace Westwind.AI.Tests
{
    [TestClass]
    public class ConfigurationTest
    {
        [TestMethod]
        public void LoadAndEncryptConfigurationTest()
        {             
            // load configuration from default file
            var config = OpenAiConnectionConfiguration.Load();
            config.Save(); // write back out with encrypted keys

            // Opens in JSON editor and lets you copy the encrypted api keys/file into project
            ShellUtils.ShellExecute(Path.GetFullPath("_AiAuthenticationConfiguration.json"));

            Assert.IsNotNull(config);
            Assert.IsTrue(config.Connections.Count > 0);
        }


        /// <summary>
        /// Example that allows you to create a configuration file
        /// </summary>
        [TestMethod]
        public void CreateConfigurationTest()
        {
            var config = new OpenAiConnectionConfiguration();
            
            config.Connections.AddRange([

                new OpenAiConnection()
                {
                    Name = "OpenAI",
                    ApiKey = "sk-....",
                    ConnectionMode = AiConnectionModes.OpenAi
                },

                new AzureOpenAiConnection()
                {
                    Name = "OpenAI",
                    Endpoint = "https://yourazuresite.openai.azure.com/",
                    ModelId = "Gpt35",
                    ApiKey = "123....",
                    ConnectionMode = AiConnectionModes.AzureOpenAi
                },
                new OpenAiConnection()
                {
                    Name = "Ollama llama3",
                    ModelId = "llama3",                    
                    ConnectionMode = AiConnectionModes.OpenAi
                },
                new OpenAiConnection()
                {
                    Name = "Ollama Phi3",
                    ModelId = "phi3",                    
                    ConnectionMode = AiConnectionModes.OpenAi
                },
            ]);

            config.Save();
        }
    }
}