using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.IO;

namespace CognitivePoCWebJobs
{
    public class EnqueueImagesInFile
    {
        public void Run([BlobTrigger("input-files/{name}")] TextReader blobContents, [ServiceBus("images")] ICollector<string> enqueuedImages, TraceWriter log)
        {
            log.Info($"C# Blob trigger EnqueueImagesInFile function started.");

            string line = blobContents.ReadLine();

            while (!string.IsNullOrEmpty(line))
            {
                string imageUrl = line.Split('\t')[1];
                enqueuedImages.Add(imageUrl);

                line = blobContents.ReadLine();
            }
        }
    }
}
