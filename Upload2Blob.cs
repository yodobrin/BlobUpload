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
