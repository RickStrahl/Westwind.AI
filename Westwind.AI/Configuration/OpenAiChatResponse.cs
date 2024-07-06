
namespace Westwind.AI.Chat
{
    public class OpenAiChatResponse
    {
        public string id { get; set; }
        public string _object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public OpenAiResponseChoice[] choices { get; set; }
        public OpenAiResponseUsage usage { get; set; }
        public object system_fingerprint { get; set; }
    }

    public class OpenAiResponseUsage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    public class OpenAiResponseChoice
    {
        public int index { get; set; }
        public OpenAiChatMessage message { get; set; }
        public object logprobs { get; set; }
        public string finish_reason { get; set; }
    }

    public class OpenAiChatMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }


    public class OpenAiErrorResponse        
    {
        public OpenAiError error { get; set; }
    }

    public class OpenAiError
    {
        public string message { get; set; }    
        public string code { get; set; }
    }
}
