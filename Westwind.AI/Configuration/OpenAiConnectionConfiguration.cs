﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// The active connection based on the ActiveImageConnectionIndex    
        /// </summary>
        [JsonIgnore]
        public BaseOpenAiConnection ActiveImageConnection
        {
            get
            {
                if (ActiveImageConnectionIndex < 0 || ActiveImageConnectionIndex >= Connections.Count)
                {
                    ActiveImageConnectionIndex = 0;
                    if (Connections.Count < 1)
                        return null;
                }

                return Connections[ActiveImageConnectionIndex];
            }
        }

        /// <summary>
        /// The index that determines which image generation connection is the active one
        /// </summary>
        public int ActiveImageConnectionIndex { get; set; } = 0;


        /// <summary>
        /// A collection of connections. The active connection is determined by the ActiveConnectionIndex
        /// </summary>
        public List<BaseOpenAiConnection> Connections { get; set; } = new List<BaseOpenAiConnection>();


        /// <summary>
        /// Key indexer that return a named connection value or null if not found.
        /// </summary>
        /// <param name="key">Name of the connection to return</param>
        /// <returns></returns>
        public BaseOpenAiConnection this[string key] => Connections.FirstOrDefault(c => c.Name.Equals(key, System.StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Key indexer that return a connection by its index.
        /// </summary>
        /// <param name="index">Index into the connections available</param>
        /// <returns></returns>
        public BaseOpenAiConnection this[int index] {
            get {
                if (index < 0 || index >= Connections.Count)
                    return null;
                return Connections[index];
            }            
        }

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

        /// <summary>
        /// Determines whether encryption is used. On by default.
        /// Note that you can add unencrypted keys to the configuration file,
        /// and values will auto-encrypt. If you explicitly save your configuration
        /// file, values will then save encrypted.
        /// 
        /// This allows max flexibility to use encrypted keys but still allow you to
        /// manually add keys unencrypted and saved them encrypted as long as you explicitly
        /// save the configuration to file.
        /// </summary>
        public static bool UseEncryption { get; set; } = true;

        /// <summary>
        /// Encryption key used for the apikey in the configuration file.
        /// 
        /// You can override this for your own application on application startup
        /// and use an application specific or application+machine specific key.
        /// </summary>
        public static byte[] EncryptionKey { get; set; } = new byte[] { 55, 233, 33, 44, 55, 100, 99, 21, 222, 55, 99, 122, 10, 43, 37, 53, 73, 99 };

        /// <summary>
        /// Encrypted keys are postfixed so it can be identified as encrypted. Unencrypted key values
        /// are automatically encrypted when accessed.
        /// </summary>
        public static string EncryptionPostFix { get; set; } = "@|-|@";

        #endregion

    }
}