using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Westwind.AI.Chat;
using Westwind.AI.Chat.Configuration;

using Westwind.Utilities;

namespace Westwind.AI
{

    public class OpenAiHttpClient
    {
        public IOpenAiConnection Configuration { get; set; }

        /// <summary>
        /// Keep track of the chat history for passing context
        /// forward.
        /// </summary>
        public List<OpenAiChatMessage> ChatHistory { get; set; } = new List<OpenAiChatMessage>();

        public OpenAiHttpClient(IOpenAiConnection connection)
        {
            Configuration = connection;
        }


        /// <summary>
        /// Optional Proxy
        /// </summary>
        public WebProxy Proxy { get; set; }

        /// <summary>
        /// Determine whether you want to capture request data in LastRequest and Response Json props
        /// </summary>
        public bool CaptureRequestData { get; set; }

        /// <summary>
        /// Captures the last request body, plus the Url, Model Id and start of API key
        /// </summary>
        public string LastRequestJson { get; set; }

        
        public string LastResponseJson { get; set; }


        /// <summary>
        /// Retrieves a completion by text from the AI service.
        /// </summary>
        /// <param name="prompt">The query to run against the AI</param>
        /// <param name="systemPrompt">
        /// Instructions for the AI on how to process the prompt.
        /// You can use a persona, job description or give descriptive instructions.
        /// </param>
        /// <param name="includeHistory"> If true includes previous requests and responses</param>
        /// <returns></returns>
        public Task<string> GetAiResponse(string prompt, string systemPrompt = null, bool includeHistory = false)
        {
            var request = new OpenAiChatRequest()
            {
                model = Configuration.ModelId,
            };

            var messages = new List<OpenAiChatMessage>();


            if (!string.IsNullOrEmpty(systemPrompt))
                messages.Add(new OpenAiChatMessage()
                {
                    role = "system",
                    content = systemPrompt ?? ""
                });

            messages.Add(new OpenAiChatMessage()
            {
                role = "user",
                content = prompt
            });

            return GetAiResponse(messages, includeHistory);
        }

        /// <summary>
        /// Returns a chat response based on multiple prompt requests or for prompts
        /// that include the current prompt history.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="includeHistory">If true includes previous requests and responses</param>
        /// <returns></returns>
        public async Task<string> GetAiResponse(IEnumerable<OpenAiChatMessage> messages, bool includeHistory = false)
        {
            SetError();

            var request = new OpenAiChatRequest()
            {
                model = Configuration.ModelId,
            };

            // Add request to history            

            if(includeHistory)
            {
                foreach(var msg in ChatHistory)
                    request.messages.Add(msg);
            }
            foreach (var msg in messages)
            {
                request.messages.Add(msg);
                ChatHistory.Add(msg);
            }

            var json = JsonSerializationUtils.Serialize(request, formatJsonOutput: true);
            var resultJson = await SendJsonHttpRequest(json, "chat/completions");

            if (string.IsNullOrEmpty(resultJson))
                return default;            

            var chatResponse = JsonSerializationUtils.Deserialize<OpenAiChatMessages>(resultJson);
            if (chatResponse == null)
            {
                SetError("Invalid response from AI service.");
                return default;
            }

            var choice = chatResponse?.choices?.FirstOrDefault();
            if (choice == null)
            {
                SetError("Invalid response from AI service.");
                return default;
            }

            ChatHistory.Add(choice.message);

            var resultText = choice.message?.content;
            return resultText;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="operationSegment">The Open AI Command operation. Segment off the baseUrl</param>
        /// <param name="jsonPayload"></param>
        /// 
        /// <returns></returns>
        public async Task<string> SendJsonHttpRequest(string jsonPayload, string operationSegment = "chat/completions")
        {
            if (Configuration == null || Configuration.IsEmpty)
            {
                SetError("No configuration provided.");
            }

            var endpointUrl = GetEndpointUrl(operationSegment);
            var http = GetHttpClient();

            if(CaptureRequestData)
                LastRequestJson = jsonPayload + "\n\n" +
                    "---\n\n" +
                    endpointUrl +"\n" + 
                    Configuration.ModelId + " " + Configuration.DecryptedApiKey?.GetMaxCharacters(5) + "..." ;                      

            var json = " {}";   // invalid json

            HttpResponseMessage message;
            try
            {
                message = await http.PostAsync(endpointUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
            }
            catch
            {
                // request hard failed
                return null;
            }

            if (message.IsSuccessStatusCode)
            {
                var jsonContent = await message.Content.ReadAsStringAsync();
                LastResponseJson = jsonContent;
                return jsonContent;
            }

            // should always fail
            if (!message.IsSuccessStatusCode)
            {
                string errorMessage = null;
                if (message.Content.Headers.ContentLength > 0 && message.Content.Headers.ContentType.ToString().StartsWith("application/json"))
                {
                    json = await message.Content.ReadAsStringAsync();
                    LastResponseJson = json;
                    var error = JsonConvert.DeserializeObject<dynamic>(json);
                    errorMessage = error.error?.message;
                }
                if (message.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    SetError("Authentication failed. Invalid API Key. " + errorMessage);
                    return null;
                }
                if (message.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    SetError("AI request failed: Invalid URL, resource not found.\n" + endpointUrl);
                    return null;
                }

                if (message.Content.Headers.ContentLength > 0 && message.Content.Headers.ContentType.ToString().StartsWith("application/json"))
                {
                    json = await message.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<dynamic>(json);
                    string msg = error.error?.message;

                    SetError($"AI request failed : {msg}");
                }
                else
                {
                    SetError("AI request failed: " + message.StatusCode.ToString());
                }
            }

            return null;
        }


        /// <summary>
        /// Retrieves an endpoint based on the current configuration
        /// </summary>
        /// <param name="operationSegment">
        /// Segment that specifies the operation:
        ///     * images/generations
        ///     * images/variations
        ///     * models
        /// </param>
        /// <returns></returns>
        public string GetEndpointUrl(string operationSegment)
        {
            var endpoint = Configuration.Endpoint.TrimEnd('/');


            if (Configuration.ConnectionMode == AiConnectionModes.AzureOpenAi)
            {
                var template = Configuration.EndpointTemplate;
                if (string.IsNullOrEmpty(Configuration.ApiVersion))
                    template = template.Replace("?api-version={3}", string.Empty);

                return string.Format(template,
                    endpoint,
                    operationSegment,
                    Configuration.ModelId,
                    Configuration.ApiVersion);
            }

            // OpenAi
            return string.Format(Configuration.EndpointTemplate, endpoint, operationSegment);
        }


        /// <summary>
        /// Creates an instance of the HttpClient and sets the API Key
        /// in the headers.
        /// </summary>
        /// <returns>Configured HttpClient instance</returns>
        public HttpClient GetHttpClient(HttpClientHandler handler = null)
        {
            handler = handler ?? new HttpClientHandler()
            {
                Proxy = Proxy
            };

            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Clear();
            if (Configuration.ConnectionMode == AiConnectionModes.AzureOpenAi)
                client.DefaultRequestHeaders.Add("api-key", Configuration.DecryptedApiKey);
            else
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Configuration.DecryptedApiKey);

            return client;
        }


        #region Error Handling

        public string ErrorMessage { get; set; }


        protected void SetError(string message = null)
        {
            if (message == null)
            {
                ErrorMessage = string.Empty;
                return;
            }
            ErrorMessage += message;
        }

        protected void SetError(Exception ex, bool checkInner = false)
        {
            if (ex == null)
            {
                ErrorMessage = string.Empty;
            }
            else
            {
                Exception e = ex;
                if (checkInner)
                    e = e.GetBaseException();

                ErrorMessage = e.Message;
            }
        }
        #endregion

    }
}