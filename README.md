# Westwind.AI

<img src='Icon.png' width=200 />

A self-contained library that talks directly to the OpenAI HTTP API without major dependencies.

This library supports:

* OpenAI Image Generation via `ImageGeneration` class
* Chat Integrations via `GenericAiChat`


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

* Summarization
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

* **OpenAiConnection / AzureOpenAiConnection**  
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


### OpenAI Connections
This is the easiest configuration you basically only need to set the ApiKey and Model name in code.

For Chat:

```cs
var config = new OpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "gpt-4o-mini"
};
```

For Images:

```cs
var config = new OpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "dall-e-3",
   OperationMode = AiOperationModes.ImageGeneration
};
```

### Azure 
Azure uses a different logon mechanism and requires that you set up an Azure site and separate deployments for each of the models you want to use. As such you specify the **Deployment Name** as the ModelId.

For Chat or Images:

```cs
var config = new AzureOpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "MyGtp35tDeployment",    // "MyDalleDeployment"
   EndPoint = "https://myAzureSite.openai.azure.com/",
   OperationMode = AiOperationModes.ImageGeneration  // or .Completions ImageGeneration
};
```

The actual model or operation mode is determined by the 

### Ollama Local
You can also use any local SLMs that support OpenAI. If you use the popular Ollama install locally you can host any of the models by running `ollama serve `  (or whatever other model that you've pulled). 

For Chat:

```cs
var config = new OpenAiConnection() {
   Endpoint="https://127.0.0.1:11434/v1/",
   ApiKey = myApiKey,
   ModelId = "phi3" // "llama3"
};
```

You can then use the `ModelId` to switch between the different locally installed models. Keep in mind that switching models is initially very slow as the large local models have to be loaded. Subsequent operations against the same model are significantly faster than first hits.

In general you'll get better results from the online models - both for performance and quality.