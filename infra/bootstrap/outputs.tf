# Values to register in GitHub (repo > Settings > Secrets and variables > Actions).
output "github_actions_client_id" {
  description = "Set as repository VARIABLE AZURE_CLIENT_ID."
  value       = azuread_application.github.client_id
}

output "azure_tenant_id" {
  description = "Set as repository VARIABLE AZURE_TENANT_ID."
  value       = data.azurerm_client_config.current.tenant_id
}

output "azure_subscription_id" {
  description = "Set as repository VARIABLE AZURE_SUBSCRIPTION_ID."
  value       = var.subscription_id
}

output "backend_resource_group_name" {
  description = "Use in envs/*/backend.tf -> resource_group_name."
  value       = azurerm_resource_group.state.name
}

output "backend_storage_account_name" {
  description = "Use in envs/*/backend.tf -> storage_account_name."
  value       = azurerm_storage_account.state.name
}

output "backend_container_name" {
  description = "Use in envs/*/backend.tf -> container_name."
  value       = azurerm_storage_container.state.name
}
