# Entra ID app registration for the DDW API (OAuth2 resource server).
# Exposes a delegated scope and an app role; no client secret (clients use PKCE / managed identity).
resource "random_uuid" "scope_access" {}
resource "random_uuid" "role_admin" {}

resource "azuread_application" "api" {
  display_name     = "DDW-API-${var.environment}"
  identifier_uris  = [var.api_identifier_uri]
  sign_in_audience = "AzureADMyOrg"

  api {
    requested_access_token_version = 2

    oauth2_permission_scope {
      id                         = random_uuid.scope_access.result
      value                      = "access_as_user"
      type                       = "User"
      admin_consent_display_name = "Access DDW as the signed-in user"
      admin_consent_description  = "Allows the app to call the DDW API on behalf of the signed-in user."
      user_consent_display_name  = "Access DDW on your behalf"
      user_consent_description   = "Allows the app to call the DDW API as you."
      enabled                    = true
    }
  }

  app_role {
    id                   = random_uuid.role_admin.result
    allowed_member_types = ["User"]
    value                = "DDW.Admin"
    display_name         = "DDW Administrator"
    description          = "Full administrative access to the DDW platform."
    enabled              = true
  }

  # Interactive sign-in for the Blazor admin portal / desktop client.
  dynamic "single_page_application" {
    for_each = length(var.spa_redirect_uris) > 0 ? [1] : []
    content {
      redirect_uris = var.spa_redirect_uris
    }
  }

  required_resource_access {
    resource_app_id = "00000003-0000-0000-c000-000000000000" # Microsoft Graph

    resource_access {
      id   = "e1fe6dd8-ba31-4d61-89e7-88639da4683d" # User.Read (delegated)
      type = "Scope"
    }
  }

  tags = ["DDW", var.environment, "terraform-managed"]
}

resource "azuread_service_principal" "api" {
  client_id                    = azuread_application.api.client_id
  app_role_assignment_required = false
  owners                       = [data.azurerm_client_config.current.object_id]

  feature_tags {
    enterprise = true
  }
}
