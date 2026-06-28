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
  default     = ""
}

variable "deploy_app_service" {
  description = "Create the App Service (requires compute quota). Set true after a quota increase."
  type        = bool
  default     = false
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
  default     = "https://mcr.microsoft.com"
}

variable "container_image" {
  description = "Image:tag for the DDW API (overridden by CI with the freshly built tag)."
  type        = string
  default     = "dotnet/samples:aspnetapp"
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
