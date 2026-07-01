using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Westwind.AI.Configuration;

namespace Westwind.AI.Chat.Transport
{
    /// <summary>
    /// Microsoft.Extensions.AI-backed chat transport.  Mirrors the history-tracking
    /// and deduplication behaviour of <see cref="OpenAiHttpClient"/> while routing
    /// execution through an <see cref="IChatClient"/>.
    /// </summary>
    internal class MicrosoftAiChatTransport : IWestwindAiChatTransport
    {
        private readonly IChatClient _chatClient;
        private readonly List<ChatMessage> _chatHistory = new List<ChatMessage>();

        public IOpenAiConnection Connection { get; }

        public MicrosoftAiChatTransport(IOpenAiConnection connection, IChatClient chatClient = null)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _chatClient = chatClient ?? OpenAiConnectionToChatClientFactory.Create(connection);
        }

        // ------------------------------------------------------------------
        // IWestwindAiChatTransport
        // ------------------------------------------------------------------

        public Task<ChatTransportResponse> CompleteAsync(
            string prompt,
            string systemPrompt = null,
            bool includeHistory = false,
            CancellationToken cancellationToken = default)
        {
            var messages = new List<ChatMessage>();

            if (!string.IsNullOrEmpty(systemPrompt))
                messages.Add(new ChatMessage(ChatRole.System, systemPrompt));

            messages.Add(new ChatMessage(ChatRole.User, prompt));

            return ExecuteAsync(messages, includeHistory, cancellationToken);
        }

        public Task<ChatTransportResponse> CompleteAsync(
            IEnumerable<OpenAiChatMessage> messages,
            bool includeHistory = false,
            CancellationToken cancellationToken = default)
        {
            if (messages == null)
                return Task.FromResult(new ChatTransportResponse { ErrorMessage = "No messages provided for chat request." });

            var chatMessages = messages.Select(ChatMessageMapper.ToMicrosoftAiMessage).ToList();
            return ExecuteAsync(chatMessages, includeHistory, cancellationToken);
        }

        // ------------------------------------------------------------------
        // Internal execution core
        // ------------------------------------------------------------------

        private async Task<ChatTransportResponse> ExecuteAsync(
            List<ChatMessage> incomingMessages,
            bool includeHistory,
            CancellationToken cancellationToken)
        {
            if (incomingMessages == null || incomingMessages.Count == 0)
                return new ChatTransportResponse { ErrorMessage = "No messages provided for chat request." };

            var sw = Stopwatch.StartNew();

            try
            {
                var sendMessages = BuildSendList(incomingMessages, includeHistory);

                var options = new ChatOptions { ModelId = Connection.ModelId };
                var completion = await _chatClient.GetResponseAsync(sendMessages, options, cancellationToken)
                    .ConfigureAwait(false);

                sw.Stop();

                var responseText = completion?.Text;

                // Persist the assistant reply to history
                if (completion?.Messages != null)
                {
                    foreach (var message in completion.Messages)
                        _chatHistory.Add(message);
                }

                return new ChatTransportResponse
                {
                    Text         = responseText,
                    TimeTaken    = sw.Elapsed,
                    NativeResponse = completion
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new ChatTransportResponse
                {
                    ErrorMessage = ex.Message,
                    TimeTaken    = sw.Elapsed
                };
            }
        }

        /// <summary>
        /// Assembles the final message list sent to the provider, prepending history
        /// when requested and deduplicating to avoid sending the same message twice
        /// (preserves the behaviour of <see cref="OpenAiHttpClient.GetChatAiResponse(IEnumerable{OpenAiChatMessage}, bool)"/>).
        /// </summary>
        private List<ChatMessage> BuildSendList(List<ChatMessage> incoming, bool includeHistory)
        {
            var sendList = new List<ChatMessage>();

            if (includeHistory)
                sendList.AddRange(_chatHistory);

            foreach (var msg in incoming)
            {
                // Deduplicate: skip messages already present in the send list
                var msgText = GetText(msg);
                bool isDuplicate = sendList.Any(existing =>
                    existing.Role == msg.Role && GetText(existing) == msgText);

                if (!isDuplicate)
                {
                    sendList.Add(msg);
                    _chatHistory.Add(msg);
                }
            }

            return sendList;
        }

        private static string GetText(ChatMessage msg) =>
            string.Concat(msg.Contents.OfType<TextContent>().Select(c => c.Text));
    }
}

