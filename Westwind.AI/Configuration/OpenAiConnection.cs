﻿using System;
using System.Text;
using Newtonsoft.Json;
using Westwind.Utilities;

namespace Westwind.AI.Chat.Configuration
{

    /// <summary>
    /// Interface that defines an OpenAI connection on what's
    /// needed to connect to the API including endpoint, model, and API key.
    /// </summary>
    public interface IOpenAiConnection
    {
        /// <summary>
        /// Name to identify 
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The API Key for the service
        /// </summary>
        string ApiKey { get; set; }

        string DecryptedApiKey { get; }

        /// <summary>
        /// The Endpoint for the service
        /// </summary>
        string Endpoint { get; set; }

        /// <summary>
        /// Template that can be used to access
        /// </summary>
        string EndpointTemplate { get; set; }

        /// <summary>
        /// Model Id used for Azure OpenAI
        /// </summary>
        string ModelId { get; set; }

        string ApiVersion { get; set; }

        AiOperationModes OperationMode { get; set;  }    

        AiConnectionModes ConnectionMode { get; set; }

        bool IsEmpty { get; }
    }


    /// <summary>
    /// Base class that can be used to access OpenAI and Azure OpenAI
    /// or any other OpenAI based service like local Ollama interface.
    /// </summary>
    public class BaseOpenAiConnection : IOpenAiConnection
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The Encrypted API Key for the service
        /// </summary>
        public string ApiKey { 
            
            set 
            {
                if (!OpenAiConnectionConfiguration.UseEncryption || string.IsNullOrEmpty(value))
                {
                    _apiKey = value;
                    return;
                }    
                
                // Already encrypted?
                if (value.EndsWith(OpenAiConnectionConfiguration.EncryptionPostFix))
                    _apiKey = value;
                else 
                    _apiKey = Encryption.EncryptString(value, OpenAiConnectionConfiguration.EncryptionKey, useBinHex: true) + OpenAiConnectionConfiguration.EncryptionPostFix;                
            }
            get
            {
                return _apiKey;
            }
        }
        private string _apiKey;

        [JsonIgnore]
        public string DecryptedApiKey
        {
            get
            {
                if (!OpenAiConnectionConfiguration.UseEncryption || string.IsNullOrEmpty(_apiKey))
                    return _apiKey;

                var encrypted = _apiKey.Replace(OpenAiConnectionConfiguration.EncryptionPostFix, string.Empty);
                var decrypted = Encryption.DecryptString(encrypted, OpenAiConnectionConfiguration.EncryptionKey, useBinHex:true);
                return decrypted;
            }
        }

        /// <summary>
        /// The Endpoint for the service
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Endpoint URL template that configures the endpoint format.
        /// {0}  - Operation Segment
        /// {1}  - Model Id (Azure)
        /// {2}  - API Version  (Azure)
        /// </summary>
        public string EndpointTemplate { get; set; } = "{0}/{1}";

        /// <summary>
        /// Model Id used for Azure OpenAI
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// An optional API version (used for Azure)
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        /// Determines whether this is an OpenAI or Azure connection
        /// </summary>
        public AiConnectionModes ConnectionMode { get; set; } = AiConnectionModes.OpenAi;

        /// <summary>
        /// Determines whether we're using completions or image generation 
        /// </summary>
        public AiOperationModes OperationMode { get; set; } = AiOperationModes.Completions;
                

        /// <summary>
        /// Determines whether the credentials are empty    
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty =>  string.IsNullOrEmpty(Endpoint);


        public override string ToString()
        {
            return Name ?? Endpoint;
        }
    }


    public class OpenAiConnection : BaseOpenAiConnection
    {
        public OpenAiConnection()
        {
            ModelId = "gpt-3.5-turbo";
            Endpoint = "https://api.openai.com/v1/";
            ConnectionMode = AiConnectionModes.OpenAi;
            EndpointTemplate = OpenAiEndPointTemplates.OpenAi;
        }
    }

    public class AzureOpenAiConnection : BaseOpenAiConnection
    {
        public AzureOpenAiConnection()
        {
            ConnectionMode = AiConnectionModes.AzureOpenAi;
            EndpointTemplate = OpenAiEndPointTemplates.AzureOpenAi;
            ApiVersion = "2024-02-15-preview";
        }
    }



    public enum AiConnectionModes
    {
        OpenAi,
        AzureOpenAi
    }

    public enum AiOperationModes
    {
        Completions,
        ImageGeneration
    }

    public static class OpenAiEndPointTemplates
    {
        // 0 - EndPoint 1 - segment
        public const string OpenAi = "{0}/{1}";

        // 0 - EndPoint 1 - segment (ie. chat/completions) 2 - Model Id 3 - Api Version
        public const string AzureOpenAi = "{0}/openai/deployments/{2}/{1}?api-version={3}";
    }
}