# Passwordless SQL connection string (Entra Managed Identity auth) stored in Key Vault.
# No password is ever placed in the connection string or in app settings.
resource "azurerm_key_vault_secret" "sql_connection" {
  name         = "DdwSqlConnectionString"
  key_vault_id = azurerm_key_vault.this.id
  content_type = "text/plain"

  value = join(";", [
    "Server=tcp:${azurerm_mssql_server.this.fully_qualified_domain_name},1433",
    "Database=${azurerm_mssql_database.this.name}",
    "Authentication=Active Directory Managed Identity",
    "User Id=${azurerm_user_assigned_identity.app.client_id}",
    "Encrypt=True",
    "TrustServerCertificate=False",
    "Connection Timeout=30",
  ])

  # The runner needs its RBAC role before it can write secrets.
  depends_on = [azurerm_role_assignment.kv_admin_runner]

  tags = local.common_tags
}

# Admin API key for write endpoints — generated and stored in Key Vault, never
# in source. Retrieve with: az keyvault secret show --vault-name <kv> --name AdminApiKey
resource "random_password" "admin_api_key" {
  length  = 40
  special = false
}

resource "azurerm_key_vault_secret" "admin_api_key" {
  name         = "AdminApiKey"
  key_vault_id = azurerm_key_vault.this.id
  value        = random_password.admin_api_key.result
  content_type = "text/plain"
  depends_on   = [azurerm_role_assignment.kv_admin_runner]
  tags         = local.common_tags
}
