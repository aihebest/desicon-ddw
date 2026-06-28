# Entra app registration is managed OUTSIDE Terraform/CI by a directory admin
# (the CI service principal is intentionally not granted Graph write rights).
# Create it once with:
#   az ad app create --display-name "DDW-API-<env>" --sign-in-audience AzureADMyOrg --query appId -o tsv
# then pass the resulting appId in via var.api_client_id.
#
# The API scope/app-role/redirect URIs can be configured by the admin on that
# registration; the infrastructure only needs its client ID for app settings.
