# User-assigned managed identity for the DDW API.
# Used for Key Vault secret access and passwordless (Entra) SQL authentication.
resource "azurerm_user_assigned_identity" "app" {
  name                = local.uami_name
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  tags                = local.common_tags
}
