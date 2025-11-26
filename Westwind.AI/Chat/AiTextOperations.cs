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
        /// A short string summarization routine that can be used to create summarized values
        /// that can be used for title and file names etc.
        /// </summary>
        /// <param name="text">Full text of prompt or other text to summarize</param>
        /// <param name="maxCharacters">Max number of characters to summarize to</param>
        /// <returns></returns>
        public async Task<string> SummarizePrompt(string text, int maxCharacters = 60)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (maxCharacters < 20)
                maxCharacters = 20;
            if (maxCharacters > 255) 
                maxCharacters = 255;

            if (text.Length <= maxCharacters)
                return text;

            string systemMessage = "You are a summarizing editor that creates concise content to create a title string. Return only the result and remove all punctuation.";
            string query = $"Summarize the following text into a maximum of {maxCharacters} characters:\n\n{text}";

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


