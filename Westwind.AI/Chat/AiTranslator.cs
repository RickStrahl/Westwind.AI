// Import packages

using System.Threading.Tasks;
using Westwind.AI.Chat.Configuration;

namespace Westwind.AI.Chat
{
    public class AiTranslator : GenericAiChat
    {        
        public AiTranslator(AiAuthenticationConfiguration aiAuthConfig) : base(aiAuthConfig) { }
        public AiTranslator(IAiCredentials credentials) : base(credentials) { }


        public async Task<string> TranslateText(string text, string sourceLang, string targetLang)
        {            

            string systemMessage = "You are a translator that translates from one language to another. Be precise and return only the translated text.";
            string query = $"Translate the following text from {sourceLang} to {targetLang}:\n{text}";
            
            string result = await ChatHttpClient.GetChatResponse(query, systemMessage);

            if (result == null)
            {
                SetError(ChatHttpClient.ErrorMessage);                
            }
            return result;
        }
    }
}

    
