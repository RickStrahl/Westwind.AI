using System;
using Westwind.AI.Configuration;

namespace Westwind.AI.Chat.Transport
{
    /// <summary>
    /// Central creation point for internal chat transports.
    ///
    /// Supported providers are routed through <see cref="MicrosoftAiChatTransport"/>.
    /// Providers that cannot be addressed via the Microsoft.Extensions.AI/OpenAI SDK
    /// fall back to <see cref="OpenAiHttpFallbackTransport"/>.
    /// </summary>
    internal static class WestwindAiChatTransportFactory
    {
        public static IWestwindAiChatTransport Create(IOpenAiConnection connection, bool preferMicrosoftTransport = true)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (preferMicrosoftTransport && SupportsMicrosoftTransport(connection))
                return new MicrosoftAiChatTransport(connection);

            return new OpenAiHttpFallbackTransport(connection);
        }

        /// <summary>
        /// Returns <c>true</c> when the connection's provider can be addressed via
        /// the Microsoft.Extensions.AI / OpenAI SDK (i.e. uses an OpenAI-compatible
        /// HTTP API with bearer authentication).
        /// </summary>
        public static bool SupportsMicrosoftTransport(IOpenAiConnection connection)
        {
            if (connection == null)
                return false;

            if (connection.OperationMode != AiOperationModes.Completions)
                return false;

            return connection.ProviderMode == AiProviderModes.OpenAi ||
                   connection.ProviderMode == AiProviderModes.AzureOpenAi ||
                   connection.ProviderMode == AiProviderModes.GitHubModels ||
                   connection.ProviderMode == AiProviderModes.Ollama ||
                   connection.ProviderMode == AiProviderModes.OllamaCloud ||
                   connection.ProviderMode == AiProviderModes.OpenRouterAi ||
                   connection.ProviderMode == AiProviderModes.Gemini ||
                   connection.ProviderMode == AiProviderModes.Mistral ||
                   connection.ProviderMode == AiProviderModes.Perplexity ||
                   connection.ProviderMode == AiProviderModes.DeepSeek ||
                   connection.ProviderMode == AiProviderModes.Nvidia ||
                   connection.ProviderMode == AiProviderModes.XOpenAi ||
                   connection.ProviderMode == AiProviderModes.Other;
        }
    }
}
