using System;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs.Host;

public static class BlobAppender
{
    public static void AppendToBlob(string fileName, string textToAppend)
    {
        CloudStorageAccount account = CloudStorageAccount.Parse(AmbientConnectionStringProvider.Instance.GetConnectionString("blobConnection"));

        CloudBlobClient client = account.CreateCloudBlobClient();
        CloudBlobContainer container = client.GetContainerReference("images");

        CloudAppendBlob appendBlob = container.GetAppendBlobReference(fileName);

        if (!appendBlob.Exists())
        {
            appendBlob.CreateOrReplace();
        }

        MemoryStream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream);
        writer.Write(textToAppend + Environment.NewLine);
        writer.Flush();
        stream.Position = 0;

        appendBlob.AppendBlock(stream);
    }
}