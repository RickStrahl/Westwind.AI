using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Westwind.AI.Chat
{
    public class OpenAiChatRequest
    {
        public string model { get; set; }

        public List<OpenAiChatMessage> messages { get; set; } = new List<OpenAiChatMessage> { new OpenAiChatMessage() };
    }
}


