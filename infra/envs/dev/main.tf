module "ddw" {
  source = "../../modules/ddw_stack"

  name_prefix = "ddw"
  environment = "dev"
  location    = var.location

  # Identity / SQL admin
  sql_admin_login     = var.sql_admin_login
  sql_admin_object_id = var.sql_admin_object_id

  # Container image (CI passes the freshly built, Trivy-scanned tag)
  container_registry_url = var.container_registry_url
  container_image        = var.container_image

  # Sizing for a dev environment
  sql_sku         = "S0"
  app_service_sku = "B1"

  # Access — dev keeps public access off by default; add CI/admin CIDRs to open it
  enable_public_network_access = false
  allowed_ip_cidrs             = var.allowed_ip_cidrs

  spa_redirect_uris = var.spa_redirect_uris

  tags = {
    owner = "ddw-platform-team"
  }
}
