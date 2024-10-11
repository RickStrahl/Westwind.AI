using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Westwind.AI.Chat;
using Westwind.AI.Configuration;

namespace Westwind.AI.Voice
{
    /// <summary>
    /// Generates text to speech using the OpenAI TTS1 model
    /// </summary>
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


        /// <summary>
        /// Converts text to speech and returns the mp3 audio as a byte array.
        /// </summary>
        /// <param name="text">Text</param>        
        /// <param name="voice">Voice used: echo, alloy, fable, onyx, nova, shimmer</param>
        /// <returns>true or false</returns>
        public async Task<byte[]> ConvertTextToSpeechBytesAsync(string text, string voice = TextToSpeechVoices.echo)
        {
            var message = await ConvertTextToSpeechToResponseAsync(text, voice);
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

        /// <summary>
        /// Converts text to speech and saves the audio as an mp3 file you specify.
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="mp3FileToSave">File to save the audio to</param>
        /// <param name="voice">Voice used: echo, alloy, fable, onyx, nova, shimmer</param>
        /// <returns>true or false</returns>
        public async Task<bool> ConvertTextToSpeechFileAsync(string text, string mp3FileToSave, string voice = TextToSpeechVoices.echo)
        {
            var message = await ConvertTextToSpeechToResponseAsync(text, voice);
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

        /// <summary>
        /// Converts text to speech and returns the audio as a HttpResponseMessage. This is the base
        /// method that makes the HTTP call and handles errors and is used by the higher level methods
        /// that return byte[] or save to a file.
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="voice">Voice used: echo, alloy, fable, onyx, nova, shimmer</param>
        /// <returns>HttpResponseMessage object or null</returns>
        public async Task<HttpResponseMessage> ConvertTextToSpeechToResponseAsync(string text, string voice = TextToSpeechVoices.echo)
        {
            var requestBody = new
            {
                model = "tts-1",
                input = text,
                voice = voice
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

    }

    public class TextToSpeechVoices
    {
        public const string echo = "echo";
        public const string alloy = "alloy";
        public const string fable = "fable";
        public const string onyx = "onyx";
        public const string nova = "nova";
        public const string shimmer = "shimmer";
    }
}
