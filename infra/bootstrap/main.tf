data "azurerm_client_config" "current" {}
data "azuread_client_config" "current" {}

# ---------------------------------------------------------------------------
# Remote state backend storage (AAD-auth only, versioned, hardened).
# ---------------------------------------------------------------------------
resource "azurerm_resource_group" "state" {
  name     = var.state_resource_group
  location = var.location
  tags     = var.tags
}

resource "azurerm_storage_account" "state" {
  name                     = var.state_storage_account
  resource_group_name      = azurerm_resource_group.state.name
  location                 = azurerm_resource_group.state.location
  account_tier             = "Standard"
  account_replication_type = "GRS"
  account_kind             = "StorageV2"

  https_traffic_only_enabled      = true
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  shared_access_key_enabled       = false # backend authenticates with Entra (use_azuread_auth)
  public_network_access_enabled   = true  # restrict to runner IPs / private endpoint in prod

  blob_properties {
    versioning_enabled = true
    delete_retention_policy {
      days = 30
    }
    container_delete_retention_policy {
      days = 30
    }
  }

  tags = var.tags
}

resource "azurerm_storage_container" "state" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.state.id
  container_access_type = "private"
}

# The operator running bootstrap needs data-plane access to create/read state blobs.
resource "azurerm_role_assignment" "state_operator" {
  scope                = azurerm_storage_account.state.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = data.azurerm_client_config.current.object_id
}

# ---------------------------------------------------------------------------
# GitHub Actions OIDC identity (secretless federated workload identity).
# ---------------------------------------------------------------------------
resource "azuread_application" "github" {
  display_name = "ddw-github-actions-oidc"
  owners       = [data.azuread_client_config.current.object_id]
  tags         = ["DDW", "ci", "terraform-managed"]
}

resource "azuread_service_principal" "github" {
  client_id = azuread_application.github.client_id
  owners    = [data.azuread_client_config.current.object_id]
}

# Federated credential for pull requests (plan / scan only).
resource "azuread_application_federated_identity_credential" "pr" {
  application_id = azuread_application.github.id
  display_name   = "github-pull-request"
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://token.actions.githubusercontent.com"
  subject        = "repo:${var.github_org}/${var.github_repo}:pull_request"
}

# Federated credential per environment (apply).
resource "azuread_application_federated_identity_credential" "env" {
  for_each       = toset(var.github_environments)
  application_id = azuread_application.github.id
  display_name   = "github-env-${each.value}"
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://token.actions.githubusercontent.com"
  subject        = "repo:${var.github_org}/${var.github_repo}:environment:${each.value}"
}

# ---------------------------------------------------------------------------
# Permissions for the CI identity.
# ---------------------------------------------------------------------------
resource "azurerm_role_assignment" "github_contributor" {
  scope                = "/subscriptions/${var.subscription_id}"
  role_definition_name = "Contributor"
  principal_id         = azuread_service_principal.github.object_id
}

# Needed because the stack module creates Key Vault role assignments.
# Grants the CI identity the ability to write role assignments under the subscription.
resource "azurerm_role_assignment" "github_rbac_admin" {
  scope                = "/subscriptions/${var.subscription_id}"
  role_definition_name = "Role Based Access Control Administrator"
  principal_id         = azuread_service_principal.github.object_id
}

resource "azurerm_role_assignment" "github_state_blob" {
  scope                = azurerm_storage_account.state.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azuread_service_principal.github.object_id
}

# Allow the CI identity to manage app registrations it owns (Entra app for the DDW API).
resource "azuread_directory_role" "app_dev" {
  display_name = "Application Developer"
}

resource "azuread_directory_role_assignment" "github_app_dev" {
  role_id             = azuread_directory_role.app_dev.template_id
  principal_object_id = azuread_service_principal.github.object_id
}
