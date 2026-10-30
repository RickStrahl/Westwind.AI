using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using OpenAI;
using Westwind.AI.Configuration;
using Westwind.Utilities;

namespace Westwind.AI.Chat.Transport
{
    /// <summary>
    /// Creates a Microsoft.Extensions.AI <see cref="IChatClient"/> from an
    /// <see cref="IOpenAiConnection"/>.  Uses the OpenAI SDK for all providers
    /// (including Azure) because every supported endpoint is OpenAI-API-compatible.
    /// </summary>
    internal static class OpenAiConnectionToChatClientFactory
    {
        /// <summary>
        /// Creates a configured <see cref="IChatClient"/> from the supplied connection.
        /// </summary>
        public static IChatClient Create(IOpenAiConnection connection, ChatRequestCapturePolicy capturePolicy = null)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var apiKey   = connection.ApiKey ?? string.Empty;
            var modelId  = connection.ModelId ?? string.Empty;
            var endpoint = BuildBaseUri(connection);

            // A non-empty credential is required by the SDK; use a placeholder for
            // providers that authenticate via other means (e.g. Ollama with no key).
            var credential = new ApiKeyCredential(string.IsNullOrEmpty(apiKey) ? "nokey" : apiKey);

            var options = new OpenAIClientOptions { Endpoint = endpoint };
            if (capturePolicy != null)
                options.AddPolicy(capturePolicy, PipelinePosition.BeforeTransport);
            return new OpenAIClient(credential, options).GetChatClient(modelId).AsIChatClient();
        }

        /// <summary>
        /// Derives the base URI expected by the OpenAI SDK from the connection's
        /// <see cref="IOpenAiConnection.Endpoint"/>, <see cref="IOpenAiConnection.ProviderMode"/>
        /// and <see cref="IOpenAiConnection.EndpointTemplate"/>.
        ///
        /// Azure connections store only the resource base URL
        /// (e.g. <c>https://resource.cognitiveservices.azure.com/</c>), so we append
        /// <c>/openai/v1</c> to match the OpenAI-compatible inference path. Older
        /// configurations may identify this path only in EndpointTemplate, since
        /// their ConnectionMode property does not populate ProviderMode.
        /// </summary>
        private static Uri BuildBaseUri(IOpenAiConnection connection)
        {            
            var endpoint = Environment.ExpandEnvironmentVariables(connection.Endpoint ?? string.Empty).Trim();

            if (connection.ProviderMode == AiProviderModes.AzureOpenAi ||
                connection.EndpointTemplate?.Contains("/openai/v1/", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Normalise: strip any trailing slash/segment already containing openai/v1
                var normalised = endpoint.TrimEnd('/');
                const string suffix = "/openai/v1";
                if (!normalised.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    normalised += suffix;
                return new Uri(normalised);
            }

            // All other providers: endpoint already points at the API base (e.g. /v1)
            return new Uri(endpoint.TrimEnd('/'));
        }
    }
}
