using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Westwind.AI.Chat;
using Westwind.AI.Images;
using Westwind.Utilities;

namespace Westwind.AI.Configuration
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

        /// <summary>
        /// Optional Api Version typically used for Azure
        /// </summary>
        string ApiVersion { get; set; }

        /// <summary>
        /// Completions or Image operation
        /// </summary>
        AiOperationModes OperationMode { get; set; }

        /// <summary>
        /// Which AI provider is used for this connection
        /// </summary>
        AiProviderModes ProviderMode { get; set; }

        bool IsEmpty { get; }
    }


    /// <summary>
    /// Base AI configuration class that contains all the base settings 
    /// required to connect to OpenAI. The default uses the bona fide
    /// OpenAi (company) API settings for defaults.
    /// 
    /// Subclasses of this class simply override default settings and 
    /// provide additional Intellisense information for specific 
    /// settings and providers.
    /// </summary>
    public class OpenAiConnection : IOpenAiConnection, INotifyPropertyChanged
    {

        public OpenAiConnection()
        {
            ModelId = "gpt-4o-mini";
            Endpoint = "https://api.openai.com/v1/";
            ProviderMode = AiProviderModes.OpenAi;
            OperationMode = AiOperationModes.Completions;
            EndpointTemplate = OpenAiEndPointTemplates.OpenAi;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The Encrypted API Key for the service
        /// </summary>
        [JsonIgnore]        
        public string ApiKey
        {

            set
            {
                _apiKey = EncryptApiKey(value);
                OnPropertyChanged();
            }
            get
            {
                return DecryptApiKey(_apiKey);
            }
        }
        [NonSerialized]
        private string _apiKey;

        /// <summary>
        /// The Encrypted API Key for the connection
        /// that is used for storing the key in a configuration file.
        /// 
        /// Value is here mainly for serialization purposes
        /// </summary>
        [JsonProperty("EncryptedApiKey")] 
        internal string EncryptedApiKey
        {
            get { return _apiKey; }
            set { 
                _apiKey = value;  
            }
        }
        

        private string _name;
        private string _endpoint;
        private string _endpointTemplate = "{0}/{1}";
        private string _modelId;
        private string _apiVersion;
        private AiProviderModes _providerMode = AiProviderModes.OpenAi;
        private AiOperationModes _operationMode = AiOperationModes.Completions;

        

        /// <summary>
        /// The Endpoint for the service
        /// </summary>
        public string Endpoint
        {
            get => _endpoint;
            set
            {
                if (value == _endpoint) return;
                _endpoint = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        /// <summary>
        /// Endpoint URL template that configures the endpoint format.
        /// {0}  - Operation Segment
        /// {1}  - Model Id (Azure)
        /// {2}  - API Version  (Azure)
        /// </summary>
        public string EndpointTemplate
        {
            get => _endpointTemplate;
            set
            {
                if (value == _endpointTemplate) return;
                _endpointTemplate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Model Id used for Azure OpenAI
        /// </summary>
        public string ModelId
        {
            get => _modelId;
            set
            {
                if (value == _modelId) return;
                _modelId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// An optional API version (used for Azure)
        /// </summary>
        public string ApiVersion
        {
            get => _apiVersion;
            set
            {
                if (value == _apiVersion) return;
                _apiVersion = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines whether this is an OpenAI or Azure connection
        /// </summary>
        public AiProviderModes ProviderMode
        {
            get => _providerMode;
            set
            {
                if (value == _providerMode) return;
                _providerMode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines whether we're using completions or image generation 
        /// </summary>
        public AiOperationModes OperationMode
        {
            get => _operationMode;
            set
            {
                if (value == _operationMode) return;
                _operationMode = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// Retrieves a Chat Client Instance
        /// </summary>
        /// <returns></returns>
        public GenericAiChatClient GetChatClient()
        {
            var completion = new GenericAiChatClient(this);
            return completion;
        }

        /// <summary>
        /// Retrieves an Image Generation Client
        /// </summary>
        /// <returns></returns>
        public OpenAiImageGeneration GetImageGenerationClient()
        {
            var client = new OpenAiImageGeneration(this);
            return client;
        }


        /// <summary>
        /// Determines whether the credentials are empty    
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(Endpoint);


        public override string ToString()
        {
            return $"{Name} - {ProviderMode} - {ModelId} - {Endpoint}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Creates a specific connection base on the connection mode
        /// </summary>
        /// <param name="providerMode"></param>
        /// <returns></returns>
        public static OpenAiConnection Create(AiProviderModes providerMode, string name = null, bool isImageGeneration = false)
        {
            if (name == null)
                name = "OpenAI Connection " + DataUtils.GenerateUniqueId(5);

            switch (providerMode)
            {
                case AiProviderModes.OpenAi:
                    return new OpenAiConnection() { Name = name, ModelId = isImageGeneration ? "dall-e-3" : "gpt-4o-mini" };
                case AiProviderModes.AzureOpenAi:
                    return new AzureOpenAiConnection() { Name = "Azure OpenAI Connection " + DataUtils.GenerateUniqueId(5) };
                case AiProviderModes.Ollama:
                    return new OllamaOpenAiConnection() { Name = "Ollama Connection " + DataUtils.GenerateUniqueId(5) };
                case AiProviderModes.Nvidia:
                    return new NvidiaOpenAiConnection() { Name = "Nvidia Connection " + DataUtils.GenerateUniqueId(5) };
                case AiProviderModes.XOpenAi:
                    return new XOpenAiConnection() { Name = "XConnection " + DataUtils.GenerateUniqueId(5) };
                case AiProviderModes.DeepSeek:
                    return new DeepSeekAiConnection() { Name = "DeepSeek Connection " + DataUtils.GenerateUniqueId(5) };
                default:
                    return new OpenAiConnection() { Name = name };
            }
        }
        /// <summary>
        /// Creates a specific connection base on the connection mode
        /// </summary>
        /// <param name="providerMode">string based connection mode</param>
        /// <returns></returns>
        public static OpenAiConnection Create(string providerMode, bool isImageGen = false)
        {
            if (!Enum.TryParse<AiProviderModes>(providerMode, out var mode))
                return new OpenAiConnection(); // default to OpenAi
            return Create(mode, isImageGeneration: isImageGen);
        }

        /// <summary>
        /// Encrypts an API key if encryption is enabled
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal string EncryptApiKey(string key)
        {            
            if (!OpenAiConnectionConfiguration.UseApiKeyEncryption ||
                string.IsNullOrEmpty(key) ||
                key.EndsWith(OpenAiConnectionConfiguration.EncryptionPostFix))
            {
                return key ?? string.Empty;
            }

            return Encryption.EncryptString(key, OpenAiConnectionConfiguration.EncryptionKey, useBinHex: true) + OpenAiConnectionConfiguration.EncryptionPostFix;
        }

        /// <summary>
        /// Returns a decrypted API key. Key also expands % % Environment Variables
        /// </summary>
        /// <param name="key">Optional - the encrypted (or unencrypted) key to return. If not passed uses this connections API key</param>
        /// <returns></returns>
        internal string DecryptApiKey(string key = null)
        {
            if (string.IsNullOrEmpty(key))
                key = _apiKey;

            if (string.IsNullOrEmpty(key) || !OpenAiConnectionConfiguration.UseApiKeyEncryption)
                return key ?? string.Empty;

            if (key.EndsWith(OpenAiConnectionConfiguration.EncryptionPostFix))
            {
                var encrypted = key.Replace(OpenAiConnectionConfiguration.EncryptionPostFix, string.Empty);
                key = Encryption.DecryptString(encrypted, OpenAiConnectionConfiguration.EncryptionKey, useBinHex: true);
            }
            else
            {
                // force encrypt the Api Key for storage direct access
                _apiKey = EncryptApiKey(key);
            }

            // Environment variables
            if (key.Contains("%"))
            {
                key = Environment.ExpandEnvironmentVariables(key);
            }

            return key;
        }

    }

    /// <summary>
    /// Azure Open AI specific connection.
    /// 
    /// Uses:
    /// Endpoint - Azure OpenAI Base Site Url (without deployment and operation paths)
    /// ModelId - Name of the deployment
    /// ApiVersion - Version of the Azure API (default provided)
    /// </summary>
    public class AzureOpenAiConnection : OpenAiConnection
    {
        public AzureOpenAiConnection()
        {
            ProviderMode = AiProviderModes.AzureOpenAi;
            EndpointTemplate = OpenAiEndPointTemplates.AzureOpenAi;
            ApiVersion = OpenAiEndPointTemplates.DefaultAzureApiVersion;
        }
    }

    public class OllamaOpenAiConnection : OpenAiConnection
    {
        public OllamaOpenAiConnection()
        {
            ProviderMode = AiProviderModes.Ollama;
            OperationMode = AiOperationModes.Completions;
            EndpointTemplate = OpenAiEndPointTemplates.OpenAi;
            Endpoint = "http://127.0.0.1:11434/v1/";
            ModelId = "llama3";
        }
    }

    public class XOpenAiConnection : OpenAiConnection
    {
        public XOpenAiConnection()
        {
            ProviderMode = AiProviderModes.Other;
            OperationMode = AiOperationModes.Completions;
            EndpointTemplate = OpenAiEndPointTemplates.OpenAi;
            Endpoint = "https://api.x.ai/v1/";
            ModelId = "grok-beta";
        }
    }

    public class NvidiaOpenAiConnection : OpenAiConnection
    {
        public NvidiaOpenAiConnection()
        {
            ProviderMode = AiProviderModes.Nvidia;
            OperationMode = AiOperationModes.Completions;
            EndpointTemplate = OpenAiEndPointTemplates.OpenAi;
            Endpoint = "https://integrate.api.nvidia.com/v1/";
            ModelId = "meta/llama-3.1-405b-instruct";
        }
    }

    public class DeepSeekAiConnection : OpenAiConnection
    {
        public DeepSeekAiConnection()
        {
            ProviderMode = AiProviderModes.DeepSeek;
            OperationMode = AiOperationModes.Completions;
            EndpointTemplate = OpenAiEndPointTemplates.OpenAi;
            Endpoint = "https://api.deepseek.com";
            ModelId = "deepseek-chat";
        }
    }


    public enum AiProviderModes
    {
        OpenAi,
        AzureOpenAi,
        Ollama,
        Nvidia,
        XOpenAi,
        Other,
        DeepSeek
    }

    public enum AiOperationModes
    {
        Completions,
        ImageGeneration
    }

    public static class OpenAiEndPointTemplates
    {
        // 0 - EndPoint 1 - segment
        public static string OpenAi = "{0}/{1}";

        // 0 - EndPoint 1 - segment (ie. chat/completions) 2 - Model Id 3 - Api Version
        public static string AzureOpenAi = "{0}/openai/deployments/{2}/{1}?api-version={3}";

        /// <summary>
        /// Azure API Version
        /// </summary>
        public static string DefaultAzureApiVersion = "2024-04-01";
    }


}