using System;
using System.IO;
using System.Threading.Tasks;

namespace Westwind.AI.Utilities
{
    /// <summary>
    /// File helper to provide missing async file byte operations in .NET Framework 
    /// </summary>
    internal class FileHelper
    {

        public static async Task<byte[]> ReadAllBytesAsync(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            using (var sourceStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            {
                var buffer = new byte[sourceStream.Length];
                int bytesRead = 0;
                while (bytesRead < buffer.Length)
                {
                    int read = await sourceStream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);
                    if (read == 0)
                        break;
                    bytesRead += read;
                }
                return buffer;
            }
        }

        public static async Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            using (var destinationStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await destinationStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }
    }
}