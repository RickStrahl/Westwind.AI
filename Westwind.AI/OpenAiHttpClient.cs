using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Westwind.AI.Configuration;
using Westwind.Utilities;


namespace Westwind.AI
{

    /// <summary>
    /// This is the low level API client that provides access to the OpenAI API with
    /// high level methods for specific operations like chat, image generation etc.
    /// 
    /// It provides the ability to use connections which can be optionally stored in
    /// configuration files and loaded from disk with encryption.
    /// 
    /// This class is used by the higher level GenericAiChat and ImageGenerations classes.
    /// </summary>
    public class OpenAiHttpClient
    {
        /// <summary>
        /// The connection that is used to connect to the service.
        /// This single connection type supports multiple models and providers through
        /// a single interface which is accessed by GetHttpClient() and GetEndpointUrl()
        /// to retrieve the correct configuration for the current connection.
        /// </summary>
        public IOpenAiConnection Connection { get; set; }

        /// <summary>
        /// Keep track of the chat history for passing context
        /// forward.
        /// </summary>
        public List<OpenAiChatMessage> ChatHistory { get; set; } = new List<OpenAiChatMessage>();

        public OpenAiHttpClient(IOpenAiConnection connection)
        {
            Connection = connection;
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

        /// <summary>
        /// Returns the raw JSON response from the last request
        /// </summary>
        public string LastResponseJson { get; set; }

        /// <summary>
        /// Returns last success response
        /// </summary>
        public OpenAiChatMessages LastChatResponse { get; set;  }


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
        public Task<string> GetChatAiResponse(string prompt, string systemPrompt = null, bool includeHistory = false)
        {
            var request = new OpenAiChatRequest()
            {
                model = Connection.ModelId,
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

            return GetChatAiResponse(messages, includeHistory);
        }

        /// <summary>
        /// Returns a chat response based on multiple prompt requests or for prompts
        /// that include the current prompt history.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="includeHistory">If true includes previous requests and responses</param>
        /// <returns></returns>
        public async Task<string> GetChatAiResponse(IEnumerable<OpenAiChatMessage> messages, bool includeHistory = false)
        {
            SetError();

            if (messages == null || !messages.Any())
            {
                SetError("No messages provided for chat request.");
                return null;
            }

            var request = new OpenAiChatRequest()
            {
                model = Connection.ModelId,
            };

            // Add request to history            

            if(includeHistory)
            {
                foreach(var msg in ChatHistory)
                    request.messages.Add(msg);
            }
            
            foreach (var msg in messages)
            {                   
                if(msg.data != null)
                {
                    if (request.messages.Any(m => m.data == msg.data))
                        continue;
                }
                else if (!string.IsNullOrEmpty(msg.Text))
                {
                    if (request.messages.Any(m => m.Text == msg.Text))
                        continue;
                }

                request.messages.Add(msg);                    
                ChatHistory.Add(msg);
            }

            // turn OpenAiContentData into an Array
            foreach(var msg in request.messages)
            {
                
                if (msg.content is OpenAiContentData)
                {                     
                    // content object must be an array 
                    msg.content = new[] { msg.content };
                }                
            }

            var json = JsonSerializationUtils.Serialize(request, formatJsonOutput: true);
            var resultJson = await SendJsonHttpRequest(json, "chat/completions");

            if (string.IsNullOrEmpty(resultJson))
                return default;            

            var chatResponse = JsonSerializationUtils.Deserialize<OpenAiChatMessages>(resultJson); // fails silently with null
            if (chatResponse == null)
            {
                SetError("Invalid response from AI service.");
                return default;
            }

            LastChatResponse = chatResponse;

            var choice = chatResponse?.choices?.FirstOrDefault();
            if (choice == null)
            {
                SetError("Invalid response from AI service.");
                return default;
            }

            ChatHistory.Add(choice.message);

            var resultText = choice.message?.content as string;
            return resultText;
        }


        public async Task<HttpResponseMessage> SendJsonHttpRequestToResponse(string jsonPayload, string operationSegment = "chat/completions")
        {
            SetError();

            if (Connection == null || Connection.IsEmpty)
            {
                SetError("No configuration provided.");
                return null;
            }

            var endpointUrl = GetEndpointUrl(operationSegment);
            string json;   // invalid json

            HttpResponseMessage message;
            using (var http = GetHttpClient())
            {
                if (CaptureRequestData)                    
                    LastRequestJson = jsonPayload + "\n\n" +
                                      "---\n\n" +
                                      endpointUrl + "\n" +
                                      Connection.ModelId + " " + Connection.ApiKey?.GetMaxCharacters(5) + "...";

                try
                {
                    var jsonContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    // explicitly clear content type - some AI Engines (nvidia) don't like charset (charset=utf-8 is default)
                    jsonContent.Headers.ContentType.CharSet = "";

                    message = await http.PostAsync(endpointUrl, jsonContent);

                    // should always fail
                    if (!message.IsSuccessStatusCode)
                    {
                        string errorMessage = null;
                        if (message.Content.Headers.ContentLength > 0 && message.Content.Headers.ContentType.ToString().StartsWith("application/json"))
                        {
                            json = await message.Content.ReadAsStringAsync();
                            if (CaptureRequestData)
                                LastResponseJson = json;

                            var error = JsonConvert.DeserializeObject<dynamic>(json);
                            try
                            {
                                errorMessage = error.error?.message;
                            }
                            catch
                            {
                                try
                                {
                                    errorMessage = error.error;
                                }catch { }
                            }                            
                        }
                        if (message.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            SetError(
                                $"Authentication failed. Invalid API Key or request not supported.\n{errorMessage}");
                            return null;
                        }
                        if (message.StatusCode == HttpStatusCode.NotFound)
                        {
                            SetError($"AI request failed - invalid Url: {endpointUrl}\n{errorMessage}");
                            return null;
                        }

                        if (message.Content.Headers.ContentLength > 0 && message.Content.Headers.ContentType.ToString().StartsWith("application/json"))
                        {
                            json = await message.Content.ReadAsStringAsync();
                            var error = JsonConvert.DeserializeObject<dynamic>(json);
                            string msg = error.error?.message;

                            SetError($"AI request failed: {msg}");
                        }
                        else
                        {
                            SetError("AI request failed: " + message.StatusCode.ToString());
                        }

                        return null;
                    }


                    return message;
                }
                catch (Exception ex)
                {
                    // request hard failed
                    SetError("Http request failed: " + ex.Message);
                    return null;
                }

                
            }
        }


        /// <summary>
        /// Low level, generic routine that sends an HTTP request to the OpenAI server. This method
        /// works to send any type - chat, image, variation etc. - to the server.
        /// </summary>
        /// <param name="operationSegment">The Open AI Command operation. Segment(s) off the baseUrl. ie `chat/completions` or `image/generation`</param>
        /// <param name="jsonPayload">Raw JSON to send to the server</param>         
        /// <returns>JSON response or null</returns>
        public async Task<string> SendJsonHttpRequest(string jsonPayload, string operationSegment = "chat/completions")
        {
            var message = await SendJsonHttpRequestToResponse(jsonPayload, operationSegment);
            if (message == null)
            {
                return null;
            }

            var jsonContent = await message.Content.ReadAsStringAsync();
            LastResponseJson = jsonContent;
            
            return jsonContent;            
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
            if (string.IsNullOrEmpty(Connection.Endpoint))
                throw new InvalidOperationException("Connection.Endpoint is not set. Cannot make AI API request.");

            var endpoint = Environment.ExpandEnvironmentVariables(Connection.Endpoint).TrimEnd('/');

            if (Connection.ProviderMode == AiProviderModes.AzureOpenAi && Connection.EndpointTemplate.Contains("deployments"))
            {
                // handle Azure when providing a full OpenAI style endpoint 
                Connection.EndpointTemplate = "{0}/openai/v1/{1}";  // same as OpenAI template
                if (Connection.Endpoint.Contains("/openai/v1"))
                    Connection.Endpoint = Connection.Endpoint.Replace("/openai/v1", string.Empty).TrimEnd('/');
            }

            return string.Format(Connection.EndpointTemplate, endpoint, operationSegment);
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
            client.DefaultRequestHeaders.Add("Accept", "application/json");            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Connection.ApiKey);

            return client;
        }

        public override string ToString()
        {
            return $"{Connection.Name?.ToString() ?? "No Connection"}  {ErrorMessage}";
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
            ErrorMessage += message + "\n";
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