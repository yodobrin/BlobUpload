/*
Sample Code is provided for the purpose of illustration only and is not intended to be used in a production environment.
THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
We grant You a nonexclusive, royalty-free right to use and modify the Sample Code and to reproduce and distribute the object code form of the Sample Code, provided that. 
You agree: 
	(i) to not use Our name, logo, or trademarks to market Your software product in which the Sample Code is embedded;
    (ii) to include a valid copyright notice on Your software product in which the Sample Code is embedded; and
	(iii) to indemnify, hold harmless, and defend Us and Our suppliers from and against any claims or lawsuits, including attorneys’ fees, that arise or result from the use or distribution of the Sample Code
**/

// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.IO;
using Azure.Storage.Sas;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Newtonsoft.Json;


namespace upload2blob
{
    public static class GetSaSToken
    {
        /*
        This Function illustrate how a SAS token can be created.
        As connection string or master key are required to generate SAS token, the function will obtain this from a KeyVault.
        There are two main options to gain access to the KeyVault by a function; either assigning it a SPN, or an identity. 
        Using managed identity provide minimal operational aspects of managing passwords experiation etc.

        It is recoemnded to leverage managed identity.
        **/

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
            // either use this attribute, or the SPN items below, there is no need for both.
            string miclientId = Environment.GetEnvironmentVariable("MANAGED_IDENTITY");
            // in case you prefer using SPN, you will need to provide both id and password, remember to regenerate passwords based on your orgnization policy.
            string clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            string clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
            //
            string tennat = Environment.GetEnvironmentVariable("TENNAT");
            string blobEndpoint = string.Format("https://{0}.blob.core.windows.net", storageAccountName);

            log.LogInformation($"Container Name: {containerName}");

            // the sample uses a parameter to test both spn or mi, when coding your solution, be sure to pick only one. and send only the required parameters.
            BlobClient blobClient = GetBlobClient(log,mode, miclientId,blobEndpoint,clientId,clientSecret,tennat,containerName,blobName);
            // attempt to get blob client via mi token
            log.LogInformation($"Creating sas uri for: {blobName}");
            Uri sas = GetServiceSasUriForBlob(log, blobClient,containerName,blobName);

            
            // in the case a sas is null, the method to obtain it failed, return a message.
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
    
        private static BlobClient GetBlobClient(ILogger log,
                                                string mode,
                                                string miClientID,
                                                string storageEndPoint,
                                                string clientId,
                                                string clientSecret,
                                                string tennat,
                                                string blobContainerName,
                                                string blobName)
            {
                log.LogInformation($"will access using {mode}");
                Azure.Identity.TokenCredentialOptions options = default;
                ClientSecretCredential spnToken = new ClientSecretCredential(tennat,clientId,clientSecret);
                ManagedIdentityCredential token = new ManagedIdentityCredential(miClientID, options);
                string saConnectionString = "";
                // note, that in a real impl, only one type should be used
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
    }


}
