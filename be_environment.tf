resource "azurerm_storage_account" "be-storage-account" {
  name                     = "bestoragaccounthcportal2"
  resource_group_name      = data.azurerm_resource_group.hcportal_resource_group.name
  location                 = data.azurerm_resource_group.hcportal_resource_group.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = {
    Environment  = "DEV"
    ResourceType = "Storage"
    Area         = "Backend"
  }
}

resource "azurerm_service_plan" "be_service_plan" {
  name                = "be_service_plan"
  resource_group_name = data.azurerm_resource_group.hcportal_resource_group.name
  location            = data.azurerm_resource_group.hcportal_resource_group.location

  os_type  = "Linux"
  sku_name = "B1"

  tags = {
    Environment  = "DEV"
    ResourceType = "Plan"
    Area         = "Backend"
  }
}


resource "azurerm_linux_function_app" "be_linux_function_app" {
  name = "behcportalfunction"

  resource_group_name = data.azurerm_resource_group.hcportal_resource_group.name
  location            = data.azurerm_resource_group.hcportal_resource_group.location
  service_plan_id     = azurerm_service_plan.be_service_plan.id

  storage_account_name       = azurerm_storage_account.be-storage-account.name
  storage_account_access_key = azurerm_storage_account.be-storage-account.primary_access_key

  site_config {
    always_on     = false
    http2_enabled = true
  }

  app_settings = {
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = false
    DOCKER_REGISTRY_SERVER_URL          = "https://hcportalcontainerregistry.azurecr.io/hcportal-be-image"
    DOCKER_REGISTRY_SERVER_USERNAME     = "hcportalcontainerregistry"
    DOCKER_REGISTRY_SERVER_PASSWORD     = "Z3qlUyTU0SNzpes9IPwLBHKyt5jLO/ap9poGHLlfom+ACRAKX7F8"

    FUNCTIONS_WORKER_RUNTIME = "dotnet"
    LinuxFxVersion           = "DOCKER|hcportalcontainerregistry.azurecr.io/hcportal-be-image:latest"

    KEY_VAULT_URL               = "7803d269736f67751c86e1bf3e4af5d8662147171164a4a6c1a856099e9f1f12"
    AZURE_FUNCTIONS_ENVIRONMENT = "Development"
    MYSQL_CONNECTION_STRING     = "Server=${azurerm_mysql_flexible_server.mysql_flexible_server.fqdn};UserID=${azurerm_mysql_flexible_server.mysql_flexible_server.administrator_login};Password=${azurerm_mysql_flexible_server.mysql_flexible_server.administrator_password};Database=${azurerm_mysql_flexible_database.db_hcportal.name};SslMode=Disabled"

    # TENANT_ID                           = azurerm_client_config.hcportal_cont_reg.tenant_id
    # CLIENT_ID                           = var.client_id
    # CLIENT_SECRET                       = var.client_secret
    # KEY_VAULT_URL                       = azurerm_key_vault.key_vault.vault_uri
    # AZURE_FUNCTIONS_ENVIRONMENT         = "Production"
  }

  tags = {
    Environment  = "DEV"
    ResourceType = "Compute"
    Area         = "Backend"
  }
}
