# Westwind.AI
 [![](https://img.shields.io/nuget/v/Westwind.AI.svg)](https://www.nuget.org/packages/Westwind.AI/) ![](https://img.shields.io/nuget/dt/Westwind.AI.svg)
 
![Westwind.AI Logi - Lightbulb Pen](https://raw.githubusercontent.com/RickStrahl/Westwind.AI/master/Icon_200.png)

A self-contained library that talks directly to the OpenAI HTTP API without major dependencies with explicit support for:

* OpenAI
* AzureOpenAI
* Ollama
* Any generic OpenAI API

The purpose of this library is to provide a **minimal dependencies client for raw OpenAi Completions and Image Generation operations**. It's meant for lightweight integrations into existing applications that have simple AI integration needs and don't need advanced features.
  
This library supports:

* Generic Completions and Chat History  via `GenericAiChatClient`
* Several pre-configured Text operations via `AiTextOperations`
    * Text Summary
    * Translation
    * Grammar Check
* Image Generation via `ImageGeneration`
* Easy connection creation and multiple connection management
* Basic Request/Response API only - no streaming APIs

.NET Targets:

* .NET8.0
* .NET472

> If you need streaming interfaces or want to do extended processing on your AI results with  custom functions or other add-on operations, then using one of the more powerful and heavy dependencies tools like [Semantic Kernel](https://github.com/microsoft/semantic-kernel) makes good sense.

## Usage Examples

### Quick Configuration
You can use manual configuration like this for connecting to the OpenAI API:

```cs
var connection = new OpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "gpt-4o-mini",
   OperationMode = AiOperationModes.Completions;
};
```

For image generation:

```cs
var connection = new OpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "dall-e-3",
   OperationMode = AiOperationModes.ImageGeneration;
};
```

More info on the various different connection providers (OpenAi, Azure, Ollama and generic Open AI) and how to create multiple providers to choose from and store them [is discussed below](#openai-connections) in more detail.

### Chat Completions
This library provides basic Chat Completions that directly pass messages to the API and return the result. If you need more sophisticated functionality for adding custom processing or additional training data, use [Microsoft Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/). It provides all the features used here, but at a significantly bigger footprint.

This library is geared towards simple interactions that provide small and fast local application integrations especially for working with local models.

```csharp
// Load pre-configured connection
// var config = OpenAiConnectionConfiguration.Load();
// var connection = config.ActiveConnection;

// Manual Connection
var connection = new OpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "gpt-4o-mini",
   OperationMode = AiOperationModes.Completions;
};


var completion = new GenericAiChatClient(Connection);
completion.AiHttpClient.CaptureRequestData = true;
          
string resultText = await completion.Complete(
    "Translate the following from English to German:\nThe sky is below, the ground is above",
    "You are a translator that translates between languages. Return only the translated text.");

Assert.IsFalse(completion.HasError, completion.ErrorMessage);
Assert.IsTrue(string.IsNullOrEmpty(resultText), 
              "No completion response was returned (null or empty).");
Console.WriteLine(resultText);

// optionally captured request and response data
Console.WriteLine(completion.AiHttpClient.LastRequestJson);
Console.WriteLine("\n\n" + completion.AiHttpClient.LastResponseJson);
```

Alternately you can pass a collection of prompts:

```cs
var completion = new GenericAiChatClient(Connection);
completion.AiHttpClient.CaptureRequestData = true;

var prompts = new List<OpenAiChatMessage>
{
    new OpenAiChatMessage { 
        content = "You are a translator that translates between languages. Return only the translated text.",
        role = "system" 
    },
    new OpenAiChatMessage {
        content = "Translate the following from English to German:\nThe sky is below, the ground is above",
        role = "user" 
    },
};

var resultText = await completion.Complete(prompts);

Assert.IsFalse(completion.HasError, completion.ErrorMessage);
Assert.IsTrue(string.IsNullOrEmpty(resultText), "No completion response was returned (null or empty).");
Console.WriteLine(resultText);

// optionally captured request and response data
Console.WriteLine(completion.AiHttpClient.LastRequestJson);
Console.WriteLine("\n\n" + completion.AiHttpClient.LastResponseJson);
```

### Keep track of Chat History
You can keep track of the Chat History if you keep the AiChatClient around and use `IncludeHistory`:

```csharp
string bornDate = DateTime.Now.AddYears(-30).ToString("MMMM yyyy");
string currentDate = DateTime.Now.ToString("MMMM yyyy");

Console.WriteLine("Born on: " + bornDate);

var completion = new GenericAiChatClient(Connection)
{
    AiHttpClient =  
    {
        CaptureRequestData = true
    }
};

// We have to provide the start date, otherwise it uses the AI training date 🤣
string result = await completion.Complete([               
    new OpenAiChatMessage { 
        content = "You are a helpful assistant that answers generic everyday questions precisely", 
        role = "system"   
    },
    new OpenAiChatMessage { 
        content = "My name is Rick and I was born in 1966 in Berlin, Germany.\nHow old am I on " + currentDate, 
        role = "user"   
    },
]);

Assert.IsNotNull(result, completion.ErrorMessage);
Console.WriteLine(result);

// continue conversion with the previous in chat history (uses completion.HttpClient.ChatHistory)
result = await completion.Complete("Tell me about my birth city.", 
                                   includeHistory: true);

Assert.IsNotNull(result, completion.ErrorMessage);
Console.WriteLine(result);

Console.WriteLine("---\n" +completion.AiHttpClient.LastRequestJson);
Console.WriteLine("---\n" + completion.AiHttpClient.LastResponseJson);
```



### Image Generation
You can use OpenAI image generation APIs using OpenAi and Azure OpenAi  to generate images from prompt text. Images are generated and captured along with revised prompts that you can save.

ImageGeneration works through an `ImagePrompt` class that acts as input and output for an individual image generation request.

**Capturing to a Url**

```cs
// Load pre-configured connection
// var config = OpenAiConnectionConfiguration.Load();
// var connection = config.ActiveImageConnection;

// Manual Connection
var connection = new OpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "dall-e-3",
   OperationMode = AiOperationModes.ImageGeneration;
};

var generator = new OpenAiImageGeneration(connection);

// Optionally capture raw request data for debugging
generator.AiHttpClient.CaptureRequestData = true;

var imagePrompt = new ImagePrompt()
{
    Prompt = "A bear holding on to a snowy mountain peak, waving a beer glass in the air. Poster style, with a black background in goldenrod line art",
    ImageSize = "1024x1024",
    ImageQuality = "standard",
    ImageStyle = "vivid"
};

// Generate and set properties on `imagePrompt` instance
bool result = await generator.Generate(imagePrompt), generator.ErrorMessage);

Assert.IsTrue(result, generator.ErrorMessage);

// prompt returns an array of images, but for Dall-e-3 it's always one
// so FirstImage returns the first image and FirstImageUrl returns the url.
var imageUrl = imagePrompt.FirstImageUrl;
Console.WriteLine(imageUrl);
Console.WriteLine(imagePrompt.RevisedPrompt);

// Display the image as a Url
ShellUtils.GoUrl(imageUrl);

// You can optionally download the image from the captured URL to a local file
// Default folder is %temp%\openai-images\images or specify `ImageFolderPath`
// imagePrompt.ImageFolderPath = "c:\\temp\\openai-images\\"; 
Assert.IsTrue(await imagePrompt.DownloadImageToFile(), "Image saving failed: " + generator.ErrorMessage);

string imageFile = imagePrompt.ImageFilePath;
Console.WriteLine(imageFile);

Console.WriteLine(JsonSerializationUtils.Serialize(imagePrompt, formatJsonOutput: true));

```

**Capture binary image as base64**

```csharp
var config = OpenAiConnectionConfiguration.Load();
var connection = config.ActiveImageConnection;

var generator = new OpenAiImageGeneration(connection);        

var imagePrompt = new ImagePrompt()
{
    Prompt = "A bear holding on to a snowy mountain peak, waving a beer glass in the air. Poster style, with a black background in goldenrod line art",
    ImageSize = "1024x1024",
    ImageQuality = "standard",
    ImageStyle = "vivid"
};

bool result = await generator.Generate(imagePrompt, 
                            outputFormat: ImageGenerationOutputFormats.Base64);

// Generate and set properties on `imagePrompt` instance
Assert.IsTrue(result, generator.ErrorMessage);

// prompt returns an array of images, but for Dall-e-3 it's always one
// so FirstImage returns the first image.
byte[] bytes =  imagePrompt.GetBytesFromBase64();
Assert.IsNotNull(bytes);

string file = await imagePrompt.SaveImageFromBase64();        
Assert.IsTrue(File.Exists(file));

// show image in OS viewer
Console.WriteLine("File generated: " + file);
ShellUtils.GoUrl(file);
```

## Specialized AI Completions
This library has a few common operations that you can use for:

* Text Summary
* Translation
* Grammar Checking

### Summarize
There's a custom helper for summarizing input text.

**Summarize from string**

```csharp
var completion = new AiTextOperations(Connection);            

string result = await completion.Summarize(
    textToSummarize, 
    numberOfSentences: 3);

Assert.IsNotNull(result, completion.ErrorMessage);
Console.WriteLine(result);
```

### Translate

```csharp
var translator = new AiTextOperations(Connection);
translator.AiHttpClient.CaptureRequestData = true;

Console.WriteLine("Using: " + Connection.Name);

string result = await translator.TranslateText(
    "The sky is below, the ground is above", "en", "de");

Assert.IsNotNull(result, translator.ErrorMessage);
Console.WriteLine(result);
```

### Check Grammar

```csharp
var orig = "Long story short one of the use cases that usually made me grab for the Newtonsoft library was dynamic parsing, but I'm glad to see that at some time at least some minimal support for dynamic parsing was added to the `System.Text.Json.JsonSerializer` class";
var checker = new AiTextOperations(Connection);
var result = await checker.CheckGrammar(orig);

Assert.IsNotNull(result, checker.ErrorMessage);

Console.WriteLine("Original:\n" + orig);

Console.WriteLine("\nAdjusted:\n");
Console.WriteLine(result);
```

## Configuration and Authorization
The library uses a single configuration mechanism via two classes:

* **OpenAiConnection / AzureOpenAiConnection / OllamaOpenAiConnectio**  
A specific configuration for an AI connection that contains an endpoint, model Id and Api keys. You can create these individually to configure a connection or use the configuration class that holds multiple connections that can be stored to disk.

* **OpenAiConnectionConfiguration**  
This class can be used as a container for multiple connections that you can easily switch between. There's an `ActiveConnection` that is selected based on an `ActiveConnectionIndex` to make it easy to switch between connections and models. The container can also be saved to disk for easy storage.

```json
{
  "ActiveConnectionIndex": 0,
  "ActiveImageConnectionIndex": 4,
  "Connections": [
    {
      "Name": "OpenAI",
      "ApiKey": "18064FE1...12E@|-|@",
      "Endpoint": "https://api.openai.com/v1/",
      "EndpointTemplate": "{0}/{1}",
      "ModelId": "gpt-3.5-turbo",
      "ApiVersion": null,
      "ConnectionMode": "OpenAi",
      "OperationMode": "Completions"
    },
    {
      "Name": "Azure OpenAi",
      "ApiKey": "01BA5CC...442@|-|@",
      "Endpoint": "https://rasopenaisample.openai.azure.com/",
      "EndpointTemplate": "{0}/openai/deployments/{2}/{1}?api-version={3}",
      "ModelId": "Gpt35",
      "ApiVersion": "2024-02-15-preview",
      "ConnectionMode": "AzureOpenAi",
      "OperationMode": "Completions"
    },
    {
     "Name": "NVidia",
     "ApiKey": "02AC6CD...342@|-|@",
     "Endpoint": "https://integrate.api.nvidia.com/v1/",
     "EndpointTemplate": "{0}/{1}",
     "ModelId": "meta/llama-3.1-405b-instruct",
     "ApiVersion": null,
     "ConnectionMode": "OpenAi",
     "OperationMode": "Completions"
    },
    {
      "Name": "Ollama llama3",
      "ApiKey": "",
      "Endpoint": "http://127.0.0.1:11434/v1/",
      "EndpointTemplate": "{0}/{1}",
      "ModelId": "llama3",
      "ApiVersion": null,
      "ConnectionMode": "OpenAi",
      "OperationMode": "Completions"
    },
    {
      "Name": "Ollama Phi3",
      "ApiKey": "",
      "Endpoint": "http://127.0.0.1:11434/v1/",
      "EndpointTemplate": "{0}/{1}",
      "ModelId": "phi3",
      "ApiVersion": null,
      "ConnectionMode": "OpenAi",
      "OperationMode": "Completions"
    },
    {
      "Name": "OpenAI Dall-E",
      "ApiKey": "1806416...2ACE@|-|@",
      "Endpoint": "https://api.openai.com/v1/",
      "EndpointTemplate": "{0}/{1}",
      "ModelId": "dall-e-3",
      "ApiVersion": null,
      "ConnectionMode": "OpenAi",
      "OperationMode": "ImageGeneration"
    },
    {
      "Name": "Azure OpenAi Dall-E",
      "ApiKey": "01B2C29E...E9EA@|-|@",
      "Endpoint": "https://rasopenaisample.openai.azure.com/",
      "EndpointTemplate": "{0}/openai/deployments/{2}/{1}?api-version={3}",
      "ModelId": "ImageGenerations",
      "ApiVersion": "2024-02-15-preview",
      "ConnectionMode": "AzureOpenAi",
      "OperationMode": "ImageGeneration"
    }
  ]
}
```

The values used depend on whether you're accessing OpenAI or an openAI compatible API or Azure OpenAi. Azure uses a deployments to manage models and uses non-standard Api key referencing.

You can also create the connections directly in code if you prefer.

### OpenAiConnection.Create()
You can create manual provider connections, but the easiest way to create a new provider connection manually is to use:

```cs
var apiKey = "sk-superseekrit";
var connection = OpenAiConnection.Create(AiProviderModes.OpenAi, "Open AI Connection");
connection.ApiKey = apiKey;

Assert.IsTrue(connection.ProviderMode == AiProviderModes.OpenAi,"Incorrect Provider Mode");
Assert.IsTrue(connection.OperationMode == AiOperationModes.Completions,"Incorrect Operation Mode");
Assert.IsTrue(connection.ModelId == "gpt-4o-mini","Incorrect Model"); // default 
// Important - API key is encrypted for storage so use DecryptedApiKey
Assert.AreEqual(connection.DecryptedApiKey, apiKey,"Incorrect ApiKey");
```            

Alternately you can use the name as a string (easier to use from UI):

```cs
var connection = OpenAiConnection.Create("OpenAi", "Open AI Connection");
```

The provider modes are:

* OpenAi
* AzureOpenAi
* Ollama

Anything else defaults to unconfigured OpenAi.

### Manual Provider Connections
You can of course also use manually create connections by specifying either the default `OpenAiConnection` provider and setting all properties manually, or by using a specific provider subclass that sets defaults based on the provider.


#### OpenAI Connections
This is the default connection that is used as the base configuration.

This is the easiest configuration as you only need to set the **ApiKey** and **ModelId** and for images specify `OperationModes.ImageGeneration`:

**For Completions**

```cs
var config = new OpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "gpt-4"
};
```



**For Images**

```cs
var config = new OpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "dall-e-3",
   OperationMode = AiOperationModes.ImageGeneration
};
```
##### Open AI Links:

- [Sign up for OpenAI API Account](https://platform.openai.com/signup)
- [Usage](https://platform.openai.com/usage)
- [Billing](https://platform.openai.com/account/billing/overview)
- [Usage Limits](https://platform.openai.com/account/limits)

###### Open AI API (applies to all providers)

- [Open AI Chat API Reference](https://platform.openai.com/docs/api-reference/chat)
- [Open AI Images API Reference](https://platform.openai.com/docs/api-reference/images)

#### AzureOpenAi Connections
Azure uses a different logon mechanism and requires that you set up an Azure site and separate deployments for each of the models you want to use. 

You need to specify the **Deployment Name** as the ModelId - Azure has a fixed model per deployment so the deployment is fixed to a model. 

You need to specify the **EndPoint** which is the *Base Url for the Azure Site* (without any site relative paths).

And you need an **ApiKey** to access the API.

**For Completions or Images**

```cs
var config = new AzureOpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "Gtp4oMiniDeployment",   
   EndPoint = "https://myAzureSite.openai.azure.com/"
   OperationMode = AiOperationModes.Completions
};
```


#### Ollama Local
You can also use any local SMLs that support OpenAI. If you use the popular Ollama AI Client locally you can host any of its models by running `ollama serve` after `ollama pull <model>` the model desired. 

Using Ollama you only specify the **ModelId** which references any of the models that are installed in your local Ollama setup (`llama3`, `phi3.5`, `mistral` etc.)

**For Completions**

```cs
var config = new OllamaOpenAiConnection() {
   ModelId = "phi3.5" // "llama3"
};
```

Note that Ollama can easily switch between models with the ModelId, but be aware that switching models is very slow as the model gets first loaded.

##### Ollama Links

* [Ollama Download](https://ollama.com)
* [Ollama Getting Started (GitHub)](https://github.com/ollama/ollama)
* [Ollama Model Library](https://ollama.com/library)


### Generic OpenAI
You can also connect to most other APIs that are using OpenAI's API. In these scenarios you'll need to provide all the relevant connection information including endpoint, api key and model id.

Here's an example using NVIDIA's OpenAI API:

```cs
// nvidia OpenAI
var config = new OpenAiConnection() {
   ApiKey = nvidiaApiKey,
   ModelId = "meta/llama-3.1-405b-instruct",
   Endpoind = "https://integrate.api.nvidia.com/v1/"
};
```
