//using System.Net.Http.Headers;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using Westwind.Utilities;

//namespace Westwind.Ai.Test
//{
//    [TestClass]
//    public class RawOpenAiImageGenerationTests
//    {
//        public string OpenAiApiKey { get; private set; }

//        public RawOpenAiImageGenerationTests()
//        {

//            // from _testconfiguration-nogit.json file
//            OpenAiApiKey = TestConfiguration.Current.OpenAiApiKey;

//            // override the default image storage path (static app wide)
//            ImagePrompt.DefaultImageStoragePath = @"images/GeneratedImages";
//        }

//        [TestMethod]
//        public async Task ImageGenerationTest()
//        {
//            string apiUrl = "https://api.openai.com/v1/images/generations";


//            var json = """
//{
//    "prompt": "A bear hollding on to a snowy mountain peak, waving a beer glass in the air. Poster style, with a black background in goldenrod line art",                       
//    "size": "1024x1024",
//    "model": "dall-e-3"
//}
//""";
            
//            dynamic response;
//            using (var client = new HttpClient())
//            {                                               
//                client.DefaultRequestHeaders.Clear();

//                // OpenAI
//                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestConfiguration.Current.OpenAiApiKey);


//                var Message = await client.PostAsync(apiUrl, new StringContent(json, new MediaTypeHeaderValue("application/json")));
//                if (Message.IsSuccessStatusCode)
//                {
//                    var content = await Message.Content.ReadAsStringAsync();
//                    response = JsonConvert.DeserializeObject<JObject>(content);

//                    var resultUrl = response.data[0].url;
//                    Console.WriteLine(resultUrl);                    
//                }
//                else
//                {
//                    if (Message.StatusCode == System.Net.HttpStatusCode.BadRequest || Message.StatusCode == System.Net.HttpStatusCode.NotFound)
//                    {
//                        if (Message.Content.Headers.ContentLength > 0 && Message.Content.Headers.ContentType?.ToString() == "application/json")
//                        {
//                            json = await Message.Content.ReadAsStringAsync();
//                            dynamic error = JsonConvert.DeserializeObject<JObject>(json);
//                            Console.WriteLine(json);

//                            string msg = error.error?.message;                            
//                            Assert.Fail(msg);
//                        }
//                    }                    
//                }
//            }
//        }
//    }

//}
////// response handling
////public class ResponseModel
////{
////    public long created
////    {
////        get;
////        set;
////    }
////    public List<ImageUrls>? data
////    {
////        get;
////        set;
////    }
////}
