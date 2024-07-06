# Westwind.AI

# Configuration

```json
{
  "AuthenticationMode": "OpenAi",
  "ActiveCredentialIndex": 2,
  "AiCredentials": [
    {
      "Name": "OpenAI",
      "ApiKey": "sk-...",
      "Endpoint": "https://api.openai.com/v1/",
      "EndpointTemplate": "{0}/{1}",
      "ModelId": "gpt-3.5-turbo",
      "AuthenticationMode": "OpenAi"
    },
    {
      "Name": "Azure OpenAi",
      "ApiKey": "123...............",
      // base Azure Endpoint
      "Endpoint": "https://rasopenaisample.openai.azure.com/",
      "ModelId": "Gpt35",  // Your Deployment Id
      "EndpointTemplate": "{0}/openai/deployments/{2}/{1}?api-version={3}",
      "ApiVersion": "2024-02-15-preview",
      "AuthenticationMode": "AzureOpenAi"
    },
    {
      "Name": "Ollama llama3",
      "ApiKey": "",
      "Endpoint": "http://127.0.0.1:11434/v1/",
      "EndpointTemplate": "{0}/{1}",
      "ModelId": "llama3",
      "AuthenticationMode": "OpenAi"
    },
    {
      "Name": "Ollama Phi3",
      "ApiKey": "",
      "Endpoint": "http://127.0.0.1:11434/v1/",
      "EndpointTemplate": "{0}/{1}",
      "ModelId": "phi3",
      "AuthenticationMode": "OpenAi"
    }
  ]
}
```
