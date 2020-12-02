# BlobUpload
This repo illustrate how can Azure function used to upload content to specific blob storage & to trigger another function which will create a wrapping html5 page which can then be used as part of a static web-site.

## High level diagram
![diagram](pics/blobupload.png)

## Required Components
This example assume you have already provisioned:
- Storage account
- Function App
Once provision, create a container.


## Functions

### Upload2Blob
The function is a http trigger function, it expects two parameters as post parameters:
- name - the name of the file to be uploaded (can differ from the actual file name)
- file - required to be passed as form data __File__ type

Successful calls will result in `200` and message confirming the upload to the specific container. 
Unsuccessful calls will result in specific error messages

### Expose2Web
The function is EventGrid triggered, it will create a simple _html_ page, with link to the file landing in the specific container (same container as in the `Upload2Blob` function).
The current code is supporting __hard coded__ `mp4` file types, feel free to enhance it.

It will use the `template.txt` template file, replace the uri of the file:
```
<video width="320" height="240" controls>
  <source src="video_url_to_replace" type="video/mp4">
</video>
```
With the location of the uploaded file (part of the event grid message).

**Tip** Need to worry about access control


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