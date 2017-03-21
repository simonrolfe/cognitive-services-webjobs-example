using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Host;
using System.Net;
using Microsoft.Azure;

namespace CognitivePoCWebJobs
{
    public class AnalyseEmotions
    {
        public static void Run([ServiceBusTrigger("images", "emotions")] string imageMessage, TraceWriter log)
        {
            log.Info($"AnalyseEmotions topic trigger processed message: {imageMessage}");

            Emotion[] emotionDetectionInfo = DetectEmotions(imageMessage, CloudConfigurationManager.GetSetting("emotionAPIKey"), log).Result;

            BlobAppender.AppendToBlob("emotions.txt", JsonConvert.SerializeObject(new { emotions = emotionDetectionInfo, imageUrl = imageMessage }));

            log.Info($"Detected Emotions for processed message {imageMessage}");
        }
        private static async Task<Emotion[]> DetectEmotions(string imageUrl, string emotionServiceApiKey, TraceWriter log)
        {
            EmotionServiceClient emotionServiceClient = new EmotionServiceClient(emotionServiceApiKey);

            int retriesLeft = int.Parse(CloudConfigurationManager.GetSetting("CognitiveServicesRetryCount"));
            int delay = int.Parse(CloudConfigurationManager.GetSetting("CognitiveServicesInitialRetryDelayms"));

            Emotion[] response = null;

            while (true)
            {
                try
                {
                    response = await emotionServiceClient.RecognizeAsync(imageUrl);
                    break;
                }
                catch (ClientException exception) when (exception.HttpStatus == (HttpStatusCode)429 && retriesLeft > 0)
                {
                    log.Info($"Emotion API call has been throttled. {retriesLeft} retries left.");
                    if (retriesLeft == 1)
                    {
                        log.Warning($"Emotion API call still throttled after {CloudConfigurationManager.GetSetting("CognitiveServicesRetryCount")} attempts, giving up.");
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
}
