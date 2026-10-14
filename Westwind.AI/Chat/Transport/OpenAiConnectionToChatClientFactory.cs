using System;
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using Westwind.AI.Configuration;

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
        public static IChatClient Create(IOpenAiConnection connection)
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
            return new OpenAIClient(credential, options).GetChatClient(modelId).AsIChatClient();
        }

        /// <summary>
        /// Derives the base URI expected by the OpenAI SDK from the connection's
        /// <see cref="IOpenAiConnection.Endpoint"/> and <see cref="IOpenAiConnection.ProviderMode"/>.
        ///
        /// Azure connections store only the resource base URL
        /// (e.g. <c>https://resource.cognitiveservices.azure.com/</c>), so we append
        /// <c>/openai/v1</c> to match the OpenAI-compatible inference path.  All other
        /// providers already include the API base path in their endpoint property.
        /// </summary>
        private static Uri BuildBaseUri(IOpenAiConnection connection)
        {
            var raw = Environment.ExpandEnvironmentVariables(connection.Endpoint ?? string.Empty).Trim();

            if (connection.ProviderMode == AiProviderModes.AzureOpenAi)
            {
                // Normalise: strip any trailing slash/segment already containing openai/v1
                var normalised = raw.TrimEnd('/');
                const string suffix = "/openai/v1";
                if (!normalised.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    normalised += suffix;
                return new Uri(normalised);
            }

            // All other providers: endpoint already points at the API base (e.g. /v1)
            return new Uri(raw.TrimEnd('/'));
        }
    }
}
