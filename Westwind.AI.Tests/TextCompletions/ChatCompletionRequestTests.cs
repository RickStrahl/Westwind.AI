using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Westwind.AI.Chat;
using Westwind.AI.Configuration;

namespace Westwind.AI.Tests.TextCompletions
{
    /// <summary>
    /// Request shape tests that run against a local HttpListener instead of a live
    /// service so they verify what is actually sent to the server.
    /// </summary>
    [TestClass]
    public class ChatCompletionRequestTests
    {
        // useHttpTransport = true forces the OpenAiHttpClient fallback via a custom API key header,
        // false uses the Microsoft.Extensions.AI / OpenAI SDK transport.
        [TestMethod]
        [DataRow(false, "high")]
        [DataRow(true, "high")]
        [DataRow(false, null)]
        [DataRow(true, null)]
        [DataRow(true, " ")]
        public async Task ReasoningEffortIsSentWithChatRequestTest(bool useHttpTransport, string reasoningEffort)
        {
            using var portReservation = new TcpListener(IPAddress.Loopback, 0);
            portReservation.Start();
            int port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
            portReservation.Stop();

            string endpoint = $"http://localhost:{port}/";
            using var listener = new HttpListener();
            listener.Prefixes.Add(endpoint);
            listener.Start();

            const string responseJson = """
                {"id":"chatcmpl-1","object":"chat.completion","created":1,"model":"test-model",
                 "choices":[{"index":0,"message":{"role":"assistant","content":"Der Himmel ist unten."},"finish_reason":"stop"}],
                 "usage":{"prompt_tokens":1,"completion_tokens":1,"total_tokens":2}}
                """;

            var requestTask = RespondAsync(listener, responseJson);

            var connection = new OpenAiConnection
            {
                Endpoint = endpoint,
                ModelId = "test-model",
                ApiKey = "test-key",
                ReasoningEffort = reasoningEffort
            };
            if (useHttpTransport)
            {
                connection.ApiKeyHeaderName = "x-test-key";
                connection.ApiKeyHeaderScheme = null;
            }

            var client = new GenericAiChatClient(connection);
            client.AiHttpClient.Timeout = TimeSpan.FromSeconds(10);

            string result = await client.Complete("Translate: The sky is below", "You are a translator.");
            var request = await requestTask.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.IsFalse(client.HasError, client.ErrorMessage);
            Assert.AreEqual("Der Himmel ist unten.", result);

            Assert.AreEqual("POST", request.Method);
            Assert.AreEqual("/chat/completions", request.Path);
            var body = JObject.Parse(request.Body);
            Assert.AreEqual("test-model", (string)body["model"]);

            if (string.IsNullOrWhiteSpace(reasoningEffort))
                Assert.IsNull(body["reasoning_effort"], "reasoning_effort must be omitted when not configured");
            else
                Assert.AreEqual(reasoningEffort, (string)body["reasoning_effort"]);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CaptureIncludesWireBodiesAndClearsBetweenRequestsTest(bool useHttpTransport)
        {
            using var portReservation = new TcpListener(IPAddress.Loopback, 0);
            portReservation.Start();
            int port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
            portReservation.Stop();

            string endpoint = $"http://localhost:{port}/";
            using var listener = new HttpListener();
            listener.Prefixes.Add(endpoint);
            listener.Start();

            var connection = new OpenAiConnection
            {
                Endpoint = endpoint,
                ModelId = "test-model",
                ApiKey = "test-key"
            };
            if (useHttpTransport)
            {
                connection.ApiKeyHeaderName = "x-test-key";
                connection.ApiKeyHeaderScheme = null;
            }

            var client = new GenericAiChatClient(connection) { CaptureRequestData = true };
            client.AiHttpClient.Timeout = TimeSpan.FromSeconds(10);
            Assert.AreEqual(0d, client.LastResponseTime);

            const string responseJson = """
                {"id":"chatcmpl-1","object":"chat.completion","created":1,"model":"test-model",
                 "choices":[{"index":0,"message":{"role":"assistant","content":"first reply"},"finish_reason":"stop"}],
                 "usage":{"prompt_tokens":12,"completion_tokens":8,"total_tokens":20,
                          "completion_tokens_details":{"reasoning_tokens":3}}}
                """;
            var firstRequest = RespondAsync(listener, responseJson);
            Assert.AreEqual("first reply", await client.Complete("first prompt"));
            var first = await firstRequest.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(JToken.DeepEquals(JObject.Parse(client.LastRequestJson.Split("\n\n---")[0]), JObject.Parse(first.Body)));
            StringAssert.Contains(client.LastRequestJson, "first prompt");
            if (useHttpTransport)
                StringAssert.Contains(client.LastRequestJson, endpoint.TrimEnd('/') + "/chat/completions");
            Assert.IsTrue(JToken.DeepEquals(JObject.Parse(responseJson), JObject.Parse(client.LastResponseJson)));
            Assert.IsNotNull(client.LastUsage);
            Assert.AreEqual(12, client.LastUsage.prompt_tokens);
            Assert.AreEqual(8, client.LastUsage.completion_tokens);
            Assert.AreEqual(20, client.LastUsage.total_tokens);
            Assert.AreEqual(3, client.LastUsage.reasoning_tokens);
            Assert.IsTrue(client.LastResponseTime > 0);

            const string errorJson = """{"error":{"message":"request rejected"}}""";
            var errorRequest = RespondAsync(listener, errorJson, 400);
            Assert.IsNull(await client.Complete("second prompt"));
            var second = await errorRequest.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(client.HasError);
            Assert.IsTrue(JToken.DeepEquals(JObject.Parse(client.LastRequestJson.Split("\n\n---")[0]), JObject.Parse(second.Body)));
            StringAssert.Contains(client.LastRequestJson, "second prompt");
            Assert.IsFalse(client.LastRequestJson.Contains("first prompt"));
            Assert.IsTrue(JToken.DeepEquals(JObject.Parse(errorJson), JObject.Parse(client.LastResponseJson)));
            Assert.IsNull(client.LastUsage);
            Assert.IsTrue(client.LastResponseTime > 0, "Failed requests should retain elapsed time");

            client.CaptureRequestData = false;
            var thirdRequest = RespondAsync(listener, responseJson);
            Assert.AreEqual("first reply", await client.Complete("third prompt"));
            await thirdRequest.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsNull(client.LastRequestJson);
            Assert.IsNull(client.LastResponseJson);
            Assert.IsNotNull(client.LastUsage, "Usage should be available without JSON capture");
            Assert.AreEqual(12, client.LastUsage.prompt_tokens);
            Assert.AreEqual(8, client.LastUsage.completion_tokens);
            Assert.AreEqual(20, client.LastUsage.total_tokens);
            Assert.AreEqual(3, client.LastUsage.reasoning_tokens);
            Assert.IsTrue(client.LastResponseTime > 0, "Timing should not require JSON capture");
        }

        private static async Task<(string Method, string Path, string Body)>
            RespondAsync(HttpListener listener, string json, int statusCode = 200)
        {
            var context = await listener.GetContextAsync();
            using var body = new MemoryStream();
            await context.Request.InputStream.CopyToAsync(body);
            var request = (context.Request.HttpMethod, context.Request.Url.AbsolutePath,
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
