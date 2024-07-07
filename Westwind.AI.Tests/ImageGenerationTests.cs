using Westwind.AI.Chat.Configuration;
using Westwind.Ai.Images;
using Westwind.Utilities;

namespace Westwind.Ai.Test;

[TestClass]
public class ImageGenerationTests
{
    public ImageGenerationTests()
    {
        Configurations = AiAuthenticationConfiguration.Load();                
        Configuration = Configurations.AiCredentials
                        .FirstOrDefault(c => c.ModelId.Contains("dall-e-3", StringComparison.OrdinalIgnoreCase));

        //Configuration = Configurations.AiCredentials
        //    .FirstOrDefault(c =>  c.Name.Contains("Azure OpenAi Dall-E"));

        if (Configuration == null)
            throw new InvalidOperationException("No Dall-e-3 configuration found.");

        ImagePrompt.DefaultImageStoragePath = Path.GetFullPath("images/GeneratedImages");
    }

    public BaseAiCredentials Configuration { get; set; }
    public AiAuthenticationConfiguration Configurations { get; set; }

    [TestMethod]
    public async Task ImageGenerationToUrlTest()
    {
        var generator = new OpenAiImageGeneration(Configuration);

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
    }

    [TestMethod]
    public async Task ImageGenerationToBase64Test()
    {
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
    }

    /// <summary>
    /// Not supported via Azure OpenAI!
    /// This only works with Dall-e-2 today and produces pretty horrid results.
    /// Try again when dall-e-3 is available for variations.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task ImageVariationToUrlTest()
    {
        var sourceImage= Path.GetFullPath("Images/PreviouslyGeneratedImage.png");

        var generator = new OpenAiImageGeneration(Configuration);
        
        var imagePrompt = new ImagePrompt()
        {
            VariationImageFilePath = sourceImage,            
            ImageSize = "1024x1024",
            ImageQuality = "standard",
            ImageStyle = "vivid",
            Model = "dall-e-2" // no effect - it uses Dall-E-2
        };

        // Generate and set properties on `imagePrompt` instance
        Assert.IsTrue(await generator.CreateVariation(imagePrompt), generator.ErrorMessage);

        // prompt returns an array of images, but for Dall-e-3 it's always one
        // so FirstImage returns the first image.
        var imageUrl = imagePrompt.FirstImageUrl;
        Console.WriteLine(imageUrl);

        // Typically the AI **fixes up the prompt**
        Console.WriteLine(imagePrompt.RevisedPrompt);

        // You can download the image from the captured URL to a local file
        // Default folder is %temp%\openai-images\images or specify `ImageFolderPath`
        // imagePrompt.ImageFolderPath = "c:\\temp\\openai-images\\"; 
        Assert.IsTrue(await imagePrompt.DownloadImageToFile(), "Image saving failed: " + generator.ErrorMessage);

        string imageFile = imagePrompt.ImageFilePath;
        Console.WriteLine(imageFile);
    }


}