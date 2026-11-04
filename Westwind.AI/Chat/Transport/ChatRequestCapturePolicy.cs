using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ClientModel.Primitives;

namespace Westwind.AI.Chat.Transport
{
    /// <summary>
    /// Captures the actual SDK wire body for a single non-streaming chat completion.
    /// AsyncLocal keeps concurrent calls on the same client from sharing diagnostics.
    /// Headers (including the API key) are never captured.
    /// </summary>
    internal sealed class ChatRequestCapturePolicy : PipelinePolicy
    {
        private readonly AsyncLocal<Capture> _current = new AsyncLocal<Capture>();

        public Capture BeginCapture()
        {
            var capture = new Capture(this, _current.Value);
            _current.Value = capture;
            return capture;
        }

        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int index)
        {
            var capture = _current.Value;
            if (capture != null)
            {
                capture.RequestUri = message.Request.Uri;
                capture.RequestJson = ReadRequest(message);
            }

            ProcessNext(message, pipeline, index);

            if (capture != null)
                capture.ResponseJson = ReadResponse(message);
        }

        public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int index)
        {
            var capture = _current.Value;
            if (capture != null)
            {
                capture.RequestUri = message.Request.Uri;
                capture.RequestJson = await ReadRequestAsync(message).ConfigureAwait(false);
            }

            await ProcessNextAsync(message, pipeline, index).ConfigureAwait(false);

            if (capture != null)
                capture.ResponseJson = await ReadResponseAsync(message).ConfigureAwait(false);
        }

        private static string ReadRequest(PipelineMessage message)
        {
            if (message.Request.Content == null) return null;
            using var stream = new MemoryStream();
            message.Request.Content.WriteTo(stream, message.CancellationToken);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static async Task<string> ReadRequestAsync(PipelineMessage message)
        {
            if (message.Request.Content == null) return null;
            using var stream = new MemoryStream();
            await message.Request.Content.WriteToAsync(stream, message.CancellationToken).ConfigureAwait(false);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static string ReadResponse(PipelineMessage message)
        {
            if (message.Response == null) return null;
            message.Response.BufferContent(message.CancellationToken);
            return message.Response.Content.ToString();
        }

        private static async Task<string> ReadResponseAsync(PipelineMessage message)
        {
            if (message.Response == null) return null;
            await message.Response.BufferContentAsync(message.CancellationToken).ConfigureAwait(false);
            return message.Response.Content.ToString();
        }

        internal sealed class Capture : IDisposable
        {
            private readonly ChatRequestCapturePolicy _owner;
            private readonly Capture _previous;

            public string RequestJson { get; set; }
            public string ResponseJson { get; set; }
            public Uri RequestUri { get; set; }

            internal Capture(ChatRequestCapturePolicy owner, Capture previous)
            {
                _owner = owner;
                _previous = previous;
            }

            public void Dispose() => _owner._current.Value = _previous;
        }
    }
}
