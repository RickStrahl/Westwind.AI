using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
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


        /// <summary>
        /// Optional Api Version typically used for Azure
        /// </summary>
        string ApiVersion { get; set; }


        /// <summary>
        /// Completions or Image operation
        /// </summary>
        AiOperationModes OperationMode { get; set;  }    


        /// <summary>
        /// Which AI provider is used for this connection
        /// </summary>
        AiProviderModes ProviderMode { get; set; }

        bool IsEmpty { get; }
    }


    /// <summary>
    /// Base class that can be used to access OpenAI and Azure OpenAI
    /// or any other OpenAI based service like local Ollama interface.
    /// </summary>
    public class BaseOpenAiConnection : IOpenAiConnection, INotifyPropertyChanged
    {
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
        public string ApiKey { 
            
            set 
            {
                if (!OpenAiConnectionConfiguration.UseEncryption || string.IsNullOrEmpty(value))
                {
                    _apiKey = value;
                    OnPropertyChanged();
                    return;
                }    
                
                // Already encrypted?
                if (value.EndsWith(OpenAiConnectionConfiguration.EncryptionPostFix))
                    _apiKey = value;
                else 
                    _apiKey = Encryption.EncryptString(value, OpenAiConnectionConfiguration.EncryptionKey, useBinHex: true) + OpenAiConnectionConfiguration.EncryptionPostFix;

                OnPropertyChanged();
            }
            get
            {
                return _apiKey;
            }
        }
        private string _apiKey;
        private string _name;
        private string _endpoint;
        private string _endpointTemplate = "{0}/{1}";
        private string _modelId;
        private string _apiVersion;
        private AiProviderModes _providerMode = AiProviderModes.OpenAi;
        private AiOperationModes _operationMode = AiOperationModes.Completions;

        [JsonIgnore]
        public string DecryptedApiKey
        {
            get
            {
                OnPropertyChanged();
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
        /// Determines whether the credentials are empty    
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty =>  string.IsNullOrEmpty(Endpoint);


        public override string ToString()
        {
            return Name ?? Endpoint;
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
        public static BaseOpenAiConnection Create(AiProviderModes providerMode, string name = null, bool isImageGeneration = false)
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
                default:
                    return new BaseOpenAiConnection() { Name = name };
            }
        }
        /// <summary>
        /// Creates a specific connection base on the connection mode
        /// </summary>
        /// <param name="connectionMode">string based connection mode</param>
        /// <returns></returns>
        public static BaseOpenAiConnection Create(string  connectionMode, bool isImageGen = false)
        {
            if(!Enum.TryParse<AiProviderModes>(connectionMode, out var mode))
                return new BaseOpenAiConnection();
            return Create(mode, isImageGeneration: isImageGen);
        }
    }


    public class OpenAiConnection : BaseOpenAiConnection
    {
        public OpenAiConnection()
        {
            ModelId = "gpt-3.5-turbo";
            Endpoint = "https://api.openai.com/v1/";
            ProviderMode = AiProviderModes.OpenAi;
            EndpointTemplate = OpenAiEndPointTemplates.OpenAi;
        }
    }

    public class AzureOpenAiConnection : BaseOpenAiConnection
    {
        public AzureOpenAiConnection()
        {
            ProviderMode = AiProviderModes.AzureOpenAi;
            EndpointTemplate = OpenAiEndPointTemplates.AzureOpenAi;
            ApiVersion = OpenAiEndPointTemplates.DefaultAzureApiVersion;
        }
    }

    public class OllamaOpenAiConnection : BaseOpenAiConnection
    {
        public OllamaOpenAiConnection()
        {
            ProviderMode = AiProviderModes.Ollama;
            EndpointTemplate = OpenAiEndPointTemplates.OpenAi;
            Endpoint= "http://127.0.0.1:11434/v1/";            
        }
    }



    public enum AiProviderModes
    {
        OpenAi,
        AzureOpenAi,
        Ollama,
        Other
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
        public static string DefaultAzureApiVersion = "2024-02-15-preview";
    }
}