variable "subscription_id" {
  description = "Azure subscription ID to bootstrap."
  type        = string
}

variable "location" {
  description = "Region for the state storage account."
  type        = string
  default     = "uksouth"
}

variable "state_resource_group" {
  description = "Resource group holding the Terraform state storage account."
  type        = string
  default     = "rg-ddw-tfstate"
}

variable "state_storage_account" {
  description = "Globally-unique storage account name for Terraform state (3-24 lowercase alphanumeric)."
  type        = string

  validation {
    condition     = can(regex("^[a-z0-9]{3,24}$", var.state_storage_account))
    error_message = "Storage account name must be 3-24 lowercase alphanumeric characters."
  }
}

variable "github_org" {
  description = "GitHub organisation or user that owns the repo."
  type        = string
}

variable "github_repo" {
  description = "GitHub repository name (without the org)."
  type        = string
}

variable "github_environments" {
  description = "GitHub Actions environments that may assume the OIDC identity."
  type        = list(string)
  default     = ["dev"]
}

variable "tags" {
  description = "Tags applied to bootstrap resources."
  type        = map(string)
  default = {
    application = "Desicon Digital Workplace"
    purpose     = "iac-bootstrap"
    managed_by  = "terraform"
  }
}
