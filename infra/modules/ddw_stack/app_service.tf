# Linux App Service (containers) hosting the DDW API.
resource "azurerm_service_plan" "this" {
  name                = local.plan_name
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  os_type             = "Linux"
  sku_name            = var.app_service_sku
  tags                = local.common_tags
}

resource "azurerm_linux_web_app" "this" {
  name                = local.app_name
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  service_plan_id     = azurerm_service_plan.this.id

  https_only                               = true
  public_network_access_enabled            = true
  client_affinity_enabled                  = false
  ftp_publish_basic_authentication_enabled = false

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app.id]
  }

  key_vault_reference_identity_id = azurerm_user_assigned_identity.app.id

  site_config {
    always_on                         = true
    http2_enabled                     = true
    minimum_tls_version               = "1.2"
    ftps_state                        = "Disabled"
    vnet_route_all_enabled            = false
    health_check_path                 = "/health"
    health_check_eviction_time_in_min = 5

    application_stack {
      docker_registry_url = var.container_registry_url
      docker_image_name   = var.container_image
    }
  }

  app_settings = {
    "WEBSITES_PORT"                              = tostring(var.container_port)
    "ASPNETCORE_ENVIRONMENT"                     = title(var.environment)
    "AZURE_CLIENT_ID"                            = azurerm_user_assigned_identity.app.client_id
    "APPLICATIONINSIGHTS_CONNECTION_STRING"      = azurerm_application_insights.this.connection_string
    "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3"
    "KeyVault__Uri"                              = azurerm_key_vault.this.vault_uri
    # Key Vault reference: App Service resolves this at runtime via the managed identity.
    "ConnectionStrings__Default" = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.sql_connection.versionless_id})"
    "AzureAd__TenantId"          = data.azurerm_client_config.current.tenant_id
    "AzureAd__ClientId"          = azuread_application.api.client_id
    "AzureAd__Audience"          = var.api_identifier_uri
  }

  logs {
    http_logs {
      file_system {
        retention_in_days = 7
        retention_in_mb   = 35
      }
    }
    application_logs {
      file_system_level = "Information"
    }
  }

  tags = local.common_tags

  depends_on = [
    azurerm_role_assignment.kv_app_reader,
    azurerm_key_vault_secret.sql_connection,
  ]
}

# App Service diagnostics to Log Analytics.
resource "azurerm_monitor_diagnostic_setting" "app" {
  name                       = "app-diagnostics"
  target_resource_id         = azurerm_linux_web_app.this.id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id

  enabled_log {
    category = "AppServiceHTTPLogs"
  }
  enabled_log {
    category = "AppServiceConsoleLogs"
  }
  enabled_log {
    category = "AppServiceAppLogs"
  }

  metric {
    category = "AllMetrics"
  }
}
