using System.Collections.Generic;
using System.IO;
using Westwind.AI.Configuration;
using Westwind.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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
    }
}