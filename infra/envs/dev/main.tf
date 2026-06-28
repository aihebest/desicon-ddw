module "ddw" {
  source = "../../modules/ddw_stack"

  name_prefix = "ddw"
  environment = "dev"
  location    = var.location

  # Identity / SQL admin
  sql_admin_login     = var.sql_admin_login
  sql_admin_object_id = var.sql_admin_object_id

  # Entra app registration (created outside CI by an admin)
  api_client_id = var.api_client_id

  # Container image (CI passes the freshly built, Trivy-scanned tag)
  container_registry_url = var.container_registry_url
  container_image        = var.container_image

  # Sizing for a dev environment
  sql_sku         = "S0"
  app_service_sku = "B1"

  # App Service is quota-gated on this subscription; enable after a quota grant.
  deploy_app_service = var.deploy_app_service

  # Access — dev allows network reach to Key Vault/SQL (data access still gated by
  # Entra RBAC). Prod sets this false and uses private endpoints.
  enable_public_network_access = true
  allowed_ip_cidrs             = var.allowed_ip_cidrs

  spa_redirect_uris = var.spa_redirect_uris

  tags = {
    owner = "ddw-platform-team"
  }
}
