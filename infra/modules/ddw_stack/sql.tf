# Azure SQL: Entra-only auth (no SQL logins), TLS 1.2, TDE, auditing + threat detection.
resource "azurerm_mssql_server" "this" {
  name                = local.sql_name
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  version             = "12.0"

  minimum_tls_version           = "1.2"
  public_network_access_enabled = var.enable_public_network_access

  # Passwordless: Entra administrator + Entra-only authentication (no SQL admin password).
  azuread_administrator {
    login_username              = var.sql_admin_login
    object_id                   = var.sql_admin_object_id
    tenant_id                   = data.azurerm_client_config.current.tenant_id
    azuread_authentication_only = true
  }

  identity {
    type = "SystemAssigned"
  }

  tags = local.common_tags
}

resource "azurerm_mssql_database" "this" {
  name      = local.sqldb_name
  server_id = azurerm_mssql_server.this.id

  sku_name    = var.sql_sku
  max_size_gb = var.sql_max_size_gb
  collation   = "SQL_Latin1_General_CP1_CI_AS"

  # Encryption at rest.
  transparent_data_encryption_enabled = true

  # Resilience.
  zone_redundant       = var.environment == "prod" ? true : false
  storage_account_type = "Geo"

  tags = local.common_tags

  lifecycle {
    prevent_destroy = false # set true for prod databases
  }
}

# Allow trusted IPs only when public access is explicitly enabled.
resource "azurerm_mssql_firewall_rule" "allowed" {
  for_each = var.enable_public_network_access ? toset(var.allowed_ip_cidrs) : toset([])

  name             = "allow-${replace(each.value, "/", "-")}"
  server_id        = azurerm_mssql_server.this.id
  start_ip_address = cidrhost(each.value, 0)
  end_ip_address   = cidrhost(each.value, -1)
}

# Server-level auditing to Log Analytics (CKV_AZURE_23/24).
# Audits to Log Analytics via the diagnostic setting below (no storage endpoint,
# so retention is governed by the workspace, not this policy).
resource "azurerm_mssql_server_extended_auditing_policy" "this" {
  server_id              = azurerm_mssql_server.this.id
  log_monitoring_enabled = true
}

resource "azurerm_monitor_diagnostic_setting" "sql_audit" {
  name                       = "sql-audit"
  target_resource_id         = "${azurerm_mssql_server.this.id}/databases/${azurerm_mssql_database.this.name}"
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id

  enabled_log {
    category = "SQLSecurityAuditEvents"
  }

  enabled_log {
    category = "DevOpsOperationsAudit"
  }

  metric {
    category = "Basic"
  }
}

# Microsoft Defender for SQL / threat detection (CKV_AZURE_26/27).
resource "azurerm_mssql_server_security_alert_policy" "this" {
  resource_group_name  = azurerm_resource_group.this.name
  server_name          = azurerm_mssql_server.this.name
  state                = "Enabled"
  retention_days       = 90
  email_account_admins = true
}
