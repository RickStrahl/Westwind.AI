using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using Westwind.Utilities;

namespace Westwind.AI.Chat.Configuration
{
    public class AiAuthenticationConfiguration
    {
        public AiAuthenticationModes AuthenticationMode { get; set; } = AiAuthenticationModes.OpenAi;

        [JsonIgnore]
        public BaseAiCredentials ActiveCredential
        {
            get
            {
                if (ActiveCredentialIndex < 0 || ActiveCredentialIndex >= AiCredentials.Count)
                {
                    ActiveCredentialIndex = 0;
                    if (AiCredentials.Count < 1)
                        return null;
                }             

                return AiCredentials[ActiveCredentialIndex];
            }
        }

        public int ActiveCredentialIndex { get; set; } = 0;

        public List<BaseAiCredentials> AiCredentials { get; set; } = new List<BaseAiCredentials>();

        public static AiAuthenticationConfiguration Load(string filename = "_AiAuthenticationConfiguration.json")
        {        
            bool exists = File.Exists(filename);
            var auth = JsonSerializationUtils.DeserializeFromFile<AiAuthenticationConfiguration>(filename, true);
            if (auth == null)
                auth = new AiAuthenticationConfiguration();

            return auth;
        }

        public bool Save(string filename = "_AiAuthenticationConfiguration.json")
        {
            return JsonSerializationUtils.SerializeToFile(this, filename, formatJsonOutput: true);
        }
    }
}