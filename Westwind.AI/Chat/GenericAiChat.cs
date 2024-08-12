using System.Collections.Generic;
using System.Threading.Tasks;
using Westwind.AI.Configuration;

namespace Westwind.AI.Chat;

public class GenericAiChat : AiBase
{

    public GenericAiChat(OpenAiConnectionConfiguration openAiAuthConfig) : base(openAiAuthConfig) 
    {
        HttpClient = new OpenAiHttpClient(openAiAuthConfig.ActiveConnection);
    }

    public GenericAiChat(IOpenAiConnection connection) : base(connection) 
    {             
        HttpClient = new OpenAiHttpClient(connection);
    }

    public async Task<string> Complete(string prompt, string systemPrompt = null, bool includeHistory = false)
    {         
        var result = await HttpClient.GetChatAiResponse(prompt, systemPrompt, includeHistory);
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
        var result = await HttpClient.GetChatAiResponse(prompts, includeHistory);
        if (result == null)
        {
            SetError(HttpClient.ErrorMessage);
        }

        return result;
    }

}