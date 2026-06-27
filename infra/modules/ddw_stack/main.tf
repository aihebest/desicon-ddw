# Core: resource group, naming, common tags.
data "azurerm_client_config" "current" {}

resource "random_string" "suffix" {
  length  = 6
  upper   = false
  special = false
  numeric = true
}

locals {
  base   = lower("${var.name_prefix}-${var.environment}")
  suffix = random_string.suffix.result

  # Globally-unique-safe names (Key Vault <=24, SQL/Web app lowercase).
  rg_name      = "rg-${local.base}"
  kv_name      = substr("kv-${var.name_prefix}${var.environment}${local.suffix}", 0, 24)
  sql_name     = "sql-${local.base}-${local.suffix}"
  sqldb_name   = "sqldb-${local.base}"
  plan_name    = "asp-${local.base}"
  app_name     = "app-${local.base}-${local.suffix}"
  law_name     = "law-${local.base}"
  appi_name    = "appi-${local.base}"
  uami_name    = "id-${local.base}"

  common_tags = merge({
    application = "Desicon Digital Workplace"
    component   = "DDW-API"
    environment = var.environment
    managed_by  = "terraform"
    cost_center = "ICT"
  }, var.tags)
}

resource "azurerm_resource_group" "this" {
  name     = local.rg_name
  location = var.location
  tags     = local.common_tags
}
