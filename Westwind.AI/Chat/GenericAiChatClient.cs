using System.Collections.Generic;
using System.Threading.Tasks;
using Westwind.AI.Configuration;

namespace Westwind.AI.Chat
{
    public class GenericAiChatClient : AiBase
    {

        public GenericAiChatClient(OpenAiConnectionConfiguration openAiAuthConfig) : base(openAiAuthConfig) 
        {
            AiHttpClient = new OpenAiHttpClient(openAiAuthConfig.ActiveConnection);
        }

        public GenericAiChatClient(IOpenAiConnection connection) : base(connection) 
        {             
            AiHttpClient = new OpenAiHttpClient(connection);
        }

        /// <summary>
        /// Does a simple completion with prompt input and optional system prompt. 
        /// </summary>
        /// <param name="prompt">Prompt what to ask for</param>
        /// <param name="systemPrompt">Optional system prompt to provide to set the LLMs 'actor' context</param>
        /// <param name="includeHistory">If true includes the accumulated history of requests</param>
        /// <returns></returns>
        public virtual async Task<string> Complete(string prompt, string systemPrompt = null, bool includeHistory = false)
        {         
            var result = await AiHttpClient.GetChatAiResponse(prompt, systemPrompt, includeHistory);
            if (result == null)
            {
                SetError(AiHttpClient.ErrorMessage);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prompts">Pass in an existing set of prompts </param>
        /// <returns></returns>
        public virtual async Task<string> Complete( IEnumerable<OpenAiChatMessage> prompts,  bool includeHistory = false)
        {
            var result = await AiHttpClient.GetChatAiResponse(prompts, includeHistory);
            if (result == null)
            {
                SetError(AiHttpClient.ErrorMessage);
            }

            return result;
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