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
using Azure.Storage.Blobs.Models;
using System.IO;
using System.Text;

using Newtonsoft.Json;


namespace upload2blob
{
    public static class Upload2Blob
    {
        const string REPLACE_STRING = "video_url_to_replace"; 
        [FunctionName("Upload2Blob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Upload2Blob function -  processing a request.");

            string connectionString = Environment.GetEnvironmentVariable("STORAGE");
            string containerName = Environment.GetEnvironmentVariable("LANDING_CONTAINER");
            log.LogInformation($"Container Name: {containerName}");

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);  
            
            dynamic uploadResponse = new System.Dynamic.ExpandoObject();

            var formdata = await req.ReadFormAsync();
                        
            string name = formdata["name"];         

            if(string.IsNullOrEmpty(name)) {
                // name of the blob must be provided
                uploadResponse.ErrorMessage = "name is a mandatory field";
                uploadResponse.Status = "Failure";
                return new BadRequestObjectResult(JsonConvert.SerializeObject(uploadResponse));

            }
            
            uploadResponse.SourceFileName = name;


            BlobClient blobClient = containerClient.GetBlobClient(name);
            
            if( req.Form.Files["file"] == null)
            {
                uploadResponse.ErrorMessage = "Please provide a file to upload";
                uploadResponse.Status = "Failure";
                return new BadRequestObjectResult(JsonConvert.SerializeObject(uploadResponse));
            }
            
            
            try{
                await blobClient.UploadAsync(req.Form.Files["file"].OpenReadStream());     
                string endpoint = await Upload2Site(connectionString,blobClient.Uri.ToString(),log);   
                uploadResponse.UploadedFile = blobClient.Uri.ToString();
                uploadResponse.ExposedURL = endpoint;
                uploadResponse.Status = "Success";        
            }catch(Exception ex)
            {
                log.LogError($"Upload2Blob function - Exception thrown during upload to blob {ex.Message}");
                uploadResponse.ErrorMessage = $"Unable to upload {name} to the container: {containerName} ";
                uploadResponse.Status = "Failure";
            }
            log.LogInformation("Upload2Blob function completed.");
            return new OkObjectResult(JsonConvert.SerializeObject(uploadResponse));
        }

        public static async Task<string> Upload2Site(string connectionString,string uri,ILogger log)
        {
            string token = Environment.GetEnvironmentVariable("SAS_TOKEN");
            string path2save = Environment.GetEnvironmentVariable("CONTENT");            
            string templateContainer = Environment.GetEnvironmentVariable("TEMPLATES");
            string baseUrl = Environment.GetEnvironmentVariable("BASE_URL");

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(path2save);
            BlobContainerClient templateContainerClient = blobServiceClient.GetBlobContainerClient(templateContainer);

            // hard coding template.txt file for this example
            string templateName = "template.txt";
            BlobClient templateClient = templateContainerClient.GetBlobClient(templateName);  

            StringBuilder htmlContentB = new StringBuilder();
            var response = await templateClient.DownloadAsync();

            using (var streamReader = new StreamReader(response.Value.Content))
            {
                while (!streamReader.EndOfStream)
                {
                var line = await streamReader.ReadLineAsync();
                htmlContentB.AppendLine(line);
                }
            }

            string htmlContent = htmlContentB.ToString();

            log.LogInformation($"Upload2Blob:Expose2Web html template contnet: {htmlContent}");

            string HtmlFileName = $"{Guid.NewGuid().ToString()}.html";
            BlobClient blobClient = containerClient.GetBlobClient(HtmlFileName);  

            string authuri = $"{uri}{token}";

            htmlContent = htmlContent.Replace(REPLACE_STRING,authuri);
            log.LogInformation($"Upload2Blob:Expose2Web html final contnet: {htmlContent}");
            byte[] byteArray = Encoding.UTF8.GetBytes(htmlContent);
            string webLink = $"{baseUrl}{HtmlFileName}";
            MemoryStream stream = new MemoryStream(byteArray);

            try{
                 await blobClient.UploadAsync(stream);      

                BlobHttpHeaders blobHttpHeaders = new BlobHttpHeaders();
                blobHttpHeaders.ContentType = "text/html";
                blobClient.SetHttpHeaders(blobHttpHeaders);  
                
            }catch(Exception ex)
            {
                log.LogError($"Upload2Blob:Expose2Web - Exception thrown during upload of html file: {ex.Message}");
                
            }

            return webLink;
        }


    }
}
