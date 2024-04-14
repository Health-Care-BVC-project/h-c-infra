locals {
  admin_password = try(random_password.admin_password[0].result, var.admin_password)
}
resource "random_password" "admin_password" {
  count       = var.admin_password == null ? 1 : 0
  length      = 20
  special     = true
  min_numeric = 1
  min_upper   = 1
  min_lower   = 1
  min_special = 1
}
resource "azurerm_virtual_network" "bd_network" {
  address_space       = ["10.0.0.0/16"]
  location            = data.azurerm_resource_group.hcportal_resource_group.location
  name                = "vnet-db-network"
  resource_group_name = data.azurerm_resource_group.hcportal_resource_group.name
}
resource "azurerm_mssql_server" "server" {
  name                         = "hcportal-server"
  resource_group_name          = data.azurerm_resource_group.hcportal_resource_group.name
  location                     = data.azurerm_resource_group.hcportal_resource_group.location
  administrator_login          = "hc_be_user"
  administrator_login_password = local.admin_password
  version                      = "12.0"
}
resource "azurerm_subnet" "db_subnet" {
  address_prefixes     = ["10.0.2.0/24"]
  name                 = "subnet-db-subnet"
  resource_group_name  = data.azurerm_resource_group.hcportal_resource_group.name
  virtual_network_name = azurerm_virtual_network.bd_network.name
  service_endpoints    = ["Microsoft.Storage"]

  delegation {
    name = "fs"

    service_delegation {
      name = "Microsoft.DBforMySQL/flexibleServers"
      actions = [
        "Microsoft.Network/virtualNetworks/subnets/join/action",
      ]
    }
  }
}
resource "azurerm_private_dns_zone" "dns_network" {
  name                = "hcportal.mysql.database.azure.com"
  resource_group_name = data.azurerm_resource_group.hcportal_resource_group.name
}
resource "azurerm_private_dns_zone_virtual_network_link" "dns_virtual_link" {
  name                  = "mysqlfsVnetZoneHcPortal.com"
  private_dns_zone_name = azurerm_private_dns_zone.dns_network.name
  resource_group_name   = data.azurerm_resource_group.hcportal_resource_group.name
  virtual_network_id    = azurerm_virtual_network.bd_network.id

  depends_on = [azurerm_subnet.db_subnet]
}
resource "azurerm_mysql_flexible_server" "mysql_flexible_server" {
  location            = data.azurerm_resource_group.hcportal_resource_group.location
  name                = "mysqlfs-hcportaldbserver"
  resource_group_name = data.azurerm_resource_group.hcportal_resource_group.name

  administrator_login    = "hc_user_login"
  administrator_password = local.admin_password

  backup_retention_days        = 7
  delegated_subnet_id          = azurerm_subnet.db_subnet.id
  geo_redundant_backup_enabled = false
  private_dns_zone_id          = azurerm_private_dns_zone.dns_network.id

  sku_name = "GP_Standard_D2ds_v4"
  version  = "8.0.21"

  high_availability {
    mode = "SameZone"
  }
  maintenance_window {
    day_of_week  = 0
    start_hour   = 8
    start_minute = 0
  }
  storage {
    iops    = 360
    size_gb = 20
  }

  depends_on = [azurerm_private_dns_zone_virtual_network_link.dns_virtual_link]
}
resource "azurerm_mysql_flexible_database" "db_hcportal" {
  charset             = "utf8mb4"
  collation           = "utf8mb4_unicode_ci"
  name                = "hcportaldb"
  resource_group_name = data.azurerm_resource_group.hcportal_resource_group.name
  server_name         = azurerm_mysql_flexible_server.mysql_flexible_server.name
}


