using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Westwind.AI.Configuration;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Westwind.Ai.Test
{
    [TestClass]
    public class RawOpenAiImageGenerationTests
    {
        public string OpenAiApiKey { get; private set; }

        public RawOpenAiImageGenerationTests()
        {

            OpenAiConnectionConfiguration config = OpenAiConnectionConfiguration.Load();
            var connection = config.Connections.FirstOrDefault(config => config.Name == "OpenAI");
            OpenAiApiKey = connection?.DecryptedApiKey;
        }

        [TestMethod]
        public async Task ImageGenerationTest()
        {
            string apiUrl = "https://api.openai.com/v1/images/generations";


            var json = """
{
    "prompt": "A bear holding on to a snowy mountain peak, waving a beer glass in the air. Poster style, with a black background in goldenrod line art",                       
    "size": "1024x1024",
    "model": "dall-e-3"
}
""";

            dynamic response;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();

                // OpenAI
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAiApiKey);

#if NETFRAMEWORK
                var message = await client.PostAsync(apiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
#else
                var message = await client.PostAsync(apiUrl, new StringContent(json, new MediaTypeHeaderValue("application/json")));
#endif
                if (message.IsSuccessStatusCode)
                {
                    var content = await message.Content.ReadAsStringAsync();
                    response = JsonConvert.DeserializeObject<JObject>(content);

                    var resultUrl = response.data[0].url;
                    Console.WriteLine(resultUrl);
                }
                else
                {
                    if (message.StatusCode == System.Net.HttpStatusCode.BadRequest || message.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        if (message.Content.Headers.ContentLength > 0 && message.Content.Headers.ContentType?.ToString() == "application/json")
                        {
                            json = await message.Content.ReadAsStringAsync();
                            dynamic error = JsonConvert.DeserializeObject<JObject>(json);
                            Console.WriteLine(json);

                            string msg = error.error?.message;
                            Assert.Fail(msg);
                        }
                    }
                }
            }
        }
    }

}
//// response handling
//public class ResponseModel
//{
//    public long created
//    {
//        get;
//        set;
//    }
//    public List<ImageUrls>? data
//    {
//        get;
//        set;
//    }
//}
