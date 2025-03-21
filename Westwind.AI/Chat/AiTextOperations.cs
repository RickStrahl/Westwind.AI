// Import packages

using System.Threading.Tasks;
using Westwind.AI.Configuration;

namespace Westwind.AI.Chat
{
    /// <summary>
    /// Text Operations like summarizing
    /// </summary>
    public class AiTextOperations : GenericAiChatClient
    {        
        public AiTextOperations(OpenAiConnectionConfiguration openAiAuthConfig) : base(openAiAuthConfig) { }
        
        public AiTextOperations(IOpenAiConnection connection) : base(connection) { }


        /// <summary>
        /// Generic completion interface that lets you provide
        /// system and user prompts.
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="systemMessage"></param>
        /// <returns></returns>
        public async Task<string> Complete(string prompt, string systemMessage = null, bool includeChatHistory = false)
        {
            if (string.IsNullOrEmpty(systemMessage))
            {
                systemMessage = "You are a text completion assistant. Complete the following text. Return only the result text.";
            }

            var result = await AiHttpClient.GetChatAiResponse(prompt, systemMessage, includeChatHistory);
            if (result == null)
            {
                SetError(AiHttpClient.ErrorMessage);
            }
            return result;
        }


        /// <summary>
        /// Summarize text to a specific number of sentences.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="numberOfWords"></param>
        /// <returns></returns>
        public async Task<string> Summarize(string text, int numberOfSentences = 5)
        {

            string systemMessage = "You are an editor writes summaries of text for end of document summaries. Return only the summarized text.";
            string query = $"Summarize the following text in {numberOfSentences} sentences:\n{text}";

            string result = await AiHttpClient.GetChatAiResponse(query, systemMessage);

            if (result == null)
            {
                SetError(AiHttpClient.ErrorMessage);
            }
            return result;
        }


        /// <summary>
        /// Translates text from one language to another.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="sourceLang"></param>
        /// <param name="targetLang"></param>
        /// <returns></returns>
        public async Task<string> TranslateText(string text, string sourceLang, string targetLang)
        {
            string systemMessage = 
                "You are a translator that translates from one language to another. " +
                "Do not translate text or comments inside of code blocks. " +
                "Be precise and return only the translated text in the result.";

            string query = $"Translate the following text from {sourceLang} to {targetLang}:\n{text}";

            string result = await AiHttpClient.GetChatAiResponse(query, systemMessage);

            if (result == null)
            {
                SetError(AiHttpClient.ErrorMessage);
            }
            return result;
        }

        /// <summary>
        /// Checks grammar of the input text and returns adjusted text.
        /// </summary>
        /// <param name="text">Text to check grammar on</param>        
        /// <returns></returns>
        public async Task<string> CheckGrammar(string text)
        {

            string message = text;

            string result = await AiHttpClient.GetChatAiResponse(message, CheckGrammarSystemPrompt);
            if (result == null)
            {
                SetError(AiHttpClient.ErrorMessage);
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
           
            
            string message = text;

            string result = await AiHttpClient.GetChatAiResponse(message, CheckGrammarSystemPrompt);

            if (result == null)
            {
                SetError(AiHttpClient.ErrorMessage);
            }
            return result;
        }


        public const string CheckGrammarSystemPrompt =
            "You are a grammar checker that corrects grammar on the input text. " +
            "Don't check grammar in code blocks or text inside of comments. " +
            "Return only the corrected text in the output.";
    }
}


