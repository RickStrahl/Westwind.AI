using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Westwind.AI.Chat.Configuration
{
    public interface IAiCredentials
    {
        /// <summary>
        /// Name to identify 
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The API Key for the service
        /// </summary>
        string ApiKey { get; set; }

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

        AiAuthenticationModes AuthenticationMode { get; set; }

        bool IsEmpty { get; }
    }


    /// <summary>
    /// Base class that can be used to access OpenAI and Azure OpenAI
    /// or any other OpenAI based service like local Ollama interface.
    /// </summary>
    public class BaseAiCredentials : IAiCredentials
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The API Key for the service
        /// </summary>
        public string ApiKey { get; set; }

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

        public AiAuthenticationModes AuthenticationMode { get; set; } = AiAuthenticationModes.OpenAi;

        /// <summary>
        /// Determines whether the credentials are empty    
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(Endpoint);

        public override string ToString()
        {
            return Name ?? Endpoint;
        }
    }


    public class OpenAiCredentials : BaseAiCredentials
    {
        public OpenAiCredentials()
        {
            ModelId = "gpt-3.5-turbo";
            Endpoint = "https://api.openai.com/v1/";
            AuthenticationMode = AiAuthenticationModes.OpenAi;
            EndpointTemplate = OpenAiEndPointTemplates.OpenAi;
        }
    }

    public class AzureOpenAiCredentials : BaseAiCredentials
    {
        public AzureOpenAiCredentials()
        {
            AuthenticationMode = AiAuthenticationModes.AzureOpenAi;
            EndpointTemplate = OpenAiEndPointTemplates.AzureOpenAi;
            ApiVersion = "2024-02-15-preview";
        }
    }


    public enum AiAuthenticationModes
    {
        OpenAi,
        AzureOpenAi
    }

    public static class OpenAiEndPointTemplates
    {
        // 0 - EndPoint 1 - segment
        public const string OpenAi = "{0}/{1}";

        // 0 - EndPoint 1 - segment (ie. chat/completions) 2 - Model Id 3 - Api Version
        public const string AzureOpenAi = "{0}/openai/deployments/{2}/{1}?api-version={3}";
    }
}