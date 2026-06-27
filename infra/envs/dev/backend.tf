terraform {
  required_version = ">= 1.6.0"

  # Remote state in the storage account created by infra/bootstrap.
  # storage_account_name is supplied at init time:
  #   terraform init -backend-config="storage_account_name=<from bootstrap output>"
  backend "azurerm" {
    resource_group_name = "rg-ddw-tfstate"
    container_name      = "tfstate"
    key                 = "dev.terraform.tfstate"
    use_azuread_auth    = true
  }
}
