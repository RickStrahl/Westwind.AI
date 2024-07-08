using System.Collections.Generic;
using System.Threading.Tasks;
using Westwind.AI.Chat.Configuration;

namespace Westwind.AI.Chat;

public class GenericAiChat : AiBase
{
    /// <summary>
    /// Instance of the internally used Http client. It holds the chat history
    /// and also can capture request and response data.
    /// </summary>
    public OpenAiHttpClient HttpClient { get; set; } 

    public GenericAiChat(OpenAiConnectionConfiguration openAiAuthConfig) : base(openAiAuthConfig) 
    {
        HttpClient = new OpenAiHttpClient(openAiAuthConfig.ActiveConnection);
    }

    public GenericAiChat(IOpenAiConnection connection) : base(connection) 
    { 
        HttpClient = new OpenAiHttpClient(connection);
    }

    public async Task<string> Complete(string prompt, string systemPrompt = null)
    {         
        var result = await HttpClient.GetAiResponse(prompt, systemPrompt);
        if (result == null)
        {
            SetError(HttpClient.ErrorMessage);
        }

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="prompts">Pass in an existing set of prompts </param>
    /// <returns></returns>
    public async Task<string> Complete( IEnumerable<OpenAiChatMessage> prompts,  bool includeHistory = false)
    {
        var result = await HttpClient.GetAiResponse(prompts, includeHistory);
        if (result == null)
        {
            SetError(HttpClient.ErrorMessage);
        }

        return result;
    }

}