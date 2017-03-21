using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Net;
using Microsoft.Azure;

namespace CognitivePoCWebJobs
{
    public class ComputerVision_OCR
    {

        [Singleton(Mode = SingletonMode.Listener)]
        public static void Run([ServiceBusTrigger("images", "vision-ocr")]string imageMessage, TraceWriter log)
        {
            log.Info($"Computer Vision OCR topic trigger processed message: {imageMessage}");

            OcrResults ocrInfo = DetectText(imageMessage, CloudConfigurationManager.GetSetting("vision-ocr-APIKey"), log).Result;

            BlobAppender.AppendToBlob("cv-ocr.txt", JsonConvert.SerializeObject(new { ocr = ocrInfo, imageUrl = imageMessage }));

            log.Info($"Completed Computer Vision OCR for processed message {imageMessage}");
        }
        private static async Task<OcrResults> DetectText(string imageUrl, string VisionServiceApiKey, TraceWriter log)
        {
            VisionServiceClient visionServiceClient = new VisionServiceClient(VisionServiceApiKey);

            int retriesLeft = int.Parse(CloudConfigurationManager.GetSetting("CognitiveServicesRetryCount"));
            int delay = int.Parse(CloudConfigurationManager.GetSetting("CognitiveServicesInitialRetryDelayms"));

            OcrResults response = null;
            while (true)
            {
                try
                {
                    response = await visionServiceClient.RecognizeTextAsync(imageUrl);
                    break;
                }
                catch (ClientException exception) when (exception.HttpStatus == (HttpStatusCode)429 && retriesLeft > 0)
                {
                    log.Info($"Computer Vision OCR call has been throttled. {retriesLeft} retries left.");
                    if (retriesLeft == 1)
                    {
                        log.Warning($"Computer Vision OCR call still throttled after {CloudConfigurationManager.GetSetting("CognitiveServicesRetryCount")} attempts, giving up.");
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
