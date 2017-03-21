using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Net;
using System.Configuration;
using Microsoft.Azure;

public static class ComputerVision_Analyse
{
    [Singleton(Mode = SingletonMode.Listener)]
    public static void Run([ServiceBusTrigger("images", "vision-analyse")] string imageMessage, TraceWriter log)
    {
        log.Info($"Computer Vision image analysis topic trigger processed message: {imageMessage}");

        AnalysisResult imageAnalysisInfo = AnalyseImage(imageMessage, CloudConfigurationManager.GetSetting("vision-analyse-APIKey"), log).Result;

        BlobAppender.AppendToBlob("cv-imageanalysis.txt", JsonConvert.SerializeObject(new { imageAnalysis = imageAnalysisInfo, imageUrl = imageMessage }));

        log.Info($"Completed Computer Vision image analysis for processed message {imageMessage}");
    }
    private static async Task<AnalysisResult> AnalyseImage(string imageUrl, string VisionServiceApiKey, TraceWriter log)
    {
        VisionServiceClient visionServiceClient = new VisionServiceClient(VisionServiceApiKey);

        List<VisualFeature> allVisualFeatures = new List<VisualFeature>
            {
                VisualFeature.Adult,
                VisualFeature.Categories,
                VisualFeature.Color,
                VisualFeature.Description,
                VisualFeature.Faces,
                VisualFeature.ImageType,
                VisualFeature.Tags
            };

        int retriesLeft = int.Parse(CloudConfigurationManager.GetSetting("CognitiveServicesRetryCount"));
        int delay = int.Parse(CloudConfigurationManager.GetSetting("CognitiveServicesInitialRetryDelayms"));

        AnalysisResult response = null;
        while (true)
        {
            try
            {
                response = await visionServiceClient.AnalyzeImageAsync(imageUrl, allVisualFeatures);
                break;
            }
            catch (ClientException exception) when (exception.HttpStatus == (HttpStatusCode)429 && retriesLeft > 0)
            {
                log.Info($"Computer Vision analysis call has been throttled or errored. {retriesLeft} retries left.");
                if (retriesLeft == 1)
                {
                    log.Warning($"Computer Vision analysis call still throttled or errored after {CloudConfigurationManager.GetSetting("CognitiveServicesRetryCount")} attempts, giving up.");
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