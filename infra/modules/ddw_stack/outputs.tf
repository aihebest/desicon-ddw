output "resource_group_name" {
  description = "Resource group containing the DDW stack."
  value       = azurerm_resource_group.this.name
}

output "app_service_default_hostname" {
  description = "Default hostname of the DDW API app service."
  value       = var.deploy_app_service ? azurerm_linux_web_app.this[0].default_hostname : ""
}

output "app_service_name" {
  description = "Name of the DDW API app service."
  value       = var.deploy_app_service ? azurerm_linux_web_app.this[0].name : ""
}

output "app_identity_principal_id" {
  description = "Principal ID of the app's user-assigned managed identity (grant SQL db_datareader/db_datawriter to this)."
  value       = azurerm_user_assigned_identity.app.principal_id
}

output "app_identity_client_id" {
  description = "Client ID of the app's managed identity."
  value       = azurerm_user_assigned_identity.app.client_id
}

output "sql_server_fqdn" {
  description = "Fully qualified domain name of the Azure SQL server."
  value       = azurerm_mssql_server.this.fully_qualified_domain_name
}

output "sql_database_name" {
  description = "Name of the DDW database."
  value       = azurerm_mssql_database.this.name
}

output "key_vault_uri" {
  description = "Key Vault URI."
  value       = azurerm_key_vault.this.vault_uri
}

output "api_app_client_id" {
  description = "Entra application (client) ID of the DDW API (managed outside CI)."
  value       = var.api_client_id
}

output "application_insights_connection_string" {
  description = "Application Insights connection string."
  value       = azurerm_application_insights.this.connection_string
  sensitive   = true
}
