# cognitive-services-webjobs-example
An example project that uses Web Jobs to call the Cognitive Services APIs to analyse a set of images.

## Setup
### Connection strings
`AzureWebJobsDashboard` - the standard WebJobs dashboard storage account string.

`AzureWebJobsStorage` - the WebJobs storage string.

`AzureWebJobsServiceBus` - the Azure Service Bus topic connection string. Requires Send and Listen rights on the topic, which must be called `images`. If given Manage rights, the subscriptions used by the jobs will be created automatically. Otherwise, subscriptions called `emotions`, `faces`, `vision-analyse`, `vision-describe`, and `vision-ocr` must be created.

`BlobConnection` - used to both read the input files from a container called `input-files`, and write the output files, to a container called `images`.

### Application configuration
`faceAPIKey` - the API key for the Cognitive Services Face API account.

`emotionAPIKey` - the API key for the Cognitive Services Emotion API account.

`vision-analyse-APIKey` - the API key for the Cognitive Services Vision API account. The same key can be used for `vision-describe-APIKey` and `vision-ocr-APIKey` below.

`vision-describe-APIKey` - the API key for the Cognitive Services Vision API account. 

`vision-ocr-APIKey` - the API key for the Cognitive Services Vision API account.

`CognitiveServicesRetryCount` - how many times to retry if the Cognitive Services calls are rate limited.

`CognitiveServicesInitialRetryDelayms` - initial retry delay in ms if the Cognitive Services calls are rate limited. Exponential back-off is used, doubling each retry.

