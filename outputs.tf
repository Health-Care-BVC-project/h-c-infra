output "resource_group_name" {
  value = data.azurerm_resource_group.hcportal_resource_group.name
}

output "kubernetes_cluster_name" {
  value = azurerm_kubernetes_cluster.k8s-frontend.name
}

output "azurerm_mysql_flexible_server" {
  value = azurerm_mysql_flexible_server.mysql_flexible_server.name
}

output "admin_login" {
  value = azurerm_mysql_flexible_server.mysql_flexible_server.administrator_login
}

output "admin_password" {
  sensitive = true
  value     = azurerm_mysql_flexible_server.mysql_flexible_server.administrator_password
}

output "mysql_flexible_server_database_name" {
  value = azurerm_mysql_flexible_database.db_hcportal.name
}

output "function_app_default_hostname" {
  value       = azurerm_linux_function_app.be_linux_function_app.default_hostname
  description = "The default hostname of the Function App."
}

