
using System.Collections.Generic;
using Newtonsoft.Json;
using Westwind.Utilities;

namespace Westwind.AI.Configuration
{
    /// <summary>
    /// Native OpenAI Chat messages that are used to send and receive JSON content from
    /// the OpenAI service.
    /// </summary>
    public class OpenAiChatRequest
    {
        public string model { get; set; }

        public List<OpenAiChatMessage> messages { get; set; } = new List<OpenAiChatMessage> { };


        public bool stream { get; set; }

        //public int? max_completion_tokens { get; set; } = null;

        public decimal temperature { get; set; } = 1;

        public decimal top_p { get; set; } = 1;

        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]       
        //public string safety_identifier { get; set; } = null;
    }

    public class OpenAiChatMessages
    {
        public string id { get; set; }
        public string _object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public OpenAiResponseChoice[] choices { get; set; }
        public OpenAiResponseUsage usage { get; set; }
        public object system_fingerprint { get; set; }

        public string finish_reason { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ToJson() => JsonSerializationUtils.Serialize(this, false, true, true);
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
        public string role { get; set; } = "user";

        public object content { get; set; }

        [JsonIgnore]
        public string Text
        {
            get
            {
                if (content is string)
                    return content as string;
                if (content is OpenAiContentData)
                    return ((OpenAiContentData)content).text;

                return null;
            }
            set => content = value;
        }

        /// <summary>
        /// Display text that can be optionally set to override
        /// the text or content of the message.
        /// 
        /// Not used internally, but can be used for UI display
        /// purposes to show a different text than actual content.
        /// </summary>
        [JsonIgnore]
        public string DisplayText
        {
            get => field ?? Text;
            set;
        }


        [JsonIgnore]
        public string ImageUrl
        {
            get
            {
                if (content is OpenAiContentData)
                    return ((OpenAiContentData)content).image_url.url as string;

                return null;
            }

            
            set => content = new OpenAiContentData { type = "image_url", image_url = new OpenAiImageUrl(value) };
        }

        /// <summary>
        /// Optionally allows directly assigning a binary string to 
        /// set a base64 Image Url
        /// </summary>
        public byte[] ImageData {  
            set
            {
                if (value == null)
                    ImageUrl = null;
                else
                    ImageUrl = HtmlUtils.BinaryToEmbeddedBase64(value);
            } 
        }


        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public byte[] data { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string type { get; set; }

        public override string ToString()
        {
            return $"{DisplayText  ?? ImageUrl}";
        }
    }

    
    public class OpenAiContentData
    {
        /// <summary>
        /// text, image_url, audio
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string type { get; set;  }

        /// <summary>
        /// Text data
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string text { get; set; }

        /// <summary>
        /// {  url: "https://..." } or "data:..."  
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public OpenAiImageUrl image_url { get; set; }   
    }

    public class OpenAiImageUrl(string url)
    {        
        public string url { get;  } = url;

        public override string ToString()
        {
            return $"{ url}";
        }
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
