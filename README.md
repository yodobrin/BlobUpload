# BlobUpload
This repo illustrate the use of Azure function with specific focus on two blob activities;  
- Upload content to specific blob storage & to create a wrapping html5 page which can then be used as part of a static web-site.
- Generate SAS token for a blob, using managed identity & keyvault

_note: This is a working skeleton._

## High level diagram - Upload2Blob
![diagram](pics/blobupload.png)

## Required Components
This example assume you have already provisioned:
- Storage account, enable static-web
- Function App

## Function

### Upload2Blob
The function is a http trigger function, it expects two parameters as post parameters:
- name - the name of the file to be uploaded (can differ from the actual file name)
- file - required to be passed as form data __File__ type

Successful calls will result in `200` and JSON message confirming the upload to the specific container. and the exposed url.
Unsuccessful calls will result in specific JSON response with specific error messages

#### Method :: Expose2Web
The method is called with the new uploaded file uri, it will create a simple _html_ page, with link to the file landing in the specific container (same container as in the `Upload2Blob` function).
The current code is supporting __hard coded__ `mp4` file types, feel free to enhance it.

It will use the `template.txt` template file which is stored in another container, replace the uri of the file:
```
<video width="320" height="240" controls>
  <source src="video_url_to_replace" type="video/mp4">
</video>
```
With the location of the uploaded file. 

**Tip** Need to worry about access control: in my solution I generated SAS token for the container, store it in the function configuration.


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
        "STORAGE": "<your designated storage connection string>",
        "CONTENT": "$web",
        "TEMPLATES": "templates",
        "SAS_TOKEN": "<SAS token for the container>",
        "BASE_URL":"<your base url - taken from the static-web blade>"    
    }
}

```
(_if u skipped this before_)Create a storage account. Enable the storage account to have static-web capabilities. this will create a `$web` container.
Create `landing` `templates` containers (or use other names as you see fit)


Open VSCode in the created directory, allow it to update with the latest __Azure Function__ plugins.
Deploy via VSCode to your subscription.

### Sample calling parameters (via postman)


![postman](pics/postman.png)


## High level diagram - GetSasToken
Sample on how to generate sas token. consider this high level architecture. 
![image](https://user-images.githubusercontent.com/37622785/125249306-c387eb00-e2fd-11eb-94c9-b4c17fdaaba4.png)

___Note:___ While the code care for two types of identities accessing the keyvault, i a real scenario, only one would be used.


### Secured Posture

#### Enable FunctionApp authentication
![image](https://user-images.githubusercontent.com/37622785/125288225-8c79ff80-e326-11eb-9375-2d8aca706429.png)

Add a provider, in our case its AAD, create a new app. I choose 401 as an unauthenticated response
![image](https://user-images.githubusercontent.com/37622785/125288304-a7e50a80-e326-11eb-8871-747695ebb532.png)

#### Enable managed identity
![image](https://user-images.githubusercontent.com/37622785/125288003-4f157200-e326-11eb-9897-5ffc4e3efbc2.png)

#### Allow MI or SPN access to your KeyVault
Provide the minimal set of operations to your applications
![image](https://user-images.githubusercontent.com/37622785/125288668-132edc80-e327-11eb-8ee1-e821cf848dc7.png)
I used old fashioned AKV access policy (not RBAC)
![image](https://user-images.githubusercontent.com/37622785/125288754-29d53380-e327-11eb-96bc-9eca8abee18e.png)


