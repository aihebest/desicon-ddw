# Desicon Enterprise Communication & Alert Platform — Go-Live Checklist

## What is LIVE today (dev environment)

| Component | Status | Where |
|---|---|---|
| **Backend API** | ✅ Running | `https://app-ddw-dev-x6zi99.azurewebsites.net` (Swagger at `/swagger`, health at `/health`) |
| **Admin Portal** (Blazor + Entra SSO) | ✅ Running | site root `/` — sign in with M365 |
| **Desktop Agent** (WPF tray) | ✅ Built & proven | `src/Ddw.Agent` → self-contained `DesiconAgent.exe` |
| **Azure SQL** (passwordless MI) | ✅ | `sql-ddw-dev-x6zi99` / `sqldb-ddw-dev` (UK South) |
| **Key Vault** (secrets) | ✅ | `kv-ddwdevx6zi99` — SQL conn, AdminApiKey, AzureAdClientSecret |
| **App Service** | ✅ | `app-ddw-dev` (South Africa North, B1) |
| **CI/CD + security scanning** | ✅ Green | GitHub Actions: Checkov, Trivy (image + fs), dependency review, OIDC, remote state |

**Proven end-to-end:** create a Bonny/Lagos-targeted announcement in the portal → the agent polls and pops it on matching PCs → Acknowledge flows back to the analytics dashboard.

---

## Before company-wide rollout (production hardening)

### Security (do first)
- [ ] **Authenticate the agent endpoints.** `/notifications/poll`, `/read`, `/ack` are currently open — anyone could poll or acknowledge as another user. Add agent auth (Entra device token, or a signed agent key + server-side identity validation).
- [ ] **Restrict the portal to authorised roles.** Today any `@desicongroup.com` user can sign in and create announcements. Limit to HR/ICT/HSE/Management via Entra app roles or security-group checks.
- [ ] **Switch portal sign-in to authorization-code flow** (currently implicit id_token).
- [ ] **Code-sign `DesiconAgent.exe`** so Windows SmartScreen doesn't warn users on first run.

### Infrastructure
- [ ] **Promote a `prod` environment** (`infra/envs/prod`): private endpoints for Key Vault & SQL (set `enable_public_network_access = false`), higher SKUs (`P1v3`, `GP_S_Gen5_2`), zone redundancy.
- [ ] **Wire CI to deploy the built image automatically** (pass the image SHA from the Docker workflow into Terraform) instead of the manual `az webapp config container set` pin.
- [ ] **Switch Checkov from report-mode to enforce** (`soft_fail: false`) once findings are triaged.
- [ ] **Scale out + session affinity** for the Blazor portal if traffic grows (Blazor Server needs sticky sessions on multi-instance).
- [ ] **Backups & alerts:** confirm SQL backups/retention; add Application Insights alerts (delivery failures, errors, availability).

### Operations / rollout
- [ ] **Mass-deploy the agent** via Intune/GPO — package `DesiconAgent.exe` as an MSI/MSIX for silent install + auto-update.
- [ ] **Seed real data:** departments, project sites, and the announcement categories your teams use.
- [ ] **Pilot** with one site (e.g. Bonny) before all of Lagos/Port Harcourt/Abuja/Project Site.
- [ ] **Runbook:** who can publish, escalation for critical/emergency broadcasts, support contact.

---

## How to run an announcement (today)

1. Open the portal, sign in with M365.
2. **New Announcement** → title, message, category, priority, require-ack.
3. **Who should receive this?** → Everyone, or Location/Department/Project/Role → **Add target**.
4. **Publish.** Matching employees' agents pick it up within ~5 minutes (or immediately on next login).
5. Track delivery/read/acknowledgement on the **Dashboard**.

## How an employee installs the agent (today)

1. Run `DesiconAgent.exe` once.
2. Enter email, pick Location, Department, (Project).
3. It lives in the system tray, auto-starts at login, and shows popups for targeted announcements.
