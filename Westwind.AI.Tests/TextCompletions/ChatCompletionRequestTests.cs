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

        private static async Task<(string Method, string Path, string Body)>
            RespondAsync(HttpListener listener, string json)
        {
            var context = await listener.GetContextAsync();
            using var body = new MemoryStream();
            await context.Request.InputStream.CopyToAsync(body);
            var request = (context.Request.HttpMethod, context.Request.Url.AbsolutePath,
                Encoding.UTF8.GetString(body.ToArray()));

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
            return request;
        }
    }
}
