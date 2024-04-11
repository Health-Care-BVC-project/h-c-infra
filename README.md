## Getting Started

H&C Portal is a web cloud-based tool aiming to help Hospitals to better manage their patients and the process while
facilitating comunication with them with a secondary portal.

This project cover the api project that runs on Azure Function apps. It can be executed 

## Pipelines

2 Github actions were created to validate changes and deploy them

1. A validation pipeline to test the solution prior to merge. It is enforced and needs to be succesful before mergint with main

2. Once the code is merge in main branch, a pipeline is executed to dockerize the project and integrate into the infrastructure repository.

## Execution

**Local:** It requires a local.settings.json file to be tested on local without docker or a remote KeyVault solution

**Deploy:** An Azure Key Vault needs to be deployed prior to execute this project in docker. To create the image, the following command can be executed:
```
docker build -t be_image .
```

And can be executed with the following flags to locate the KeyVault endpoint:
```
docker run -p 7071:80 /
  -e TENANT_ID=your_tenant_id /
  -e CLIENT_ID=your_client_id /
  -e CLIENT_SECRET=your_client_secret /
  -e KEY_VAULT_URL=your_key_vault_url /
  -e AZURE_FUNCTIONS_ENVIRONMENT=Production /
  be_image```
**Azure functions run on 7071 port by default.**
