using System;

namespace Westwind.AI.Chat.Transport
{
    /// <summary>
    /// Standard response envelope for internal chat transports.
    /// </summary>
    internal class ChatTransportResponse
    {
        public string Text { get; set; }

        public string ErrorMessage { get; set; }

        /// <summary>
        /// Provider-specific native response object (e.g. <c>ChatCompletion</c> from
        /// Microsoft.Extensions.AI or <c>OpenAiChatMessages</c> from the HTTP fallback).
        /// </summary>
        public object NativeResponse { get; set; }

        /// <summary>
        /// Wall-clock time taken by the underlying API call.
        /// </summary>
        public TimeSpan TimeTaken { get; set; }

        public bool Success => string.IsNullOrEmpty(ErrorMessage) && Text != null;
    }
}
