using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Microsoft.Extensions.AI;
using Westwind.AI.Configuration;

namespace Westwind.AI.Chat.Transport
{
    /// <summary>
    /// Bidirectional mapping between <see cref="OpenAiChatMessage"/> (the library's
    /// existing public model) and <see cref="ChatMessage"/> (Microsoft.Extensions.AI).
    /// </summary>
    internal static class ChatMessageMapper
    {
        // -----------------------------------------------------------------------
        // OpenAiChatMessage → ChatMessage
        // -----------------------------------------------------------------------

        /// <summary>
        /// Converts an <see cref="OpenAiChatMessage"/> to a Microsoft.Extensions.AI
        /// <see cref="ChatMessage"/>.  Handles text, image-URL, and base-64-data-URI
        /// content payloads.
        /// </summary>
        public static ChatMessage ToMicrosoftAiMessage(OpenAiChatMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var role = RoleFromString(message.role);
            var contents = BuildAiContents(message.content);
            return new ChatMessage(role, contents);
        }

        // -----------------------------------------------------------------------
        // ChatMessage → OpenAiChatMessage
        // -----------------------------------------------------------------------

        /// <summary>
        /// Converts a Microsoft.Extensions.AI <see cref="ChatMessage"/> back to an
        /// <see cref="OpenAiChatMessage"/>.  Only text content is mapped; image
        /// round-trips are not required by the current library surface.
        /// </summary>
        public static OpenAiChatMessage FromMicrosoftAiMessage(ChatMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var role = RoleToString(message.Role);

            var uriContent = message.Contents.OfType<UriContent>().FirstOrDefault();
            if (uriContent != null)
            {
                return new OpenAiChatMessage
                {
                    role = role,
                    content = new OpenAiContentData
                    {
                        type = "image_url",
                        image_url = new OpenAiImageUrl(uriContent.Uri.ToString())
                    }
                };
            }

            var dataContent = message.Contents.OfType<DataContent>().FirstOrDefault();
            if (dataContent != null)
            {
                return new OpenAiChatMessage
                {
                    role = role,
                    content = new OpenAiContentData
                    {
                        type = "image_url",
                        image_url = new OpenAiImageUrl(dataContent.Uri?.ToString())
                    }
                };
            }

            var text = string.Concat(message.Contents.OfType<TextContent>().Select(c => c.Text));
            return new OpenAiChatMessage { role = role, Text = text };
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static ChatRole RoleFromString(string role) => role?.ToLowerInvariant() switch
        {
            "system"    => ChatRole.System,
            "assistant" => ChatRole.Assistant,
            "tool"      => ChatRole.Tool,
            _           => ChatRole.User
        };

        private static string RoleToString(ChatRole role)
        {
            if (role == ChatRole.System)    return "system";
            if (role == ChatRole.Assistant) return "assistant";
            if (role == ChatRole.Tool)      return "tool";
            return "user";
        }

        private static IList<AIContent> BuildAiContents(object content)
        {
            var list = new List<AIContent>();

            if (content is string text)
            {
                list.Add(new TextContent(text));
                return list;
            }

            if (content is OpenAiContentData single)
            {
                list.Add(MapContentData(single));
                return list;
            }

            // Arrays of OpenAiContentData may arrive when the caller has already
            // wrapped content into the array form required by the raw HTTP API.
            if (content is IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (item is OpenAiContentData cd)
                        list.Add(MapContentData(cd));
                    else if (item is string s)
                        list.Add(new TextContent(s));
                }
            }

            return list;
        }

        private static AIContent MapContentData(OpenAiContentData data)
        {
            if (data.type == "image_url" && data.image_url != null)
            {
                var url = data.image_url.url;
                if (!string.IsNullOrEmpty(url))
                {
                    if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        return new DataContent(url, "image/*");

                    return new UriContent(url, "image/*");
                }
            }

            return new TextContent(data.text ?? string.Empty);
        }
    }
}
