# BlobUpload
This repo illustrate how can Azure function used to upload content to specific blob storage.

## High level diagram
![diagram](pics/blobupload.png)

## Required Components
This example assume you have already provisioned:
- Storage account
- Function App
Once provision, create a container.


## Functions
The first function used is `Upload2Blob`.
The function expects two parameters as post parameters:
- name - the name of the file to be uploaded (can differ from the actual file name)
- file - required to be passed as form data __File__ type

## Deployment

Clone this repo to your local machine. 
Change/create the local.setting.json with:

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
Open VSCode in the created directory, allow it to update with the latest __Azure Function__ plugins.
Deploy via VSCode to your subscription.


### Sample calling parameters (via postman)


![postman](pics/postman.png)