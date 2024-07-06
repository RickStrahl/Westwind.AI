using System;
using System.Threading.Tasks;
using Westwind.AI.Chat.Configuration;

namespace Westwind.AI.Chat
{
    public class AiGrammarChecker : GenericAiChat
    {
        public AiGrammarChecker(AiAuthenticationConfiguration aiAuthConfig) : base(aiAuthConfig) { }
        public AiGrammarChecker(IAiCredentials credentials) : base(credentials) { }


        /// <summary>
        /// Checks grammar of the input text and returns adjusted text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<string> CheckGrammar(string text)
        {
        
            string system = "You are a grammar checker. Return only the corrected text in the output.";
            string message = text;

            string result = await ChatHttpClient.GetChatResponse(message, system);

            if (result == null)
            {
                SetError(ChatHttpClient.ErrorMessage);
            }
            return result;
        }

        /// <summary>
        /// Checks grammar of the input text and returns adjusted text as a diff.
        /// 
        /// NOTE: this is not working very well.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<string> CheckGrammarAsDiff(string text)
        {            
            string system = "You are a grammar checker that corrects input text into grammatically correct grammar. Return only the corrected text in the output. Return the output in .diff format";
            string message = text;

            string result = await ChatHttpClient.GetChatResponse(message, system);

            if (result == null)
            {
                SetError(ChatHttpClient.ErrorMessage);
            }
            return result;
        }
    }
}