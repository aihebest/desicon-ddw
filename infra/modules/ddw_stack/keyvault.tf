# Key Vault: RBAC-authorized, soft-delete + purge protection on, network-restricted.
resource "azurerm_key_vault" "this" {
  name                = local.kv_name
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  # Use Azure RBAC instead of legacy access policies (CKV_AZURE_189 family).
  enable_rbac_authorization = true

  # Data-protection hardening.
  soft_delete_retention_days    = 90
  purge_protection_enabled      = true
  public_network_access_enabled = var.enable_public_network_access

  network_acls {
    bypass = "AzureServices"
    # Dev allows network reach (data access still gated by RBAC); prod denies and
    # uses private endpoints. Add CIDRs to allowed_ip_cidrs to restrict further.
    default_action = var.enable_public_network_access ? "Allow" : "Deny"
    ip_rules       = var.allowed_ip_cidrs
  }

  tags = local.common_tags
}

# The Terraform runner (CI service principal) needs to write the initial secrets.
resource "azurerm_role_assignment" "kv_admin_runner" {
  scope                = azurerm_key_vault.this.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

# The app's managed identity may only READ secrets (least privilege).
resource "azurerm_role_assignment" "kv_app_reader" {
  scope                = azurerm_key_vault.this.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.app.principal_id
}

# The human admin (SQL AAD admin) may read secrets — e.g. the AdminApiKey.
resource "azurerm_role_assignment" "kv_admin_user" {
  scope                = azurerm_key_vault.this.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = var.sql_admin_object_id
}

# Stream Key Vault audit/data-plane logs to the workspace.
resource "azurerm_monitor_diagnostic_setting" "kv" {
  name                       = "kv-diagnostics"
  target_resource_id         = azurerm_key_vault.this.id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id

  enabled_log {
    category = "AuditEvent"
  }

  metric {
    category = "AllMetrics"
  }
}
