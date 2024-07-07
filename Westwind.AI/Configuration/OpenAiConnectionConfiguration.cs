using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Westwind.Utilities;

namespace Westwind.AI.Chat.Configuration
{

    /// <summary>
    /// A container of multiple OpenAI connections that can be persisted to disk.
    /// 
    /// This class facilitates managing multiple connections and selecting the active one
    /// so you can quickly switche between different models and providers.
    /// </summary>
    public class OpenAiConnectionConfiguration
    {
        /// <summary>
        /// The active connection based on the ActiveConnectionIndex    
        /// </summary>
        [JsonIgnore]
        public BaseOpenAiConnection ActiveConnection
        {
            get
            {
                if (ActiveConnectionIndex < 0 || ActiveConnectionIndex >= Connections.Count)
                {
                    ActiveConnectionIndex = 0;
                    if (Connections.Count < 1)
                        return null;
                }             

                return Connections[ActiveConnectionIndex];
            }
        }

        /// <summary>
        /// The index that determines which connection is the active one
        /// </summary>
        public int ActiveConnectionIndex { get; set; } = 0;

        /// <summary>
        /// A collection of connections. The active connection is determined by the ActiveConnectionIndex
        /// </summary>
        public List<BaseOpenAiConnection> Connections { get; set; } = new List<BaseOpenAiConnection>();

        
        /// <summary>
        /// Loads configuration from a file on disk
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static OpenAiConnectionConfiguration Load(string filename = "_AiAuthenticationConfiguration.json")
        {        
            bool exists = File.Exists(filename);
            var auth = JsonSerializationUtils.DeserializeFromFile<OpenAiConnectionConfiguration>(filename, true);
            if (auth == null)
                auth = new OpenAiConnectionConfiguration();

            return auth;
        }

        /// <summary>
        /// Saves this set of connections to a file on disk
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool Save(string filename = "_AiAuthenticationConfiguration.json")
        {
            return JsonSerializationUtils.SerializeToFile(this, filename, formatJsonOutput: true);
        }

        #region Global Encryption Configuration

        public static bool UseEncryption { get; set; } = true;
        public static byte[] EncryptionKey { get; set; } = new byte[] { 55, 233, 33, 44, 55, 100, 99, 21, 222, 55, 99, 122, 10, 43, 37, 53, 73, 99 };
        public static string EncryptionPostFix { get; set; } = "@|-|@";

        #endregion

    }
}