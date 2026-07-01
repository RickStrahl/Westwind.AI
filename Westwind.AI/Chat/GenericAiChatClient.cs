using System.Collections.Generic;
using System.Threading.Tasks;
using Westwind.AI.Chat.Transport;
using Westwind.AI.Configuration;

namespace Westwind.AI.Chat
{
    public class GenericAiChatClient : AiBase
    {
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
            var response = await Transport.CompleteAsync(prompts, includeHistory);
            MapTransportResponseToDiagnostics(response);
            return response.Text;
        }

        /// <summary>
        /// Maps transport response diagnostics to AiHttpClient fields for backwards compatibility.
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

            // Map timing
            AiHttpClient.TimeTaken = response.TimeTaken;

            // Map native response for diagnostics
            if (response.NativeResponse is OpenAiChatMessages openAiResponse)
                AiHttpClient.LastChatResponse = openAiResponse;
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