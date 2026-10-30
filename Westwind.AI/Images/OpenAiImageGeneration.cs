using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Westwind.AI.Chat;
using Westwind.AI.Configuration;
using Westwind.AI;
using System.Linq;
using Westwind.Utilities;

namespace Westwind.AI.Images
{

    /// <summary>
    /// Open AI Image Generation
    /// </summary>
    public class OpenAiImageGeneration : AiBase
    {
        public OpenAiImageGeneration(OpenAiConnectionConfiguration openAiAuthConfig) : base(openAiAuthConfig)
        {
            AiHttpClient = new OpenAiHttpClient(openAiAuthConfig.ActiveConnection);
            AiHttpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public OpenAiImageGeneration(IOpenAiConnection connection) : base(connection)
        {
            AiHttpClient = new OpenAiHttpClient(connection);
            AiHttpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        #region Image Generation API Calls

        /// <summary>
        /// Generate an image from the provided prompt, optionally using BaseImageFiles as references.
        /// </summary>
        /// <param name="prompt">Prompt text for image to create</param>
        /// <param name="createImageFile">if true creates an image file and saves it into the OpanAiAddin\Images folder</param>
        /// <param name="outputFormat">determines whether result is returned as url or base64 data</param>
        /// <returns></returns>
        public async Task<bool> Generate(ImagePrompt prompt,
            bool createImageFile = false,
            ImageGenerationOutputFormats outputFormat = ImageGenerationOutputFormats.None)
        {
            SetError();
            bool hasBaseImages = prompt.BaseImages?.Count > 0;               

            // structure for posting to the API
            var requiredImage = new ImageRequest()
            {
                prompt = prompt.Prompt?.Trim(),
                n = prompt.ImageCount,
                size = prompt.ImageSize,
                model = prompt.Model,
                style = prompt.ImageStyle,
                quality = prompt.ImageQuality,
                background = prompt.ImageBackground              
            };

            if (prompt.Model.Contains("dall-e"))
            {
                switch (outputFormat)
                {
                    case ImageGenerationOutputFormats.Url:
                        requiredImage.response_format = "url";
                        break;
                    case ImageGenerationOutputFormats.Base64:
                        requiredImage.response_format = null;
                        break;
                    case ImageGenerationOutputFormats.None:
                        requiredImage.response_format = null;
                        break;
                    default:
                        requiredImage.response_format = null;
                        break;
                }              
            }
            else
            {
                requiredImage.response_format = null;
                requiredImage.style = null;
                requiredImage.moderation = hasBaseImages ? null : "low";
            }

            if (hasBaseImages)
            {
                requiredImage.images = new List<ImageRequestImage>();
                foreach (string imageFile in prompt.BaseImages)
                {
                    if (string.IsNullOrWhiteSpace(imageFile))
                    {
                        SetError("Missing image file in BaseImageFiles.");  
                        return false;
                    }

                    if (imageFile.StartsWith("http://") || imageFile.StartsWith("https://"))
                    {
                        var httpSettings = new HttpClientRequestSettings()                        
                        {
                            Url = imageFile
                        };

                        try
                        {
                            var bytes = await HttpClientUtils.DownloadBytesAsync(httpSettings);
                            var mimeType = httpSettings.ResponseContentType;
                            requiredImage.images.Add(new ImageRequestImage
                            {
                                image_url = HtmlUtils.BinaryToEmbeddedBase64(bytes, mimeType)
                            });
                            continue;
                        }
                        catch
                        {
                            SetError("Image generation failed: Unable to download reference image from URL: " + imageFile);
                            return false;
                        }                        
                    }   
                    if (imageFile.StartsWith("data:"))
                    {
                        requiredImage.images.Add(new ImageRequestImage
                        {
                            image_url = imageFile 
                        });
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(imageFile) || !File.Exists(imageFile))
                    {
                        SetError("Image generation failed: Reference image file not found: " + imageFile);
                        return false;
                    }

                    string mediaType;
                    switch (Path.GetExtension(imageFile).ToLowerInvariant())
                    {
                        case ".png":
                            mediaType = "image/png";
                            break;
                        case ".jpg":
                        case ".jpeg":
                            mediaType = "image/jpeg";
                            break;
                        case ".webp":
                            mediaType = "image/webp";
                            break;
                        default:
                            SetError("Image generation failed: Unsupported reference image format: " + imageFile);
                            return false;
                    }

                    try
                    {
                        byte[] bytes = await File.ReadAllBytesAsync(imageFile);
                        requiredImage.images.Add(new ImageRequestImage
                        {
                            image_url = HtmlUtils.BinaryToEmbeddedBase64(bytes, mediaType)
                        });
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        SetError("Image generation failed: " + ex.Message);
                        return false;
                    }
                }
            }

            var imageResults = new List<ImageResult>();
            ImageResults response;

            var json = JsonConvert.SerializeObject(requiredImage, Formatting.Indented);
            var result = await AiHttpClient.SendJsonHttpRequest(json,
                hasBaseImages ? "images/edits" : "images/generations");

            if (!string.IsNullOrWhiteSpace(result))
            {
                if (AiHttpClient.CaptureRequestData)
                    AiHttpClient.LastResponseJson = result;

                response = JsonConvert.DeserializeObject<ImageResults>(result);

                if (response?.data == null || response.data.Count == 0)
                {
                    SetError("Image generation failed: No images returned.");
                    return false;
                }

                foreach (var url in response.data)
                {
                    var res = new ImageResult()
                    {
                        Url = url.url,
                        Base64Data = url.b64_json,
                        RevisedPrompt = url.revised_prompt
                    };
                    imageResults.Add(res);
                }
                prompt.ImageUrls = imageResults.ToArray();

                if (createImageFile)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(imageResults.First().Url))
                            await prompt.DownloadImageToFile();
                        else
                            prompt.WriteDataToImageFile(imageResults.First().Base64Data);
                        
                    }
                    catch (Exception ex)
                    {
                        SetError("Download failed: " + ex.Message);
                        return false;
                    }
                }
                return true;
            }

            // error
            SetError("Image generation failed: " +
                     (result != null ? "Empty response from image API." : AiHttpClient.ErrorMessage));

            return false;
        }

        //	curl https://api.openai.com/v1/images/generations \
        //  -H "Content-Type: application/json" \
        //  -H "Authorization: Bearer $OPENAI_API_KEY" \
        //  -d '{
        //	  "model": "dall-e-3",
        //    "prompt": "a white siamese cat",
        //    "n": 1,
        //    "size": "1024x1024",
        //    "model": "dall-e-3",
        //    "style": "vivid",
        //    "quality": "standard"       
        //  }

        // }


        // ReSharper disable once ArrangeModifiersOrder
        public async Task<bool> ValidateApiKey(string openAiKey)
        {

            var key = openAiKey;
            if (!Regex.IsMatch(key, @"^sk-[a-zA-Z0-9]{32,}$"))
                return false;


            using (var client = AiHttpClient.GetHttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(3);
                client.DefaultRequestHeaders.Clear();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

                var endPointUrl = AiHttpClient.GetEndpointUrl("models");
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync("https://api.openai.com/v1/models");
                }
                catch
                {
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
        }
        #endregion

    }

    #region OpenAI JSON Structures

    /// <summary>
    /// Open AI Image Request object in the format the API expects
    /// </summary>
    internal class ImageRequest
    {
        
        public string prompt { get; set; }

        /// <summary>
        /// Reference images as data URLs. Only sent to images/edits.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<ImageRequestImage> images { get; set; }


        /// <summary>
        /// The name of the model to use. dall-e-3 or gpt-image-1, gpt-image-1.5 
        /// IMPORTANT: Azure uses the deployment name here so use Connection.ModelId 
        ///            if running from configuration.
        /// </summary>
        public string model { get; set; } = "gpt-image-1.5";


        /// <summary>
        /// Number of images to generate. Must be between 1 and 10. Dall-E-3 only supports 1 image at a time. gpt-image-1 supports up to 10.
        /// </summary>
        public int n { get; set; } = 1;

        /// <summary>
        /// Image size: Dall-E supports 256x256, 512x512, and 1024x1024. 
        /// gpt-image-1 supports 256x256, 512x512, 1024x1024, 1536x1024, 1792x1024, and 1024x1792.
        /// </summary>        
        public string size { get; set; } = "1024x1024";

        /// <summary>
        /// Response format for Dall-E only
        /// url or b64_json (base64-encoded JSON object with the image data).
        /// All others use b64_json by default and the value is not allowed
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string response_format { get; set; } = null;  


        /// <summary>
        /// Image style: Dall-E only: natural or vivid
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string style { get; set; } = null;  


        /// <summary>
        /// Image Quality
        /// Dall-E: auto, standard, hd
        /// Gpt-image: auto, high, medium, low
        /// </summary>
        public string quality { get; set; } = "auto";


        /// <summary>        
        /// Determines the amount of moderation applied to the generated image: low, auto
        /// Not supported on Dall-E
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string moderation { get; set; } = null;  


        /// <summary>
        /// Background: transparent, opaque, auto
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string background { get; set; } = null;  
       
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string output_format { get; set; } = null; // png, jpg, webp   - gpt-image only
    }

    internal class ImageRequestImage
    {
        public string image_url { get; set; }
    }

    internal class ImageUrlItem
    {
        public string url { get; set; }

        public string b64_json { get; set; }

        public string revised_prompt { get; set; }
    }

    internal class ImageResults
    {
        public long created { get; set; }
        public List<ImageUrlItem> data { get; set; }
    }

    public enum ImageGenerationOutputFormats
    {
        Url,
        Base64,
        None  // use for gtp-image
    }

    #endregion
}