# Observability: workspace-based Application Insights over Log Analytics.
resource "azurerm_log_analytics_workspace" "this" {
  name                = local.law_name
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  sku                 = "PerGB2018"
  retention_in_days   = var.log_retention_days
  tags                = local.common_tags
}

resource "azurerm_application_insights" "this" {
  name                = local.appi_name
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  workspace_id        = azurerm_log_analytics_workspace.this.id
  application_type    = "web"
  # Keep telemetry inside the workspace; disable broad local auth.
  local_authentication_disabled = true
  tags                          = local.common_tags
}
