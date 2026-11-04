using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Westwind.AI.Chat.Transport;
using Westwind.AI.Configuration;
using Westwind.Utilities;

namespace Westwind.AI.Chat
{
    public class GenericAiChatClient : AiBase
    {
        /// <summary>Opt in to capturing the request and response JSON for each completion.</summary>
        public bool CaptureRequestData
        {
            get => AiHttpClient.CaptureRequestData;
            set => AiHttpClient.CaptureRequestData = value;
        }

        /// <summary>Request body from the last completion, or null when capture is off.</summary>
        public string LastRequestJson { get; private set; }

        /// <summary>Response body from the last completion, or null when capture is off.</summary>
        public string LastResponseJson { get; private set; }

        /// <summary>Token usage of the last successful completion, whether or not JSON capture is enabled.</summary>
        public OpenAiResponseUsage LastUsage { get; private set; }

        /// <summary>Elapsed milliseconds of the last completion attempt, including failed requests.</summary>
        public double LastResponseTime { get; private set; }

        public GenericAiChatClient(OpenAiConnectionConfiguration openAiAuthConfig) : base(openAiAuthConfig) 
        {
        }

        public GenericAiChatClient(IOpenAiConnection connection) : base(connection) 
        {             
        }

        /// <summary>
        /// Does a simple completion with prompt input and optional system prompt. 
        /// Routes through internal transport abstraction (Microsoft.AI or HTTP fallback).
        /// </summary>
        /// <param name="prompt">Prompt what to ask for</param>
        /// <param name="systemPrompt">Optional system prompt to provide to set the LLMs 'actor' context</param>
        /// <param name="includeHistory">If true includes the accumulated history of requests</param>
        /// <returns></returns>
        public virtual async Task<string> Complete(string prompt, string systemPrompt = null, bool includeHistory = false)
        {
            ClearCapture();
            var response = await Transport.CompleteAsync(prompt, systemPrompt, includeHistory);
            MapTransportResponseToDiagnostics(response);
            return response.Text;
        }

        /// <summary>
        /// Completes chat with multiple messages.
        /// Routes through internal transport abstraction (Microsoft.AI or HTTP fallback).
        /// </summary>
        /// <param name="prompts">Pass in an existing set of prompts </param>
        /// <param name="includeHistory">If true includes the accumulated history of requests</param>
        /// <returns></returns>
        public virtual async Task<string> Complete(IEnumerable<OpenAiChatMessage> prompts, bool includeHistory = false)
        {
            ClearCapture();
            var response = await Transport.CompleteAsync(prompts, includeHistory);
            MapTransportResponseToDiagnostics(response);
            return response.Text;
        }

        private void ClearCapture()
        {
            LastRequestJson = null;
            LastResponseJson = null;
            LastUsage = null;
            LastResponseTime = 0;
        }

        /// <summary>
        /// Maps transport response diagnostics onto the chat client.
        /// </summary>
        private void MapTransportResponseToDiagnostics(ChatTransportResponse response)
        {
            if (response == null)
                return;

            // Map error message
            if (!string.IsNullOrEmpty(response.ErrorMessage))
                SetError(response.ErrorMessage);
            else
                SetError(null);

            LastResponseTime = response.TimeTaken.TotalMilliseconds;

            if (CaptureRequestData)
            {
                LastRequestJson = response.RequestJson;
                LastResponseJson = JsonSerializationUtils.FormatJsonString(response.ResponseJson);
            }

            if (!string.IsNullOrEmpty(response.ErrorMessage))
                return;

            if (response.NativeResponse is ChatResponse chatResponse && chatResponse.Usage != null)
            {
                LastUsage = new OpenAiResponseUsage
                {
                    prompt_tokens = (int)(chatResponse.Usage.InputTokenCount ?? 0),
                    completion_tokens = (int)(chatResponse.Usage.OutputTokenCount ?? 0),
                    total_tokens = (int)(chatResponse.Usage.TotalTokenCount ?? 0),
                    reasoning_tokens = (int)(chatResponse.Usage.ReasoningTokenCount ?? 0)
                };
            }
            else if (response.NativeResponse is OpenAiChatMessages openAiResponse && openAiResponse.usage != null)
            {
                var usage = openAiResponse.usage;
                LastUsage = new OpenAiResponseUsage
                {
                    prompt_tokens = usage.prompt_tokens,
                    completion_tokens = usage.completion_tokens,
                    total_tokens = usage.total_tokens,
                    reasoning_tokens = usage.completion_tokens_details?.reasoning_tokens ?? usage.reasoning_tokens
                };
            }
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                return $"Error: {ErrorMessage}  Connection: {Connection?.Name}";

            if (Connection != null)
                return $"{Connection.Name}";
            
            return base.ToString();
        }

    }
}
