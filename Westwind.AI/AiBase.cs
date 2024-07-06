﻿using System;
using Westwind.AI.Chat.Configuration;

namespace Westwind.AI.Chat
{
    public class AiBase
    {
        public IAiCredentials Credentials { get; set; }

        public AiBase(AiAuthenticationConfiguration aiAuthConfig)
        {
            Credentials = aiAuthConfig.ActiveCredential;            
            if (Credentials == null)
                throw new InvalidOperationException("No active credentials available.");
        }

        public AiBase(IAiCredentials credentials)
        {
            Credentials = credentials;
            if (Credentials == null)
                throw new InvalidOperationException("No active credentials available.");
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