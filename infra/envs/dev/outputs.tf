output "app_url" {
  description = "Public URL of the DDW API."
  value       = "https://${module.ddw.app_service_default_hostname}"
}

output "app_service_name" {
  value = module.ddw.app_service_name
}

output "resource_group_name" {
  value = module.ddw.resource_group_name
}

output "sql_server_fqdn" {
  value = module.ddw.sql_server_fqdn
}

output "key_vault_uri" {
  value = module.ddw.key_vault_uri
}

output "api_app_client_id" {
  value = module.ddw.api_app_client_id
}

output "app_identity_principal_id" {
  description = "Grant this principal db_datareader/db_datawriter in the DDW database."
  value       = module.ddw.app_identity_principal_id
}
