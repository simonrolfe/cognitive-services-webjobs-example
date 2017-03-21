using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Net;
using Microsoft.Azure;

public static class ComputerVision_Describe
{
    [Singleton(Mode = SingletonMode.Listener)]
    public static void Run([ServiceBusTrigger("images", "vision-describe")] string imageMessage, TraceWriter log)
    {
        log.Info($"Computer Vision image description topic trigger processed message: {imageMessage}");

        AnalysisResult imageDescription = DescribeImage(imageMessage, CloudConfigurationManager.GetSetting("vision-describe-APIKey"), log).Result;

        BlobAppender.AppendToBlob("cv-imagedescriptions.txt", JsonConvert.SerializeObject(new { imageDesription = imageDescription, imageUrl = imageMessage }));

        log.Info($"Completed Computer Vision image description for processed message {imageMessage}");
    }
    private static async Task<AnalysisResult> DescribeImage(string imageUrl, string VisionServiceApiKey, TraceWriter log)
    {
        VisionServiceClient visionServiceClient = new VisionServiceClient(VisionServiceApiKey);

        int retriesLeft = int.Parse(CloudConfigurationManager.GetSetting("CognitiveServicesRetryCount"));
        int delay = int.Parse(CloudConfigurationManager.GetSetting("CognitiveServicesInitialRetryDelayms"));

        AnalysisResult response = null;
        while (true)
        {
            try
            {
                response = await visionServiceClient.DescribeAsync(imageUrl);
                break;
            }
            catch (ClientException exception) when (exception.HttpStatus == (HttpStatusCode)429 && retriesLeft > 0)
            {
                log.Info($"Computer Vision description call has been throttled. {retriesLeft} retries left.");
                if (retriesLeft == 1)
                {
                    log.Warning($"Computer Vision description call still throttled after {CloudConfigurationManager.GetSetting("CognitiveServicesRetryCount")} attempts, giving up.");
                }

                await Task.Delay(delay);
                retriesLeft--;
                delay *= 2;
                continue;
            }
        }

        return response;
    }
}
