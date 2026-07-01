using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Westwind.AI.Configuration;

namespace Westwind.AI.Chat.Transport
{
    /// <summary>
    /// Internal transport abstraction for chat completion execution.
    /// </summary>
    internal interface IWestwindAiChatTransport
    {
        IOpenAiConnection Connection { get; }

        Task<ChatTransportResponse> CompleteAsync(string prompt, string systemPrompt = null, bool includeHistory = false, CancellationToken cancellationToken = default);

        Task<ChatTransportResponse> CompleteAsync(IEnumerable<OpenAiChatMessage> messages, bool includeHistory = false, CancellationToken cancellationToken = default);
    }
}
