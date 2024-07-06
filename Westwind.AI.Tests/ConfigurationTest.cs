using Westwind.AI.Chat.Configuration;

namespace Westwind.AI.Tests
{
    [TestClass]
    public class ConfigurationTest
    {
        [TestMethod]
        public void LoadConfigurationTest()
        {             
            // load configuration from default file
            var config = AiAuthenticationConfiguration.Load();
            Assert.IsNotNull(config);
            Assert.IsTrue(config.AiCredentials.Count > 0);
        }

        [TestMethod]
        public void CreateConfigurationTest()
        {
            var config = new AiAuthenticationConfiguration();
            
            config.AiCredentials.AddRange([

                new OpenAiCredentials()
                {
                    Name = "OpenAI",
                    ApiKey = "sk-....",
                    AuthenticationMode = AiAuthenticationModes.OpenAi
                },

                new AzureOpenAiCredentials()
                {
                    Name = "OpenAI",
                    Endpoint = "https://yourazuresite.openai.azure.com/",
                    ModelId = "Gpt35",
                    ApiKey = "123....",
                    AuthenticationMode = AiAuthenticationModes.AzureOpenAi
                },
                new OpenAiCredentials()
                {
                    Name = "Ollama llama3",
                    ModelId = "llama3",                    
                    AuthenticationMode = AiAuthenticationModes.OpenAi
                },
                new OpenAiCredentials()
                {
                    Name = "Ollama Phi3",
                    ModelId = "phi3",                    
                    AuthenticationMode = AiAuthenticationModes.OpenAi
                },
            ]);

            config.Save();
        }
    }
}