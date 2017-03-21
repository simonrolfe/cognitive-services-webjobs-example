using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using Microsoft.Azure;

public class AnalyseFaces
{

    [Singleton(Mode = SingletonMode.Listener)]
    public static void Run([ServiceBusTrigger("images", "faces")] string imageMessage, TraceWriter log)
    {
        log.Info($"AnalyseFaces topic trigger processed message: {imageMessage}");

        Face[] faceDetectionInfo = DetectFaces(imageMessage, CloudConfigurationManager.GetSetting("faceAPIKey"), log).Result;

        BlobAppender.AppendToBlob("faces.txt", JsonConvert.SerializeObject(new { faces = faceDetectionInfo, imageUrl = imageMessage }));

        log.Info($"Detected faces for processed message {imageMessage}");
    }
    private static async Task<Face[]> DetectFaces(string imageUrl, string faceServiceApiKey, TraceWriter log)
    {
        FaceServiceClient faceServiceClient = new FaceServiceClient(faceServiceApiKey);

        List<FaceAttributeType> allFaceAttributes = new List<FaceAttributeType> {
                FaceAttributeType.Age,
                FaceAttributeType.FacialHair,
                FaceAttributeType.Gender,
                FaceAttributeType.Glasses,
                FaceAttributeType.HeadPose,
                FaceAttributeType.Smile
            };

        int retriesLeft = int.Parse(CloudConfigurationManager.GetSetting("CognitiveServicesRetryCount"));
        int delay = int.Parse(CloudConfigurationManager.GetSetting("CognitiveServicesInitialRetryDelayms"));

        Face[] response = null;

        while (true)
        {
            try
            {
                response = await faceServiceClient.DetectAsync(imageUrl, returnFaceId: true, returnFaceLandmarks: true, returnFaceAttributes: allFaceAttributes);
                break;
            }
            catch (FaceAPIException exception) when (exception.HttpStatus == (HttpStatusCode)429 && retriesLeft > 0)
            {
                log.Info($"Face API call has been throttled. {retriesLeft} retries left.");
                if (retriesLeft == 1)
                {
                    log.Warning($"Face API call still throttled after {CloudConfigurationManager.GetSetting("CognitiveServicesRetryCount")} attempts, giving up.");
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