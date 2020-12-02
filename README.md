# BlobUpload
This repo illustrate how can Azure function used to upload content to specific blob storage.

## High level diagram
![diagram](pics/blobupload.png)

## Required Components
This example assume you have already provisioned:
- Storage account
- Function App
Once provision, create a container.
Replace the settings with your values:
```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "<your function storage connection string>",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "LANDING_CONTAINER": "<your container name>",
        "STORAGE": "<your designated storage connection string>"
    }
}

```