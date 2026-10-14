using System.Collections.Generic;
using System.IO;
using Westwind.AI.Configuration;
using Westwind.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Westwind.AI;
using Westwind.AI.Chat;

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


            Console.WriteLine(JsonSerializationUtils.Serialize(config, formatJsonOutput: true));

            foreach(var conn in config.Connections)
            {
                Console.WriteLine(conn.Name + " " + conn.ApiKey);                 
            }

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
            
            config.Connections.AddRange(new List<OpenAiConnection>
            {
                new OpenAiConnection()
                {
                    Name = "OpenAI",
                    ApiKey = "sk-....",
                    ProviderMode = AiProviderModes.OpenAi
                },

                new AzureOpenAiConnection()
                {
                    Name = "OpenAI",
                    Endpoint = "https://yourazuresite.openai.azure.com/",
                    ModelId = "Gpt35",
                    ApiKey = "123....",
                    ProviderMode = AiProviderModes.AzureOpenAi
                },
                new OpenAiConnection()
                {
                    Name = "Ollama llama3",
                    ModelId = "llama3",
                    ProviderMode = AiProviderModes.OpenAi
                },
                new OpenAiConnection()
                {
                    Name = "Ollama Phi3",
                    ModelId = "phi3",
                    ProviderMode = AiProviderModes.OpenAi
                },
            }.AsReadOnly());

            config.Save();
        }

        [TestMethod]
        public void CreateProviderTest()
        {
            var apiKey = "sk-superseekrit";
            var connection = OpenAiConnection.Create(AiProviderModes.OpenAi, "Open AI Connection");
            connection.ApiKey = apiKey;
            // connection.ModelId = "gpt-4o-mini";  // default
            Console.WriteLine(connection.ApiKey);            
            Console.WriteLine(JsonSerializationUtils.Serialize(connection, false, true));

            Assert.IsTrue(connection.ProviderMode == AiProviderModes.OpenAi,"Incorrect Provider Mode");
            Assert.IsTrue(connection.OperationMode == AiOperationModes.Completions,"Incorrect Operation Mode");
            Assert.IsTrue(connection.ModelId == "gpt-4o-mini","Incorrect Model");
            
        }

        [TestMethod]
        public void DefaultApiKeyHeaderTest()
        {
            var connection = new OpenAiConnection
            {
                ApiKey = "test-key"
            };

            using var client = new OpenAiHttpClient(connection).GetHttpClient();

            Assert.AreEqual("Bearer", client.DefaultRequestHeaders.Authorization?.Scheme);
            Assert.AreEqual("test-key", client.DefaultRequestHeaders.Authorization?.Parameter);
        }

        [TestMethod]
        public void CustomApiKeyHeaderTest()
        {
            var connection = new OpenAiConnection
            {
                ApiKey = "google-test-key",
                ApiKeyHeaderName = "x-goog-api-key",
                ApiKeyHeaderScheme = null
            };

            using var client = new OpenAiHttpClient(connection).GetHttpClient();

            Assert.IsNull(client.DefaultRequestHeaders.Authorization);
            Assert.AreEqual(
                "google-test-key",
                client.DefaultRequestHeaders.GetValues("x-goog-api-key").Single());
        }

        [TestMethod]
        public void AzureEndpointEnvironmentVariableTest()
        {
            const string variableName = "WESTWIND_AI_TEST_SITE";
            var previousValue = Environment.GetEnvironmentVariable(variableName);

            try
            {
                Environment.SetEnvironmentVariable(variableName, "https://test-resource.cognitiveservices.azure.com");
                var connection = new AzureOpenAiConnection
                {
                    Endpoint = $"%{variableName}%"
                };

                var endpoint = new OpenAiHttpClient(connection).GetEndpointUrl("chat/completions");
                Assert.AreEqual(
                    "https://test-resource.cognitiveservices.azure.com/openai/v1/chat/completions",
                    endpoint);

                // Construction exercises the Microsoft.Extensions.AI transport path.
                var client = new GenericAiChatClient(connection);
                Assert.IsNotNull(client);
            }
            finally
            {
                Environment.SetEnvironmentVariable(variableName, previousValue);
            }
        }

        [TestMethod]
        public void ApiKeyEnvironmentVariableExpandsWhenEncryptionIsDisabledTest()
        {
            const string variableName = "WESTWIND_AI_TEST_API_KEY";
            var previousValue = Environment.GetEnvironmentVariable(variableName);
            var previousEncryptionSetting = OpenAiConnectionConfiguration.UseApiKeyEncryption;

            try
            {
                Environment.SetEnvironmentVariable(variableName, "expanded-test-key");
                OpenAiConnectionConfiguration.UseApiKeyEncryption = false;

                var connection = new OpenAiConnection
                {
                    ApiKey = $"%{variableName}%"
                };

                Assert.AreEqual("expanded-test-key", connection.ApiKey);
            }
            finally
            {
                OpenAiConnectionConfiguration.UseApiKeyEncryption = previousEncryptionSetting;
                Environment.SetEnvironmentVariable(variableName, previousValue);
            }
        }
    }
}