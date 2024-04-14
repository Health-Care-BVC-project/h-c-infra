provider "azurerm" {
  features {}
}

data "azurerm_resource_group" "hcportal_resource_group" {
  name = "hcportal-resourcegroup"
}

data "azurerm_container_registry" "hcportal_cont_reg" {
  name                = "hcPortalContainerRegistry"
  resource_group_name = data.azurerm_resource_group.hcportal_resource_group.name
}


# resource "null_resource" "docker_build_and_push" {
#   provisioner "local-exec" {
#     command = <<EOF
#       az login
#       az acr login --name ${data.azurerm_container_registry.hcportal_cont_reg.name}

#       docker load -i ./frontend_image/hcportal-fe-image.tar
#       docker tag hcportal-fe-image:latest ${data.azurerm_container_registry.hcportal_cont_reg.name}/hcportal-fe-image:latest
#       docker push ${data.azurerm_container_registry.hcportal_cont_reg.name}/image1:latest

#       docker load -i ./backend_image/hcportal-be-image.tar
#       docker tag hcportal-be-image:latest ${data.azurerm_container_registry.hcportal_cont_reg.name}/hcportal-be-image=:latest
#       docker push ${data.azurerm_container_registry.hcportal_cont_reg.name}/image2:latest
#     EOF

#   }
#   triggers = {
#     dockerfile_hash = filemd5("./backend_image/hcportal-be-image.tar")
#     dockerfile_hash = filemd5("./frontend_image/hcportal-fe-image.tar")
#   }
# }

# resource "azurerm_key_vault" "key_vault" {
#   name                       = "hcportal-KeyVault"
#   location                   = data.azurerm_resource_group.hcportal_resource_group.location
#   resource_group_name        = data.azurerm_resource_group.hcportal_resource_group.name
#   tenant_id                  = data.azurerm_client_config.current.tenant_id
#   sku_name                   = "standard"
#   purge_protection_enabled   = false
#   soft_delete_retention_days = 7
#   enable_rbac_authorization  = false

#   access_policy {
#     tenant_id = data.azurerm_client_config.current.tenant_id
#     object_id = data.azurerm_client_config.current.object_id

#     key_permissions = [
#       "get",
#     ]

#     secret_permissions = [
#       "get",
#       "set",
#     ]
#   }
# }
