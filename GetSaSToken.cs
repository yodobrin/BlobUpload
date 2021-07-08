using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using System.Text;
using Azure.Storage.Sas;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Newtonsoft.Json;


namespace upload2blob
{
    public static class GetSaSToken
    {
        
        private static BlobClient GetBlobClient(ILogger log, string mode,string miClientID,string storageEndPoint, string clientId,string clientSecret, string tennat,string blobContainerName, string blobName)
        {
            log.LogInformation($"will access using {mode}");
            Azure.Identity.TokenCredentialOptions options = default;
            ClientSecretCredential spnToken = new ClientSecretCredential(tennat,clientId,clientSecret);
            ManagedIdentityCredential token = new ManagedIdentityCredential(miClientID, options);
            string saConnectionString = "";
            if (mode.Equals("spn"))
            {
                var client = new SecretClient(new Uri("https://gensaskv.vault.azure.net/"), spnToken);
                saConnectionString = client.GetSecret("sa-cs").Value.Value;
            }else{
                var client = new SecretClient(new Uri("https://gensaskv.vault.azure.net/"), token);
                saConnectionString = client.GetSecret("sa-cs").Value.Value;
            }
            
            BlobClient blobClient = new BlobClient(saConnectionString,blobContainerName,blobName);
            log.LogInformation($"got blob client via token, blobClient.CanGenerateSasUri ? {blobClient.CanGenerateSasUri}");
            return blobClient;
        }



        [FunctionName("GetSaSToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetSaSToken processed a request.");

            string blobName = req.Query["blobName"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            blobName = blobName ?? data?.blobName;
            string mode = req.Query["mode"];
            mode = mode ?? data?.mode;
            
            string containerName = Environment.GetEnvironmentVariable("LANDING_CONTAINER");
            string storageAccountName = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_NAME");
            string miclientId = Environment.GetEnvironmentVariable("MANAGED_IDENTITY");

            string clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            string clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
            string tennat = Environment.GetEnvironmentVariable("TENNAT");
            string blobEndpoint = string.Format("https://{0}.blob.core.windows.net", storageAccountName);

            log.LogInformation($"Container Name: {containerName}");


            BlobClient blobClient = GetBlobClient(log,mode, miclientId,blobEndpoint,clientId,clientSecret,tennat,containerName,blobName);
            string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/{1}", storageAccountName,containerName);

            
            log.LogInformation($"Creating sas uri for: {blobName}");
            Uri sas = GetServiceSasUriForBlob(log, blobClient,containerName,blobName);

            // attempt to get blob client via mi token
            string responseMessage = "No SaS for you";
            if(sas!=null)
            {
                 responseMessage = $"{sas.ToString()}";
            }

            return new OkObjectResult(responseMessage);
        }
        private static Uri GetServiceSasUriForBlob(ILogger log,BlobClient blobClient,
            string containerName, string blobName, 
            string storedPolicyName = null)
        {
            // Check whether this BlobClient object has been authorized with Shared Key.
            log.LogInformation($"trying to get sas blobClient==null?{blobClient==null}. ");
            
            if (blobClient!=null && blobClient.CanGenerateSasUri)
            {
                // Create a SAS token that's valid for one hour.
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = containerName, 
                    BlobName = blobName,
                    Resource = "b"
                };

                if (storedPolicyName == null)
                {
                    sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);
                    sasBuilder.SetPermissions(BlobSasPermissions.Read |
                        BlobSasPermissions.Write);
                }
                else
                {
                    sasBuilder.Identifier = storedPolicyName;
                }

                Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

                return sasUri;
            }
            else
            {
                log.LogError("BlobClient must be authorized with Shared Key credentials to create a service SAS.");
                return null;
            }
        }
    }


}
