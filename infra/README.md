# DDW Infrastructure & DevSecOps Pipeline

Infrastructure-as-Code for the Desicon Digital Workplace, with security scanning wired into every pull request. Terraform provisions the Azure stack (Azure SQL, App Service for Containers, Key Vault, Entra app registration, Application Insights). GitHub Actions runs **Checkov** on the Terraform, **Trivy** on the Docker image, and **dependency scanning** on every PR. Azure access is **secretless** via OIDC workload-identity federation.

```
infra/
├─ bootstrap/            # one-time: remote state storage + GitHub OIDC identity
├─ modules/ddw_stack/    # the reusable stack module (SQL, App Service, KV, Entra, App Insights)
├─ envs/dev/             # dev environment that calls the module (remote backend)
└─ .checkov.yaml         # IaC policy config
.github/workflows/
├─ terraform-ci.yml      # fmt + validate + Checkov, then plan (PR) / apply (main)
├─ docker-security.yml   # build image + Trivy scan, push only if clean
└─ dependency-scan.yml   # dependency-review + Trivy fs + dotnet --vulnerable
```

## What gets provisioned

| Resource | Hardening built in |
|---|---|
| Azure SQL (server + database) | Entra-only auth (no SQL passwords), TLS 1.2, TDE, auditing + Defender alerts, public access off |
| App Service (Linux container) | HTTPS-only, TLS 1.2, FTPS disabled, user-assigned identity, Key Vault references, App Insights |
| Key Vault | RBAC authorization, purge protection, soft-delete 90d, network default-deny, diagnostics |
| Entra app registration | OAuth2 scope + app role for the DDW API, no client secret (PKCE / managed identity) |
| Application Insights | Workspace-based over Log Analytics, local auth disabled |
| Managed identity | Passwordless SQL + Key Vault access, least privilege (Secrets *User*, not Officer) |

---

## Prerequisites

- Terraform >= 1.6, Azure CLI, and `Owner` (or `Contributor` + `User Access Administrator`) on the target subscription for the one-time bootstrap.
- A GitHub repository for this project.
- An Entra security group to act as SQL admin (e.g. `DDW-SQL-Admins-Dev`) — note its object ID.

---

## Step 1 — Bootstrap (run once, locally)

Creates the remote-state storage account and the secretless GitHub OIDC identity.

```bash
az login
cd infra/bootstrap

terraform init
terraform apply \
  -var="subscription_id=<SUB_ID>" \
  -var="state_storage_account=stddwtfstate$RANDOM" \
  -var="github_org=<YOUR_GH_ORG>" \
  -var="github_repo=<YOUR_REPO>"
```

Record the outputs — you'll register them in GitHub next:

```bash
terraform output
# github_actions_client_id, azure_tenant_id, azure_subscription_id,
# backend_storage_account_name, backend_resource_group_name, backend_container_name
```

> Bootstrap intentionally uses **local state**. Keep its `terraform.tfstate` secure (it's git-ignored).

---

## Step 2 — Configure GitHub

Repo → **Settings → Secrets and variables → Actions → Variables** (these are not secrets — OIDC means no stored credentials):

| Variable | Value |
|---|---|
| `AZURE_CLIENT_ID` | `github_actions_client_id` from bootstrap |
| `AZURE_TENANT_ID` | `azure_tenant_id` |
| `AZURE_SUBSCRIPTION_ID` | `azure_subscription_id` |
| `TF_STATE_STORAGE_ACCOUNT` | `backend_storage_account_name` |
| `DDW_SQL_ADMIN_LOGIN` | e.g. `DDW-SQL-Admins-Dev` |
| `DDW_SQL_ADMIN_OBJECT_ID` | object ID of that Entra group |

Then create a GitHub **Environment** named `dev` (Settings → Environments) and add **required reviewers** so `terraform apply` and image pushes are gated by a human approval.

---

## Step 3 — First deploy

```bash
cd infra/envs/dev
cp terraform.tfvars.example terraform.tfvars   # fill in values
terraform init -backend-config="storage_account_name=<backend_storage_account_name>"
terraform plan
terraform apply
```

After the first apply, grant the app's managed identity database access (one-off, run against the DB as the Entra admin):

```sql
CREATE USER [id-ddw-dev] FROM EXTERNAL PROVIDER;   -- the user-assigned identity name
ALTER ROLE db_datareader ADD MEMBER [id-ddw-dev];
ALTER ROLE db_datawriter ADD MEMBER [id-ddw-dev];
```

From here on, **don't deploy from your laptop** — let the pipeline do it.

---

## Step 4 — The pipeline (every PR from now on)

```
open PR  ─►  terraform-ci:  fmt ─► validate ─► Checkov ─► plan (commented on PR)
            docker-security: build image ─► Trivy scan (HIGH/CRITICAL = fail)
            dependency-scan: dependency-review ─► Trivy fs ─► dotnet --vulnerable
                                   │
                          all green + review
                                   ▼
merge to main ─►  terraform apply (gated by 'dev' environment)
                  docker push to GHCR (only the scanned, clean image)
```

### What each scanner does

- **Checkov** (`bridgecrewio/checkov-action`) — static analysis of the Terraform against hundreds of Azure security policies (encryption, TLS, public exposure, logging). Findings appear in the **Security** tab as SARIF; a violation fails the build. Intentional exceptions live in `infra/.checkov.yaml` with written justification.
- **Trivy image scan** (`aquasecurity/trivy-action`) — scans the built container for OS/library CVEs, embedded secrets and Dockerfile misconfig. HIGH/CRITICAL with an available fix fails the build, so a vulnerable image is **never pushed**.
- **Dependency scanning** — GitHub `dependency-review-action` blocks PRs that introduce vulnerable dependencies; Trivy filesystem scan and `dotnet list package --vulnerable` catch transitive CVEs in lockfiles.

All three upload SARIF, so results show up under the repo's **Security → Code scanning** tab and annotate the PR.

---

## Run the scanners locally (optional, before pushing)

```bash
terraform fmt -recursive -check
checkov -d infra --config-file infra/.checkov.yaml
trivy fs --severity HIGH,CRITICAL .
docker build -t ddw-api:dev . && trivy image --severity HIGH,CRITICAL ddw-api:dev
```

## Promoting to prod

Copy `envs/dev` to `envs/prod`, switch SKUs up (`P1v3`, `GP_S_Gen5_2`), set `enable_public_network_access = false` with private endpoints, add a `prod` GitHub environment with stricter reviewers, and add a `prod` federated credential in bootstrap (`github_environments = ["dev","prod"]`).
