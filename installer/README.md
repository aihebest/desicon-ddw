# Desicon Alerts Agent — MSI Packaging & Deployment

A single self-contained MSI that installs the desktop alert agent on any Windows 10/11 PC
with **zero prerequisites** (the .NET runtime is bundled). It installs per-machine and
auto-starts at every user's login, then signs each user in silently with their own
Microsoft 365 account.

## What the installer does

- Installs the agent to `C:\Program Files\Desicon Alerts\`.
- Adds a machine-wide auto-start entry (`HKLM\…\Run`) so it launches for every user at login.
- On first launch each user signs in once via their Microsoft 365 account (system browser),
  then it runs silently forever (DPAPI-encrypted token cache, per user).
- Clean major-upgrades (new version replaces old); blocks downgrades.

## Build the MSI

Run from the `installer/` folder (needs the .NET 8 SDK and the WiX tool):

```bash
# 1) install the WiX build tool (once per machine)
dotnet tool install --global wix
wix --version

# 2) publish the agent self-contained (bundles .NET; ~150 MB)
dotnet publish ../src/Ddw.Agent/Ddw.Agent.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=false -o publish

# 3) build the installer
wix build DesiconAlerts.wxs -arch x64 -o DesiconAlerts.msi
```

Output: `DesiconAlerts.msi`.

## Test on one PC

```bash
# silent install (no UI) — same switch GPO/Intune use
msiexec /i DesiconAlerts.msi /qn /norestart

# silent uninstall
msiexec /x DesiconAlerts.msi /qn
```

After install, log off/on (or launch `C:\Program Files\Desicon Alerts\DesiconAgent.exe`).
The ℹ️ icon appears in the system tray; alerts pop on their own.

## Deploy company-wide via Group Policy (domain machines: Lagos, Port Harcourt, Abuja)

1. Copy `DesiconAlerts.msi` to a UNC share every domain PC can read,
   e.g. `\\desicon-fs\software\DesiconAlerts.msi`.
2. In **Group Policy Management**, create/edit a GPO linked to the OU with the target PCs.
3. **Computer Configuration → Policies → Software Settings → Software installation →
   New → Package** → point to the UNC path → choose **Assigned**.
4. The MSI installs at next reboot; the agent auto-starts at each user's login.

> Per-machine assignment installs once for the whole PC — ideal for shared site computers.

## Standalone / Entra-only machines (project sites not on the domain)

These aren't reached by GPO. Options:
- Run `msiexec /i DesiconAlerts.msi /qn` once per PC (USB or remote support), **or**
- If/when these PCs are enrolled in **Intune**, wrap this same MSI with the
  Microsoft Win32 Content Prep Tool into a `.intunewin` and deploy as a Win32 app
  (install: `msiexec /i DesiconAlerts.msi /qn`). Ask and I'll produce that wrapper.

Either way the agent only needs the user's Microsoft 365 account — no domain membership required.
