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
   ModelId = "dall-e-3"
};
```

> Note this is the simplest use case for OpenAI. Depending on what provider you connect to you may have to provide additional connection information like an endpoint url [More info below](#configuration-and-authorization).

Alternately you can use Configuration manager that can hold multiple connections so you can quickly rotate switch between different providers and models:

```cs
Configurations = OpenAiConnectionConfiguration.Load();
var connection = Configuration.ActiveConnection;  // based on index
connection = Configurations.Connections.FirstOrDefault(c=> c.Name == "Azure OpenAI")
```

### Image Generation
Capturing to a Url:

```cs
Configurations = OpenAiConnectionConfiguration.Load();
var config = Configurations.ActiveConnection;

var generator = new OpenAiImageGeneration(Config);

var imagePrompt = new ImagePrompt()
{
    Prompt = "A bear holding on to a snowy mountain peak, waving a beer glass in the air. Poster style, with a black background in goldenrod line art",
    ImageSize = "1024x1024",
    ImageQuality = "standard",
    ImageStyle = "vivid"
};

// Generate and set properties on `imagePrompt` instance
Assert.IsTrue(await generator.Generate(imagePrompt), generator.ErrorMessage);

// prompt returns an array of images, but for Dall-e-3 it's always one
// so FirstImage returns the first image and FirstImageUrl returns the url.
var imageUrl = imagePrompt.FirstImageUrl;
Console.WriteLine(imageUrl);

// Display the image as a Url
ShellUtils.GoUrl(imageUrl);

// Typically the AI **fixes up the prompt**
Console.WriteLine(imagePrompt.RevisedPrompt);

// You can download the image from the captured URL to a local file
// Default folder is %temp%\openai-images\images or specify `ImageFolderPath`
// imagePrompt.ImageFolderPath = "c:\\temp\\openai-images\\"; 
Assert.IsTrue(await imagePrompt.DownloadImageToFile(), "Image saving failed: " + generator.ErrorMessage);

string imageFile = imagePrompt.ImageFilePath;
Console.WriteLine(imageFile);

Console.WriteLine(JsonSerializationUtils.Serialize(imagePrompt, formatJsonOutput: true));
```

To capture base64 content:

```csharp
var generator = new OpenAiImageGeneration(Configuration);        

var imagePrompt = new ImagePrompt()
{
    Prompt = "A bear holding on to a snowy mountain peak, waving a beer glass in the air. Poster style, with a black background in goldenrod line art",
    ImageSize = "1024x1024",
    ImageQuality = "standard",
    ImageStyle = "vivid"
};

bool result = await generator.Generate(imagePrompt, outputFormat: ImageGenerationOutputFormats.Base64);

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

### Chat Completions
This library provides basic Chat Completions that directly pass messages to the API and return the result. If you need more sophisticated functionality for adding custom processing or additional training data, use [Microsoft Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/). It provides all the features used here, but at a significantly bigger footprint.

This library is geared towards simple interactions that provide small and fast local application integrations especially for working with local models.

```csharp
[TestMethod]
public async Task GenericCompletionTest()
{
    var completion = new GenericAiChat(Connection);

    string result = await completion.Complete(                
        "Translate the following from English to German:\nThe sky is below, the ground is above",
        "You are a translator that translates between languages. Return only the translated text.");

    // optionally captured request and response data
    Assert.IsNotNull(result, completion.ErrorMessage);
    Console.WriteLine(result); 
}
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
  "Connections": [
    {
      "Name": "OpenAI",
      "ApiKey": "sk-wp...",
      "Endpoint": "https://api.openai.com/v1/",
      "EndpointTemplate": "{0}/{1}",
      "ModelId": "gpt-3.5-turbo",
      "ConnectionMode": "OpenAi",
      "OperationMode": "Completions"
    },
    {
      "Name": "Azure OpenAi",
      "ApiKey": "a26e3...",
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
      "ConnectionMode": "OpenAi",
      "OperationMode": "Completions"
    },
    {
      "Name": "Ollama Phi3",
      "ApiKey": "",
      "Endpoint": "http://127.0.0.1:11434/v1/",
      "EndpointTemplate": "{0}/{1}",
      "ModelId": "phi3",
      "ConnectionMode": "OpenAi",
      "OperationMode": "Completions"
    },
    {
      "Name": "OpenAI Dall-E",
      "ApiKey": "sk-wpDP...",
      "Endpoint": "https://api.openai.com/v1/",
      "EndpointTemplate": "{0}/{1}",
      "ModelId": "dall-e-3",
      "ConnectionMode": "OpenAi",
      "OperationMode": "ImageGeneration"
    },
    {
      "Name": "Azure OpenAi Dall-E",
      "ApiKey": "62...",
      "Endpoint": "https://rasopenaisample.openai.azure.com/",
      "EndpointTemplate": "{0}/openai/deployments/{2}/{1}?api-version={3}",
      "ModelId": "ImageGenerations",
      "ApiVersion": "2024-02-15-preview",
      "ConnectionMode": "AzureOpenAi",
      "OperationMode": "ImageGeneration"
    }
  ]
```

The values used depend on whether you're accessing OpenAI or an openAI compatible API or Azure OpenAi. Azure uses a deployments to manage models and uses non-standard Api key referencing.

You can also create the connections directly in code if you prefer.


### OpenAI Connections
This is the easiest configuration you basically only need to set the ApiKey and Model name in code.

For Chat:

```cs
var config = new OpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "gpt-4"
};
```

For Images:

```cs
var config = new OpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "dall-e-3"
};
```

### Azure 
Azure uses a different logon mechanism and requires that you set up an Azure site and separate deployments for each of the models you want to use. As such you specify the **Deployment Name** as the ModelId.

For Chat or Images:

```cs
var config = new AzureOpenAiConnection() {
   ApiKey = myApiKey,
   ModelId = "MyGtp35tDeployment",    // "MyDalleDeployment"
   EndPoint = "https://myAzureSite.openai.azure.com/"
};
```

### Ollama Local
You can also use any local SMLs that support OpenAI. If you use the popular Ollama docker install locally you can host any of the models by running `ollama serve llama3`  (or whatever other model that you've pulled).

```cs
var config = new OpenAiConnection() {
   Endpoint="https://127.0.0.1:11434/v1/",
   ApiKey = myApiKey,
   ModelId = "phi3" // "llama3"
};
```

The model works with any downloaded model. Note that ollama also will automatically switch between models, but it can be slow as new models are loaded.