using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Westwind.AI.Chat;
using Westwind.AI.Chat.Configuration;
using Westwind.AI;

namespace Westwind.Ai.Images
{

    /// <summary>
    /// Open AI Image Generation
    /// </summary>
    public class OpenAiImageGeneration : AiBase
    {

        protected OpenAiHttpClient ChatHttpClient { get; set; }

        public OpenAiImageGeneration(OpenAiConnectionConfiguration openAiAuthConfig) : base(openAiAuthConfig)
        {
            ChatHttpClient = new OpenAiHttpClient(openAiAuthConfig.ActiveConnection);
        }

        public OpenAiImageGeneration(IOpenAiConnection connection) : base(connection)
        {
            ChatHttpClient = new OpenAiHttpClient(connection);
        }




        #region Image Generation API Calls

        /// <summary>
        /// Generate an image from the provided prompt
        /// </summary>
        /// <param name="prompt">Prompt text for image to create</param>
        /// <param name="createImageFile">if true creates an image file and saves it into the OpanAiAddin\Images folder</param>
        /// <param name="outputFormat">determines whether result is returned as url or base64 data</param>
        /// <returns></returns>
        public async Task<bool> Generate(ImagePrompt prompt,
            bool createImageFile = false,
            ImageGenerationOutputFormats outputFormat = ImageGenerationOutputFormats.Url)
        {
            // structure for posting to the API
            var requiredImage = new ImageRequest()
            {
                prompt = prompt.Prompt?.Trim(),
                n = prompt.ImageCount,
                size = prompt.ImageSize,
                model = prompt.Model,
                style = prompt.ImageStyle,
                quality = prompt.ImageQuality,
                response_format = outputFormat == ImageGenerationOutputFormats.Base64 ? "b64_json" : "url"
            };

            var imageResults = new List<ImageResult>();
            ImageResults response;


            using (var client = ChatHttpClient.GetHttpClient())
            {
                var json = JsonConvert.SerializeObject(requiredImage);
                string endPointUrl = ChatHttpClient.GetEndpointUrl("images/generations");
                var message = await client.PostAsync(endPointUrl, new StringContent(json, Encoding.UTF8, "application/json"));

                if (message.IsSuccessStatusCode)
                {
                    var content = await message.Content.ReadAsStringAsync();
                    response = JsonConvert.DeserializeObject<ImageResults>(content);

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
                            await prompt.DownloadImageToFile();
                        }
                        catch (Exception ex)
                        {
                            SetError("Download failed: " + ex.Message);
                            return false;
                        }
                    }
                    return true;
                }


                if (message.Content.Headers.ContentLength > 0 && message.Content.Headers.ContentType?.ToString() == "application/json")
                {
                    json = await message.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<dynamic>(json);
                    string msg = error.error?.message;
                    SetError($"Image generation failed: {msg}");
                }

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

        }



        /// <summary>
        /// Currently doesn't work with Dall-E-3
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="createImageFile"></param>
        /// <returns></returns>
        public async Task<bool> CreateVariation(ImagePrompt prompt,
            bool createImageFile = false,
            ImageGenerationOutputFormats outputFormat = ImageGenerationOutputFormats.Url)
        {
            var imageFile = prompt.VariationImageFilePath;
            if (string.IsNullOrEmpty(imageFile) || !File.Exists(imageFile))
            {
                SetError("Input image file not found for variation.");
                return false;
            }

            var imglink = new List<ImageResult>();

            var ext = Path.GetExtension(imageFile).ToLower();
            var filename = Path.GetFileName(imageFile);

            using (var client = ChatHttpClient.GetHttpClient())
            {
                var formContent = new MultipartFormDataContent();
                HttpResponseMessage message;
                using (var stream = File.OpenRead(imageFile))
                {
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                    //fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
                    formContent.Add(fileContent, "image", filename);

                    //formContent.Add(new StringContent(prompt.Model), "model");
                    formContent.Add(new StringContent(prompt.ImageSize), "size");

                    formContent.Add(new StringContent(outputFormat == ImageGenerationOutputFormats.Url ? "url" : "b64_json"), "response_format");

                    var endPointUrl = ChatHttpClient.GetEndpointUrl("images/variations");
                    message = await client.PostAsync(endPointUrl, formContent);
                }

                if (message.IsSuccessStatusCode)
                {
                    var content = await message.Content.ReadAsStringAsync();
                    var response = JsonConvert.DeserializeObject<ImageResults>(content);

                    foreach (var url in response.data)
                    {
                        var result = new ImageResult()
                        {
                            Url = url.url,
                            Base64Data = url.b64_json,
                            RevisedPrompt = url.revised_prompt
                        };
                        imglink.Add(result);
                    }
                    prompt.ImageUrls = imglink.ToArray();

                    if (createImageFile)
                    {
                        try
                        {
                            await prompt.DownloadImageToFile();
                        }
                        catch (Exception ex)
                        {
                            SetError("Download failed: " + ex.Message);
                            return false;
                        }
                    }
                    return true;
                }

                if (message.Content.Headers.ContentLength > 0 && message.Content.Headers.ContentType?.ToString() == "application/json")
                {
                    var json = await message.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<dynamic>(json);
                    string msg = error.error?.message;
                    //string code = error.error?.code;

                    SetError($"Image generation failed: {msg}");
                }

                return false;
            }

        }

        // ReSharper disable once ArrangeModifiersOrder
        public async Task<bool> ValidateApiKey(string openAiKey)
        {

            var key = openAiKey;
            if (!Regex.IsMatch(key, @"^sk-[a-zA-Z0-9]{32,}$"))
                return false;


            using var client = ChatHttpClient.GetHttpClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            client.DefaultRequestHeaders.Clear();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

            var endPointUrl = ChatHttpClient.GetEndpointUrl("models");
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
        #endregion

    }

    #region OpenAI JSON Structures

    /// <summary>
    /// Open AI Image Request object in the format the API expects
    /// </summary>
    internal class ImageRequest
    {
        public string prompt { get; set; }

        public string model { get; set; } = "dall-e-3";

        public int n { get; set; } = 1;

        public string size { get; set; } = "1024x1024";

        public string response_format { get; set; } = "url";  // b64_json

        public string style { get; set; } = "vivid";  // natural
        public string quality { get; set; } = "standard";
    }

    internal class ImageUrls
    {
        public string url { get; set; }

        public string b64_json { get; set; }

        public string revised_prompt { get; set; }
    }

    internal class ImageResults
    {
        public long created { get; set; }
        public List<ImageUrls> data { get; set; }

    }

    public enum ImageGenerationOutputFormats
    {
        Url,
        Base64
    }

    #endregion
}