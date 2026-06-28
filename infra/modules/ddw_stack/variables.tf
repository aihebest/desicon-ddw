variable "name_prefix" {
  description = "Short application prefix used in resource names (lowercase, no spaces)."
  type        = string
  default     = "ddw"

  validation {
    condition     = can(regex("^[a-z0-9]{2,8}$", var.name_prefix))
    error_message = "name_prefix must be 2-8 lowercase alphanumeric characters."
  }
}

variable "environment" {
  description = "Environment name (dev, test, prod)."
  type        = string

  validation {
    condition     = contains(["dev", "test", "staging", "prod"], var.environment)
    error_message = "environment must be one of: dev, test, staging, prod."
  }
}

variable "location" {
  description = "Azure region for all resources."
  type        = string
  default     = "uksouth"
}

variable "tags" {
  description = "Additional tags merged onto every resource."
  type        = map(string)
  default     = {}
}

# ---------- SQL ----------
variable "sql_admin_login" {
  description = "Display name of the Entra principal that will be SQL AAD admin (e.g. a security group like 'DDW-SQL-Admins')."
  type        = string
}

variable "sql_admin_object_id" {
  description = "Entra object ID of the SQL AAD admin principal (group or user)."
  type        = string
}

variable "sql_sku" {
  description = "Azure SQL Database SKU."
  type        = string
  default     = "S0"
}

variable "sql_max_size_gb" {
  description = "Maximum database size in GB."
  type        = number
  default     = 10
}

# ---------- App Service ----------
variable "app_service_sku" {
  description = "App Service Plan SKU (Linux). P1v3+ recommended for production."
  type        = string
  default     = "P1v3"
}

variable "deploy_app_service" {
  description = "Create the App Service plan + web app. Requires App Service compute quota on the subscription."
  type        = bool
  default     = true
}

variable "container_registry_url" {
  description = "Container registry URL hosting the DDW API image."
  type        = string
  default     = "https://mcr.microsoft.com"
}

variable "container_image" {
  description = "Image name and tag for the DDW API (e.g. desicon/ddw-api:sha-abc123)."
  type        = string
  default     = "dotnet/samples:aspnetapp"
}

variable "container_port" {
  description = "Port the container listens on."
  type        = number
  default     = 8080
}

# ---------- Network / access ----------
variable "enable_public_network_access" {
  description = "Whether SQL and Key Vault accept public network traffic. Keep false in prod (use private endpoints)."
  type        = bool
  default     = false
}

variable "allowed_ip_cidrs" {
  description = "CIDRs allowed through Key Vault / SQL firewall when public access is enabled (e.g. CI runners, admin IPs)."
  type        = list(string)
  default     = []
}

# ---------- Identity (Entra app registration for the DDW API) ----------
variable "api_identifier_uri" {
  description = "App ID URI for the DDW API Entra app registration (e.g. api://ddw-api)."
  type        = string
  default     = "api://ddw-api"
}

variable "api_client_id" {
  description = "Client ID of the pre-created Entra app registration for the DDW API (managed outside CI)."
  type        = string
  default     = ""
}

variable "spa_redirect_uris" {
  description = "Redirect URIs for the admin portal / desktop interactive sign-in."
  type        = list(string)
  default     = []
}

# ---------- Observability ----------
variable "log_retention_days" {
  description = "Retention in days for Log Analytics."
  type        = number
  default     = 30
}
