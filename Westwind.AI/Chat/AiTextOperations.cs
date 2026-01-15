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
        /// <remarks>
        /// System Prompt uses AiTextOperations.SummarizeSystemPrompt
        /// </remarks>
        /// <param name="text">Text to summarize</param>
        /// <param name="numberOfSentences">Number of sentences to summarize text to</param>
        /// <param name="systemPrompt">Optionally provide a custom system prompt</param>
        /// <returns></returns>
        public async Task<string> Summarize(string text, int numberOfSentences = 5, string systemPrompt = null)
        {
            if (string.IsNullOrWhiteSpace(systemPrompt))
                systemPrompt = SummarizeSystemPrompt;

            string query = $"Summarize the following text in {numberOfSentences} sentences:\n{text}";

            string result = await AiHttpClient.GetChatAiResponse(query, systemPrompt);

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
        /// <remarks>
        /// System Prompt uses AiTextOperations.SummarizePromptAsTitleSystemPrompt
        /// </remarks>
        /// <param name="text">Full text of prompt or other text to summarize</param>
        /// <param name="maxCharacters">Max number of characters to summarize to</param>
        /// <param name="systemPrompt">Optionally pass in a system prompt to replace default one</param>
        /// <returns></returns>
        public async Task<string> SummarizePrompt(string text, int maxCharacters = 60, string systemPrompt = null)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (maxCharacters < 20)
                maxCharacters = 20;
            if (maxCharacters > 255)
                maxCharacters = 255;

            if (text.Length <= maxCharacters)
                return text;

            if (string.IsNullOrEmpty(systemPrompt))
                systemPrompt = SummarizePromptAsTitleSystemPrompt;

            string query = $"Summarize the following text into a maximum of {maxCharacters} characters:\n\n{text}";
            string result = await AiHttpClient.GetChatAiResponse(query, systemPrompt);

            if (result == null)
            {
                SetError(AiHttpClient.ErrorMessage);
            }
            return result;
        }


        /// <summary>
        /// Translates text from one language to another.
        /// </summary>
        /// <remarks>
        /// System Prompt uses AiTextOperations.TranslateSystemPrompt
        /// </remarks>
        /// <param name="text">Text to translate</param>
        /// <param name="sourceLang">Source language</param>
        /// <param name="targetLang">Target language</param>
        /// <param name="systemPrompt">Optionally provide a custom system prompt</param>
       /// 
        /// <returns></returns>
        public async Task<string> TranslateText(string text, string sourceLang, string targetLang, string systemPrompt = null)
        {
            if (string.IsNullOrEmpty(systemPrompt)) 
                systemPrompt = TranslateSystemPrompt;            

            string query = $"Translate the following text from {sourceLang} to {targetLang}:\n{text}";

            string result = await AiHttpClient.GetChatAiResponse(query, systemPrompt);

            if (result == null)
            {
                SetError(AiHttpClient.ErrorMessage);
            }
            return result;
        }

        /// <summary>
        /// Checks grammar of the input text and returns adjusted text.
        /// </summary>
        /// <remarks>
        /// System Prompt uses AiTextOperations.CheckGrammarSystemPrompt
        /// </remarks>
        /// <param name="text">Text to clean up</param>
        /// <param name="systemPrompt">Optionally provide a custom system prompt</param>
        /// <returns></returns>
        public async Task<string> CheckGrammar(string text, string systemPrompt = null)
        {
            string message = text;

            if (string.IsNullOrEmpty(systemPrompt))
                systemPrompt = CheckGrammarSystemPrompt;

            string result = await AiHttpClient.GetChatAiResponse(message, systemPrompt);
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


        public static string CheckGrammarSystemPrompt =
            """
            Task:
            Clean up and correct the input text.
        
            Instructions:
        
            Fix spelling, grammar, punctuation, and capitalization errors
        
            Improve clarity and readability without changing the meaning
        
            Preserve the original tone, voice, and intent
        
            Keep formatting (line breaks, lists, headings, code blocks) unchanged

            Keep Markdown formatting if present
        
            Do not add new content or remove information
        
            Do not simplify technical terms or alter code, commands, file paths, URLs, or identifiers
        
            Normalize obvious inconsistencies (quotes, spacing, punctuation)
        
            Keep contractions, jargon, and informal phrasing if they appear intentional
        
            Output:
            Return only the corrected version of the selected text, with no explanations or commentary.
            """;


        public static string TranslateSystemPrompt =
            """
            You are a translation engine that converts text from the source language to the target language.

            Rules:

            Translate only natural language content.

            Do NOT translate text inside code blocks, inline code, or comments.

            Preserve formatting, markdown formatting, line breaks, and punctuation.

            Keep technical terms, identifiers, file paths, URLs, and proper nouns unchanged unless they are commonly translated.

            Be precise and literal unless the target language requires minor grammatical adjustment.

            Do not add explanations, notes, or extra text.

            Output:
            Return only the translated text.
            """;

        public static string SummarizeSystemPrompt =
            """
            You are a technical editor summarizing to produce a summary of the provided text.
            
            Rules:
            
            Summarize the key points and conclusions of the text.
            
            Be concise and clear while preserving the original meaning.
            
            Maintain a neutral informative tone
            
            Do not introduce new information or opinions.
            
            Preserve important terminology.           

            Keep any introduction if used to the same voice as the original text

            Avoid using introductory statements like  "the text describes" or "the author states"
            
            Output:
            Return only the summarized text.
            """;

        public static string SummarizePromptAsTitleSystemPrompt =
            """
            You are a summarization engine that generates a short concise title from the provided text.
            
            Rules:
            
            Condense the content to its core idea.
            
            Use clear natural language suitable for a title.
            
            Remove all punctuation characters.
            
            Preserve important keywords.
            
            Do not add new concepts or interpretation.
            
            Do not include quotes, markdown or formatting.
            
            Output:
            Return only the title text.
            """;
    }

}


