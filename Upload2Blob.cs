using System;
// using System.IO;
// using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
// using Newtonsoft.Json;


using Azure.Storage.Blobs;
// using Azure.Storage.Blobs.Models;

namespace upload2blob
{
    public static class Upload2Blob
    {
        [FunctionName("Upload2Blob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Upload2Blob function -  processing a request.");

            string connectionString = Environment.GetEnvironmentVariable("STORAGE");
            string containerName = Environment.GetEnvironmentVariable("LANDING_CONTAINER");
            log.LogInformation($"Container Name: {containerName}");

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);  

            log.LogInformation($" the request content type is: {req.ContentType} -- {req.HasFormContentType} -- ");
            
            var formdata = await req.ReadFormAsync();

                        
            string name = formdata["name"];
            
            
            BlobClient blobClient = containerClient.GetBlobClient(name);
            
            string responseMessage = null;
            try{
                await blobClient.UploadAsync(req.Form.Files["file"].OpenReadStream());        
                responseMessage = $"Uploaded {name} to the container: {containerName}";                
            }catch(Exception ex)
            {
                log.LogError($"Upload2Blob function - Exception found {ex.Message}");
                responseMessage = $"Unable to upload {name} to the container: {containerName} ";
            }
            log.LogInformation("Upload2Blob function completed.");
            return new OkObjectResult(responseMessage);
        }


    }
}
