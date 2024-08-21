using System;
using Westwind.AI.Configuration;

namespace Westwind.AI.Chat
{
    public class AiBase
    {
        public IOpenAiConnection Connection { get; set; }

        public OpenAiHttpClient AiHttpClient { get; set; }

        public AiBase(OpenAiConnectionConfiguration openAiAuthConfig)
        {
            Connection = openAiAuthConfig.ActiveConnection;            
            if (Connection == null)
                throw new InvalidOperationException("No active credentials available.");
            AiHttpClient = new OpenAiHttpClient(openAiAuthConfig.ActiveConnection);
        }

        public AiBase(IOpenAiConnection connection)
        {
            Connection = connection;
            if (Connection == null)
                throw new InvalidOperationException("No active credentials available.");
            AiHttpClient = new OpenAiHttpClient(Connection);
        }


        #region Error Handling

        public bool IsError => !string.IsNullOrEmpty(ErrorMessage);
        public string ErrorMessage { get; set; }


        protected void SetError(string message =null)
        {
            if (message == null )
            {
                ErrorMessage = string.Empty;
                return;
            }
            ErrorMessage += message;
        }

        protected void SetError(Exception ex, bool checkInner = false)
        {
            if (ex == null)
            {
                ErrorMessage = string.Empty;
            }
            else
            {
                Exception e = ex;
                if (checkInner)
                    e = e.GetBaseException();

                ErrorMessage = e.Message;
            }
        }
        #endregion
    }
}