variable "subscription_id" {
  description = "Target Azure subscription ID."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
  default     = "uksouth"
}

variable "api_client_id" {
  description = "Client ID of the pre-created Entra app registration for the DDW API."
  type        = string
  default     = "cdc21c25-679e-4f62-a157-86f438e57f85"
}

variable "deploy_app_service" {
  description = "Create the App Service (needs compute quota in app_service_location)."
  type        = bool
  default     = true
}

variable "app_service_location" {
  description = "Region for the App Service. South Africa North has Basic quota on this subscription."
  type        = string
  default     = "southafricanorth"
}

variable "sql_admin_login" {
  description = "Entra principal display name to set as SQL AAD admin (e.g. group 'DDW-SQL-Admins-Dev')."
  type        = string
}

variable "sql_admin_object_id" {
  description = "Entra object ID of the SQL AAD admin principal."
  type        = string
}

variable "container_registry_url" {
  description = "Registry hosting the DDW API image."
  type        = string
  default     = "https://ghcr.io"
}

variable "container_image" {
  description = "Image:tag for the DDW API. Pinned to an immutable commit SHA so App Service pulls the exact build (it caches :latest)."
  type        = string
  default     = "aihebest/desicon-ddw/ddw-api:8057b8377ab6168a909aad5bd1a8927c6a69a5ff"
}

variable "spa_redirect_uris" {
  description = "Redirect URIs for interactive sign-in."
  type        = list(string)
  default     = []
}

variable "allowed_ip_cidrs" {
  description = "CIDRs allowed through SQL/Key Vault firewall if public access is enabled."
  type        = list(string)
  default     = []
}
