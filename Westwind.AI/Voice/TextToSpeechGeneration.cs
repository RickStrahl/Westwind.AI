using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Westwind.AI.Chat;
using Westwind.AI.Configuration;

namespace Westwind.AI.Voice
{
    public class OpenAiTextToSpeech : AiBase
    {

        public OpenAiTextToSpeech(OpenAiConnectionConfiguration openAiAuthConfig) : base(openAiAuthConfig)
        {
            AiHttpClient = new OpenAiHttpClient(openAiAuthConfig.ActiveConnection);
        }

        public OpenAiTextToSpeech(IOpenAiConnection connection) : base(connection)
        {
            AiHttpClient = new OpenAiHttpClient(connection);
        }

        public async Task<HttpResponseMessage> ConvertTextToSpeechToResponseAsync(string text, string fileToSaveTo = null)
        {
            var requestBody = new
            {
                model = "tts-1",
                input = text,
                voice = "echo"
            };


            try
            {
                var json = JsonConvert.SerializeObject(requestBody);
                var message = await AiHttpClient.SendJsonHttpRequestToResponse(json, "audio/speech");

                if (message == null)
                {
                    SetError(AiHttpClient.ErrorMessage);
                    return null;
                }

                return message;
            }
            catch (Exception ex)
            {
                SetError(ex);
                return null;
            }
        }

        public async Task<byte[]> ConvertTextToSpeechBytesAsync(string text)
        {

            var message = await ConvertTextToSpeechToResponseAsync(text);
            if (message == null)
                return null;

            try
            {
                var mp3 = await message.Content.ReadAsByteArrayAsync();
                return mp3;
            }
            catch (Exception ex)
            {
                SetError("Unable to read response data: " + ex.Message);
                return null;
            }
        }

        public async Task<bool> ConvertTextToSpeechFileAsync(string text, string mp3FileToSave)
        {
            var message = await ConvertTextToSpeechToResponseAsync(text);
            if (message == null)
                return false;
            try
            {
                using (var responseStream = await message.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(mp3FileToSave, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await responseStream.CopyToAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                SetError("Unable to read response data: " + ex.Message);
                return false;
            }

            return true;
        }

    }
}
