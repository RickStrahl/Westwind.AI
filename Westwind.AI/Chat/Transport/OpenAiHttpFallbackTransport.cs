using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Westwind.AI.Configuration;

namespace Westwind.AI.Chat.Transport
{
    /// <summary>
    /// Existing HTTP transport wrapped behind the new transport abstraction.
    /// </summary>
    internal class OpenAiHttpFallbackTransport : IWestwindAiChatTransport
    {
        private readonly OpenAiHttpClient _httpClient;

        public IOpenAiConnection Connection { get; }

        public OpenAiHttpFallbackTransport(IOpenAiConnection connection)
        {
            Connection = connection;
            _httpClient = new OpenAiHttpClient(connection);
        }

        public async Task<ChatTransportResponse> CompleteAsync(string prompt, string systemPrompt = null, bool includeHistory = false, CancellationToken cancellationToken = default)
        {
            var result = await _httpClient.GetChatAiResponse(prompt, systemPrompt, includeHistory).ConfigureAwait(false);
            return new ChatTransportResponse
            {
                Text = result,
                ErrorMessage = result == null ? _httpClient.ErrorMessage : null,
                NativeResponse = _httpClient.LastChatResponse
            };
        }

        public async Task<ChatTransportResponse> CompleteAsync(IEnumerable<OpenAiChatMessage> messages, bool includeHistory = false, CancellationToken cancellationToken = default)
        {
            var result = await _httpClient.GetChatAiResponse(messages, includeHistory).ConfigureAwait(false);
            return new ChatTransportResponse
            {
                Text = result,
                ErrorMessage = result == null ? _httpClient.ErrorMessage : null,
                NativeResponse = _httpClient.LastChatResponse
            };
        }
    }
}
