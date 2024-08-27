using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Westwind.AI.Utilities;
using Westwind.Utilities;

namespace Westwind.Ai.Images
{
    /// <summary>
    /// Open AI Prompt container that holds both the request data
    /// and response urls and data if retrieved.
    /// </summary>
    public class ImagePrompt : INotifyPropertyChanged
    {

        public static string DefaultImageStoragePath = Path.Combine(Path.GetTempPath(), "OpenAi-Images", "Images");

        #region Input Properties


        /// <summary>
        /// The prompt text to use to generate the image
        /// </summary>
        public string Prompt
        {
            get => _prompt;
            set
            {
                if (value == _prompt) return;
                _prompt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        /// <summary>
        /// If using Variations mode this is the image file to use as a
        /// base for the variation.
        /// </summary>
        public string VariationImageFilePath
        {
            get => _variationImageFile;
            set
            {
                if (value == _variationImageFile) return;
                _variationImageFile = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Values dall-e-3 (default) or dall-e-2
        /// * 1024x1024  (default)
        /// * 1792x1024
        /// * 1024x1792
        /// 
        /// Values: dall-e-2
        /// * 1024x1024
        /// * 512x512
        /// * 256x256
        /// </summary>
        public string ImageSize
        {
            get => _imageSize;
            set
            {
                if (value == _imageSize) return;
                _imageSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Values:
        /// * vivid  (default)
        /// * natural
        /// </summary>
        public string ImageStyle
        {
            get => _imageStyle;
            set
            {
                if (value == _imageStyle) return;
                _imageStyle = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Values:
        /// * standard
        /// * hd
        /// </summary>
        public string ImageQuality
        {
            get => _imageQuality;
            set
            {
                if (value == _imageQuality) return;
                _imageQuality = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// For Dall-e-3 this is always 1
        /// </summary>
        public int ImageCount
        {
            get => _imageCount;
            set
            {
                if (value == _imageCount) return;
                _imageCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Values:
        /// * dall-e-3 (default)
        /// * dall-e-2 
        /// </summary>
        public string Model
        {
            get => _model;
            set
            {
                if (value == _model) return;
                _model = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Result Properties

        public ImageResult[] ImageUrls
        {
            get => _imageUrls;
            set
            {
                if (Equals(value, _imageUrls)) return;
                _imageUrls = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FirstImageUrl));
                OnPropertyChanged(nameof(FirstImageUri));
                OnPropertyChanged(nameof(FirstImage));
                OnPropertyChanged(nameof(HasImageFile));
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(RevisedPrompt));
                OnPropertyChanged(nameof(HasRevisedPrompt));
            }
        }


       
        /// <summary>
        /// If data is downloaded in Base64 JSON format
        /// </summary>
        [JsonIgnore]
        public string Base64Data
        {
            get
            {
                return FirstImage?.Base64Data;
            }
        }


        [JsonIgnore]
        public byte[] ByteData => EmbeddedBase64ToBinary(Base64Data).bytes;

        /// <summary>
        /// Decoded an embedded base64 resource string into its binary content and mime type
        /// </summary>
        /// <param name="base64Data">Embedded Base64 data (data:mime/type;b64data) </param>
        /// <returns></returns>
        private static (byte[] bytes, string mimeType) EmbeddedBase64ToBinary(string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data))
                return (null, null);

            var parts = base64Data.Split(',');
            if (parts.Length != 2)
                return (null, null);

            var mimeType = parts[0].Replace("data:", "").Replace(";base64", "");
            var data = parts[1];

            var bytes = Convert.FromBase64String(data);

            return (bytes, mimeType);
        }

        [JsonIgnore]
        public string RevisedPrompt
        {
            get
            {
                return FirstImage?.RevisedPrompt;
            }
        }

        /// <summary>
        /// Name of a captured image. File only, no path
        /// 
        /// This name is used to save and retrieve a file
        /// when using DownloadImageToFile() or when reading
        /// the byte data
        /// </summary>
        public string ImageFilename
        {
            get => _imageFilename;
            set
            {
                if (value == _imageFilename) return;
                _imageFilename = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImageFilePath));
                OnPropertyChanged(nameof(HasImageFile));
            }
        }

        #endregion

        #region Helper Properties

        /// <summary>
        /// The full path to the image associated with this
        /// Prompt or null/empty.
        /// </summary>
        [JsonIgnore]
        public string ImageFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(ImageFilename))
                    return ImageFilename;

                return GetImageFilename(ImageFilename);
            }
        }

        [JsonIgnore]
        public ImageResult FirstImage
        {
            get
            {
                return ImageUrls.FirstOrDefault();
            }
        }

        [JsonIgnore]
        public string FirstImageUrl
        {
            get
            {
                return ImageUrls.FirstOrDefault()?.Url;
            }
        }


        [JsonIgnore]
        public Uri FirstImageUri
        {
            get
            {
                var img = FirstImageUrl;
                return img != null ? new Uri(img) : null;
            }
        }

        [JsonIgnore]
        public bool HasRevisedPrompt =>
            !string.IsNullOrEmpty(RevisedPrompt);

        [JsonIgnore]
        public bool IsEmpty =>
            string.IsNullOrEmpty(Prompt) &&
            string.IsNullOrEmpty(RevisedPrompt) &&
            string.IsNullOrEmpty(ImageFilename) &&
            ImageUrls == null;

        [JsonIgnore]
        public bool HasImageFile =>
            !string.IsNullOrEmpty(ImageFilename)
            && File.Exists(ImageFilePath);


        [JsonIgnore]
        public string ImageFolderPath
        {
            get { return _imageFolderPath; }
            set
            {
                if (value == _imageFolderPath) return;
                _imageFolderPath = value;
                OnPropertyChanged();
            }
        }

        #endregion


        #region URL based Image Access

        /// <summary>
        /// Retrieves Base64 Image data from the saved file in
        /// Image Url data format that can be embedded into
        /// a document.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task<string> GetBase64DataFromImageFile(string filename = null)
        {
            var fname = GetImageFilename(filename);
            if (File.Exists(fname))
            {

#if NETFRAMEWORK
                var bytes = await FileHelper.ReadAllBytesAsync(fname);
#else
                var bytes = await File.ReadAllBytesAsync(fname);
#endif
                return HtmlUtils.BinaryToEmbeddedBase64(bytes, "image/png");
            }

            return null;
        }


        /// <summary>
        /// Retrieves bytes from the image file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task<byte[]> GetBytesFromImageFile(string filename = null)
        {
            var fname = GetImageFilename(filename);

            if (File.Exists(fname))
            {
#if NETFRAMEWORK
                return await FileHelper.ReadAllBytesAsync(fname);
#else
                return await File.ReadAllBytesAsync(fname);
#endif
            }

            return null;
        }

        /// <summary>
        /// This method fixes up the image file name to the image
        /// path. If a full path is provided it's used as is.    
        /// </summary>
        /// <param name="fileOnlyName">filename to resolve. If omitted ImageFilename is used</param>
        /// <returns></returns>
        public string GetImageFilename(string fileOnlyName = null)
        {
            if (string.IsNullOrEmpty(fileOnlyName))
                fileOnlyName = ImageFilename;
            if (string.IsNullOrEmpty(fileOnlyName))
                return fileOnlyName;

            if (fileOnlyName.Contains('\\'))
                return fileOnlyName;

            return Path.Combine(ImageFolderPath, fileOnlyName);
        }

        /// <summary>
        /// Downloads image to file based on an image url (or the first image if not provided)
        /// * Downloads file
        /// * Saves into image folder
        /// * Sets Filename to the file downloaded (filename only)
        /// </summary>
        /// <returns>true or false</returns>
        public async Task<bool> DownloadImageToFile(string imageurl = null)
        {
            if (string.IsNullOrEmpty(imageurl))
                imageurl = FirstImageUrl;

            byte[] data = null;
            try
            {
                data = await HttpUtils.HttpRequestBytesAsync(FirstImageUrl);
            }
            catch
            {
                return false;
            }

            try
            {
                ImageFilename = await WriteDataToImageFileAsync(data);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Writes binary data to an image file in the image file folder
        /// </summary>
        /// <param name="data">binary data to write</param>
        /// <returns>returns the file name only (no path)</returns>
        public async Task<string> WriteDataToImageFileAsync(byte[] data)
        {
            var shortFilename = "_" + DataUtils.GenerateUniqueId(8) + ".png";

            if (!Directory.Exists(ImageFolderPath))
                Directory.CreateDirectory(ImageFolderPath);
            var filename = Path.Combine(ImageFolderPath, shortFilename);

#if NETFRAMEWORK            
            await FileHelper.WriteAllBytesAsync(filename, data);            
#else
            await File.WriteAllBytesAsync(filename, data);
#endif

            return shortFilename;
        }

        public ImagePrompt CopyFrom(ImagePrompt existing = null, bool noImageData = false)
        {
            if (existing == null)
                existing = new ImagePrompt();

            Prompt = existing.Prompt;
            ImageSize = existing.ImageSize ?? "1024x1024";
            ImageQuality = existing.ImageQuality ?? "standard";
            ImageStyle = existing.ImageStyle ?? "vivid";
            Model = existing.Model ?? "dall-e-3";

            if (!noImageData)
            {
                ImageUrls = existing.ImageUrls;
            }

            return existing;
        }
#endregion

        #region Base64 Operations

        /// <summary>
        /// Returns the bytes from Base64 data results
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytesFromBase64()
        {
            return FirstImage?.GetBytesFromBase64();
        }


        /// <summary>
        /// Saves base64 content to a file. If filename is provided
        /// saves to that file, if no file is provided it goes into the 
        /// generated images file folder and ImageFilename is set to the
        /// generated file name on the prompt.
        /// </summary>
        /// <param name="filename">Optional - file to write to. If not provided file is created in image storage folder</param>
        /// <returns></returns>
        public async Task<string> SaveImageFromBase64(string filename = null)
        {
            if (FirstImage == null)
                return null;

            var imageBytes = FirstImage.GetBytesFromBase64();
            if (imageBytes == null) return null;
            
            if (string.IsNullOrEmpty(filename))
            {
                // create a new file and store it on the image prompt
                ImageFilename = await WriteDataToImageFileAsync(imageBytes);
                return ImageFilePath;
            }

            // write to a specific location
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
                Directory.CreateDirectory(Path.GetDirectoryName(filename));

#if NETFRAMEWORK
            await FileHelper.WriteAllBytesAsync(filename, imageBytes);
#else
            await File.WriteAllBytesAsync(filename, imageBytes);
#endif
            return filename;
        }


#endregion

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Prompt))
                return "(empty ImagePrompt)";

            return StringUtils.TextAbstract(Prompt, 45);
        }


        #region Property Changed
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region fields
        private ImageResult[] _imageUrls = new ImageResult[] { };
        private string _prompt;
        private string _imageSize = "1024x1024";
        private int _imageCount = 1;
        private string _imageStyle = "vivid";  // natural
        private string _imageFilename;
        private string _model = "dall-e-3";
        private string _imageQuality = "standard";
        private string _variationImageFile;
        private string _imageFolderPath = ImagePrompt.DefaultImageStoragePath;

        #endregion
    }

    /// <summary>
    /// Class that wraps the an Image result from the OpenAI API.
    /// 
    /// The API returns one or more image responses in an array.
    /// </summary>
    public class ImageResult
    {
        public string Url { get; set; }

        public string Base64Data { get; set; }

        public string RevisedPrompt { get; set; }

        /// <summary>
        /// Returns the
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytesFromBase64()
        {
            if (string.IsNullOrEmpty(Base64Data)) return default;
            return Convert.FromBase64String(Base64Data);
        }

        /// <summary>
        /// Saves the image stored in Base64Data to a file.
        /// </summary>
        /// <returns></returns>
        public bool SaveFileFromBase64(string filename = null)
        {
            var bytes = GetBytesFromBase64();
            if (bytes == null) return false;

            if (string.IsNullOrEmpty(filename))
            {
                filename = new ImagePrompt().GetImageFilename();
            }

            try
            {                
                var folder = Path.GetDirectoryName(Path.GetFullPath(filename));
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                File.WriteAllBytes(filename, bytes);
            }
            catch
            {
                return false;
            }

            return true;
        }


        /// <summary>
        /// Retrieves bytes from the image URL or null if it's
        /// not available.
        /// </summary>
        public async Task<byte[]> DownloadBytesFromUrl()
        {
            if (string.IsNullOrEmpty(Url)) return default;
            try
            {
                return await HttpUtils.HttpRequestBytesAsync(Url);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DownloadFileFromUrl(string targetFilename)
        {
            if (string.IsNullOrEmpty(Url)) return false;
            try
            {
                string filename = await HttpUtils.DownloadImageToFileAsync(Url, targetFilename);
                if (string.IsNullOrEmpty(filename))
                    return false;
                return true;
            }
            catch
            {
                return false;
            }
        }


        public async Task<byte[]> DownloadBytes()
        {
            if (string.IsNullOrEmpty(Base64Data) && string.IsNullOrEmpty(Url))
                return null;

            if (string.IsNullOrEmpty(Base64Data))
                return await DownloadBytesFromUrl();

            var imageBytes = GetBytesFromBase64();

            if (imageBytes == null) return null;

            return imageBytes;
        }
    }
}