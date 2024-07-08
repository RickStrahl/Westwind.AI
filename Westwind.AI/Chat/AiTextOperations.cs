﻿// Import packages

using System.Threading.Tasks;
using Westwind.AI.Chat;
using Westwind.AI.Chat.Configuration;

namespace Westwind.AI.Specialized
{
    /// <summary>
    /// Text Operations like summarizing
    /// </summary>
    public class AiTextOperations : GenericAiChat
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

            string result = await HttpClient.GetAiResponse(query, systemMessage);

            if (result == null)
            {
                SetError(HttpClient.ErrorMessage);
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

            string systemMessage = "You are a translator that translates from one language to another. Be precise and return only the translated text.";
            string query = $"Translate the following text from {sourceLang} to {targetLang}:\n{text}";

            string result = await HttpClient.GetAiResponse(query, systemMessage);

            if (result == null)
            {
                SetError(HttpClient.ErrorMessage);
            }
            return result;
        }

        /// <summary>
        /// Checks grammar of the input text and returns adjusted text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<string> CheckGrammar(string text)
        {
            string system = "You are a grammar checker that corrects grammar on the input text. Return only the corrected text in the output.";
            string message = text;

            string result = await HttpClient.GetAiResponse(message, system);

            if (result == null)
            {
                SetError(HttpClient.ErrorMessage);
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

            string result = await HttpClient.GetAiResponse(message, system);

            if (result == null)
            {
                SetError(HttpClient.ErrorMessage);
            }
            return result;
        }

    }
}

