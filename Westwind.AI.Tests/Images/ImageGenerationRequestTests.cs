using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Westwind.AI.Configuration;
using Westwind.AI.Images;

namespace Westwind.Ai.Test
{
    [TestClass]
    public class ImageGenerationRequestTests
    {
        [TestMethod]
        [DataRow(-1, 200, false)]
        [DataRow(0, 200, false)]
        [DataRow(1, 200, false)]
        [DataRow(2, 200, false)]
        [DataRow(2, 400, false)]
        [DataRow(2, 200, true)]
        [DataRow(2, 204, false)]
        public async Task GenerateRoutesRequestTest(int baseImageCount, int statusCode, bool emptyResponse)
        {
            using var portReservation = new TcpListener(IPAddress.Loopback, 0);
            portReservation.Start();
            int port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
            portReservation.Stop();

            string endpoint = $"http://localhost:{port}/";
            using var listener = new HttpListener();
            listener.Prefixes.Add(endpoint);
            listener.Start();

            string base64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=";
            byte[] imageBytes = Convert.FromBase64String(base64);
            string responseJson = statusCode == 204 ? string.Empty : statusCode != 200
                ? """{"error":{"message":"Reference image rejected"}}"""
                : emptyResponse ? """{"data":[]}"""
                : new JObject
                {
                    ["data"] = new JArray(new JObject { ["b64_json"] = base64 })
                }.ToString();

            var requestTask = RespondAsync(listener, statusCode, responseJson);
            var generator = new OpenAiImageGeneration(new OpenAiConnection
            {
                Endpoint = endpoint,
                ModelId = "gpt-image-1.5",
                ApiKey = "test-key",
                ApiKeyHeaderName = "x-test-key",
                ApiKeyHeaderScheme = null,
                OperationMode = AiOperationModes.ImageGeneration
            });
            generator.AiHttpClient.Timeout = TimeSpan.FromSeconds(10);
            generator.AiHttpClient.CaptureRequestData = true;

            var prompt = new ImagePrompt
            {
                Model = "gpt-image-2",
                Prompt = "Combine the landscape and stylized sun.",
                ImageQuality = "high",
                ImageBackground = "opaque",
                ImageStyle = "vivid",
                BaseImages = baseImageCount < 0 ? null : new List<string>
                {
                    Path.Combine(AppContext.BaseDirectory, "Images", "ArlingtonBackLoop.png"),
                    Path.Combine(AppContext.BaseDirectory, "Images", "StylizedSun.png")
                }[..baseImageCount]
            };

            bool result = await generator.Generate(prompt);
            var request = await requestTask.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.AreEqual("POST", request.Method);
            Assert.AreEqual("test-key", request.ApiKey);
            Assert.AreEqual(baseImageCount > 0 ? "/images/edits" : "/images/generations", request.Path);
            StringAssert.StartsWith(request.ContentType, "application/json");
            var body = JObject.Parse(request.Body);
            Assert.AreEqual(prompt.Prompt, (string)body["prompt"]);
            Assert.AreEqual(prompt.Model, (string)body["model"]);
            Assert.AreEqual(1, (int)body["n"]);
            Assert.AreEqual("1024x1024", (string)body["size"]);
            Assert.AreEqual("high", (string)body["quality"]);
            Assert.AreEqual("opaque", (string)body["background"]);
            Assert.IsNull(body["style"]);
            Assert.IsNull(body["response_format"]);
            StringAssert.Contains(generator.AiHttpClient.LastRequestJson, request.Body);
            StringAssert.Contains(generator.AiHttpClient.LastRequestJson, endpoint.TrimEnd('/') + request.Path);

            if (baseImageCount > 0)
            {
                var images = body["images"] as JArray;
                Assert.IsNotNull(images);
                Assert.AreEqual(baseImageCount, images.Count);
                for (int i = 0; i < baseImageCount; i++)
                {
                    string dataUrl = (string)images[i]["image_url"];
                    StringAssert.StartsWith(dataUrl, "data:image/png;base64,");
                    byte[] uploadedBytes = Convert.FromBase64String(dataUrl.Split(',')[1]);
                    CollectionAssert.AreEqual(
                        await File.ReadAllBytesAsync(prompt.BaseImages[i]), uploadedBytes);
                }
                Assert.IsNull(body["moderation"]);
            }
            else
            {
                Assert.IsNull(body["images"]);
                Assert.AreEqual("low", (string)body["moderation"]);
            }

            Assert.AreEqual(responseJson, generator.AiHttpClient.LastResponseJson);
            if (statusCode != 200 || emptyResponse)
            {
                Assert.IsFalse(result);
                StringAssert.Contains(generator.ErrorMessage,
                    emptyResponse ? "No images returned" :
                    statusCode == 204 ? "Empty response" : "Reference image rejected");
                return;
            }

            Assert.IsTrue(result, generator.ErrorMessage);
            Assert.AreEqual(1, prompt.ImageUrls.Length);
            CollectionAssert.AreEqual(imageBytes, prompt.GetBytesFromBase64());
        }

        [TestMethod]
        [DataRow(@"Images\missing.png", "not found")]
        [DataRow(null, "not found")]
        [DataRow("Westwind.AI.Tests.dll", "Unsupported reference image format")]
        public async Task GenerateRejectsInvalidReferenceTest(string filename, string expectedError)
        {
            var generator = new OpenAiImageGeneration(new OpenAiConnection
            {
                Endpoint = "http://localhost:1/",
                OperationMode = AiOperationModes.ImageGeneration
            });
            string imageFile = filename == null ? null : Path.Combine(AppContext.BaseDirectory, filename);
            var prompt = new ImagePrompt
            {
                Model = "gpt-image-2",
                Prompt = "Combine the images.",
                BaseImages = [imageFile]
            };

            Assert.IsFalse(await generator.Generate(prompt));
            StringAssert.Contains(generator.ErrorMessage, expectedError);
        }

        private static async Task<(string Method, string Path, string ContentType, string ApiKey, string Body)>
            RespondAsync(HttpListener listener, int statusCode, string json)
        {
            var context = await listener.GetContextAsync();
            using var body = new MemoryStream();
            await context.Request.InputStream.CopyToAsync(body);
            var request = (context.Request.HttpMethod, context.Request.Url.AbsolutePath,
                context.Request.ContentType, context.Request.Headers["x-test-key"],
                Encoding.UTF8.GetString(body.ToArray()));

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
            return request;
        }
    }
}
